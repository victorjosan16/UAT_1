import type { MetadataRoute } from "next";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://posterart.ro";

export default function sitemap(): MetadataRoute.Sitemap {
  const rutePublice = ["", "/despre-noi", "/servicii", "/preturi", "/contact"];

  return rutePublice.map((ruta) => ({
    url: `${SITE_URL}${ruta}`,
    lastModified: new Date(),
    changeFrequency: ruta === "" ? "daily" : "weekly",
    priority: ruta === "" ? 1 : 0.7,
  }));
}
