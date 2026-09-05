import type { Metadata } from "next";
import ContactClient from "./ContactClient";

export const metadata: Metadata = {
  title: "Contact | PosterART",
  description: "Ia legătura cu echipa PosterART — date de contact și formular de mesaje.",
};

export default function ContactPage() {
  return <ContactClient />;
}
