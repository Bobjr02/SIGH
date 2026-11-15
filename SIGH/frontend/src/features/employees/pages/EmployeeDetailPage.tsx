import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { Scale } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { DetailNav } from '../../../components/ui/PageHeader';
import { DetailHeader, DetailTabs, TabPanel } from '../../../components/ui/DetailHeader';
import { Field, SectionCard } from '../../../components/ui/SectionCard';
import { ErrorState } from '../../../components/ui/States';
import { EmployeeStatusChip } from '../../../components/ui/StatusChip';
import { label } from '../../../i18n/labels';
import { formatDate, formatDays, formatNumber } from '../../../utils/formatters';
import { recordErrorMessage, recordKeys, recordsApi, type RecordFilters } from '../../records/api/recordsApi';
import { RecordsTable } from '../../records/components/RecordsTable';
import { employeeKeys, employeesApi, formatCpf, type DecisionSupport } from '../api/employeesApi';

const Stat: React.FC<{ label: string; value: React.ReactNode; hint?: string }> = ({ label: text, value, hint }) => (
  <div className="rounded-lg border border-line bg-surface-2 p-3">
    <p className="text-xs text-muted">{text}</p>
    <p className="text-xl font-bold text-ink">{value}</p>
    {hint && <p className="text-xs text-muted">{hint}</p>}
  </div>
);

const CountList: React.FC<{ title: string; items: { label: string; count: number }[] }> = ({ title, items }) => (
  <div>
    <h3 className="mb-1 text-sm font-semibold text-ink">{title}</h3>
    <ul aria-label={title} className="list-disc pl-5 text-sm text-ink">
      {items.map((t) => (
        <li key={t.label}>
          {t.label}: <strong>{formatNumber(t.count)}</strong>
        </li>
      ))}
    </ul>
  </div>
);

/** Quadro de apoio à decisão: só informativo, nunca sugere medida. */
export const DecisionSupportPanel: React.FC<{ data: DecisionSupport }> = ({ data }) => (
  <SectionCard
    ariaLabel="Apoio à decisão"
    title={
      <span className="inline-flex items-center gap-2">
        <Scale size={18} aria-hidden="true" /> Apoio à decisão
        <span className="rounded border border-line-strong px-1.5 py-0.5 text-xs font-semibold text-muted">Informativo</span>
      </span>
    }
  >
    {data.totalMeasures === 0 ? (
      <p className="text-sm text-muted">Nenhuma medida disciplinar aplicada.</p>
    ) : (
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
          <Stat label="Medidas aplicadas" value={formatNumber(data.totalMeasures)} />
          <Stat label="Advertências" value={formatNumber(data.warnings)} hint={`${data.verbalWarnings} verbal(is), ${data.writtenWarnings} escrita(s)`} />
          <Stat
            label="Suspensões"
            value={formatNumber(data.suspensions)}
            hint={`${data.suspensionDays} dia(s) no total${
              data.suspensionsWithoutPeriod
                ? ` +${data.suspensionsWithoutPeriod} suspensão${data.suspensionsWithoutPeriod > 1 ? 'ões' : ''} sem dias informados`
                : ''
            }`}
          />
          <Stat label="Última medida" value={formatDate(data.lastMeasureAt)} />
          <Stat label="Últimos 6 meses" value={formatNumber(data.measuresLast6Months)} />
          <Stat label="Últimos 12 meses" value={formatNumber(data.measuresLast12Months)} />
        </div>
        {(data.historicalMeasures ?? 0) > 0 && data.byOrigin && (
          <div>
            <h3 className="mb-1 text-sm font-semibold text-ink">Por origem</h3>
            <ul aria-label="Medidas por origem" className="list-disc pl-5 text-sm text-ink">
              {data.byOrigin.map((o) => (
                <li key={o.label} data-testid="origin-breakdown">
                  <strong>
                    {o.total} {o.label.toLowerCase()}
                  </strong>{' '}
                  — {o.historical} histórica{o.historical === 1 ? '' : 's'}, {o.sigh} no SIGH
                </li>
              ))}
            </ul>
          </div>
        )}
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <CountList title="Medidas por tipo" items={data.byType} />
          <CountList title="Medidas por motivo" items={data.byInfractionType} />
        </div>
      </div>
    )}
  </SectionCard>
);

const History: React.FC<{ employeeId: string }> = ({ employeeId }) => {
  const [filters, setFilters] = useState<RecordFilters>({ employeeId, pageNumber: 1, pageSize: 20 });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });
  return (
    <section aria-label="Histórico disciplinar">
      <RecordsTable
        ariaLabel="Histórico disciplinar"
        hideEmployee
        reportColumns
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar o histórico.') : null}
        emptyMessage="Nenhuma medida registrada para este funcionário."
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
      />
    </section>
  );
};

