import React, { useState } from 'react';
import {
  FileText,
  Clock,
  CheckCircle,
  AlertTriangle,
  Users,
  Shield,
  BarChart3,
  RefreshCw,
} from 'lucide-react';
import { useDisciplinaryDashboard } from '../hooks/useDisciplinaryReports';
import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { DashboardKpiCard } from '../components/DashboardKpiCard';
import { DistributionChartCard } from '../components/DashboardCharts';
import { ReportFilterBar } from '../components/ReportFilterBar';
import { DashboardFilters } from '../types';

export const DisciplinaryDashboardPage: React.FC = () => {
  const permissions = useDisciplinaryReportPermissions();
  const [filters, setFilters] = useState<DashboardFilters>({
    companyId: '00000000-0000-0000-0000-000000000001',
  });

  const { data: dashboard, isLoading, isError, refetch } = useDisciplinaryDashboard(
    filters,
    permissions.canViewDashboard
  );

  if (!permissions.canViewDashboard) {
    return (
      <div className="p-8 text-center bg-white rounded-xl border border-slate-200">
        <Shield className="w-12 h-12 text-rose-500 mx-auto mb-3" />
        <h2 className="text-lg font-bold text-slate-800">Acesso Restrito</h2>
        <p className="text-sm text-slate-500 mt-1">
          Você não possui permissão para visualizar o dashboard disciplinar (`Disciplinary.Reports.ViewDashboard`).
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
            <BarChart3 className="w-7 h-7 text-indigo-600" />
            Dashboard Disciplinar
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Visão gerencial consolidada e indicadores operacionais de processos e medidas disciplinares.
          </p>
        </div>

        <button
          onClick={() => refetch()}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-700 bg-white border border-slate-200 hover:bg-slate-50 rounded-lg shadow-sm transition-colors"
        >
          <RefreshCw className="w-3.5 h-3.5 text-slate-500" />
          Atualizar Dados
        </button>
      </div>

      {/* Filter Bar */}
      <ReportFilterBar
        dateFrom={filters.dateFrom}
        onDateFromChange={(val) => setFilters((f) => ({ ...f, dateFrom: val }))}
        dateTo={filters.dateTo}
        onDateToChange={(val) => setFilters((f) => ({ ...f, dateTo: val }))}
        status={filters.status}
        onStatusChange={(val) => setFilters((f) => ({ ...f, status: val as any }))}
        priority={filters.priority}
        onPriorityChange={(val) => setFilters((f) => ({ ...f, priority: val as any }))}
        onReset={() =>
          setFilters({ companyId: '00000000-0000-0000-0000-000000000001' })
        }
        showExportButton={false}
      />

      {/* Loading state */}
      {isLoading && (
        <div className="p-12 text-center bg-white rounded-xl border border-slate-200 space-y-3">
          <div className="animate-spin w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full mx-auto" />
          <p className="text-xs font-medium text-slate-500">Calculando indicadores disciplinares...</p>
        </div>
      )}

      {/* Error state */}
      {isError && (
        <div className="p-6 text-center bg-rose-50 border border-rose-200 rounded-xl">
          <AlertTriangle className="w-8 h-8 text-rose-600 mx-auto mb-2" />
          <h3 className="text-sm font-bold text-rose-800">Falha ao carregar dashboard</h3>
          <p className="text-xs text-rose-600 mt-1">
            Não foi possível recuperar os dados analíticos do servidor. Tente novamente.
          </p>
          <button
            onClick={() => refetch()}
            className="mt-3 px-3 py-1.5 bg-rose-600 text-white text-xs font-medium rounded-lg hover:bg-rose-700 transition-colors"
          >
            Tentar Novamente
          </button>
        </div>
      )}

      {/* Dashboard Data */}
      {dashboard && !isLoading && (
        <>
          {/* KPI Cards Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <DashboardKpiCard
              title="Total de Processos"
              value={dashboard.totalCases}
              subtitle={`${dashboard.openCases} abertos / em andamento`}
              icon={FileText}
              colorClassName="bg-indigo-50 text-indigo-600"
            />
            <DashboardKpiCard
              title="Tempo Médio de Resolução"
              value={`${dashboard.averageResolutionTimeInDays} dias`}
              subtitle={`${dashboard.concludedCases} processos concluídos`}
              icon={Clock}
              colorClassName="bg-amber-50 text-amber-600"
            />
            <DashboardKpiCard
              title="Medidas Aplicadas"
              value={dashboard.measuresApplied}
              subtitle="Advertências e suspensões"
              icon={AlertTriangle}
              colorClassName="bg-rose-50 text-rose-600"
            />
            <DashboardKpiCard
              title="Funcionários Notificados"
              value={dashboard.employeesWithCases}
              subtitle="Com registro no período"
              icon={Users}
              colorClassName="bg-emerald-50 text-emerald-600"
            />
          </div>

          {/* Distribution Charts Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <DistributionChartCard
              title="Distribuição por Status"
              items={dashboard.casesByStatus}
              colorPalette={['bg-slate-400', 'bg-sky-500', 'bg-indigo-500', 'bg-amber-500', 'bg-purple-500', 'bg-emerald-500', 'bg-rose-400']}
            />

            <DistributionChartCard
              title="Distribuição por Prioridade"
              items={dashboard.casesByPriority}
              colorPalette={['bg-sky-500', 'bg-indigo-500', 'bg-amber-500', 'bg-rose-600']}
            />

            <DistributionChartCard
              title="Abertura por Mês"
              items={dashboard.casesByMonth}
              colorPalette={['bg-indigo-600', 'bg-indigo-500', 'bg-indigo-400', 'bg-indigo-300']}
            />

            <DistributionChartCard
              title="Tipos de Infração Frequentes"
              items={dashboard.casesByInfractionType}
              colorPalette={['bg-purple-500', 'bg-pink-500', 'bg-rose-500', 'bg-amber-500']}
            />

            <DistributionChartCard
              title="Medidas Aplicadas por Tipo"
              items={dashboard.measuresByType}
              colorPalette={['bg-amber-500', 'bg-orange-500', 'bg-rose-500', 'bg-purple-600']}
            />

            <DistributionChartCard
              title="Processos por Setor / Departamento"
              items={dashboard.casesByDepartment}
              colorPalette={['bg-teal-500', 'bg-sky-500', 'bg-indigo-500', 'bg-emerald-500']}
            />
          </div>
        </>
      )}
    </div>
  );
};
