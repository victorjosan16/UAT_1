# Serviciu de procesare automată a imaginilor pentru print

Modul independent (backend + frontend simplu) pentru pregătirea automată a
imaginilor destinate printării (tricouri, căni, bannere, postere etc.).
Gândit ca serviciu separat, ce poate fi integrat ulterior într-un CRM
existent printr-un webhook.

## Stack

- **Backend:** Python + FastAPI
- **Procesare imagine:** Pillow, OpenCV (`opencv-python-headless`), `rembg`
  (eliminare fundal), upscaling AI cu punct de extensie pentru
  Real-ESRGAN / `cv2.dnn_superres` (fallback funcțional: Lanczos)
- **Frontend:** HTML + JS simplu, fără framework
- **Storage:** sistem de fișiere local (`backend/uploads/`, `backend/processed/`)

## Structură

```
backend/
  app/
    main.py               # rutare API (HTTP)
    config.py              # căi pe disc, opțiuni din mediu
    schemas.py              # modele Pydantic (profiluri, joburi, raport)
    job_store.py             # stare joburi (în memorie)
    product_profiles.py       # încărcare profiluri din JSON
    webhook.py                 # notificare CRM extern
    processing/
      pipeline.py               # orchestrarea pașilor a-e din cerință
      color.py                   # conversie RGB <-> CMYK
      upscaling.py                # upscaling (fallback Lanczos + hook AI)
      background_removal.py        # eliminare fundal (rembg, opțional)
      errors.py
  profiles/
    product_profiles.json      # profiluri de produs (NU e hardcodat în cod)
  uploads/                    # imagini brute încărcate
  processed/                   # rezultate + rapoarte JSON
  requirements.txt
frontend/
  index.html
  app.js
  style.css
```

## Instalare și rulare

```bash
cd backend
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt

uvicorn app.main:app --reload --port 8000
```

Deschide `http://localhost:8000/` — FastAPI servește și frontend-ul static
din `frontend/` (montat pe `/`), deci nu ai nevoie de un server separat.

> Notă: `rembg` (eliminare fundal) aduce dependențe mai grele
> (`onnxruntime`) și poate lipsi pe unele platforme. Dacă nu e instalat,
> pipeline-ul nu eșuează — pasul este omis automat, iar raportul include
> un avertisment explicit despre asta.

## API

| Metodă | Rută                | Descriere                                                            |
| ------ | ------------------- | --------------------------------------------------------------------- |
| GET    | `/profiles`          | Listează profilurile de produs disponibile                            |
| POST   | `/upload`             | `multipart/form-data`: `file`, `tip_produs`, opțional `webhook_url`    |
| GET    | `/status/{job_id}`     | Status curent + raportul complet (când e gata)                        |
| GET    | `/preview/{job_id}`     | Imaginea finală, ca PNG (pentru afișare în browser)                   |
| GET    | `/report/{job_id}`       | Doar raportul JSON                                                    |
| GET    | `/download/{job_id}`      | Arhivă `.zip` cu imaginea procesată + raportul JSON                   |

`POST /upload` răspunde imediat cu `202 Accepted` și un `job_id` — procesarea
rulează asincron (FastAPI `BackgroundTasks`), iar frontend-ul face polling pe
`/status/{job_id}` până la `done` sau `error`.

## Pipeline de procesare

Pentru fiecare imagine, în ordine:

1. **Validare** — deschidere + verificare integritate (Pillow); imagini
   corupte sau formate nesuportate întorc o eroare clară, fără să pice
   serverul.
2. **Verificare rezoluție/DPI** — se calculează DPI-ul efectiv al imaginii
   raportat la dimensiunea fizică cerută de profil (+ bleed). Dacă e sub
   `dpi_minim`, se aplică upscaling.
3. **Eliminare fundal** — doar dacă profilul are `necesita_eliminare_fundal: true`.
4. **Conversie spațiu de culoare** — RGB sau CMYK, conform profilului.
5. **Redimensionare/poziționare** — imaginea e adusă la dimensiunea canvas-ului
   țintă (dimensiune produs + bleed pe toate laturile), cu decupare centrată
   dacă proporțiile diferă; se verifică și dacă există conținut cu contrast
   ridicat foarte aproape de marginea de tăiere (zonă de siguranță).
6. **Raport JSON** — rezoluție finală, DPI calculat, avertismente, status
   (`ok` / `atentie` / `eroare`) și o listă de `checks` (folosită de frontend
   pentru iconițele verde/galben/roșu).

Fișierul CMYK final e salvat ca `.tiff` (Pillow nu poate scrie JPEG CMYK în
mod fiabil pentru print); pentru afișare în browser se generează separat un
preview RGB PNG.

## Cum adaug un profil de produs nou

Profilurile locuiesc în `backend/profiles/product_profiles.json` și **nu**
sunt hardcodate în cod. Pentru a adăuga un produs nou, adaugă o intrare nouă
în acest fișier:

