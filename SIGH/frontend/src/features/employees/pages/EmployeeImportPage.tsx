import React, { useState } from 'react';
import { isAxiosError } from 'axios';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
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
import { FileSpreadsheet } from 'lucide-react';
import { employeeKeys, employeesApi, type ImportChange, type ImportIssue, type ImportPreview, type ImportResult } from '../api/employeesApi';

const MAX_BYTES = 25 * 1024 * 1024;

const errorMessage = (error: unknown, fallback: string) =>
  isAxiosError<{ message?: string; detail?: string }>(error)
    ? error.response?.data?.message || error.response?.data?.detail || fallback
    : error instanceof Error
      ? error.message
      : fallback;

type TabKey = 'created' | 'updated' | 'toTerminate' | 'reactivated' | 'errors' | 'warnings';

const ChangesTable: React.FC<{ items: ImportChange[]; label: string }> = ({ items, label }) =>
  items.length === 0 ? (
    <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
      Nenhum.
    </Typography>
  ) : (
    <Table size="small" aria-label={label}>
      <TableHead>
        <TableRow>
          <TableCell sx={{ fontWeight: 700 }}>Matrícula</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Nome</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Campo</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Valor atual</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Valor novo</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {items.flatMap((item) =>
          item.changes.map((c, i) => (
            <TableRow key={`${item.employeeNumber}-${c.field}`}>
              <TableCell sx={{ fontFamily: 'monospace' }}>{i === 0 ? item.employeeNumber : ''}</TableCell>
              <TableCell>{i === 0 ? item.fullName : ''}</TableCell>
              <TableCell>{c.field}</TableCell>
              <TableCell>{c.oldValue ?? '-'}</TableCell>
              <TableCell>{c.newValue ?? '-'}</TableCell>
            </TableRow>
          )),
        )}
      </TableBody>
    </Table>
  );

