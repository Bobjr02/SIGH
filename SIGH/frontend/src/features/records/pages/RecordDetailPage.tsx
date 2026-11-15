import React, { useState } from 'react';
import { Link as RouterLink, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField } from '@mui/material';
import { Check, Pencil, X, XCircle } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { CaseAuditTimeline } from '../../audit/components/CaseAuditTimeline';
import { DetailNav } from '../../../components/ui/PageHeader';
import { DetailHeader, DetailTabs, TabPanel } from '../../../components/ui/DetailHeader';
import { Field, SectionCard } from '../../../components/ui/SectionCard';
import { ErrorState } from '../../../components/ui/States';
import { RecordStatusChip } from '../../../components/ui/StatusChip';
import { label } from '../../../i18n/labels';
import { formatDateTime, formatDays } from '../../../utils/formatters';
import { formatBusinessDate, historySealOf, MeasureType, recordErrorMessage, recordKeys, recordsApi, RecordOrigin, RecordStatus } from '../api/recordsApi';
import { AttachmentsPanel } from '../components/AttachmentsPanel';
import { appliedByOf } from '../components/RecordsTable';

type ReasonAction = 'reject' | 'cancel';
type TabKey = 'dados' | 'anexos' | 'historico';

/** Detalhe da medida: abas Dados | Anexos | Histórico (auditoria, com Audit.View); só os botões que o usuário pode usar. */
export const RecordDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const [tab, setTab] = useState<TabKey>('dados');
  const [reasonFor, setReasonFor] = useState<ReasonAction | null>(null);
  const [reason, setReason] = useState('');
  const [message, setMessage] = useState<string | null>((location.state as { saved?: string } | null)?.saved ?? null);
  const [error, setError] = useState<string | null>(null);

  const detail = useQuery({ queryKey: recordKeys.detail(id), queryFn: () => recordsApi.get(id), retry: false });

  const done = async (text: string) => {
    setMessage(text);
    setError(null);
    await queryClient.invalidateQueries({ queryKey: recordKeys.all });
  };

  const approve = useMutation({
    mutationFn: () => recordsApi.approve(id),
    onSuccess: () => done('Medida aprovada.'),
    onError: (e) => setError(recordErrorMessage(e, 'Não foi possível aprovar.')),
  });

  const withReason = useMutation({
    mutationFn: ({ action, text }: { action: ReasonAction; text: string }) =>
      action === 'reject' ? recordsApi.reject(id, text) : recordsApi.cancel(id, text),
    onSuccess: (_, { action }) => {
      setReasonFor(null);
      setReason('');
      return done(action === 'reject' ? 'Medida rejeitada e devolvida ao registrante.' : 'Medida cancelada.');
    },
    onError: (e) => setError(recordErrorMessage(e, 'Não foi possível concluir a ação.')),
  });

  if (detail.isLoading) {
    return (
      <div className="flex flex-col gap-4" aria-busy="true" aria-label="Carregando medida">
        <div className="h-16 w-1/2 animate-pulse rounded-lg bg-surface-2" />
        <div className="h-64 animate-pulse rounded-lg bg-surface-2" />
      </div>
    );
  }
  if (detail.isError || !detail.data) {
    return (
      <div data-testid="record-not-found">
        <DetailNav crumbs={[{ label: 'Medidas', to: '/medidas' }, { label: 'Detalhes' }]} backTo="/medidas" />
        <ErrorState message={recordErrorMessage(detail.error, 'Medida não encontrada.')} />
      </div>
    );
  }

  const record = detail.data;
  const s = record.summary;
  const a = record.actions;
  const historical = s.origin === RecordOrigin.Historical;
  const canSeeHistory = hasPermission(PERMISSIONS.AUDIT_VIEW);
  const tabs: { key: TabKey; label: string }[] = [
    { key: 'dados', label: 'Dados' },
    { key: 'anexos', label: `Anexos (${record.evidences.length})` },
    ...(canSeeHistory ? [{ key: 'historico' as const, label: 'Histórico' }] : []),
  ];

  return (
    <div>
      <DetailNav crumbs={[{ label: 'Medidas', to: '/medidas' }, { label: s.caseNumber }]} backTo="/medidas" />
      <DetailHeader
        name={`${label('DisciplinaryMeasureType', s.measureType)} — ${s.caseNumber}`}
        avatarName={s.employeeName}
        chip={<RecordStatusChip status={s.status} />}
        subtitle={
          <>
            <RouterLink to={`/employees/${s.employeeId}`} className="font-medium text-primary-text hover:underline">
              {s.employeeName}
            </RouterLink>{' '}
            · Matrícula {s.employeeNumber} · {s.managementUnitCode ?? '—'}
          </>
        }
        actions={
          <>
            {a.canApprove && (
              <Button variant="contained" color="success" startIcon={<Check size={16} />} disabled={approve.isPending} onClick={() => approve.mutate()}>
                Aprovar
              </Button>
            )}
            {a.canReject && (
              <Button variant="outlined" color="error" startIcon={<X size={16} />} onClick={() => setReasonFor('reject')}>
                Rejeitar
              </Button>
            )}
            {a.canCorrect && (
              <Button variant="contained" startIcon={<Pencil size={16} />} onClick={() => navigate(`/medidas/${id}/corrigir`)}>
                Corrigir e reenviar
              </Button>
            )}
            {a.canCancel && (
              <Button variant="outlined" startIcon={<XCircle size={16} />} onClick={() => setReasonFor('cancel')}>
                Cancelar medida
              </Button>
            )}
          </>
        }
      />

      <div className="mb-4 flex flex-col gap-2">
        {message && (
          <Alert severity="success" onClose={() => setMessage(null)}>
            {message}
          </Alert>
        )}
        {error && (
          <Alert severity="error" role="alert" onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        {historical && (
          <Alert severity="info" data-testid="history-seal">
            {historySealOf(s.importedAt)}. Medida aplicada antes do SIGH, sem decisão nem aprovação registradas aqui; somente leitura (anexos, como a
            carta assinada antiga, ainda podem ser incluídos).
          </Alert>
        )}
        {record.lastRejectionReason && s.status !== RecordStatus.Approved && (
          <Alert severity="warning" data-testid="rejection-reason">
            Rejeitada por {record.lastRejectedByName ?? 'aprovador'} em {formatDateTime(record.lastRejectedAt)}: {record.lastRejectionReason}
          </Alert>
        )}
      </div>

      <DetailTabs<TabKey> idPrefix="medida" value={tab} onChange={setTab} tabs={tabs} />

      <TabPanel active={tab === 'dados'} idPrefix="medida" tabKey="dados">
        <SectionCard title="Dados da medida">
          <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Funcionário" value={`${s.employeeNumber} – ${s.employeeName}`} />
            <Field label="Gerência (hoje)" value={s.managementUnitCode ? `${s.managementUnitCode} – ${s.managementUnitName}` : null} />
            <Field label="Gerência na época" value={record.managementUnitCodeAtTime} />
            <Field label="Setor" value={s.departmentName} />
            <Field label="Medida" value={label('DisciplinaryMeasureType', s.measureType)} />
            <Field label="Motivo" value={s.infractionTypeName} />
            <Field label="Data da medida" value={formatBusinessDate(s.measureDate)} />
            <Field label="Ocorrência(s)" value={record.occurrenceDates.map(formatBusinessDate).join(', ')} />
            <Field
              label="Vigência"
              value={
                s.measureType === MeasureType.Suspension && record.suspensionStart
                  ? `${formatBusinessDate(record.suspensionStart)} a ${formatBusinessDate(record.suspensionEnd)} (${formatDays(s.suspensionDays)})`
                  : s.suspensionDaysUnknown
                    ? 'Dias: não informado'
                    : '—'
              }
            />
            <Field label={historical ? 'Importada por' : 'Registrada por'} value={`${s.registeredByName ?? '—'} em ${formatDateTime(s.registeredAt)}`} />
            <Field label="Aplicado por" value={<span data-testid="applied-by">{appliedByOf(s)}</span>} />
            {record.approvedAt && <Field label="Aprovada em" value={formatDateTime(record.approvedAt)} />}
            {record.cancelledAt && <Field label="Cancelada" value={`${formatDateTime(record.cancelledAt)}: ${record.cancellationReason ?? '—'}`} />}
            <Field label="Descrição / observação" value={record.description} className="sm:col-span-2 lg:col-span-3" />
          </dl>
        </SectionCard>
      </TabPanel>

      <TabPanel active={tab === 'anexos'} idPrefix="medida" tabKey="anexos">
        <AttachmentsPanel recordId={id} evidences={record.evidences} canAttach={a.canAttach} canRemove={a.canRemoveEvidence} />
      </TabPanel>

      {canSeeHistory && (
        <TabPanel active={tab === 'historico'} idPrefix="medida" tabKey="historico">
          <CaseAuditTimeline caseId={id} />
        </TabPanel>
      )}

      <Dialog open={reasonFor !== null} onClose={() => setReasonFor(null)} aria-labelledby="reason-title" fullWidth maxWidth="sm">
        <DialogTitle id="reason-title">{reasonFor === 'reject' ? 'Rejeitar medida' : 'Cancelar medida'}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={3}
            sx={{ mt: 1 }}
            label={reasonFor === 'reject' ? 'Motivo da rejeição' : 'Motivo do cancelamento'}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReasonFor(null)}>Voltar</Button>
          <Button
            variant="contained"
            color={reasonFor === 'reject' ? 'error' : 'primary'}
            disabled={!reason.trim() || withReason.isPending}
            onClick={() => reasonFor && withReason.mutate({ action: reasonFor, text: reason.trim() })}
          >
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>
    </div>
  );
};

export default RecordDetailPage;
