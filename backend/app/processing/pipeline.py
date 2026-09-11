"""Pipeline-ul de procesare a imaginii pentru print.

Pași (conform cerinței):
  a. verifică rezoluția vs. dpi_minim -> upscaling dacă e nevoie
  b. elimină fundalul, dacă profilul cere asta
  c. convertește spațiul de culoare (RGB/CMYK)
  d. redimensionează/poziționează respectând bleed-ul
  e. generează raportul JSON
"""
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

from app.processing.background_removal import remove_background
from app.processing.color import convert_color_space
from app.processing.errors import ImageValidationError
from app.processing.upscaling import upscale_to
from app.schemas import CheckResult, CheckSeveritate, ProcessingReport, ProductProfile, SpatiuCuloare

CM_PER_INCH = 2.54
SAFETY_MARGIN_CM = 0.3  # zonă de siguranță standard ~3mm, independentă de bleed


def _open_and_validate(path: Path) -> Image.Image:
    try:
        with Image.open(path) as check_img:
            check_img.verify()
    except Exception as exc:
        raise ImageValidationError(f"Fișier imagine invalid sau corupt: {exc}") from exc

    try:
        image = Image.open(path)
        image.load()
    except Exception as exc:
        raise ImageValidationError(f"Nu s-a putut încărca imaginea: {exc}") from exc
    return image


def _target_canvas_px(profile: ProductProfile) -> tuple[int, int]:
    bleed_cm = profile.zona_bleed_mm / 10
    width_cm = profile.dimensiune_cm.latime + 2 * bleed_cm
    height_cm = profile.dimensiune_cm.inaltime + 2 * bleed_cm
    width_px = max(1, round(width_cm / CM_PER_INCH * profile.dpi_minim))
    height_px = max(1, round(height_cm / CM_PER_INCH * profile.dpi_minim))
    return width_px, height_px


def _effective_dpi(orig_w: int, orig_h: int, target_w_px: int, target_h_px: int, dpi: int) -> float:
    dpi_w = orig_w / (target_w_px / dpi)
    dpi_h = orig_h / (target_h_px / dpi)
    return min(dpi_w, dpi_h)


def _cover_resize(image: Image.Image, target_w: int, target_h: int) -> tuple[Image.Image, float]:
    """Scalează imaginea să acopere canvas-ul țintă, apoi decupează centrat."""
    orig_w, orig_h = image.size
    scale = max(target_w / orig_w, target_h / orig_h)
    new_w = max(1, round(orig_w * scale))
    new_h = max(1, round(orig_h * scale))
    resized = image.resize((new_w, new_h), Image.LANCZOS)
    left = (new_w - target_w) // 2
    top = (new_h - target_h) // 2
    cropped = resized.crop((left, top, left + target_w, top + target_h))
    crop_ratio = 1 - (target_w * target_h) / (new_w * new_h)
    return cropped, max(0.0, crop_ratio)


