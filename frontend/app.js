const API_BASE = ""; // same origin (servit de FastAPI); schimbă dacă rulezi frontend separat

const el = (id) => document.getElementById(id);
const form = el("upload-form");
const tipProdusSelect = el("tip_produs");
const profileMeta = el("profile-meta");
const fileInput = el("file");
const submitBtn = el("submit-btn");
const statusLine = el("status-line");
const originalPreview = el("original-preview");
const finalPreview = el("final-preview");
const reportSection = el("report-section");
const reportStatus = el("report-status");
const reportSummary = el("report-summary");
const reportChecks = el("report-checks");
const downloadLink = el("download-link");
const errorSection = el("error-section");
const errorBox = el("error-box");

let profiles = {};
let pollTimer = null;

const ICONS = { ok: "✅", atentie: "⚠️", eroare: "❌" };
const STATUS_LABEL = { ok: "OK", atentie: "Atenție", eroare: "Eroare" };

function resetUI() {
  reportSection.hidden = true;
  errorSection.hidden = true;
  downloadLink.hidden = true;
  finalPreview.innerHTML = '<span class="placeholder">Se procesează...</span>';
  clearTimeout(pollTimer);
}

async function loadProfiles() {
  const res = await fetch(`${API_BASE}/profiles`);
  profiles = await res.json();
  tipProdusSelect.innerHTML = "";
  for (const [key, profile] of Object.entries(profiles)) {
    const opt = document.createElement("option");
    opt.value = key;
    opt.textContent = profile.nume;
    tipProdusSelect.appendChild(opt);
  }
  renderProfileMeta();
}

function renderProfileMeta() {
  const p = profiles[tipProdusSelect.value];
  if (!p) {
    profileMeta.hidden = true;
    return;
  }
  profileMeta.hidden = false;
  profileMeta.innerHTML = `
    <strong>${p.nume}</strong> — ${p.dimensiune_cm.latime} × ${p.dimensiune_cm.inaltime} cm ·
    ${p.dpi_minim} DPI minim · spațiu culoare ${p.spatiu_culoare} ·
    bleed ${p.zona_bleed_mm}mm ${p.necesita_eliminare_fundal ? "· eliminare fundal automată" : ""}
  `;
}

tipProdusSelect.addEventListener("change", renderProfileMeta);

fileInput.addEventListener("change", () => {
  const file = fileInput.files[0];
  if (!file) return;
  const url = URL.createObjectURL(file);
  originalPreview.innerHTML = `<img src="${url}" alt="Imagine originală" />`;
});

function renderChecks(report) {
  reportStatus.innerHTML = `<span class="status-badge ${report.status}">${ICONS[report.status]} Status general: ${STATUS_LABEL[report.status]}</span>`;

  reportSummary.innerHTML = `
    <div><span class="label">Rezoluție originală</span>${report.rezolutie_originala.latime_px} × ${report.rezolutie_originala.inaltime_px}px</div>
    <div><span class="label">Rezoluție finală</span>${report.rezolutie_finala.latime_px} × ${report.rezolutie_finala.inaltime_px}px</div>
    <div><span class="label">DPI original (calculat)</span>${report.dpi_original_calculat}</div>
    <div><span class="label">DPI necesar</span>${report.dpi_minim_necesar}</div>
    <div><span class="label">Spațiu culoare final</span>${report.spatiu_culoare_final}</div>
    <div><span class="label">Upscaling aplicat</span>${report.upscaling_aplicat ? "Da" : "Nu"}</div>
    <div><span class="label">Fundal eliminat</span>${report.fundal_eliminat ? "Da" : "Nu"}</div>
  `;

  reportChecks.innerHTML = "";
  for (const check of report.checks) {
    const li = document.createElement("li");
    li.className = check.status;
    li.innerHTML = `<span class="icon">${ICONS[check.status]}</span><div><span class="check-name">${check.nume.replaceAll("_", " ")}:</span>${check.mesaj}</div>`;
    reportChecks.appendChild(li);
  }
}

async function pollStatus(jobId) {
  const res = await fetch(`${API_BASE}/status/${jobId}`);
  if (!res.ok) {
    showError(`Nu s-a putut obține statusul job-ului (HTTP ${res.status}).`);
    return;
  }
  const data = await res.json();
  statusLine.hidden = false;
  statusLine.textContent = `Status: ${data.status}`;

  if (data.status === "done") {
    finalPreview.innerHTML = `<img src="${API_BASE}/preview/${jobId}?t=${Date.now()}" alt="Imagine procesată" />`;
    reportSection.hidden = false;
    renderChecks(data.report);
    downloadLink.hidden = false;
    downloadLink.href = `${API_BASE}/download/${jobId}`;
    submitBtn.disabled = false;
  } else if (data.status === "error") {
    showError(data.eroare || "Procesarea a eșuat.");
    submitBtn.disabled = false;
  } else {
    pollTimer = setTimeout(() => pollStatus(jobId), 1200);
  }
}

function showError(message) {
  errorSection.hidden = false;
  errorBox.textContent = message;
  finalPreview.innerHTML = '<span class="placeholder">Procesare eșuată</span>';
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  resetUI();
  submitBtn.disabled = true;

  const formData = new FormData();
  formData.append("file", fileInput.files[0]);
  formData.append("tip_produs", tipProdusSelect.value);

  try {
    const res = await fetch(`${API_BASE}/upload`, { method: "POST", body: formData });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.detail || `Eroare la upload (HTTP ${res.status})`);
    }
    const data = await res.json();
    statusLine.hidden = false;
    statusLine.textContent = `Job creat: ${data.job_id} (status: ${data.status})`;
    pollStatus(data.job_id);
  } catch (err) {
    showError(err.message);
    submitBtn.disabled = false;
  }
});

loadProfiles().catch(() => showError("Nu s-au putut încărca profilurile de produs de la backend."));
