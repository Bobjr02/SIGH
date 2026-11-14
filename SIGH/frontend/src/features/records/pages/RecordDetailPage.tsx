import React, { useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { CaseAuditTimeline } from '../../audit/components/CaseAuditTimeline';
import { formatBusinessDate, historySealOf, recordErrorMessage, recordKeys, recordsApi, RecordOrigin } from '../api/recordsApi';
import { AttachmentsPanel } from '../components/AttachmentsPanel';
import { RecordStatusChip } from '../components/RecordStatusChip';

const Field: React.FC<{ label: string; value?: React.ReactNode }> = ({ label, value }) => (
  <Box>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" sx={{ fontWeight: 600 }}>
      {value ?? '—'}
    </Typography>
  </Box>
);

const formatInstant = (value?: string | null) => (value ? new Date(value).toLocaleString('pt-BR') : '—');

type ReasonAction = 'reject' | 'cancel';

/** Detalhe da medida: dados, anexos, linha do tempo (com Audit.View) e só os botões que o usuário pode usar. */
export const RecordDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
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

  if (detail.isLoading) return <CircularProgress aria-label="Carregando" />;
  if (detail.isError || !detail.data) {
    return (
      <Alert severity="error" data-testid="record-not-found">
        {recordErrorMessage(detail.error, 'Medida não encontrada.')}
      </Alert>
    );
  }

  const record = detail.data;
  const s = record.summary;
  const a = record.actions;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
          {s.measureTypeLabel} — {s.caseNumber}
        </Typography>
        <RecordStatusChip status={s.status} />
      </Box>

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
      {s.origin === RecordOrigin.Historical && (
        <Alert severity="info" data-testid="history-seal">
          {historySealOf(s.importedAt)}. Medida aplicada antes do SIGH, sem decisão nem aprovação registradas aqui; somente leitura
          (anexos, como a carta assinada antiga, ainda podem ser incluídos).
        </Alert>
      )}
      {record.lastRejectionReason && s.status !== 3 && (
        <Alert severity="warning" data-testid="rejection-reason">
          Rejeitada por {record.lastRejectedByName ?? 'aprovador'} em {formatInstant(record.lastRejectedAt)}: {record.lastRejectionReason}
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        {a.canApprove && (
          <Button variant="contained" color="success" disabled={approve.isPending} onClick={() => approve.mutate()}>
            Aprovar
          </Button>
        )}
        {a.canReject && (
          <Button variant="outlined" color="error" onClick={() => setReasonFor('reject')}>
            Rejeitar
          </Button>
        )}
        {a.canCorrect && (
          <Button variant="contained" onClick={() => navigate(`/medidas/${id}/corrigir`)}>
            Corrigir e reenviar
          </Button>
        )}
        {a.canCancel && (
          <Button variant="outlined" color="inherit" onClick={() => setReasonFor('cancel')}>
            Cancelar medida
          </Button>
        )}
      </Box>

      <Paper elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' }, gap: 2 }}>
        <Field label="Funcionário" value={`${s.employeeNumber} – ${s.employeeName}`} />
        <Field label="Gerência (hoje)" value={s.managementUnitCode ? `${s.managementUnitCode} – ${s.managementUnitName}` : null} />
        <Field label="Gerência na época" value={record.managementUnitCodeAtTime} />
        <Field label="Setor" value={s.departmentName} />
        <Field label="Motivo" value={s.infractionTypeName} />
        <Field label="Data da medida" value={formatBusinessDate(s.measureDate)} />
        <Field label="Ocorrência(s)" value={record.occurrenceDates.map(formatBusinessDate).join(', ')} />
        {s.suspensionDaysUnknown && <Field label="Suspensão" value="Dias: não informado" />}
        {record.suspensionStart && (
          <Field
            label="Suspensão"
            value={`${formatBusinessDate(record.suspensionStart)} a ${formatBusinessDate(record.suspensionEnd)} (${s.suspensionDays} dia${(s.suspensionDays ?? 0) > 1 ? 's' : ''})`}
          />
        )}
        <Field
          label={s.origin === RecordOrigin.Historical ? 'Importada por' : 'Registrada por'}
          value={`${s.registeredByName ?? '—'} em ${formatInstant(s.registeredAt)}`}
        />
        {record.approvedAt && <Field label="Aprovada por" value={`${record.approvedByName ?? '—'} em ${formatInstant(record.approvedAt)}`} />}
        {record.cancelledAt && <Field label="Cancelada" value={`${formatInstant(record.cancelledAt)}: ${record.cancellationReason}`} />}
        <Box sx={{ gridColumn: '1 / -1' }}>
          <Field label="Descrição / observação" value={record.description} />
        </Box>
      </Paper>

      <AttachmentsPanel recordId={id} evidences={record.evidences} canAttach={a.canAttach} canRemove={a.canRemoveEvidence} />

      {hasPermission(PERMISSIONS.AUDIT_VIEW) && (
        <Paper component="section" aria-label="Linha do tempo" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider' }}>
          <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mb: 1 }}>
            Linha do tempo
          </Typography>
          <CaseAuditTimeline caseId={id} />
        </Paper>
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
    </Box>
  );
};

export default RecordDetailPage;
