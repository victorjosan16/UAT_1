"""Stocare (în memorie) a stării job-urilor de procesare.

Pentru o versiune de producție, acest store ar trebui înlocuit cu o bază
de date (ex. Redis / Postgres) - interfața de mai jos e suficient de
îngustă încât înlocuirea să nu afecteze restul aplicației.
"""
import threading
from dataclasses import dataclass, field
from typing import Dict, Optional

from app.schemas import JobStatus, ProcessingReport


@dataclass
class Job:
    job_id: str
    tip_produs: str
    upload_path: str
    status: JobStatus = JobStatus.QUEUED
    eroare: Optional[str] = None
    processed_path: Optional[str] = None
    preview_path: Optional[str] = None
    report: Optional[ProcessingReport] = None
    webhook_url: Optional[str] = None


class JobStore:
    def __init__(self) -> None:
        self._jobs: Dict[str, Job] = {}
        self._lock = threading.Lock()

    def create(self, job: Job) -> None:
        with self._lock:
            self._jobs[job.job_id] = job

    def get(self, job_id: str) -> Optional[Job]:
        with self._lock:
            return self._jobs.get(job_id)

    def update(self, job_id: str, **fields) -> None:
        with self._lock:
            job = self._jobs.get(job_id)
            if job is None:
                return
            for key, value in fields.items():
                setattr(job, key, value)


job_store = JobStore()