const IssuesTable: React.FC<{ items: ImportIssue[]; label: string }> = ({ items, label }) =>
  items.length === 0 ? (
    <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
      Nenhum.
    </Typography>
  ) : (
    <Table size="small" aria-label={label}>
      <TableHead>
        <TableRow>
          <TableCell sx={{ fontWeight: 700 }}>Linha</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Matrícula</TableCell>
          <TableCell sx={{ fontWeight: 700 }}>Mensagem</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {items.map((issue, i) => (
          <TableRow key={`${issue.row}-${i}`}>
            <TableCell>{issue.row ?? '-'}</TableCell>
            <TableCell sx={{ fontFamily: 'monospace' }}>{issue.employeeNumber ?? '-'}</TableCell>
            <TableCell>{issue.message}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );

export const EmployeeImportPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<'preview' | 'confirm' | null>(null);
  const [tab, setTab] = useState<TabKey>('created');
  const [massConfirmed, setMassConfirmed] = useState(false);

  const batches = useQuery({ queryKey: employeeKeys.importBatches, queryFn: employeesApi.importBatches });

  const choose = (selected: File | null) => {
    setPreview(null);
    setResult(null);
    setMassConfirmed(false);
    setError(null);
    if (selected && !selected.name.toLowerCase().endsWith('.xlsx')) {
      setFile(null);
      setError('Envie a planilha exportada do SAP no formato .xlsx.');
      return;
    }
    if (selected && selected.size > MAX_BYTES) {
      setFile(null);
      setError('A planilha passa do limite de 25 MB.');
      return;
    }
    setFile(selected);
  };

  const runPreview = async () => {
    if (!file) return;
    setBusy('preview');
    setError(null);
    try {
      const p = await employeesApi.previewImport(file);
      setPreview(p);
      setTab(p.errors.length > 0 ? 'errors' : 'created');
    } catch (err) {
      setError(errorMessage(err, 'Não foi possível pré-visualizar a planilha.'));
    } finally {
      setBusy(null);
      void queryClient.invalidateQueries({ queryKey: employeeKeys.importBatches });
    }
  };

  const confirm = async () => {
    if (!file || !preview) return;
    setBusy('confirm');
    setError(null);
    try {
      setResult(await employeesApi.confirmImport(preview.batchId, file, massConfirmed));
      setPreview(null);
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
    } catch (err) {
      setError(errorMessage(err, 'Não foi possível confirmar a importação.'));
    } finally {
      setBusy(null);
    }
  };

  const tabs: { key: TabKey; label: string; count: number }[] = preview
    ? [
        { key: 'created', label: 'Novos', count: preview.created.length },
        { key: 'updated', label: 'Alterados', count: preview.updated.length },
        { key: 'toTerminate', label: 'A desligar', count: preview.toTerminate.length },
        { key: 'reactivated', label: 'Reativados', count: preview.reactivated.length },
        { key: 'errors', label: 'Erros', count: preview.errors.length },
        { key: 'warnings', label: 'Avisos', count: preview.warnings.length },
      ]
    : [];

  const blockedByMass = !!preview?.requiresMassTerminationConfirmation && !massConfirmed;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800, display: 'flex', alignItems: 'center', gap: 1 }}>
        <FileSpreadsheet size={28} /> Importar funcionários (SAP)
      </Typography>

      <Paper elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', borderRadius: 2, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <Typography variant="body2" color="text.secondary">
          Envie a exportação .xlsx do SAP. A pré-visualização não grava nada; confira as abas e depois confirme. Funcionários ausentes
          do arquivo são desligados com a data de hoje (nunca apagados).
        </Typography>
        <input
          type="file"
          accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
          aria-label="Planilha do SAP"
          disabled={!!busy}
          onChange={(e) => choose(e.target.files?.[0] ?? null)}
        />
        <Box>
          <Button variant="contained" onClick={runPreview} disabled={!file || !!busy}>
            {busy === 'preview' ? <CircularProgress size={20} color="inherit" /> : 'Pré-visualizar'}
          </Button>
        </Box>
        {error && <Alert severity="error">{error}</Alert>}
        {result && (
          <Alert severity="success">
            Importação aplicada: {result.summary.created} novo(s), {result.summary.updated} alterado(s),{' '}
            {result.summary.terminated + result.summary.terminatedByAbsence} desligado(s), {result.summary.reactivated} reativado(s),{' '}
            {result.summary.unchanged} sem mudança.
          </Alert>
        )}
      </Paper>

      {preview && (
        <Paper component="section" aria-label="Pré-visualização" elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
          <Box sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
              Pré-visualização de {preview.fileName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {preview.summary.totalRows} linha(s) · {preview.summary.unchanged} sem mudança · válida até{' '}
              {new Date(preview.expiresAt).toLocaleTimeString('pt-BR')}
            </Typography>
            {preview.requiresMassTerminationConfirmation && (
              <Alert severity="warning" role="alert">
                Esta importação desliga {preview.toTerminate.length} funcionário(s) ausente(s) do arquivo (
                {preview.absenceTerminationPercent.toLocaleString('pt-BR')}% dos {preview.activeBefore} ativos), acima do limite de{' '}
                {preview.maxTerminationPercent.toLocaleString('pt-BR')}%. Confira a aba &quot;A desligar&quot; antes de continuar.
                <Box>
                  <FormControlLabel
                    control={<Checkbox checked={massConfirmed} onChange={(e) => setMassConfirmed(e.target.checked)} />}
                    label="Confirmo o desligamento em massa"
                  />
                </Box>
              </Alert>
            )}
          </Box>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}>
            {tabs.map((t) => (
              <Tab key={t.key} value={t.key} label={`${t.label} (${t.count})`} />
            ))}
          </Tabs>
          <Box sx={{ overflowX: 'auto' }}>
            {tab === 'created' && <ChangesTable items={preview.created} label="Novos" />}
            {tab === 'updated' && <ChangesTable items={preview.updated} label="Alterados" />}
            {tab === 'toTerminate' && <ChangesTable items={preview.toTerminate} label="A desligar" />}
            {tab === 'reactivated' && <ChangesTable items={preview.reactivated} label="Reativados" />}
            {tab === 'errors' && <IssuesTable items={preview.errors} label="Erros" />}
            {tab === 'warnings' && <IssuesTable items={preview.warnings} label="Avisos" />}
          </Box>
          <Box sx={{ p: 2.5, display: 'flex', gap: 1.5, alignItems: 'center' }}>
            <Button variant="contained" color="primary" onClick={confirm} disabled={!!busy || blockedByMass}>
              {busy === 'confirm' ? <CircularProgress size={20} color="inherit" /> : 'Confirmar importação'}
            </Button>
            {preview.errors.length > 0 && (
              <Typography variant="body2" color="text.secondary">
                As {preview.errors.length} linha(s) com erro não serão importadas.
              </Typography>
            )}
          </Box>
        </Paper>
      )}

      <Paper component="section" aria-label="Importações anteriores" elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, overflowX: 'auto' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700, p: 2.5, pb: 1 }}>
          Importações anteriores
        </Typography>
        <Table size="small" aria-label="Histórico de importações">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>Data</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Arquivo</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Situação</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Por</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Novos / Alterados / Desligados / Reativados</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Erros</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(batches.data?.items ?? []).map((b) => (
              <TableRow key={b.id}>
                <TableCell>{new Date(b.appliedAt ?? b.previewedAt).toLocaleString('pt-BR')}</TableCell>
                <TableCell>{b.fileName}</TableCell>
                <TableCell>
                  <Chip size="small" color={b.status === 'Applied' ? 'success' : 'default'} label={b.status === 'Applied' ? 'Aplicada' : 'Só pré-visualizada'} />
                </TableCell>
                <TableCell>{b.appliedByName ?? b.previewedByName ?? '-'}</TableCell>
                <TableCell>
                  {b.summary.created} / {b.summary.updated} / {b.summary.terminated + b.summary.terminatedByAbsence} / {b.summary.reactivated}
                </TableCell>
                <TableCell>{b.summary.errors}</TableCell>
              </TableRow>
            ))}
            {batches.data && batches.data.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  Nenhuma importação.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
};

export default EmployeeImportPage;
