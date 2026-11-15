import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { MenuItem, TextField } from '@mui/material';
import { AlertTriangle, ArrowRight, Calendar, Check, Clock, FilePlus2, Gavel, LayoutDashboard, Users, X } from 'lucide-react';
import { PageHeader } from '../../components/ui/PageHeader';
import { KpiCard } from '../../components/ui/KpiCard';
import { LineChart } from '../../components/ui/LineChart';
import { SectionCard } from '../../components/ui/SectionCard';
import { EmptyState, ErrorState } from '../../components/ui/States';
import { label, options } from '../../i18n/labels';
import { formatDate, formatDateTime, formatDays } from '../../utils/formatters';
import { OverviewPeriod, recordErrorMessage, recordKeys, recordsApi, RecordActivityKind, type RecordActivity } from '../records/api/recordsApi';

const MONTHS = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];
const MONTHS_FULL = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];

const ACTIVITY_STYLE: Record<RecordActivityKind, { icon: React.ReactNode; className: string }> = {
  [RecordActivityKind.Registered]: { icon: <FilePlus2 size={17} />, className: 'bg-primary-soft text-primary-text' },
  [RecordActivityKind.Approved]: { icon: <Check size={17} />, className: 'bg-success-soft text-success' },
  [RecordActivityKind.Rejected]: { icon: <X size={17} />, className: 'bg-danger-soft text-danger' },
};

const Activity: React.FC<{ item: RecordActivity }> = ({ item }) => (
  <li className="flex items-start gap-3" data-testid="activity-item">
    <span className={`mt-0.5 inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${ACTIVITY_STYLE[item.kind].className}`} aria-hidden="true">
      {ACTIVITY_STYLE[item.kind].icon}
    </span>
    <div className="min-w-0">
      <p className="text-sm font-semibold text-ink">
        {label('RecordActivityKind', item.kind)}{' '}
        <RouterLink to={`/medidas/${item.recordId}`} className="font-medium text-primary-text hover:underline">
          {item.caseNumber}
        </RouterLink>
      </p>
      <p className="truncate text-xs text-muted">
        {label('DisciplinaryMeasureType', item.measureType)} · {item.employeeName}
      </p>
      <p className="text-xs text-muted">
        {formatDateTime(item.at)}
        {item.actorName ? ` – ${item.actorName}` : ''}
      </p>
    </div>
  </li>
);

/**
 * Painel: indicadores do período (com a variação em relação ao período anterior), evolução mensal e
 * atividades recentes. Dados reais e no escopo do usuário (o gestor só vê as gerências dele).
 */
export const DashboardPage: React.FC = () => {
  const [period, setPeriod] = useState<OverviewPeriod>(OverviewPeriod.Last30Days);
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: recordKeys.overview(period),
    queryFn: () => recordsApi.overview(period),
    placeholderData: keepPreviousData,
  });

  const points = (data?.byMonth ?? []).map((m) => ({ label: MONTHS[m.month - 1], fullLabel: `${MONTHS_FULL[m.month - 1]} de ${m.year}`, value: m.count }));

  return (
    <div data-testid="dashboard">
      <PageHeader
        icon={<LayoutDashboard size={22} />}
        title="Painel"
        subtitle={data ? `Visão geral das medidas de ${formatDate(data.from)} a ${formatDate(data.to)}` : 'Visão geral das medidas disciplinares'}
        actions={
          <TextField
            select
            size="small"
            label="Período"
            value={period}
            onChange={(e) => setPeriod(Number(e.target.value) as OverviewPeriod)}
            sx={{ minWidth: 200 }}
            slotProps={{ input: { startAdornment: <Calendar size={16} style={{ marginRight: 8 }} aria-hidden="true" /> } }}
          >
            {options('OverviewPeriod').map((o) => (
              <MenuItem key={o.value} value={o.value}>
                {o.label}
              </MenuItem>
            ))}
          </TextField>
        }
      />

      {isError ? (
        <ErrorState message={recordErrorMessage(error, 'Erro ao carregar o painel.')} onRetry={() => refetch()} />
      ) : isLoading || !data ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-busy="true" aria-label="Carregando painel">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-40 animate-pulse rounded-lg bg-surface-2" />
          ))}
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <KpiCard testId="kpi-measures" icon={<Gavel size={20} />} tone="primary" value={data.measures.current} previous={data.measures.previous} label="Medidas no período" />
            <KpiCard testId="kpi-pending" icon={<Clock size={20} />} tone="warning" value={data.pendingApproval} label="Aguardando aprovação" hint="Situação atual" />
            <KpiCard
              testId="kpi-suspensions"
              icon={<AlertTriangle size={20} />}
              tone="danger"
              value={data.suspensions.current}
              previous={data.suspensions.previous}
              label="Suspensões no período"
              hint={`${formatDays(data.suspensionDays.current)} de suspensão no total`}
            />
            <KpiCard testId="kpi-employees" icon={<Users size={20} />} tone="success" value={data.employees.current} previous={data.employees.previous} label="Funcionários com medida no período" />
          </div>

          <div className="mt-4 grid grid-cols-1 gap-4 xl:grid-cols-3">
            <SectionCard
              title="Evolução de medidas"
              className="xl:col-span-2"
              actions={
                <span className="inline-flex items-center gap-2 text-xs text-muted">
                  <span className="h-2.5 w-2.5 rounded-full bg-[var(--sigh-chart-line)]" aria-hidden="true" /> Medidas por mês
                </span>
              }
            >
              <LineChart points={points} ariaLabel="Evolução de medidas por mês" seriesLabel="Medidas" />
            </SectionCard>
            <SectionCard
              title="Atividades recentes"
              actions={
                <RouterLink to="/medidas" className="inline-flex items-center gap-1 text-xs font-semibold text-primary-text hover:underline">
                  Ver todas <ArrowRight size={13} aria-hidden="true" />
                </RouterLink>
              }
            >
              {data.recentActivities.length === 0 ? (
                <EmptyState message="Nenhuma atividade recente." />
              ) : (
                <ul className="flex flex-col gap-4">
                  {data.recentActivities.map((a) => (
                    <Activity key={`${a.kind}-${a.recordId}-${a.at}`} item={a} />
                  ))}
                </ul>
              )}
            </SectionCard>
          </div>
        </>
      )}
    </div>
  );
};

export default DashboardPage;
