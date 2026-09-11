"""Integrare cu un CRM extern.

După finalizarea procesării, dacă este configurat un URL de webhook
(implicit prin `CRM_WEBHOOK_URL`, sau per-request prin câmpul
`webhook_url` la upload), trimitem automat rezultatul (raportul +
link-uri de download) către CRM printr-un POST HTTP.

Trimiterea este "best-effort": o eroare la notificarea CRM-ului nu
trebuie să strice job-ul de procesare, care rămâne "done" din
perspectiva utilizatorului - eroarea e doar logată.
"""
import logging

import requests

from app.schemas import ProcessingReport

logger = logging.getLogger("print_service.webhook")


def notify_crm(webhook_url: str, job_id: str, report: ProcessingReport) -> bool:
    if not webhook_url:
        return False

    payload = {
        "event": "image_processing.completed",
        "job_id": job_id,
        "report": report.model_dump(mode="json"),
    }
    try:
        response = requests.post(webhook_url, json=payload, timeout=10)
        response.raise_for_status()
        return True
    except requests.RequestException:
        logger.exception("Notificarea CRM-ului a eșuat pentru job %s (webhook: %s)", job_id, webhook_url)
        return False
