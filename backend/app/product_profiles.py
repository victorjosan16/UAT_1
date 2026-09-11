"""Încărcare și acces la profilurile de produs.

Profilurile NU sunt hardcodate în cod - locuiesc în
`profiles/product_profiles.json`. Pentru a adăuga un produs nou, vezi
README.md ("Cum adaug un profil de produs nou").
"""
import json
import threading
from typing import Dict

from app.config import PROFILES_FILE
from app.schemas import ProductProfile

_lock = threading.Lock()
_cache: Dict[str, ProductProfile] | None = None


class ProfileNotFoundError(Exception):
    pass


def _load_from_disk() -> Dict[str, ProductProfile]:
    if not PROFILES_FILE.exists():
        raise FileNotFoundError(f"Fișierul de profiluri nu există: {PROFILES_FILE}")
    with open(PROFILES_FILE, "r", encoding="utf-8") as f:
        raw = json.load(f)
    return {key: ProductProfile(**value) for key, value in raw.items()}


def get_all_profiles(force_reload: bool = False) -> Dict[str, ProductProfile]:
    global _cache
    with _lock:
        if _cache is None or force_reload:
            _cache = _load_from_disk()
        return dict(_cache)


def get_profile(tip_produs: str) -> ProductProfile:
    profiles = get_all_profiles()
    if tip_produs not in profiles:
        disponibile = ", ".join(sorted(profiles.keys()))
        raise ProfileNotFoundError(
            f"Tip de produs necunoscut: '{tip_produs}'. Tipuri disponibile: {disponibile}"
        )
    return profiles[tip_produs]