type TabKey = 'dados' | 'historico' | 'apoio';

/** Detalhe do funcionário: Dados | Histórico disciplinar | Apoio à decisão. O CPF completo continua visível aqui. */
export const EmployeeDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canSeeRecords = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_VIEW);
  const canSeeSupport = hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY);
  const [tab, setTab] = useState<TabKey>('dados');

  const employee = useQuery({ queryKey: employeeKeys.detail(id), queryFn: () => employeesApi.getById(id) });
  const support = useQuery({ queryKey: employeeKeys.decisionSupport(id), queryFn: () => employeesApi.decisionSupport(id), enabled: canSeeSupport });

  if (employee.isLoading) {
    return (
      <div className="flex flex-col gap-4" aria-busy="true" aria-label="Carregando funcionário">
        <div className="h-16 w-1/2 animate-pulse rounded-lg bg-surface-2" />
        <div className="h-64 animate-pulse rounded-lg bg-surface-2" />
      </div>
    );
  }
  if (employee.isError || !employee.data) {
    return (
      <div>
        <DetailNav crumbs={[{ label: 'Funcionários', to: '/employees' }, { label: 'Detalhes' }]} backTo="/employees" />
        <ErrorState message={(employee.error as Error | null)?.message ?? 'Funcionário não encontrado.'} />
      </div>
    );
  }

  const e = employee.data;
  const tabs: { key: TabKey; label: string }[] = [
    { key: 'dados', label: 'Dados' },
    ...(canSeeRecords ? [{ key: 'historico' as const, label: 'Histórico disciplinar' }] : []),
    ...(canSeeSupport ? [{ key: 'apoio' as const, label: 'Apoio à decisão' }] : []),
  ];

  return (
    <div>
      <DetailNav crumbs={[{ label: 'Funcionários', to: '/employees' }, { label: e.fullName }]} backTo="/employees" />
      <DetailHeader
        name={e.fullName}
        chip={<EmployeeStatusChip status={e.status} />}
        subtitle={
          <>
            Matrícula {e.employeeNumber}
            {e.jobTitleName ? ` · ${e.jobTitleName}` : ''}
            {e.managementUnitCode ? ` · ${e.managementUnitCode}` : ''}
          </>
        }
      />
      <DetailTabs<TabKey> idPrefix="funcionario" value={tab} onChange={setTab} tabs={tabs} />

      <TabPanel active={tab === 'dados'} idPrefix="funcionario" tabKey="dados">
        <SectionCard title="Dados cadastrais" ariaLabel="Dados cadastrais">
          <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2 lg:grid-cols-4">
            <Field label="CPF" value={<span data-testid="employee-cpf">{formatCpf(e.cpf)}</span>} />
            <Field label="Admissão" value={formatDate(e.admissionDate)} />
            <Field label="Desligamento" value={formatDate(e.terminationDate)} />
            <Field label="Situação" value={label('EmployeeStatus', e.status)} />
            <Field label="Cargo" value={e.jobTitleCode ? `${e.jobTitleCode} – ${e.jobTitleName}` : e.jobTitleName} />
            <Field label="Setor" value={e.departmentCode ? `${e.departmentCode} – ${e.departmentName}` : e.departmentName} />
            <Field label="Gerência" value={e.managementUnitCode ? `${e.managementUnitCode} – ${e.managementUnitName}` : e.managementUnitName} />
            <Field label="Grupo / Turma" value={`${e.group ? label('EmployeeGroup', e.group) : '—'} / ${e.team ?? '—'}`} />
          </dl>
        </SectionCard>
        {canSeeSupport && support.data && support.data.totalMeasures > 0 && (
          <p className="text-sm text-muted">
            {formatNumber(support.data.totalMeasures)} medida(s) no histórico · {formatDays(support.data.suspensionDays)} de suspensão. Veja as abas
            “Histórico disciplinar” e “Apoio à decisão”.
          </p>
        )}
      </TabPanel>

      {canSeeRecords && (
        <TabPanel active={tab === 'historico'} idPrefix="funcionario" tabKey="historico">
          <History employeeId={id} />
        </TabPanel>
      )}

      {canSeeSupport && (
        <TabPanel active={tab === 'apoio'} idPrefix="funcionario" tabKey="apoio">
          {support.data && <DecisionSupportPanel data={support.data} />}
          {support.isError && <ErrorState message={(support.error as Error).message} />}
          {support.isLoading && <div className="h-40 animate-pulse rounded-lg bg-surface-2" />}
        </TabPanel>
      )}
    </div>
  );
};

export default EmployeeDetailPage;
