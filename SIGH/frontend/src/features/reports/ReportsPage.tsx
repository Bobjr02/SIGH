import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Alert, Button } from '@mui/material';
import { BarChart3, CalendarClock, Download, Gavel, Timer, Users } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { PERMISSIONS } from '../../auth/permissions';
import { PageHeader } from '../../components/ui/PageHeader';
import { KpiCard } from '../../components/ui/KpiCard';
import { DistributionBars } from '../../components/ui/LineChart';
import { SectionCard } from '../../components/ui/SectionCard';
import { formatNumber } from '../../utils/formatters';
import { recordErrorMessage, recordKeys, recordsApi, type RecordFilters as Filters } from '../records/api/recordsApi';
import { RecordFilters } from '../records/components/RecordFilters';
import { RecordsTable } from '../records/components/RecordsTable';

const STATUS_COLORS: Record<number, string> = {
  1: 'var(--sigh-warning-solid)',
  2: 'var(--sigh-danger-solid)',
  3: 'var(--sigh-success-solid)',
  4: 'var(--sigh-neutral-solid)',
  5: 'var(--sigh-info-solid)',
};

/**
 * Relatórios das medidas (substitui "Consulta de Processos Disciplinares" e "Medidas Disciplinares
 * Aplicadas", que refletiam o processo em etapas): filtros, distribuição por situação e por tipo, tabela
 * com "Aplicado por" e "Dias até a aprovação", e exportação CSV. Tudo no escopo do usuário.
 */
export const ReportsPage: React.FC = () => {
  const { hasPermission } = useAuth();
  const [filters, setFilters] = useState<Filters>({ pageNumber: 1, pageSize: 20 });
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const options = useQuery({ queryKey: recordKeys.options, queryFn: recordsApi.options });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });
  const { pageNumber: _page, ...statsFilters } = filters;
  const stats = useQuery({ queryKey: recordKeys.stats(statsFilters), queryFn: () => recordsApi.stats(statsFilters), placeholderData: keepPreviousData });

  const exportCsv = async () => {
    setExporting(true);
    setExportError(null);
    try {
      await recordsApi.exportCsv(filters);
    } catch (e) {
      setExportError(recordErrorMessage(e, 'Não foi possível exportar.'));
    } finally {
      setExporting(false);
    }
  };

  const s = stats.data;

  return (
    <div data-testid="reports-page">
      <PageHeader
        icon={<BarChart3 size={22} />}
        title="Relatórios"
        subtitle="Medidas disciplinares por período, tipo, motivo, gerência, setor, situação e origem"
        actions={
          hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_EXPORT) && (
            <Button variant="contained" startIcon={<Download size={16} />} onClick={exportCsv} disabled={exporting} data-testid="export-csv">
              {exporting ? 'Exportando…' : 'Exportar CSV'}
            </Button>
          )
        }
      />
      {exportError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setExportError(null)}>
          {exportError}
        </Alert>
      )}

      <RecordFilters value={filters} options={options.data} onChange={setFilters} />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <KpiCard testId="report-total" icon={<Gavel size={20} />} value={s?.total ?? 0} label="Medidas encontradas" />
        <KpiCard testId="report-employees" icon={<Users size={20} />} tone="success" value={s?.employees ?? 0} label="Funcionários" />
        <KpiCard testId="report-suspension-days" icon={<CalendarClock size={20} />} tone="danger" value={s?.suspensionDays ?? 0} label="Dias de suspensão" />
        <KpiCard
          testId="report-days-to-approval"
          icon={<Timer size={20} />}
          tone="warning"
          value={s?.averageDaysToApproval ?? 0}
          label="Dias até a aprovação (média)"
          hint={s?.averageDaysToApproval === null || s?.averageDaysToApproval === undefined ? 'Nenhuma medida aprovada nos filtros' : undefined}
        />
      </div>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <SectionCard title="Distribuição por situação" testId="by-status">
          <DistributionBars
            ariaLabel="Distribuição por situação"
            items={(s?.byStatus ?? []).map((i) => ({ key: i.value, label: i.label, count: i.count, percentage: i.percentage, color: STATUS_COLORS[i.value] }))}
          />
        </SectionCard>
        <SectionCard title="Distribuição por tipo de medida" testId="by-type">
          <DistributionBars ariaLabel="Distribuição por tipo de medida" items={(s?.byType ?? []).map((i) => ({ key: i.value, label: i.label, count: i.count, percentage: i.percentage }))} />
        </SectionCard>
      </div>

      <div className="mt-6">
        <h2 className="mb-3 text-lg font-semibold text-ink">
          Medidas {s ? <span className="text-sm font-normal text-muted">({formatNumber(s.total)})</span> : null}
        </h2>
        <RecordsTable
          ariaLabel="Medidas do relatório"
          reportColumns
          data={list.data}
          isLoading={list.isLoading}
          error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar o relatório.') : null}
          emptyMessage="Nenhuma medida para os filtros escolhidos."
          onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
        />
      </div>
    </div>
  );
};

export default ReportsPage;
