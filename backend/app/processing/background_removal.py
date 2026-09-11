"""Eliminare fundal (pas 2).

Folosește `rembg` dacă este instalat. Dacă pachetul (sau dependențele
lui, ex. onnxruntime) lipsesc, funcția degradează grațios: întoarce
imaginea nemodificată și semnalează acest lucru apelantului, care adaugă
un avertisment în raport - pipeline-ul nu eșuează niciodată din această
cauză.
"""
from PIL import Image

_session = None
_import_error: Exception | None = None


def _get_session():
    global _session, _import_error
    if _session is not None or _import_error is not None:
        return _session
    try:
        from rembg import new_session

        _session = new_session("u2net")
    except Exception as exc:  # pragma: no cover - depinde de mediu
        _import_error = exc
    return _session


def remove_background(image: Image.Image) -> tuple[Image.Image, bool]:
    """Întoarce (imagine_procesată, s_a_reușit)."""
    session = _get_session()
    if session is None:
        return image, False

    from rembg import remove

    rgba = image.convert("RGBA")
    result = remove(rgba, session=session)
    return result, True
