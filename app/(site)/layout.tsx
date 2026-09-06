import { SiteProvider } from "@/lib/site-context";
import { SiteHeader } from "@/components/SiteHeader";
import { SiteFooter } from "@/components/SiteFooter";
import { CartDrawer } from "@/components/CartDrawer";
import { ToastNotification } from "@/components/ToastNotification";

export default function SiteLayout({ children }: { children: React.ReactNode }) {
  return (
    <SiteProvider>
      <main className="min-h-screen bg-gray-50 flex flex-col">
        <SiteHeader />
        <div className="flex-1">{children}</div>
        <SiteFooter />
      </main>
      <CartDrawer />
      <ToastNotification />
    </SiteProvider>
  );
}
