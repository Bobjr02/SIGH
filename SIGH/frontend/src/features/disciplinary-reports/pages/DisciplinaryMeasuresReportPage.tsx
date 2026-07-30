import React, { useState } from 'react';
import { AlertOctagon, Shield, ChevronLeft, ChevronRight } from 'lucide-react';
import { useDisciplinaryMeasuresReport } from '../hooks/useDisciplinaryReports';
import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { ReportFilterBar } from '../components/ReportFilterBar';
import { MeasureReportFilters } from '../types';

export const DisciplinaryMeasuresReportPage: React.FC = () => {
  const permissions = useDisciplinaryReportPermissions();
  const [filters, setFilters] = useState<MeasureReportFilters>({
    companyId: '00000000-0000-0000-0000-000000000001',
    pageNumber: 1,
    pageSize: 10,
  });

  const { data: pagedData, isLoading, isError } = useDisciplinaryMeasuresReport(
    filters,
    permissions.canViewMeasures
  );

  if (!permissions.canViewMeasures) {
    return (
      <div className="p-8 text-center bg-white rounded-xl border border-slate-200">
        <Shield className="w-12 h-12 text-rose-500 mx-auto mb-3" />
        <h2 className="text-lg font-bold text-slate-800">Acesso Restrito</h2>
        <p className="text-sm text-slate-500 mt-1">
          Você não possui permissão para consultar o relatório de medidas (`Disciplinary.Reports.ViewMeasures`).
        </p>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <AlertOctagon className="w-7 h-7 text-rose-600" />
          Medidas Disciplinares Aplicadas
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Consulta detalhada de advertências verbais, escritas, suspensões e demissões por justa causa.
        </p>
      </div>

      {/* Filter Bar */}
      <ReportFilterBar
        searchTerm={filters.searchTerm}
        onSearchTermChange={(val) => setFilters((f) => ({ ...f, searchTerm: val, pageNumber: 1 }))}
        dateFrom={filters.appliedFrom}
        onDateFromChange={(val) => setFilters((f) => ({ ...f, appliedFrom: val, pageNumber: 1 }))}
        dateTo={filters.appliedTo}
        onDateToChange={(val) => setFilters((f) => ({ ...f, appliedTo: val, pageNumber: 1 }))}
        measureType={filters.measureType}
        onMeasureTypeChange={(val) => setFilters((f) => ({ ...f, measureType: val, pageNumber: 1 }))}
        onReset={() =>
          setFilters({
            companyId: '00000000-0000-0000-0000-000000000001',
            pageNumber: 1,
            pageSize: 10,
          })
        }
        showExportButton={false}
      />

      {/* Table Container */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="p-12 text-center space-y-3">
            <div className="animate-spin w-8 h-8 border-4 border-rose-600 border-t-transparent rounded-full mx-auto" />
            <p className="text-xs text-slate-500">Carregando medidas disciplinares...</p>
          </div>
        ) : isError ? (
          <div className="p-8 text-center text-rose-600 text-xs font-medium">
            Erro ao carregar o relatório de medidas. Tente novamente.
          </div>
        ) : pagedData && pagedData.items.length === 0 ? (
          <div className="p-12 text-center text-slate-400 text-xs">
            Nenhuma medida disciplinar registrada para os filtros informados.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-500 uppercase font-semibold text-[10px] tracking-wider">
                  <tr>
                    <th className="px-4 py-3">Medida</th>
                    <th className="px-4 py-3">Funcionário Sancionado</th>
                    <th className="px-4 py-3">Processo</th>
                    <th className="px-4 py-3">Data Aplicação</th>
                    <th className="px-4 py-3">Vigência</th>
                    <th className="px-4 py-3">Aplicado Por</th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {pagedData?.items.map((row) => (
                    <tr key={row.measureId} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-amber-50 text-amber-800 border border-amber-200 uppercase">
                          {row.measureType}
                        </span>
                        <p className="text-xs text-slate-500 mt-1 line-clamp-1">{row.description}</p>
                      </td>
                      <td className="px-4 py-3 font-semibold text-slate-900">{row.employeeName}</td>
                      <td className="px-4 py-3 font-mono font-bold text-indigo-600">{row.caseNumber}</td>
                      <td className="px-4 py-3 text-slate-500 whitespace-nowrap">
                        {new Date(row.appliedAt).toLocaleDateString('pt-BR')}
                      </td>
                      <td className="px-4 py-3 text-slate-600 whitespace-nowrap">
                        {row.effectiveFrom
                          ? `${new Date(row.effectiveFrom).toLocaleDateString('pt-BR')} até ${
                              row.effectiveTo
                                ? new Date(row.effectiveTo).toLocaleDateString('pt-BR')
                                : 'Indeterminado'
                            }`
                          : '-'}
                      </td>
                      <td className="px-4 py-3 text-slate-600">{row.appliedByEmployeeName}</td>
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
                  <strong className="text-slate-800">{pagedData.totalPages}</strong> ({pagedData.totalCount} medidas)
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
