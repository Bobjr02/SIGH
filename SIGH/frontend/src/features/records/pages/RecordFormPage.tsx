import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  MenuItem,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import { Plus, Trash2 } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { employeeKeys, employeesApi, EmployeeStatus, type EmployeeListItem } from '../../employees/api/employeesApi';
import { DecisionSupportPanel } from '../../employees/pages/EmployeeDetailPage';
import { EmployeePicker } from '../components/EmployeePicker';
import {
  formatBusinessDate,
  MeasureType,
  PossibleDuplicateError,
  recordErrorMessage,
  recordKeys,
  recordsApi,
  suspensionDays,
  todayIso,
  type PossibleDuplicate,
  type RecordInput,
} from '../api/recordsApi';

interface FormState {
  employee: EmployeeListItem | null;
  infractionTypeId: string;
  measureType: MeasureType | '';
  occurrenceDates: string[];
  measureDate: string;
  suspensionStart: string;
  suspensionEnd: string;
  description: string;
}

const EMPTY: FormState = {
  employee: null,
  infractionTypeId: '',
  measureType: '',
  occurrenceDates: [''],
  measureDate: '',
  suspensionStart: '',
  suspensionEnd: '',
  description: '',
};


/** Validação no cliente (a mesma regra é aplicada no servidor). */
export const validateRecordForm = (form: FormState, today: string): string | null => {
  const dates = form.occurrenceDates.filter(Boolean);
  if (!form.employee) return 'Escolha o funcionário.';
  if (!form.infractionTypeId) return 'Escolha o motivo.';
  if (form.measureType === '') return 'Escolha o tipo de medida.';
  if (dates.length === 0) return 'Informe pelo menos uma data de ocorrência.';
  if (dates.some((d) => d > today)) return 'Nenhuma data de ocorrência pode estar no futuro.';
  if (!form.measureDate) return 'Informe a data da medida.';
  if (form.measureType === MeasureType.Suspension) {
    if (!form.suspensionStart || !form.suspensionEnd) return 'A suspensão exige data de início e de fim.';
    if (form.suspensionEnd < form.suspensionStart) return 'O fim da suspensão não pode ser anterior ao início.';
  }
  if (!form.description.trim()) return 'A descrição/observação é obrigatória.';
  return null;
};

/**
 * Registrar medida (POST) ou corrigir uma rejeitada (PUT, rota /medidas/:id/corrigir).
 * Ao escolher o funcionário, mostra o quadro de apoio à decisão (medidas anteriores).
 */
