import React from 'react';
import { Search, FilterX, Download, Calendar } from 'lucide-react';
import { DisciplinaryCaseStatus, DisciplinaryCasePriority } from '../../disciplinary/types';

interface ReportFilterBarProps {
  searchTerm?: string;
  onSearchTermChange?: (value: string) => void;
  dateFrom?: string;
  onDateFromChange?: (value: string) => void;
  dateTo?: string;
  onDateToChange?: (value: string) => void;
  status?: string;
  onStatusChange?: (value: string) => void;
  priority?: string;
  onPriorityChange?: (value: string) => void;
  measureType?: string;
  onMeasureTypeChange?: (value: string) => void;
  onReset?: () => void;
  onExportCsv?: () => void;
  isExporting?: boolean;
  showExportButton?: boolean;
}

export const ReportFilterBar: React.FC<ReportFilterBarProps> = ({
  searchTerm,
  onSearchTermChange,
  dateFrom,
  onDateFromChange,
  dateTo,
  onDateToChange,
  status,
  onStatusChange,
  priority,
  onPriorityChange,
  measureType,
  onMeasureTypeChange,
  onReset,
  onExportCsv,
  isExporting,
  showExportButton = true,
}) => {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm mb-6 space-y-3">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-3">
        {/* Search Term */}
        {onSearchTermChange && (
          <div className="relative">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <input
              type="text"
              value={searchTerm || ''}
              onChange={(e) => onSearchTermChange(e.target.value)}
              placeholder="Buscar por termo ou número..."
              className="w-full pl-9 pr-3 py-1.5 text-xs bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white"
            />
          </div>
        )}

        {/* Date From */}
        {onDateFromChange && (
          <div className="flex items-center gap-1 bg-slate-50 border border-slate-200 rounded-lg px-2.5 py-1">
            <Calendar className="h-3.5 w-3.5 text-slate-400 shrink-0" />
            <span className="text-[10px] uppercase font-bold text-slate-400 shrink-0">De:</span>
            <input
              type="date"
              value={dateFrom || ''}
              onChange={(e) => onDateFromChange(e.target.value)}
              className="w-full text-xs bg-transparent focus:outline-none text-slate-700"
            />
          </div>
        )}

        {/* Date To */}
        {onDateToChange && (
          <div className="flex items-center gap-1 bg-slate-50 border border-slate-200 rounded-lg px-2.5 py-1">
            <Calendar className="h-3.5 w-3.5 text-slate-400 shrink-0" />
            <span className="text-[10px] uppercase font-bold text-slate-400 shrink-0">Até:</span>
            <input
              type="date"
              value={dateTo || ''}
              onChange={(e) => onDateToChange(e.target.value)}
              className="w-full text-xs bg-transparent focus:outline-none text-slate-700"
            />
          </div>
        )}

        {/* Status Select */}
        {onStatusChange && (
          <select
            value={status || ''}
            onChange={(e) => onStatusChange(e.target.value)}
            className="w-full text-xs bg-slate-50 border border-slate-200 rounded-lg px-3 py-1.5 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Todos os Status</option>
            <option value={String(DisciplinaryCaseStatus.Draft)}>Rascunho</option>
            <option value={String(DisciplinaryCaseStatus.Open)}>Aberto</option>
            <option value={String(DisciplinaryCaseStatus.UnderInvestigation)}>Em Investigação</option>
            <option value={String(DisciplinaryCaseStatus.AwaitingDecision)}>Aguardando Decisão</option>
            <option value={String(DisciplinaryCaseStatus.Decided)}>Decidido</option>
            <option value={String(DisciplinaryCaseStatus.Completed)}>Concluído</option>
            <option value={String(DisciplinaryCaseStatus.Cancelled)}>Cancelado</option>
          </select>
        )}

        {/* Priority Select */}
        {onPriorityChange && (
          <select
            value={priority || ''}
            onChange={(e) => onPriorityChange(e.target.value)}
            className="w-full text-xs bg-slate-50 border border-slate-200 rounded-lg px-3 py-1.5 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Todas as Prioridades</option>
            <option value={String(DisciplinaryCasePriority.Low)}>Baixa</option>
            <option value={String(DisciplinaryCasePriority.Normal)}>Normal</option>
            <option value={String(DisciplinaryCasePriority.High)}>Alta</option>
            <option value={String(DisciplinaryCasePriority.Critical)}>Crítica</option>
          </select>
        )}

        {/* Measure Type Select */}
        {onMeasureTypeChange && (
          <select
            value={measureType || ''}
            onChange={(e) => onMeasureTypeChange(e.target.value)}
            className="w-full text-xs bg-slate-50 border border-slate-200 rounded-lg px-3 py-1.5 text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Todas as Medidas</option>
            <option value="VerbalWarning">Advertência Verbal</option>
            <option value="WrittenWarning">Advertência Escrita</option>
            <option value="Suspension">Suspensão</option>
            <option value="TerminationWithCause">Demissão por Justa Causa</option>
          </select>
        )}
      </div>

      <div className="flex items-center justify-end gap-2 pt-1 border-t border-slate-100">
        {onReset && (
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition-colors"
          >
            <FilterX className="w-3.5 h-3.5" />
            Limpar Filtros
          </button>
        )}

        {showExportButton && onExportCsv && (
          <button
            type="button"
            onClick={onExportCsv}
            disabled={isExporting}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 rounded-lg transition-colors"
          >
            <Download className="w-3.5 h-3.5" />
            {isExporting ? 'Exportando CSV...' : 'Exportar CSV'}
          </button>
        )}
      </div>
    </div>
  );
};
