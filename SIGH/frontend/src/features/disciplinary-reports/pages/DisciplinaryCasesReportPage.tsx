import React, { useState } from 'react';
import { FileSpreadsheet, Shield, ArrowUpDown, ChevronLeft, ChevronRight } from 'lucide-react';
import { useDisciplinaryCaseReport, useExportCaseReportCsv } from '../hooks/useDisciplinaryReports';
import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { ReportFilterBar } from '../components/ReportFilterBar';
import { CaseReportFilters } from '../types';

export const DisciplinaryCasesReportPage: React.FC = () => {
  const permissions = useDisciplinaryReportPermissions();
  const [filters, setFilters] = useState<CaseReportFilters>({
    companyId: '00000000-0000-0000-0000-000000000001',
    pageNumber: 1,
    pageSize: 10,
    sortBy: 'OpenedAt',
    sortDirection: 'desc',
  });

  const { data: pagedData, isLoading, isError, refetch } = useDisciplinaryCaseReport(
    filters,
    permissions.canViewCases
  );

  const exportMutation = useExportCaseReportCsv();

  const handleSort = (field: string) => {
    setFilters((prev) => {
      const isSameField = prev.sortBy === field;
      const nextDir = isSameField && prev.sortDirection === 'desc' ? 'asc' : 'desc';
      return { ...prev, sortBy: field, sortDirection: nextDir, pageNumber: 1 };
    });
  };

  if (!permissions.canViewCases) {
    return (
      <div className="p-8 text-center bg-white rounded-xl border border-slate-200">
        <Shield className="w-12 h-12 text-rose-500 mx-auto mb-3" />
        <h2 className="text-lg font-bold text-slate-800">Acesso Restrito</h2>
        <p className="text-sm text-slate-500 mt-1">
          Você não possui permissão para consultar o relatório de processos (`Disciplinary.Reports.ViewCases`).
        </p>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <FileSpreadsheet className="w-7 h-7 text-indigo-600" />
            Consulta de Processos Disciplinares
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Relatório analítico consolidado dos processos disciplinares por empresa, status e setor.
          </p>
        </div>
      </div>

      {/* Filter Bar */}
      <ReportFilterBar
        searchTerm={filters.searchTerm}
        onSearchTermChange={(val) => setFilters((f) => ({ ...f, searchTerm: val, pageNumber: 1 }))}
        dateFrom={filters.dateFrom}
        onDateFromChange={(val) => setFilters((f) => ({ ...f, dateFrom: val, pageNumber: 1 }))}
        dateTo={filters.dateTo}
        onDateToChange={(val) => setFilters((f) => ({ ...f, dateTo: val, pageNumber: 1 }))}
        status={filters.status}
        onStatusChange={(val) => setFilters((f) => ({ ...f, status: val as any, pageNumber: 1 }))}
        priority={filters.priority}
        onPriorityChange={(val) => setFilters((f) => ({ ...f, priority: val as any, pageNumber: 1 }))}
        onReset={() =>
          setFilters({
            companyId: '00000000-0000-0000-0000-000000000001',
            pageNumber: 1,
            pageSize: 10,
            sortBy: 'OpenedAt',
            sortDirection: 'desc',
          })
        }
        showExportButton={permissions.canExport}
        isExporting={exportMutation.isPending}
        onExportCsv={() => exportMutation.mutate(filters)}
      />

      {/* Data Table Container */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="p-12 text-center space-y-3">
            <div className="animate-spin w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full mx-auto" />
            <p className="text-xs text-slate-500">Carregando processos...</p>
          </div>
        ) : isError ? (
          <div className="p-8 text-center text-rose-600 text-xs font-medium">
            Erro ao carregar o relatório. Tente novamente.
          </div>
        ) : pagedData && pagedData.items.length === 0 ? (
          <div className="p-12 text-center text-slate-400 text-xs">
            Nenhum processo localizado para os filtros informados.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-500 uppercase font-semibold text-[10px] tracking-wider">
                  <tr>
                    <th
                      onClick={() => handleSort('CaseNumber')}
                      className="px-4 py-3 cursor-pointer hover:bg-slate-100"
                    >
                      <div className="flex items-center gap-1">
                        Processo
                        <ArrowUpDown className="w-3 h-3 text-slate-400" />
                      </div>
                    </th>
                    <th
                      onClick={() => handleSort('Title')}
                      className="px-4 py-3 cursor-pointer hover:bg-slate-100"
                    >
                      <div className="flex items-center gap-1">
                        Título / Motivo
                        <ArrowUpDown className="w-3 h-3 text-slate-400" />
                      </div>
                    </th>
                    <th className="px-4 py-3">Setor</th>
                    <th className="px-4 py-3">Acusado Principal</th>
                    <th
                      onClick={() => handleSort('Status')}
                      className="px-4 py-3 cursor-pointer hover:bg-slate-100"
                    >
                      <div className="flex items-center gap-1">
                        Status
                        <ArrowUpDown className="w-3 h-3 text-slate-400" />
                      </div>
                    </th>
                    <th
                      onClick={() => handleSort('OpenedAt')}
                      className="px-4 py-3 cursor-pointer hover:bg-slate-100"
                    >
                      <div className="flex items-center gap-1">
                        Abertura
                        <ArrowUpDown className="w-3 h-3 text-slate-400" />
                      </div>
                    </th>
                    <th className="px-4 py-3 text-center">Resolução (Dias)</th>
                    <th className="px-4 py-3 text-center">Medidas</th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {pagedData?.items.map((row) => (
                    <tr key={row.caseId} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-4 py-3 font-mono font-bold text-indigo-600">
                        {row.caseNumber}
                      </td>
                      <td className="px-4 py-3 font-medium text-slate-900 max-w-[220px] truncate">
                        {row.title}
                      </td>
                      <td className="px-4 py-3 text-slate-600">{row.departmentName}</td>
                      <td className="px-4 py-3 text-slate-800 font-medium">{row.mainAccusedEmployeeName}</td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold bg-slate-100 text-slate-700 uppercase">
                          {row.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-slate-500 whitespace-nowrap">
                        {new Date(row.openedAt).toLocaleDateString('pt-BR')}
                      </td>
                      <td className="px-4 py-3 text-center font-mono font-semibold text-slate-700">
                        {row.resolutionTimeInDays != null ? `${row.resolutionTimeInDays} d` : '-'}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${row.measuresCount > 0 ? 'bg-rose-50 text-rose-600' : 'bg-slate-100 text-slate-500'}`}>
                          {row.measuresCount}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            {pagedData && pagedData.totalPages > 1 && (
              <div className="flex items-center justify-between px-4 py-3 bg-slate-50 border-t border-slate-200 text-xs">
                <span className="text-slate-500">
                  Página <strong className="text-slate-800">{pagedData.pageNumber}</strong> de{' '}
                  <strong className="text-slate-800">{pagedData.totalPages}</strong> ({pagedData.totalCount} registros)
                </span>

                <div className="flex items-center gap-1">
                  <button
                    disabled={!pagedData.hasPreviousPage}
                    onClick={() => setFilters((f) => ({ ...f, pageNumber: f.pageNumber! - 1 }))}
                    className="p-1.5 rounded bg-white border border-slate-200 text-slate-600 hover:bg-slate-100 disabled:opacity-40"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <button
                    disabled={!pagedData.hasNextPage}
                    onClick={() => setFilters((f) => ({ ...f, pageNumber: f.pageNumber! + 1 }))}
                    className="p-1.5 rounded bg-white border border-slate-200 text-slate-600 hover:bg-slate-100 disabled:opacity-40"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};