export const RecordFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const correcting = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();
  const [form, setForm] = useState<FormState>(EMPTY);
  const [error, setError] = useState<string | null>(null);
  const [duplicates, setDuplicates] = useState<PossibleDuplicate[] | null>(null);
  const today = todayIso();

  const options = useQuery({ queryKey: recordKeys.options, queryFn: recordsApi.options });
  const existing = useQuery({ queryKey: recordKeys.detail(id ?? ''), queryFn: () => recordsApi.get(id!), enabled: correcting });

  // Correção: preenche o formulário com a medida rejeitada.
  useEffect(() => {
    const record = existing.data;
    if (!record) return;
    setForm({
      employee: {
        id: record.summary.employeeId,
        employeeNumber: record.summary.employeeNumber,
        fullName: record.summary.employeeName,
        cpfMasked: '',
        admissionDate: '',
        status: EmployeeStatus.Active,
        managementUnitCode: record.summary.managementUnitCode,
      },
      infractionTypeId: record.summary.infractionTypeId ?? '',
      measureType: record.summary.measureType,
      occurrenceDates: record.occurrenceDates.length ? record.occurrenceDates : [''],
      measureDate: record.summary.measureDate ?? '',
      suspensionStart: record.suspensionStart ?? '',
      suspensionEnd: record.suspensionEnd ?? '',
      description: record.description,
    });
  }, [existing.data]);

  const canSeeHistory = hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY);
  const support = useQuery({
    queryKey: employeeKeys.decisionSupport(form.employee?.id ?? ''),
    queryFn: () => employeesApi.decisionSupport(form.employee!.id),
    enabled: Boolean(form.employee) && canSeeHistory,
  });

  const days = suspensionDays(form.suspensionStart, form.suspensionEnd);
  const isSuspension = form.measureType === MeasureType.Suspension;

  const input = useMemo<RecordInput | null>(() => {
    if (!form.employee || form.measureType === '') return null;
    return {
      employeeId: form.employee.id,
      infractionTypeId: form.infractionTypeId,
      measureType: form.measureType,
      occurrenceDates: form.occurrenceDates.filter(Boolean),
      measureDate: form.measureDate,
      suspensionStart: isSuspension ? form.suspensionStart || null : null,
      suspensionEnd: isSuspension ? form.suspensionEnd || null : null,
      description: form.description.trim(),
    };
  }, [form, isSuspension]);

  const save = useMutation({
    mutationFn: (payload: RecordInput) => (correcting ? recordsApi.correct(id!, payload) : recordsApi.register(payload)),
    onSuccess: async (record) => {
      await queryClient.invalidateQueries({ queryKey: recordKeys.all });
      navigate(`/medidas/${record.summary.id}`, { state: { saved: correcting ? 'Medida corrigida e reenviada para aprovação.' : 'Medida registrada. Aguardando aprovação.' } });
    },
    onError: (e) => {
      if (e instanceof PossibleDuplicateError) setDuplicates(e.duplicates);
      else setError(recordErrorMessage(e, 'Não foi possível salvar a medida.'));
    },
  });

  const submit = (confirmPossibleDuplicate = false) => {
    setError(null);
    const problem = validateRecordForm(form, today);
    if (problem || !input) {
      setError(problem);
      return;
    }
    setDuplicates(null);
    save.mutate({ ...input, confirmPossibleDuplicate });
  };

  const set = (patch: Partial<FormState>) => setForm((f) => ({ ...f, ...patch }));

  if (correcting && existing.isError) return <Alert severity="error">{recordErrorMessage(existing.error, 'Medida não encontrada.')}</Alert>;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        {correcting ? 'Corrigir medida rejeitada' : 'Registrar medida'}
      </Typography>
      {correcting && existing.data?.lastRejectionReason && (
        <Alert severity="warning" data-testid="rejection-reason">
          Rejeitada por {existing.data.lastRejectedByName ?? 'aprovador'}: {existing.data.lastRejectionReason}
        </Alert>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '3fr 2fr' }, gap: 2, alignItems: 'start' }}>
        <Paper
          component="form"
          aria-label={correcting ? 'Corrigir medida' : 'Registrar medida'}
          elevation={0}
          sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', display: 'flex', flexDirection: 'column', gap: 2 }}
          onSubmit={(e) => {
            e.preventDefault();
            submit(false);
          }}
        >
          <EmployeePicker value={form.employee} onChange={(employee) => set({ employee })} />

          <TextField select label="Motivo" required value={form.infractionTypeId} onChange={(e) => set({ infractionTypeId: e.target.value })}>
            {options.data?.infractionTypes.map((t) => (
              <MenuItem key={t.id} value={t.id}>
                {t.name}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            select
            label="Tipo de medida"
            required
            value={form.measureType}
            onChange={(e) => set({ measureType: Number(e.target.value) as MeasureType })}
          >
            {options.data?.measureTypes.map((t) => (
              <MenuItem key={t.value} value={t.value}>
                {t.label}
              </MenuItem>
            ))}
          </TextField>

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Data(s) da ocorrência
            </Typography>
            {form.occurrenceDates.map((date, index) => (
              <Box key={index} sx={{ display: 'flex', gap: 1, mb: 1 }}>
                <TextField
                  type="date"
                  size="small"
                  label={`Ocorrência ${index + 1}`}
                  value={date}
                  slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today } }}
                  onChange={(e) => set({ occurrenceDates: form.occurrenceDates.map((d, i) => (i === index ? e.target.value : d)) })}
                />
                {form.occurrenceDates.length > 1 && (
                  <IconButton aria-label={`Remover ocorrência ${index + 1}`} onClick={() => set({ occurrenceDates: form.occurrenceDates.filter((_, i) => i !== index) })}>
                    <Trash2 size={16} />
                  </IconButton>
                )}
              </Box>
            ))}
            <Button size="small" startIcon={<Plus size={14} />} onClick={() => set({ occurrenceDates: [...form.occurrenceDates, ''] })}>
              Outra data
            </Button>
          </Box>

          <TextField type="date" label="Data da medida" required value={form.measureDate} slotProps={{ inputLabel: { shrink: true } }} onChange={(e) => set({ measureDate: e.target.value })} />

          {isSuspension && (
            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
              <TextField type="date" label="Início da suspensão" required value={form.suspensionStart} slotProps={{ inputLabel: { shrink: true } }} onChange={(e) => set({ suspensionStart: e.target.value })} />
              <TextField type="date" label="Fim da suspensão" required value={form.suspensionEnd} slotProps={{ inputLabel: { shrink: true } }} onChange={(e) => set({ suspensionEnd: e.target.value })} />
              {days !== null && (
                <Typography variant="body2" data-testid="suspension-days">
                  {days} dia{days > 1 ? 's' : ''} de suspensão
                </Typography>
              )}
            </Box>
          )}

          <TextField label="Descrição / observação" required multiline minRows={3} value={form.description} onChange={(e) => set({ description: e.target.value })} />

          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}

          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
            <Button onClick={() => navigate(-1)}>Voltar</Button>
            <Button type="submit" variant="contained" disabled={save.isPending}>
              {correcting ? 'Reenviar para aprovação' : 'Registrar medida'}
            </Button>
          </Box>
        </Paper>

        <Box>
          {form.employee && canSeeHistory && support.data && <DecisionSupportPanel data={support.data} />}
          {form.employee && canSeeHistory && support.isLoading && <Typography color="text.secondary">Carregando o histórico…</Typography>}
        </Box>
      </Box>

      <Dialog open={duplicates !== null} onClose={() => setDuplicates(null)} aria-labelledby="duplicate-title">
        <DialogTitle id="duplicate-title">Possível duplicidade</DialogTitle>
        <DialogContent>
          <Typography gutterBottom>
            Já existe medida do mesmo tipo, pendente ou aprovada, para este funcionário na mesma data de ocorrência:
          </Typography>
          <List dense>
            {duplicates?.map((d) => (
              <ListItem key={`${d.id}-${d.occurrenceDate}`}>
                {d.caseNumber} — ocorrência em {formatBusinessDate(d.occurrenceDate)}
              </ListItem>
            ))}
          </List>
          <Typography>Registre mesmo assim só se for outra ocorrência no mesmo dia.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDuplicates(null)}>Revisar</Button>
          <Button variant="contained" color="warning" onClick={() => submit(true)}>
            Registrar mesmo assim
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default RecordFormPage;
