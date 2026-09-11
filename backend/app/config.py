"""Configurare centrală: căi pe disc și opțiuni citite din mediu."""
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

UPLOAD_DIR = Path(os.getenv("UPLOAD_DIR", BASE_DIR / "uploads"))
PROCESSED_DIR = Path(os.getenv("PROCESSED_DIR", BASE_DIR / "processed"))
PROFILES_FILE = Path(os.getenv("PROFILES_FILE", BASE_DIR / "profiles" / "product_profiles.json"))

UPLOAD_DIR.mkdir(parents=True, exist_ok=True)
PROCESSED_DIR.mkdir(parents=True, exist_ok=True)

# URL implicit de webhook către CRM extern (poate fi suprascris per-request).
# Vezi app/webhook.py pentru integrarea viitoare cu CRM-ul.
DEFAULT_CRM_WEBHOOK_URL = os.getenv("CRM_WEBHOOK_URL", "")

MAX_UPLOAD_SIZE_MB = int(os.getenv("MAX_UPLOAD_SIZE_MB", "25"))
ALLOWED_CONTENT_TYPES = {"image/jpeg", "image/png", "image/webp", "image/bmp", "image/tiff"}
