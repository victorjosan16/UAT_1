"""Upscaling de imagine (pas 2, cu fallback funcțional încă din MVP).

Interfața este pregătită pentru un model AI real (Real-ESRGAN sau
`cv2.dnn_superres`) - vezi `_try_ai_upscale`. Dacă niciun model AI nu e
disponibil, se folosește un fallback Lanczos (Pillow), care e suficient
pentru MVP și e mereu marcat clar în raport (`metoda`).
"""
from PIL import Image


def _try_ai_upscale(image: Image.Image, target_size: tuple[int, int]) -> Image.Image | None:
    """Punct de extensie pentru Real-ESRGAN / opencv-superres.

    Întoarce None dacă niciun model AI nu este disponibil în mediul curent,
    caz în care apelantul recurge la fallback-ul clasic.
    """
    try:
        import cv2  # noqa: F401
        from cv2 import dnn_superres  # type: ignore

        # Punct de extensie: încarcă un model .pb (EDSR/ESPCN/FSRCNN) dacă
        # există pe disc. Fără un model configurat, nu facem upscaling AI.
        return None
    except Exception:
        return None


def upscale_to(image: Image.Image, target_width: int, target_height: int) -> tuple[Image.Image, str]:
    """Mărește imaginea la (target_width, target_height).

    Întoarce (imagine_scalată, metoda_folosită).
    """
    ai_result = _try_ai_upscale(image, (target_width, target_height))
    if ai_result is not None:
        return ai_result, "ai_superres"

    scaled = image.resize((target_width, target_height), Image.LANCZOS)
    return scaled, "lanczos_fallback"
