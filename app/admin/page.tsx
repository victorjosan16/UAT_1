"use client";

import { DollarSign, AlertTriangle, ShoppingCart, UserCheck } from "lucide-react";

const KPI_MOCK = [
  { titlu: "Vânzări azi", valoare: "2.450 lei", icon: DollarSign, culoare: "text-brand-primary bg-brand-primary/10" },
  { titlu: "Alerte Stoc", valoare: "4 produse", icon: AlertTriangle, culoare: "text-red-600 bg-red-50" },
  { titlu: "Coșuri Abandonate", valoare: "7", icon: ShoppingCart, culoare: "text-brand-accent bg-brand-accent/10" },
  { titlu: "Follow-up Clienți", valoare: "3 restante", icon: UserCheck, culoare: "text-blue-600 bg-blue-50" },
];

export default function AdminDashboard() {
  return (
    <div className="space-y-6 animate-in fade-in">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Dashboard</h1>
        <p className="text-sm text-gray-500 mt-1">Vedere hibridă: E-commerce + CRM</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {KPI_MOCK.map((kpi) => {
          const Icon = kpi.icon;
          return (
            <div
              key={kpi.titlu}
              className="bg-white rounded-2xl p-5 shadow-sm hover:shadow-xl transition-shadow border border-gray-100"
            >
              <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${kpi.culoare}`}>
                <Icon className="w-5 h-5" />
              </div>
              <p className="text-sm text-gray-500 mt-4">{kpi.titlu}</p>
              <p className="text-xl font-semibold text-gray-900 mt-1">{kpi.valoare}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