def _detect_edge_content_near_border(image: Image.Image, border_px: int) -> bool:
    if border_px <= 0:
        return False
    arr = np.array(image.convert("L"))
    edges = cv2.Canny(arr, 100, 200)
    h, w = edges.shape
    border_px = min(border_px, h // 2, w // 2)
    if border_px <= 0:
        return False
    mask = np.zeros_like(edges, dtype=bool)
    mask[:border_px, :] = True
    mask[-border_px:, :] = True
    mask[:, :border_px] = True
    mask[:, -border_px:] = True
    if not mask.any():
        return False
    density = edges[mask].mean() / 255
    return density > 0.02  # prag empiric


def run_pipeline(
    job_id: str, upload_path: Path, profile: ProductProfile, output_dir: Path
) -> tuple[ProcessingReport, Path, Path]:
    image = _open_and_validate(upload_path)
    original_width, original_height = image.size

    checks: list[CheckResult] = []
    avertismente: list[str] = []

    target_width_px, target_height_px = _target_canvas_px(profile)
    effective_dpi = _effective_dpi(
        original_width, original_height, target_width_px, target_height_px, profile.dpi_minim
    )

    upscaling_aplicat = False
    if effective_dpi < profile.dpi_minim:
        scale_needed = profile.dpi_minim / effective_dpi
        new_w = max(1, round(original_width * scale_needed))
        new_h = max(1, round(original_height * scale_needed))
        image, metoda_upscale = upscale_to(image, new_w, new_h)
        upscaling_aplicat = True
        mesaj = (
            f"Rezoluție insuficientă ({effective_dpi:.0f} DPI < {profile.dpi_minim} DPI necesar). "
            f"S-a aplicat upscaling ({metoda_upscale})."
        )
        avertismente.append(mesaj)
        checks.append(CheckResult(nume="rezolutie", status=CheckSeveritate.ATENTIE, mesaj=mesaj))
        if metoda_upscale == "lanczos_fallback":
            avertismente.append(
                "Upscaling AI (Real-ESRGAN / opencv-superres) nu este disponibil în acest mediu "
                "- s-a folosit un fallback Lanczos de calitate standard."
            )
    else:
        checks.append(
            CheckResult(
                nume="rezolutie",
                status=CheckSeveritate.OK,
                mesaj=f"Rezoluție suficientă ({effective_dpi:.0f} DPI >= {profile.dpi_minim} DPI necesar).",
            )
        )

    fundal_eliminat = False
    if profile.necesita_eliminare_fundal:
        image, reusit = remove_background(image)
        fundal_eliminat = reusit
        if reusit:
            checks.append(
                CheckResult(nume="eliminare_fundal", status=CheckSeveritate.OK, mesaj="Fundal eliminat automat.")
            )
        else:
            mesaj = (
                "Eliminarea automată a fundalului este cerută de profilul produsului, dar "
                "biblioteca rembg nu este disponibilă în acest mediu - pasul a fost omis."
            )
            avertismente.append(mesaj)
            checks.append(CheckResult(nume="eliminare_fundal", status=CheckSeveritate.ATENTIE, mesaj=mesaj))

    final_image, crop_ratio = _cover_resize(image, target_width_px, target_height_px)

    if crop_ratio > 0.15:
        mesaj = (
            f"Poziționarea în cadru a necesitat decuparea a ~{crop_ratio * 100:.0f}% din imaginea "
            "originală pentru a respecta proporțiile produsului. Verificați încadrarea."
        )
        avertismente.append(mesaj)
        checks.append(CheckResult(nume="incadrare", status=CheckSeveritate.ATENTIE, mesaj=mesaj))
    else:
        checks.append(
            CheckResult(nume="incadrare", status=CheckSeveritate.OK, mesaj="Încadrare fără decupări semnificative.")
        )

    safety_px = round(SAFETY_MARGIN_CM / CM_PER_INCH * profile.dpi_minim)
    if _detect_edge_content_near_border(final_image, safety_px):
        mesaj = (
            f"Detectat conținut cu contrast ridicat (posibil text/element grafic) în zona de "
            f"siguranță de la margine (~{profile.zona_bleed_mm}mm bleed + siguranță). "
            "Verificați poziționarea înainte de tăiere."
        )
        avertismente.append(mesaj)
        checks.append(CheckResult(nume="zona_de_siguranta", status=CheckSeveritate.ATENTIE, mesaj=mesaj))
    else:
        checks.append(
            CheckResult(
                nume="zona_de_siguranta",
                status=CheckSeveritate.OK,
                mesaj="Niciun element cu contrast ridicat detectat lângă marginea de tăiere.",
            )
        )

    final_image = convert_color_space(final_image, profile.spatiu_culoare)
    checks.append(
        CheckResult(
            nume="spatiu_culoare", status=CheckSeveritate.OK, mesaj=f"Convertit la {profile.spatiu_culoare.value}."
        )
    )

    output_dir.mkdir(parents=True, exist_ok=True)
    if profile.spatiu_culoare == SpatiuCuloare.CMYK:
        processed_path = output_dir / f"{job_id}_final.tiff"
        final_image.save(processed_path, format="TIFF")
    else:
        processed_path = output_dir / f"{job_id}_final.png"
        final_image.save(processed_path, format="PNG")

    preview_path = output_dir / f"{job_id}_preview.png"
    final_image.convert("RGB").save(preview_path, format="PNG")

    status = CheckSeveritate.OK
    if any(c.status == CheckSeveritate.EROARE for c in checks):
        status = CheckSeveritate.EROARE
    elif any(c.status == CheckSeveritate.ATENTIE for c in checks):
        status = CheckSeveritate.ATENTIE

    report = ProcessingReport(
        job_id=job_id,
        tip_produs=profile.nume,
        status=status,
        rezolutie_originala={"latime_px": original_width, "inaltime_px": original_height},
        rezolutie_finala={"latime_px": final_image.width, "inaltime_px": final_image.height},
        dpi_original_calculat=round(effective_dpi, 1),
        dpi_final_calculat=float(profile.dpi_minim),
        dpi_minim_necesar=profile.dpi_minim,
        spatiu_culoare_final=profile.spatiu_culoare.value,
        upscaling_aplicat=upscaling_aplicat,
        fundal_eliminat=fundal_eliminat,
        avertismente=avertismente,
        checks=checks,
    )

    report_path = output_dir / f"{job_id}_report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        f.write(report.model_dump_json(indent=2))

    return report, processed_path, preview_path
