"""Rutare API - punctul de intrare FastAPI.

Logica de procesare a imaginii locuiește în `app/processing/`, iar
profilurile de produs în `app/product_profiles.py` + `profiles/*.json`.
Acest fișier se ocupă doar de HTTP: validare cereri, orchestrare job-uri,
răspunsuri.
"""
import logging
import uuid
import zipfile
from pathlib import Path

from fastapi import BackgroundTasks, FastAPI, File, Form, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles

from app.config import (
    ALLOWED_CONTENT_TYPES,
    DEFAULT_CRM_WEBHOOK_URL,
    MAX_UPLOAD_SIZE_MB,
    PROCESSED_DIR,
    UPLOAD_DIR,
)
from app.job_store import Job, job_store
from app.processing.errors import ImageValidationError
from app.processing.pipeline import run_pipeline
from app.product_profiles import ProfileNotFoundError, get_all_profiles, get_profile
from app.schemas import JobStatus, JobStatusResponse, UploadResponse
from app.webhook import notify_crm

logger = logging.getLogger("print_service.api")

app = FastAPI(
    title="Serviciu de procesare imagini pentru print",
    description="Modul independent, pregătit pentru integrare ulterioară cu un CRM.",
    version="0.1.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


def _run_job(job_id: str) -> None:
    job = job_store.get(job_id)
    if job is None:
        return
    job_store.update(job_id, status=JobStatus.PROCESSING)
    try:
        profile = get_profile(job.tip_produs)
        report, processed_path, preview_path = run_pipeline(
            job_id=job_id,
            upload_path=Path(job.upload_path),
            profile=profile,
            output_dir=PROCESSED_DIR,
        )
        job_store.update(
            job_id,
            status=JobStatus.DONE,
            report=report,
            processed_path=str(processed_path),
            preview_path=str(preview_path),
        )
        webhook_url = job.webhook_url or DEFAULT_CRM_WEBHOOK_URL
        if webhook_url:
            notify_crm(webhook_url, job_id, report)
    except ImageValidationError as exc:
        logger.warning("Job %s a eșuat (imagine invalidă): %s", job_id, exc)
        job_store.update(job_id, status=JobStatus.ERROR, eroare=str(exc))
    except Exception as exc:  # noqa: BLE001 - vrem să capturăm orice eroare neașteptată
        logger.exception("Job %s a eșuat neașteptat", job_id)
        job_store.update(job_id, status=JobStatus.ERROR, eroare=f"Eroare internă de procesare: {exc}")


@app.get("/profiles")
def list_profiles():
    profiles = get_all_profiles()
    return {key: profile.model_dump() for key, profile in profiles.items()}


@app.post("/upload", response_model=UploadResponse, status_code=202)
async def upload_image(
    background_tasks: BackgroundTasks,
    file: UploadFile = File(...),
    tip_produs: str = Form(...),
    webhook_url: str | None = Form(default=None),
):
    try:
        get_profile(tip_produs)
    except ProfileNotFoundError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc

    if file.content_type not in ALLOWED_CONTENT_TYPES:
        raise HTTPException(
            status_code=415,
            detail=f"Format nesuportat: {file.content_type}. Formate acceptate: {', '.join(sorted(ALLOWED_CONTENT_TYPES))}",
        )

    job_id = str(uuid.uuid4())
    suffix = Path(file.filename or "upload").suffix or ".bin"
    upload_path = UPLOAD_DIR / f"{job_id}{suffix}"

    size = 0
    max_bytes = MAX_UPLOAD_SIZE_MB * 1024 * 1024
    try:
        with open(upload_path, "wb") as out:
            while chunk := await file.read(1024 * 1024):
                size += len(chunk)
                if size > max_bytes:
                    raise HTTPException(status_code=413, detail=f"Fișier prea mare (limită {MAX_UPLOAD_SIZE_MB}MB).")
                out.write(chunk)
    except HTTPException:
        upload_path.unlink(missing_ok=True)
        raise
    finally:
        await file.close()

    job = Job(job_id=job_id, tip_produs=tip_produs, upload_path=str(upload_path), webhook_url=webhook_url)
    job_store.create(job)

    background_tasks.add_task(_run_job, job_id)

    return UploadResponse(job_id=job_id, status=JobStatus.QUEUED, tip_produs=tip_produs)


@app.get("/status/{job_id}", response_model=JobStatusResponse)
def get_status(job_id: str):
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job inexistent.")
    return JobStatusResponse(job_id=job.job_id, status=job.status, tip_produs=job.tip_produs, eroare=job.eroare, report=job.report)


@app.get("/preview/{job_id}")
def get_preview(job_id: str):
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job inexistent.")
    if job.status != JobStatus.DONE or not job.preview_path:
        raise HTTPException(status_code=409, detail=f"Job-ul nu este finalizat (status: {job.status}).")
    return FileResponse(job.preview_path, media_type="image/png")


@app.get("/report/{job_id}")
def get_report(job_id: str):
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job inexistent.")
    if job.status == JobStatus.ERROR:
        raise HTTPException(status_code=422, detail=job.eroare)
    if job.status != JobStatus.DONE or job.report is None:
        raise HTTPException(status_code=409, detail=f"Job-ul nu este finalizat (status: {job.status}).")
    return job.report


@app.get("/download/{job_id}")
def download_result(job_id: str):
    """Descarcă un arhivă .zip cu imaginea procesată + raportul JSON."""
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=404, detail="Job inexistent.")
    if job.status != JobStatus.DONE or not job.processed_path:
        raise HTTPException(status_code=409, detail=f"Job-ul nu este finalizat (status: {job.status}).")

    processed_path = Path(job.processed_path)
    zip_path = PROCESSED_DIR / f"{job_id}_download.zip"
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(processed_path, arcname=processed_path.name)
        report_path = PROCESSED_DIR / f"{job_id}_report.json"
        if report_path.exists():
            zf.write(report_path, arcname=report_path.name)

    return FileResponse(zip_path, media_type="application/zip", filename=f"{job_id}.zip")


# Servește frontend-ul static (dacă folderul există lângă backend/).
_frontend_dir = Path(__file__).resolve().parent.parent.parent / "frontend"
if _frontend_dir.exists():
    app.mount("/", StaticFiles(directory=str(_frontend_dir), html=True), name="frontend")
