export async function incarcaImagineCloudinary(fisier: File): Promise<string> {
  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
  const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;
  if (!cloudName || !uploadPreset) {
    throw new Error("Cloudinary nu este configurat (variabilele de mediu lipsesc).");
  }

  const formData = new FormData();
  formData.append("file", fisier);
  formData.append("upload_preset", uploadPreset);

  const raspuns = await fetch(`https://api.cloudinary.com/v1_1/${cloudName}/image/upload`, {
    method: "POST",
    body: formData,
  });

  if (!raspuns.ok) {
    throw new Error("Eroare la încărcarea pozei pe Cloudinary.");
  }

  const data = await raspuns.json();
  return data.secure_url as string;
}
