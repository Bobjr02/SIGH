import React, { useEffect, useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  LinearProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { Download, FileText, Paperclip, Plus, Trash2 } from 'lucide-react';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';
import { evidenceApi } from '../evidence/evidenceApi';
import {
  EVIDENCE_FILE_ACCEPT,
  EVIDENCE_TYPE_LABELS,
  formatBytes,
  shortHash,
  validateEvidenceFile,
  evidenceTypeAcceptsFile,
} from '../evidence/evidenceFiles';
import { DisciplinaryCaseStatus, EvidenceType, type EvidenceDto } from '../types';

export const errorMessageOf = (error: unknown, fallback: string): string => {
  if (isAxiosError<{ message?: string; detail?: string }>(error)) {
    if (error.response?.status === 413) return 'O arquivo excede o tamanho máximo permitido.';
    return error.response?.data?.message || error.response?.data?.detail || fallback;
  }
  return error instanceof Error ? error.message : fallback;
};

interface EvidenceSectionProps {
  caseId: string;
  caseStatus: DisciplinaryCaseStatus;
  evidences: EvidenceDto[];
  canAdd: boolean;
  canRemove: boolean;
  canDownload: boolean;
  onRegisterClick: () => void;
  /** Evidência recém-criada que deve abrir direto o envio do arquivo. */
  pendingUploadEvidenceId?: string | null;
  onPendingUploadHandled?: () => void;
}

export const EvidenceSection: React.FC<EvidenceSectionProps> = ({
  caseId,
  caseStatus,
  evidences,
  canAdd,
  canRemove,
  canDownload,
  onRegisterClick,
  pendingUploadEvidenceId,
  onPendingUploadHandled,
}) => {
  const queryClient = useQueryClient();
  const closed = caseStatus === DisciplinaryCaseStatus.Completed || caseStatus === DisciplinaryCaseStatus.Cancelled;
  const [uploadFor, setUploadFor] = useState<EvidenceDto | null>(null);
  const [removeFor, setRemoveFor] = useState<EvidenceDto | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  useEffect(() => {
    if (!pendingUploadEvidenceId) return;
    const evidence = evidences.find((e) => e.id === pendingUploadEvidenceId);
    if (evidence) {
      setUploadFor(evidence);
      onPendingUploadHandled?.();
    }
  }, [pendingUploadEvidenceId, evidences, onPendingUploadHandled]);

  const refresh = () => queryClient.invalidateQueries({ queryKey: disciplinaryQueryKeys.cases.detail(caseId) });

  const download = async (evidence: EvidenceDto) => {
    setDownloadError(null);
    try {
      await evidenceApi.downloadFile(caseId, evidence.id, evidence.originalFileName ?? 'evidencia');
    } catch (error) {
      setDownloadError(errorMessageOf(error, 'Não foi possível baixar o arquivo.'));
    }
  };

  return (
    <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
          <FileText size={20} color="#16a34a" /> Evidências ({evidences.length})
        </Typography>
        {canAdd && !closed && (
          <Button size="small" variant="outlined" startIcon={<Plus size={16} />} onClick={onRegisterClick}>
            Registrar Evidência
          </Button>
        )}
      </Box>

      {closed && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Processo encerrado: as evidências não podem mais ser alteradas.
        </Alert>
      )}
      {downloadError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setDownloadError(null)}>
          {downloadError}
        </Alert>
      )}

      {evidences.length > 0 ? (
        <Table size="small" aria-label="Evidências">
          <TableHead sx={{ backgroundColor: '#f8fafc' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>Evidência</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Arquivo</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Tamanho</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>SHA-256</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Enviado por</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">
                Ações
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {evidences.map((ev) => (
              <TableRow key={ev.id} data-testid={`evidence-row-${ev.id}`}>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {ev.title}
                  </Typography>
                  <Chip label={EVIDENCE_TYPE_LABELS[ev.evidenceType] ?? 'Outro'} size="small" variant="outlined" sx={{ mt: 0.5 }} />
                </TableCell>
                <TableCell>{ev.hasFile ? ev.originalFileName : ev.evidenceType === EvidenceType.Text ? 'Texto' : 'Sem arquivo'}</TableCell>
                <TableCell>{ev.hasFile ? formatBytes(ev.fileSize) : '-'}</TableCell>
                <TableCell>
                  {ev.integrityHash ? (
                    <Tooltip title={ev.integrityHash}>
                      <Box component="span" sx={{ fontFamily: 'monospace' }}>
                        {shortHash(ev.integrityHash)}
                      </Box>
                    </Tooltip>
                  ) : (
                    '-'
                  )}
                </TableCell>
                <TableCell>{ev.hasFile ? (ev.fileUploadedByName ?? '-') : '-'}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {ev.hasFile && canDownload && (
                    <Tooltip title="Baixar arquivo">
                      <IconButton size="small" aria-label={`Baixar ${ev.originalFileName}`} onClick={() => download(ev)}>
                        <Download size={18} />
                      </IconButton>
                    </Tooltip>
                  )}
                  {!ev.hasFile && evidenceTypeAcceptsFile(ev.evidenceType) && canAdd && !closed && (
                    <Button size="small" startIcon={<Paperclip size={16} />} onClick={() => setUploadFor(ev)}>
                      Anexar arquivo
                    </Button>
                  )}
                  {canRemove && !closed && (
                    <Tooltip title="Remover evidência">
                      <IconButton size="small" aria-label={`Remover ${ev.title}`} onClick={() => setRemoveFor(ev)}>
                        <Trash2 size={18} />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
          Nenhuma evidência registrada.
        </Typography>
      )}

      {uploadFor && (
        <UploadEvidenceFileDialog
          caseId={caseId}
          evidence={uploadFor}
          onClose={() => setUploadFor(null)}
          onUploaded={() => {
            setUploadFor(null);
            void refresh();
          }}
        />
      )}
      {removeFor && (
        <RemoveEvidenceDialog
          caseId={caseId}
          evidence={removeFor}
          onClose={() => setRemoveFor(null)}
          onRemoved={() => {
            setRemoveFor(null);
            void refresh();
          }}
        />
      )}
    </Paper>
  );
};

interface UploadDialogProps {
  caseId: string;
  evidence: EvidenceDto;
  onClose: () => void;
  onUploaded: () => void;
}

export const UploadEvidenceFileDialog: React.FC<UploadDialogProps> = ({ caseId, evidence, onClose, onUploaded }) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [progress, setProgress] = useState<number | null>(null);

  const choose = (selected: File | null) => {
    setFile(selected);
    setError(selected ? validateEvidenceFile(selected, evidence.evidenceType) : null);
  };

  const upload = async () => {
    if (!file || error) return;
    setProgress(0);
    try {
      await evidenceApi.uploadFile(caseId, evidence.id, file, setProgress);
      onUploaded();
    } catch (err) {
      setProgress(null);
      setError(errorMessageOf(err, 'Não foi possível enviar o arquivo.'));
    }
  };

  const uploading = progress !== null;

  return (
    <Dialog open onClose={uploading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Anexar arquivo: {evidence.title}</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Formatos: pdf, jpg/jpeg, png, webp, mp4, mp3, m4a, docx e xlsx, até 25 MB. Depois de enviado, o arquivo não pode ser
          substituído.
        </Typography>
        <input
          ref={inputRef}
          type="file"
          accept={EVIDENCE_FILE_ACCEPT}
          aria-label="Arquivo da evidência"
          disabled={uploading}
          onChange={(e) => choose(e.target.files?.[0] ?? null)}
        />
        {file && !error && (
          <Typography variant="body2" sx={{ mt: 1 }}>
            {file.name} ({formatBytes(file.size)})
          </Typography>
        )}
        {error && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {error}
          </Alert>
        )}
        {uploading && (
          <Box sx={{ mt: 2 }}>
            <LinearProgress variant="determinate" value={progress} aria-label="Progresso do envio" />
            <Typography variant="caption" color="text.secondary">
              Enviando… {progress}%
            </Typography>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={uploading}>
          Cancelar
        </Button>
        <Button variant="contained" onClick={upload} disabled={!file || !!error || uploading}>
          Enviar arquivo
        </Button>
      </DialogActions>
    </Dialog>
  );
};

interface RemoveDialogProps {
  caseId: string;
  evidence: EvidenceDto;
  onClose: () => void;
  onRemoved: () => void;
}

const RemoveEvidenceDialog: React.FC<RemoveDialogProps> = ({ caseId, evidence, onClose, onRemoved }) => {
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (reason.trim().length < 5) {
      setError('Informe o motivo da remoção (mínimo 5 caracteres).');
      return;
    }
    setBusy(true);
    try {
      await evidenceApi.remove(caseId, evidence.id, reason.trim());
      onRemoved();
    } catch (err) {
      setError(errorMessageOf(err, 'Não foi possível remover a evidência.'));
      setBusy(false);
    }
  };

  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Remover evidência</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" sx={{ mb: 2 }}>
          A remoção é lógica: o registro e o arquivo de <strong>{evidence.title}</strong> continuam guardados para auditoria.
        </Typography>
        <TextField
          label="Motivo da remoção"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          fullWidth
          multiline
          minRows={2}
          error={!!error}
          helperText={error}
          disabled={busy}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          Cancelar
        </Button>
        <Button color="error" variant="contained" onClick={submit} disabled={busy}>
          Remover
        </Button>
      </DialogActions>
    </Dialog>
  );
};
