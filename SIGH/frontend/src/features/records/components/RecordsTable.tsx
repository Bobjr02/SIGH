import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Eye, Gavel } from 'lucide-react';
import type { PagedResult } from '../../employees/api/employeesApi';
import { formatBusinessDate, historySealOf, RecordOrigin, RecordStatus, type RecordSummary } from '../api/recordsApi';
import { DataTable, type Column } from '../../../components/ui/DataTable';
import { InitialsAvatar } from '../../../components/ui/InitialsAvatar';
import { RecordStatusChip } from '../../../components/ui/StatusChip';
import { ActionIcon } from '../../../components/ui/ActionIcon';
import { label } from '../../../i18n/labels';
import { formatDays } from '../../../utils/formatters';

interface RecordsTableProps {
  data?: PagedResult<RecordSummary>;
  isLoading: boolean;
  error?: string | null;
  emptyMessage?: string;
  onPageChange?: (page: number) => void;
  /** Destaca as rejeitadas (página inicial do registrante). */
  highlightRejected?: boolean;
  ariaLabel?: string;
  /** Colunas extras dos relatórios: vigência, aplicado por e dias até a aprovação. */
  reportColumns?: boolean;
  /** Esconde a coluna Funcionário (aba de histórico do funcionário). */
  hideEmployee?: boolean;
}

/** "Aplicado por": quem aprovou; no histórico, "Histórico importado"; nas demais, "—". */
export const appliedByOf = (r: RecordSummary) =>
  r.origin === RecordOrigin.Historical ? 'Histórico importado' : r.status === RecordStatus.Approved ? (r.appliedByName ?? '—') : '—';

/** Vigência: só suspensões têm período; advertências e justa causa mostram "—". */
export const validityOf = (r: RecordSummary) =>
  r.suspensionStart && r.suspensionEnd ? `${formatBusinessDate(r.suspensionStart)} a ${formatBusinessDate(r.suspensionEnd)}` : '—';

const measureText = (r: RecordSummary) =>
  `${label('DisciplinaryMeasureType', r.measureType)}${r.suspensionDays ? ` (${formatDays(r.suspensionDays)})` : ''}${
    r.suspensionDaysUnknown ? ' (dias: não informado)' : ''
  }`;

export const RecordsTable: React.FC<RecordsTableProps> = ({
  data,
  isLoading,
  error,
  emptyMessage,
  onPageChange,
  highlightRejected,
  ariaLabel = 'Medidas',
  reportColumns,
  hideEmployee,
}) => {
  const columns: Column<RecordSummary>[] = [
    {
      key: 'number',
      header: 'Número',
      render: (r) => (
        <RouterLink to={`/medidas/${r.id}`} className="font-semibold text-primary-text hover:underline">
          {r.caseNumber}
        </RouterLink>
      ),
    },
    ...(hideEmployee
      ? []
      : [
          {
            key: 'employee',
            header: 'Funcionário',
            render: (r: RecordSummary) => (
              <span className="flex items-center gap-2.5">
                <InitialsAvatar name={r.employeeName} size={28} />
                <span className="min-w-0">
                  <span className="block font-medium">{r.employeeName}</span>
                  <span className="block text-xs text-muted">Matrícula {r.employeeNumber}</span>
                </span>
              </span>
            ),
          },
        ]),
    { key: 'unit', header: 'Gerência', render: (r) => r.managementUnitCode ?? '—', hideOnMobile: true },
    { key: 'measure', header: 'Medida', render: measureText },
    { key: 'reason', header: 'Motivo', render: (r) => r.infractionTypeName ?? '—', hideOnMobile: true },
    { key: 'date', header: 'Data da medida', render: (r) => formatBusinessDate(r.measureDate), className: 'whitespace-nowrap' },
    ...(reportColumns
      ? [
          { key: 'validity', header: 'Vigência', render: validityOf, className: 'whitespace-nowrap', hideOnMobile: true },
          { key: 'appliedBy', header: 'Aplicado por', render: (r: RecordSummary) => <span data-testid="applied-by">{appliedByOf(r)}</span>, hideOnMobile: true },
          {
            key: 'days',
            header: 'Dias até a aprovação',
            align: 'right' as const,
            render: (r: RecordSummary) => (r.daysToApproval === null || r.daysToApproval === undefined ? '—' : formatDays(r.daysToApproval)),
            hideOnMobile: true,
          },
        ]
      : []),
    {
      key: 'status',
      header: 'Situação',
      render: (r) => (
        <span className="flex flex-col items-start gap-1">
          <RecordStatusChip status={r.status} />
          {r.origin === RecordOrigin.Historical && (
            <span className="text-xs text-muted" data-testid="history-seal">
              {historySealOf(r.importedAt)}
            </span>
          )}
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Ações',
      align: 'right',
      render: (r) => <ActionIcon title="Ver medida" icon={<Eye size={17} />} to={`/medidas/${r.id}`} />,
    },
  ];

  return (
    <DataTable
      ariaLabel={ariaLabel}
      columns={columns}
      rows={data?.items}
      rowKey={(r) => r.id}
      isLoading={isLoading}
      error={error}
      emptyMessage={emptyMessage ?? 'Nenhuma medida encontrada.'}
      emptyIcon={<Gavel size={32} />}
      emptyTestId="records-empty"
      rowTestId="record-row"
      rowClassName={(r) => (highlightRejected && r.status === RecordStatus.Rejected ? 'bg-danger-soft outline outline-2 -outline-offset-2 outline-[var(--sigh-danger-solid)]' : undefined)}
      paging={
        data && onPageChange
          ? { pageNumber: data.pageNumber, pageSize: data.pageSize, totalCount: data.totalCount, onPageChange, noun: data.totalCount === 1 ? 'medida' : 'medidas' }
          : undefined
      }
    />
  );
};
