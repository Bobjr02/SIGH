import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  Typography,
} from '@mui/material';
import { Archive, FileUp, Lock } from 'lucide-react';
import { formatBusinessDate } from '../../records/api/recordsApi';
import {
  historyErrorMessage,
  historyImportApi,
  historyKeys,
  type HistoryIssue,
  type HistoryPreview,
  type HistoryRow,
} from '../api/historyImportApi';

const formatInstant = (value?: string | null) => (value ? new Date(value).toLocaleString('pt-BR') : '—');

const period = (r: HistoryRow) =>
  r.periodStart ? `${formatBusinessDate(r.periodStart)} a ${formatBusinessDate(r.periodEnd)}` : r.measureType === 4 ? 'sem período' : '—';

const days = (r: HistoryRow) => (r.measureType !== 4 ? '—' : r.daysUnknown ? 'não informado' : String(r.suspensionDays));

const RowsTable: React.FC<{ rows: HistoryRow[]; label: string }> = ({ rows, label }) => (
  <Table size="small" aria-label={label}>
    <TableHead>
      <TableRow>
        <TableCell>Linha</TableCell>
        <TableCell>Matrícula</TableCell>
        <TableCell>Funcionário</TableCell>
        <TableCell>Data</TableCell>
        <TableCell>Medida</TableCell>
        <TableCell>Motivo</TableCell>
        <TableCell>Gerência da época</TableCell>
        <TableCell>Período</TableCell>
        <TableCell>Dias</TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {rows.map((r) => (
        <TableRow key={r.row} data-testid="history-row">
          <TableCell>{r.row}</TableCell>
          <TableCell>{r.employeeNumber}</TableCell>
          <TableCell>{r.employeeName}</TableCell>
          <TableCell>{formatBusinessDate(r.date)}</TableCell>
          <TableCell>{r.measureTypeLabel}</TableCell>
          <TableCell>
            {r.infractionName} {r.newInfractionType && <Chip size="small" color="info" label="novo" />}
          </TableCell>
          <TableCell>{r.managementUnitCode ?? r.managementUnitName}</TableCell>
          <TableCell>{period(r)}</TableCell>
          <TableCell>{days(r)}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

const IssuesTable: React.FC<{ issues: HistoryIssue[]; label: string }> = ({ issues, label }) => (
  <Table size="small" aria-label={label}>
    <TableHead>
      <TableRow>
        <TableCell>Linha</TableCell>
        <TableCell>Matrícula</TableCell>
        <TableCell>Mensagem</TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {issues.map((i, n) => (
        <TableRow key={`${i.row}-${n}`} data-testid="history-issue">
          <TableCell>{i.row ?? '—'}</TableCell>
          <TableCell>{i.employeeNumber ?? '—'}</TableCell>
          <TableCell>{i.message}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

const PreviewTabs: React.FC<{ preview: HistoryPreview }> = ({ preview }) => {
  const [tab, setTab] = useState(0);
  const s = preview.summary;
  const tabs = [
    { label: `A importar (${s.toImport})`, content: <RowsTable rows={preview.toImport} label="A importar" /> },
    { label: `Já existentes (${s.existing})`, content: <RowsTable rows={preview.existing} label="Já existentes" /> },
    {
      label: `A corrigir (${s.toUpdate})`,
      content: (
        <Table size="small" aria-label="A corrigir">
          <TableHead>
            <TableRow>
              <TableCell>Linha</TableCell>
              <TableCell>Matrícula</TableCell>
              <TableCell>Registro</TableCell>
              <TableCell>O que muda</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {preview.toUpdate.map((u) => (
              <TableRow key={u.caseId}>
                <TableCell>{u.row.row}</TableCell>
                <TableCell>{u.row.employeeNumber}</TableCell>
                <TableCell>{u.caseNumber}</TableCell>
                <TableCell>{u.changes.map((c) => `${c.field}: ${c.oldValue ?? '—'} → ${c.newValue ?? '—'}`).join('; ')}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ),
    },
    {
      label: `Motivos novos (${s.newInfractionTypes})`,
      content: (
        <Box component="ul" aria-label="Motivos novos" sx={{ m: 0, pl: 3 }}>
          {preview.newInfractionTypes.map((m) => (
            <li key={m}>
              <Typography variant="body2">{m} — será criado na confirmação, ativo.</Typography>
            </li>
          ))}
        </Box>
      ),
    },
    { label: `Avisos (${s.warnings})`, content: <IssuesTable issues={preview.warnings} label="Avisos" /> },
    { label: `Rejeitadas (${s.rejected})`, content: <IssuesTable issues={preview.rejected} label="Rejeitadas" /> },
    {
      label: `Ausentes da planilha atual (${s.absent})`,
      content: (
        <>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Registros importados antes que não estão neste arquivo. Nada é apagado: confira com o RH.
          </Typography>
          <Table size="small" aria-label="Ausentes da planilha atual">
            <TableHead>
              <TableRow>
                <TableCell>Registro</TableCell>
                <TableCell>Matrícula</TableCell>
                <TableCell>Data</TableCell>
                <TableCell>Medida</TableCell>
                <TableCell>Motivo</TableCell>
                <TableCell>Importado em</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {preview.absent.map((a) => (
                <TableRow key={a.caseId}>
                  <TableCell>{a.caseNumber}</TableCell>
                  <TableCell>{a.employeeNumber}</TableCell>
                  <TableCell>{formatBusinessDate(a.date)}</TableCell>
                  <TableCell>{a.measureTypeLabel}</TableCell>
                  <TableCell>{a.infractionName}</TableCell>
                  <TableCell>{formatInstant(a.importedAt)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      ),
    },
  ];

  return (
    <Box>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" aria-label="Pré-visualização do histórico">
        {tabs.map((t) => (
          <Tab key={t.label} label={t.label} />
        ))}
      </Tabs>
      <Box sx={{ pt: 2 }} role="tabpanel">
        {tabs[tab].content}
      </Box>
    </Box>
  );
};

/**
 * Importação do histórico disciplinar (planilha de penalidades), até o go-live: upload → pré-visualização
 * (nada gravado) → confirmação com o mesmo arquivo. Depois do encerramento, só os lotes ficam visíveis.
 */
export const HistoryImportPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<HistoryPreview | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [closing, setClosing] = useState(false);
  const [closeConfirmed, setCloseConfirmed] = useState(false);

  const status = useQuery({ queryKey: historyKeys.status, queryFn: historyImportApi.status });
  const batches = useQuery({ queryKey: historyKeys.batches, queryFn: historyImportApi.batches });
  const refresh = () => Promise.all([queryClient.invalidateQueries({ queryKey: historyKeys.status }), queryClient.invalidateQueries({ queryKey: historyKeys.batches })]);

  const doPreview = useMutation({
    mutationFn: (f: File) => historyImportApi.preview(f),
    onSuccess: async (p) => {
      setPreview(p);
      setError(null);
      setMessage(null);
      await refresh();
    },
    onError: (e) => setError(historyErrorMessage(e, 'Não foi possível pré-visualizar.')),
  });

  const doConfirm = useMutation({
    mutationFn: () => historyImportApi.confirm(preview!.batchId, file!),
    onSuccess: async (r) => {
      setPreview(null);
      setFile(null);
      setError(null);
      setMessage(`Histórico importado: ${r.imported} registro(s) novo(s), ${r.updated} corrigido(s), ${r.infractionTypesCreated} motivo(s) criado(s).`);
      await refresh();
    },
    onError: (e) => setError(historyErrorMessage(e, 'Não foi possível confirmar.')),
  });

  const doClose = useMutation({
    mutationFn: historyImportApi.close,
    onSuccess: async () => {
      setClosing(false);
      setPreview(null);
      setMessage('Importação de histórico encerrada. O SIGH passa a ser a única fonte das medidas.');
      await refresh();
    },
    onError: (e) => setError(historyErrorMessage(e, 'Não foi possível encerrar.')),
  });

  const closed = status.data?.isClosed === true;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        Importar histórico disciplinar
      </Typography>
      <Typography color="text.secondary">
        Planilha de penalidades anterior ao SIGH. Pode ser importada mais de uma vez até o go-live; depois, encerre a importação.
        Importe antes a base de funcionários do SAP: o histórico só se liga a funcionários que já existem.
      </Typography>

      {closed && (
        <Alert severity="info" icon={<Lock size={18} />} data-testid="history-closed">
          Importação encerrada em {formatInstant(status.data?.closedAt)} por {status.data?.closedByName ?? '—'}. Não há reabertura pela tela.
        </Alert>
      )}
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

      {!closed && (
        <Paper elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
          <Button component="label" variant="outlined" startIcon={<FileUp size={16} />}>
            Escolher planilha (.xlsx)
            <input
              hidden
              type="file"
              accept=".xlsx"
              aria-label="Planilha de penalidades"
              onChange={(e) => {
                setFile(e.target.files?.[0] ?? null);
                setPreview(null);
                e.target.value = '';
              }}
            />
          </Button>
          <Typography variant="body2">{file?.name ?? 'Nenhum arquivo escolhido'}</Typography>
          <Button variant="contained" disabled={!file || doPreview.isPending} onClick={() => file && doPreview.mutate(file)}>
            Pré-visualizar
          </Button>
          <Box sx={{ flexGrow: 1 }} />
          <Button color="error" variant="outlined" startIcon={<Archive size={16} />} onClick={() => { setCloseConfirmed(false); setClosing(true); }}>
            Encerrar importação de histórico
          </Button>
        </Paper>
      )}

      {preview && (
        <Paper component="section" aria-label="Pré-visualização" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
            Pré-visualização — nada foi gravado
          </Typography>
          <Typography variant="body2" data-testid="history-summary">
            {preview.summary.totalRows} linhas com dados: {preview.summary.toImport} a importar, {preview.summary.existing} já existentes,{' '}
            {preview.summary.toUpdate} a corrigir, {preview.summary.rejected} rejeitadas, {preview.summary.warnings} avisos,{' '}
            {preview.summary.newInfractionTypes} motivo(s) novo(s), {preview.summary.absent} ausente(s). Confirme até {formatInstant(preview.expiresAt)}.
          </Typography>
          {preview.fileWarnings.map((w) => (
            <Alert key={w} severity="warning">
              {w}
            </Alert>
          ))}
          <PreviewTabs preview={preview} />
          <Box>
            <Button variant="contained" color="success" disabled={doConfirm.isPending || !file} onClick={() => doConfirm.mutate()}>
              Confirmar importação
            </Button>
          </Box>
        </Paper>
      )}

      <Paper component="section" aria-label="Lotes" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mb: 1 }}>
          Lotes
        </Typography>
        <Table size="small" aria-label="Lotes de importação do histórico">
          <TableHead>
            <TableRow>
              <TableCell>Arquivo</TableCell>
              <TableCell>Situação</TableCell>
              <TableCell>Pré-visualizado</TableCell>
              <TableCell>Aplicado</TableCell>
              <TableCell>Linhas</TableCell>
              <TableCell>Importadas</TableCell>
              <TableCell>Corrigidas</TableCell>
              <TableCell>Rejeitadas</TableCell>
              <TableCell>Avisos</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {batches.data?.items.map((b) => (
              <TableRow key={b.id} data-testid="history-batch">
                <TableCell>{b.fileName}</TableCell>
                <TableCell>{b.status === 'Applied' ? 'Aplicado' : 'Só pré-visualizado'}</TableCell>
                <TableCell>
                  {formatInstant(b.previewedAt)} · {b.previewedByName ?? '—'}
                </TableCell>
                <TableCell>{b.appliedAt ? `${formatInstant(b.appliedAt)} · ${b.appliedByName ?? '—'}` : '—'}</TableCell>
                <TableCell>{b.summary.totalRows}</TableCell>
                <TableCell>{b.summary.toImport}</TableCell>
                <TableCell>{b.summary.toUpdate}</TableCell>
                <TableCell>{b.summary.rejected}</TableCell>
                <TableCell>{b.summary.warnings}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={closing} onClose={() => setClosing(false)} aria-labelledby="close-history-title">
        <DialogTitle id="close-history-title">Encerrar a importação de histórico?</DialogTitle>
        <DialogContent>
          <Typography gutterBottom>
            Depois de encerrada, o SIGH passa a ser a única fonte das medidas e a importação não pode mais ser usada. Não há reabertura
            pela tela (só por configuração do servidor, com auditoria).
          </Typography>
          <FormControlLabel
            control={<Checkbox checked={closeConfirmed} onChange={(e) => setCloseConfirmed(e.target.checked)} />}
            label="Entendo que o encerramento é definitivo"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setClosing(false)}>Voltar</Button>
          <Button color="error" variant="contained" disabled={!closeConfirmed || doClose.isPending} onClick={() => doClose.mutate()}>
            Encerrar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default HistoryImportPage;
