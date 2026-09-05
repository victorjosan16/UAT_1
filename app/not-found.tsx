import Link from "next/link";
import { Home, Search } from "lucide-react";

export default function NotFound() {
  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="max-w-md w-full text-center">
        <div className="w-16 h-16 rounded-2xl bg-brand-primary mx-auto flex items-center justify-center text-white font-bold text-2xl mb-6">
          PA
        </div>
        <p className="text-7xl font-bold text-brand-primary mb-2">404</p>
        <h1 className="text-2xl font-bold text-gray-900 mb-3">
          Pagina nu a fost găsită
        </h1>
        <p className="text-gray-500 mb-8">
          Ne pare rău, pagina pe care o cauți nu există sau a fost mutată.
        </p>
        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <Link
            href="/"
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-brand-primary text-white px-5 py-3 font-medium hover:opacity-90 transition-opacity"
          >
            <Home className="w-4 h-4" />
            Înapoi acasă
          </Link>
          <Link
            href="/preturi"
            className="inline-flex items-center justify-center gap-2 rounded-xl border border-gray-200 bg-white text-gray-700 px-5 py-3 font-medium hover:border-brand-primary hover:text-brand-primary transition-colors"
          >
            <Search className="w-4 h-4" />
            Vezi produsele
          </Link>
        </div>
      </div>
    </div>
  );
}
