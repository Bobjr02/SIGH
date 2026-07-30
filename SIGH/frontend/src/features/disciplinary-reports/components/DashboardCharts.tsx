import React from 'react';
import { DashboardDistributionItemDto } from '../types';

interface ChartCardProps {
  title: string;
  items: DashboardDistributionItemDto[];
  colorPalette?: string[];
  emptyMessage?: string;
}

const DEFAULT_COLORS = [
  'bg-indigo-500',
  'bg-sky-500',
  'bg-emerald-500',
  'bg-amber-500',
  'bg-rose-500',
  'bg-purple-500',
  'bg-teal-500',
  'bg-slate-500',
];

export const DistributionChartCard: React.FC<ChartCardProps> = ({
  title,
  items,
  colorPalette = DEFAULT_COLORS,
  emptyMessage = 'Nenhum dado registrado para o período.',
}) => {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
      <h3 className="text-sm font-semibold text-slate-800 mb-4">{title}</h3>

      {items.length === 0 ? (
        <div className="py-8 text-center text-xs text-slate-400">
          {emptyMessage}
        </div>
      ) : (
        <div className="space-y-3.5">
          {items.map((item, index) => {
            const barColor = colorPalette[index % colorPalette.length];
            return (
              <div key={item.id || item.label || index} className="space-y-1">
                <div className="flex justify-between items-center text-xs">
                  <span className="font-medium text-slate-700 truncate max-w-[200px]">
                    {item.label}
                  </span>
                  <div className="flex items-center gap-2">
                    <span className="text-slate-500 font-mono">{item.value}</span>
                    <span className="font-semibold text-slate-900 w-12 text-right">
                      {item.percentage}%
                    </span>
                  </div>
                </div>

                <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
                  <div
                    className={`h-2 rounded-full transition-all duration-500 ${barColor}`}
                    style={{ width: `${Math.min(Math.max(item.percentage, 2), 100)}%` }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