```json
"sticker": {
  "nume": "Sticker",
  "dimensiune_cm": { "latime": 8, "inaltime": 8 },
  "dpi_minim": 300,
  "spatiu_culoare": "RGB",
  "zona_bleed_mm": 2,
  "necesita_eliminare_fundal": true
}
```

Cheia (`sticker`) este `tip_produs`-ul trimis către `/upload`. Nu e nevoie de
niciun redeploy de cod sau restart — fișierul e citit din nou dacă apelezi
`get_all_profiles(force_reload=True)`, sau simplu repornește serverul (mai
simplu, în MVP-ul curent). Frontend-ul populează automat selectorul din
`GET /profiles`.

Câmpuri obligatorii per profil:

- `nume` — etichetă afișată în UI
- `dimensiune_cm.latime` / `dimensiune_cm.inaltime` — dimensiunea finală a
  produsului, în cm
- `dpi_minim` — rezoluția minimă acceptată la dimensiunea de mai sus
- `spatiu_culoare` — `"RGB"` sau `"CMYK"`
- `zona_bleed_mm` — bleed pe fiecare latură, în mm
- `necesita_eliminare_fundal` — `true`/`false`

## Integrare viitoare cu CRM (webhook)

Structura este deja pregătită pentru integrare:

- La `POST /upload` poți trimite opțional un câmp `webhook_url`. Dacă lipsește,
  se folosește variabila de mediu `CRM_WEBHOOK_URL` (implicit gol = dezactivat).
- La finalul procesării (`app/main.py::_run_job`), dacă un webhook e
  configurat, `app/webhook.py::notify_crm` face `POST` către CRM cu:

```json
{
  "event": "image_processing.completed",
  "job_id": "...",
  "report": { "...": "raportul complet, vezi /report/{job_id}" }
}
```

- Trimiterea e "best-effort": dacă CRM-ul nu răspunde, job-ul rămâne `done`
  pentru utilizator — eroarea e doar logată, nu strică procesarea.
- Pentru producție, `job_store.py` ar trebui înlocuit cu o bază de date reală
  (interfața e deja izolată, exact pentru acest motiv), iar autentificarea pe
  webhook (semnătură HMAC, token) ar trebui adăugată în `webhook.py`.

## Deploy frontend pe Firebase Hosting

Firebase Hosting servește doar fișiere statice — **nu** poate rula backend-ul
FastAPI. Configurația din acest repo (`firebase.json`) publică doar folderul
`frontend/`; backend-ul trebuie rulat separat (Codespaces, Cloud Run, VPS
etc.) și expus pe un URL public.

1. Rulează backend-ul undeva accesibil public și notează URL-ul (ex.
   `https://backend-xxxxx.a.run.app`).
2. Editează `frontend/config.js` și pune acel URL:
   ```js
   window.PRINT_SERVICE_API_BASE = "https://backend-xxxxx.a.run.app";
   ```
3. Asigură-te că backend-ul acceptă cereri de pe domeniul Firebase (CORS e
   deja permisiv — `allow_origins=["*"]` în `app/main.py`).
4. Din rădăcina repo-ului:
   ```bash
   npm install -g firebase-tools   # o singură dată
   firebase login
   firebase use --add               # selectează/asociază proiectul Firebase
   firebase deploy --only hosting
   ```

Fără backend accesibil public la pasul 2, aplicația se încarcă dar upload-ul
va eșua (nu are unde trimite cererea).

## Variabile de mediu

| Variabilă           | Implicit                          | Descriere                                |
| -------------------- | ---------------------------------- | ------------------------------------------ |
| `UPLOAD_DIR`           | `backend/uploads`                    | Folder imagini brute                       |
| `PROCESSED_DIR`         | `backend/processed`                   | Folder rezultate + rapoarte                |
| `PROFILES_FILE`          | `backend/profiles/product_profiles.json` | Fișier profiluri produs               |
| `CRM_WEBHOOK_URL`          | *(gol)*                              | URL implicit pentru notificare CRM        |
| `MAX_UPLOAD_SIZE_MB`         | `25`                                | Dimensiune maximă fișier încărcat (MB)    |

## Ce urmează (pasul 2)

Versiunea curentă e deja funcțională end-to-end (upload → resize → conversie
CMYK → calcul DPI → raport), cu eliminare de fundal și upscaling incluse și
funcționale, dar cu fallback-uri clare când dependențele AI grele
(`rembg`/model Real-ESRGAN) nu sunt instalate:

- **Eliminare fundal**: funcțională via `rembg`, dacă e instalat. Fără el,
  pasul e omis și marcat ca avertisment în raport.
- **Upscaling AI**: `app/processing/upscaling.py` are un punct de extensie
  (`_try_ai_upscale`) pregătit pentru Real-ESRGAN sau `cv2.dnn_superres`
  (necesită un model `.pb`/`.pth` descărcat separat). Până atunci, se
  folosește un fallback Lanczos, mereu marcat explicit în raport.
