import type { Metadata } from "next";
import PreturiClient from "./PreturiClient";

export const metadata: Metadata = {
  title: "Prețuri | PosterART",
  description: "Lista completă de prețuri pentru produsele PosterART, actualizată live.",
};

export default function PreturiPage() {
  return <PreturiClient />;
}
