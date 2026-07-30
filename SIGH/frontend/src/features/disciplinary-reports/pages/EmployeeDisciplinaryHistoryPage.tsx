import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { UserCheck, Shield, AlertTriangle, Briefcase, Calendar } from 'lucide-react';
import { useEmployeeDisciplinaryHistory } from '../hooks/useDisciplinaryReports';
import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { EmployeeHistoryTimeline } from '../components/EmployeeHistoryTimeline';
import { ReportFilterBar } from '../components/ReportFilterBar';
import { EmployeeHistoryFilters } from '../types';

export const EmployeeDisciplinaryHistoryPage: React.FC = () => {
  const { employeeId: routeEmployeeId } = useParams<{ employeeId?: string }>();
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>(
    routeEmployeeId || '11111111-2222-3333-4444-555555555555'
  );

  const permissions = useDisciplinaryReportPermissions();
  const [filters, setFilters] = useState<EmployeeHistoryFilters>({
    companyId: '00000000-0000-0000-0000-000000000001',
    pageNumber: 1,
    pageSize: 10,
  });

  const { data: history, isLoading, isError } = useEmployeeDisciplinaryHistory(
    selectedEmployeeId,
    filters,
    permissions.canViewEmployeeHistory
  );

  if (!permissions.canViewEmployeeHistory) {
    return (
      <div className="p-8 text-center bg-white rounded-xl border border-slate-200">
        <Shield className="w-12 h-12 text-rose-500 mx-auto mb-3" />
        <h2 className="text-lg font-bold text-slate-800">Acesso Restrito</h2>
        <p className="text-sm text-slate-500 mt-1">
          Você não possui permissão para consultar o histórico disciplinar do funcionário (`Disciplinary.Reports.ViewEmployeeHistory`).
        </p>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <UserCheck className="w-7 h-7 text-indigo-600" />
          Histórico Disciplinar do Funcionário
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Linha do tempo consolidada e indicadores disciplinares acumulados por colaborador.
        </p>
      </div>

      {/* Employee ID Selector Bar */}
      <div className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm flex flex-wrap items-center gap-3">
        <label className="text-xs font-semibold text-slate-700 whitespace-nowrap">
          ID do Funcionário:
        </label>
        <input
          type="text"
          value={selectedEmployeeId}
          onChange={(e) => setSelectedEmployeeId(e.target.value)}
          placeholder="Informe o GUID do funcionário..."
          className="flex-1 min-w-[280px] text-xs bg-slate-50 border border-slate-200 rounded-lg px-3 py-1.5 font-mono text-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      </div>

      {/* Date & Status Filter Bar */}
      <ReportFilterBar
        dateFrom={filters.dateFrom}
        onDateFromChange={(val) => setFilters((f) => ({ ...f, dateFrom: val }))}
        dateTo={filters.dateTo}
        onDateToChange={(val) => setFilters((f) => ({ ...f, dateTo: val }))}
        status={filters.status}
        onStatusChange={(val) => setFilters((f) => ({ ...f, status: val as any }))}
        onReset={() =>
          setFilters({
            companyId: '00000000-0000-0000-0000-000000000001',
            pageNumber: 1,
            pageSize: 10,
          })
        }
        showExportButton={false}
      />

      {/* Content */}
      {isLoading ? (
        <div className="p-12 text-center bg-white rounded-xl border border-slate-200 space-y-3">
          <div className="animate-spin w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full mx-auto" />
          <p className="text-xs text-slate-500">Recuperando histórico disciplinar do funcionário...</p>
        </div>
      ) : isError ? (
        <div className="p-8 text-center bg-rose-50 border border-rose-200 rounded-xl">
          <AlertTriangle className="w-8 h-8 text-rose-600 mx-auto mb-2" />
          <h3 className="text-sm font-bold text-rose-800">Funcionário não localizado</h3>
          <p className="text-xs text-rose-600 mt-1">
            Nenhum histórico foi localizado para o ID informado na empresa atual.
          </p>
        </div>
      ) : history ? (
        <div className="space-y-6">
          {/* Employee Header Profile Box */}
          <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-full bg-indigo-50 border border-indigo-200 flex items-center justify-center text-indigo-600 font-bold text-lg">
                {history.employeeName.charAt(0)}
              </div>
              <div>
                <h2 className="text-lg font-bold text-slate-900">{history.employeeName}</h2>
                <div className="flex items-center gap-3 text-xs text-slate-500 mt-0.5">
                  <span className="flex items-center gap-1">
                    <Briefcase className="w-3.5 h-3.5" />
                    Matrícula: <strong className="text-slate-700">{history.registrationNumber}</strong>
                  </span>
                  <span>•</span>
                  <span>{history.companyName}</span>
                </div>
              </div>
            </div>

            {/* Quick Metrics */}
            <div className="flex items-center gap-4 border-l border-slate-200 pl-4 text-xs">
              <div className="text-center">
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Total Processos</span>
                <span className="text-base font-bold text-slate-900">{history.totalCases}</span>
              </div>
              <div className="text-center">
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Em Aberto</span>
                <span className="text-base font-bold text-amber-600">{history.openCases}</span>
              </div>
              <div className="text-center">
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Medidas</span>
                <span className="text-base font-bold text-rose-600">{history.totalMeasures}</span>
              </div>
            </div>
          </div>

          {/* Timeline */}
          <div>
            <h3 className="text-sm font-bold text-slate-800 mb-4 flex items-center gap-2">
              <Calendar className="w-4 h-4 text-indigo-600" />
              Histórico de Ocorrências e Processos
            </h3>
            <EmployeeHistoryTimeline cases={history.cases as any[]} />
          </div>
        </div>
      ) : null}
    </div>
  );
};
