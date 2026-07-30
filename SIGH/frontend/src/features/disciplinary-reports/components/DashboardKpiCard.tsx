import React from 'react';
import { LucideIcon } from 'lucide-react';

interface DashboardKpiCardProps {
  title: string;
  value: number | string;
  subtitle?: string;
  icon: LucideIcon;
  colorClassName?: string;
  badgeText?: string;
}

export const DashboardKpiCard: React.FC<DashboardKpiCardProps> = ({
  title,
  value,
  subtitle,
  icon: Icon,
  colorClassName = 'bg-blue-50 text-blue-600',
  badgeText,
}) => {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm hover:shadow-md transition-all">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
          {title}
        </span>
        <div className={`p-2.5 rounded-lg ${colorClassName}`}>
          <Icon className="w-5 h-5" />
        </div>
      </div>

      <div className="mt-3 flex items-baseline gap-2">
        <span className="text-2xl font-bold text-slate-900">{value}</span>
        {badgeText && (
          <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700">
            {badgeText}
          </span>
        )}
      </div>

      {subtitle && (
        <p className="mt-1 text-xs text-slate-500 line-clamp-1">{subtitle}</p>
      )}
    </div>
  );
};
