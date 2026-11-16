import React, { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Alert, Box, Button, IconButton, List, ListItem, ListItemText, Paper, TextField, Tooltip, Typography } from '@mui/material';
import { Download, Paperclip, Trash2 } from 'lucide-react';
import { FilePicker } from '../../../components/ui/FilePicker';
import { evidenceApi } from '../../disciplinary/evidence/evidenceApi';
import { EVIDENCE_FILE_ACCEPT, EVIDENCE_FILE_EXTENSIONS, formatBytes, MAX_EVIDENCE_FILE_BYTES } from '../../disciplinary/evidence/evidenceFiles';
import { recordErrorMessage, recordKeys, recordsApi, type RecordEvidence } from '../api/recordsApi';

interface AttachmentsPanelProps {
  recordId: string;
  evidences: RecordEvidence[];
  canAttach: boolean;
  canRemove: boolean;
}

/**
 * Anexos da medida (o caso típico é a carta assinada, escaneada ou fotografada). Reaproveita as
 * evidências: cria a evidência e envia o arquivo; o servidor confere tipo, tamanho e assinatura.
 */
export const AttachmentsPanel: React.FC<AttachmentsPanelProps> = ({ recordId, evidences, canAttach, canRemove }) => {
  const queryClient = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState('Carta assinada');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = () => queryClient.invalidateQueries({ queryKey: recordKeys.detail(recordId) });

  const upload = async () => {
    if (!file) return;
    setError(null);
    const evidenceType = EVIDENCE_FILE_EXTENSIONS[file.name.split('.').pop()?.toLowerCase() ?? ''];
    if (!evidenceType) return; // o FilePicker já recusou a extensão
    setBusy(true);
    try {
      const evidenceId = await recordsApi.createAttachment(recordId, evidenceType, title.trim() || file.name);
      await evidenceApi.uploadFile(recordId, evidenceId, file);
      await refresh();
    } catch (e) {
      setError(recordErrorMessage(e, 'Não foi possível anexar o arquivo.'));
      // O anexo pode ter sido criado sem o arquivo (recusado pelo servidor): a lista mostra "Sem arquivo".
      await refresh();
    } finally {
      setFile(null);
      setBusy(false);
    }
  };

  const remove = async (evidence: RecordEvidence) => {
    const reason = window.prompt(`Motivo para remover "${evidence.title}":`);
    if (!reason?.trim()) return;
    try {
      await evidenceApi.remove(recordId, evidence.id, reason.trim());
      await refresh();
    } catch (e) {
      setError(recordErrorMessage(e, 'Não foi possível remover o anexo.'));
    }
  };

  return (
    <Paper component="section" aria-label="Anexos" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider' }}>
      <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mb: 1 }}>
        Anexos
      </Typography>
      {evidences.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          Nenhum anexo.
        </Typography>
      )}
      <List dense>
        {evidences.map((e) => (
          <ListItem
            key={e.id}
            data-testid="attachment"
            secondaryAction={
              <Box>
                {e.hasFile && (
                  <Tooltip title="Baixar">
                    <IconButton aria-label={`Baixar ${e.title}`} onClick={() => evidenceApi.downloadFile(recordId, e.id, e.originalFileName ?? 'anexo').catch((err) => setError(recordErrorMessage(err, 'Erro no download.')))}>
                      <Download size={16} />
                    </IconButton>
                  </Tooltip>
                )}
                {canRemove && (
                  <Tooltip title="Remover">
                    <IconButton aria-label={`Remover ${e.title}`} onClick={() => remove(e)}>
                      <Trash2 size={16} />
                    </IconButton>
                  </Tooltip>
                )}
              </Box>
            }
          >
            <ListItemText primary={e.title} secondary={e.hasFile ? `${e.originalFileName} · ${formatBytes(e.fileSize)}` : 'Sem arquivo'} />
          </ListItem>
        ))}
      </List>
      {canAttach && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, mt: 1 }}>
          <TextField size="small" label="Título do anexo" value={title} onChange={(e) => setTitle(e.target.value)} sx={{ maxWidth: 360 }} />
          <FilePicker
            label="Arquivo do anexo"
            buttonText="Selecionar arquivo"
            extensions={Object.keys(EVIDENCE_FILE_EXTENSIONS)}
            accept={EVIDENCE_FILE_ACCEPT}
            validate={(f) => (f.size > MAX_EVIDENCE_FILE_BYTES ? 'O arquivo excede o tamanho máximo permitido.' : null)}
            hint={`Formatos: ${Object.keys(EVIDENCE_FILE_EXTENSIONS).join(', ')}; até ${formatBytes(MAX_EVIDENCE_FILE_BYTES)}.`}
            file={file}
            onChange={(f) => {
              setError(null);
              setFile(f);
            }}
            disabled={busy}
            inputTestId="attachment-input"
          />
          <Box>
            <Button variant="contained" startIcon={<Paperclip size={16} />} disabled={!file || busy} onClick={() => void upload()}>
              {busy ? 'Enviando…' : 'Anexar arquivo'}
            </Button>
          </Box>
        </Box>
      )}
      {error && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {error}
        </Alert>
      )}
    </Paper>
  );
};
