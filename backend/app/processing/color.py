"""Conversii de spațiu de culoare."""
from PIL import Image

from app.schemas import SpatiuCuloare


def convert_color_space(image: Image.Image, target: SpatiuCuloare) -> Image.Image:
    if target == SpatiuCuloare.CMYK:
        if image.mode != "RGB":
            image = image.convert("RGB")
        return image.convert("CMYK")

    # RGB implicit
    if image.mode not in ("RGB", "RGBA"):
        return image.convert("RGB")
    return image
