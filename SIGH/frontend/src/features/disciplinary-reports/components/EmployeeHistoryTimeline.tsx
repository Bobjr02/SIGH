import React from 'react';
import { Calendar, AlertCircle, FileText, CheckCircle, ShieldAlert } from 'lucide-react';
import { EmployeeDisciplinaryCaseSummaryDto } from '../types';

interface EmployeeHistoryTimelineProps {
  cases: EmployeeDisciplinaryCaseSummaryDto[];
  onSelectCase?: (caseId: string) => void;
}

export const EmployeeHistoryTimeline: React.FC<EmployeeHistoryTimelineProps> = ({
  cases,
  onSelectCase,
}) => {
  if (cases.length === 0) {
    return (
      <div className="bg-white rounded-xl border border-slate-200 p-8 text-center">
        <CheckCircle className="w-10 h-10 text-emerald-500 mx-auto mb-2" />
        <h4 className="text-sm font-semibold text-slate-800">Nenhum processo disciplinar</h4>
        <p className="text-xs text-slate-500 mt-1">
          O funcionário não possui registros de ocorrências ou processos no período selecionado.
        </p>
      </div>
    );
  }

  return (
    <div className="relative border-l-2 border-slate-200 ml-4 space-y-6 py-2">
      {cases.map((c) => (
        <div key={c.caseId} className="relative pl-6">
          {/* Node Badge */}
          <div className="absolute -left-[17px] top-1.5 w-8 h-8 rounded-full bg-white border-2 border-indigo-600 flex items-center justify-center text-indigo-600 shadow-sm">
            <ShieldAlert className="w-4 h-4" />
          </div>

          <div
            onClick={() => onSelectCase && onSelectCase(c.caseId)}
            className={`bg-white rounded-xl border border-slate-200 p-4 shadow-sm hover:border-indigo-300 hover:shadow-md transition-all ${
              onSelectCase ? 'cursor-pointer' : ''
            }`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-2">
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono font-bold text-indigo-600">
                  {c.caseNumber}
                </span>
                <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold uppercase bg-slate-100 text-slate-700">
                  {c.status}
                </span>
                <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold uppercase bg-amber-50 text-amber-700 border border-amber-200">
                  {c.priority}
                </span>
              </div>

              <div className="flex items-center gap-1 text-xs text-slate-400">
                <Calendar className="w-3.5 h-3.5" />
                <span>{new Date(c.openedAt).toLocaleDateString('pt-BR')}</span>
              </div>
            </div>

            <h4 className="text-sm font-bold text-slate-900 mt-2">{c.title}</h4>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mt-3 pt-3 border-t border-slate-100 text-xs">
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Responsável</span>
                <span className="text-slate-700 font-medium truncate block">{c.responsibleEmployee}</span>
              </div>
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Ocorrências</span>
                <span className="text-slate-700 font-semibold">{c.occurrencesCount}</span>
              </div>
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Decisões</span>
                <span className="text-slate-700 font-semibold">{c.decisionsCount}</span>
              </div>
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Medidas Aplicadas</span>
                <span className="text-slate-700 font-semibold text-rose-600">{c.measuresCount}</span>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
