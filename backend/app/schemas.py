"""Modele Pydantic folosite în toată aplicația."""
from datetime import datetime
from enum import Enum
from typing import Dict, List, Optional

from pydantic import BaseModel, Field


class SpatiuCuloare(str, Enum):
    RGB = "RGB"
    CMYK = "CMYK"


class DimensiuneCm(BaseModel):
    latime: float
    inaltime: float


class ProductProfile(BaseModel):
    nume: str
    dimensiune_cm: DimensiuneCm
    dpi_minim: int = Field(gt=0)
    spatiu_culoare: SpatiuCuloare
    zona_bleed_mm: float = Field(ge=0)
    necesita_eliminare_fundal: bool = False


class JobStatus(str, Enum):
    QUEUED = "queued"
    PROCESSING = "processing"
    DONE = "done"
    ERROR = "error"


class CheckSeveritate(str, Enum):
    OK = "ok"
    ATENTIE = "atentie"
    EROARE = "eroare"


class CheckResult(BaseModel):
    nume: str
    status: CheckSeveritate
    mesaj: str


class ProcessingReport(BaseModel):
    job_id: str
    tip_produs: str
    status: CheckSeveritate
    rezolutie_originala: Dict[str, int]
    rezolutie_finala: Optional[Dict[str, int]] = None
    dpi_original_calculat: Optional[float] = None
    dpi_final_calculat: Optional[float] = None
    dpi_minim_necesar: int
    spatiu_culoare_final: Optional[str] = None
    upscaling_aplicat: bool = False
    fundal_eliminat: bool = False
    avertismente: List[str] = Field(default_factory=list)
    checks: List[CheckResult] = Field(default_factory=list)
    creat_la: datetime = Field(default_factory=datetime.utcnow)


class UploadResponse(BaseModel):
    job_id: str
    status: JobStatus
    tip_produs: str


class JobStatusResponse(BaseModel):
    job_id: str
    status: JobStatus
    tip_produs: str
    eroare: Optional[str] = None
    report: Optional[ProcessingReport] = None
