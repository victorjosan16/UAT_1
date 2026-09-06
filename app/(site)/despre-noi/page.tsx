import type { Metadata } from "next";
import DespreNoiClient from "./DespreNoiClient";

export const metadata: Metadata = {
  title: "Despre Noi | PosterART",
  description: "Povestea și valorile din spatele PosterART — tipografie digitală axată pe viteză și calitate.",
};

export default function DespreNoiPage() {
  return <DespreNoiClient />;
}
