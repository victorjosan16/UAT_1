import type { Metadata } from "next";
import ServiciiClient from "./ServiciiClient";

export const metadata: Metadata = {
  title: "Servicii | PosterART",
  description: "Descoperă serviciile de printare PosterART: cărți de vizită, flyere, bannere și multe altele.",
};

export default function ServiciiPage() {
  return <ServiciiClient />;
}
