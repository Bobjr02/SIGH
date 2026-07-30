import React, { useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Alert,
  CircularProgress,
  FormHelperText,
  Typography,
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { addEvidenceSchema } from '../schemas';
import { EvidenceType, OccurrenceDto } from '../types';

interface AddEvidenceModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
  occurrences?: OccurrenceDto[];
}

export const AddEvidenceModal: React.FC<AddEvidenceModalProps> = ({
  open,
  onClose,
  onSubmit,
  loading,
  errorMessage,
  occurrences = [],
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(addEvidenceSchema),
    defaultValues: {
      title: '',
      description: '',
      evidenceType: EvidenceType.Document,
      locationReference: '',
      collectedAt: new Date().toISOString().slice(0, 16),
      occurrenceId: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        title: '',
        description: '',
        evidenceType: EvidenceType.Document,
        locationReference: '',
        collectedAt: new Date().toISOString().slice(0, 16),
        occurrenceId: '',
      });
    }
  }, [open, reset]);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Registrar Metadados de Evidência</DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent dividers>
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2.5 }}>
              {errorMessage}
            </Alert>
          )}

          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
            * Registre os metadados e localização física/digital do documento ou prova associada ao processo.
          </Typography>

          <Grid container spacing={2}>
            <Grid size={{ xs: 12 }}>
              <Controller
                name="title"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Título da Evidência *"
                    placeholder="Ex: Registro de Ponto / Relatório de Auditoria"
                    fullWidth
                    size="small"
                    error={!!errors.title}
                    helperText={errors.title?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="evidenceType"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.evidenceType}>
                    <InputLabel id="evidence-type-label">Tipo de Evidência *</InputLabel>
                    <Select
                      {...field}
                      labelId="evidence-type-label"
                      label="Tipo de Evidência *"
                      disabled={loading}
                    >
                      <MenuItem value={EvidenceType.Document}>Documento</MenuItem>
                      <MenuItem value={EvidenceType.Image}>Imagem / Foto</MenuItem>
                      <MenuItem value={EvidenceType.Video}>Vídeo / Gravação</MenuItem>
                      <MenuItem value={EvidenceType.Audio}>Áudio</MenuItem>
                      <MenuItem value={EvidenceType.Text}>Texto / Declaração</MenuItem>
                      <MenuItem value={EvidenceType.Other}>Outro</MenuItem>
                    </Select>
                    {errors.evidenceType && (
                      <FormHelperText>{errors.evidenceType.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="collectedAt"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="datetime-local"
                    label="Data da Coleta *"
                    fullWidth
                    size="small"
                    error={!!errors.collectedAt}
                    helperText={errors.collectedAt?.message as string}
                    disabled={loading}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Controller
                name="locationReference"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Local de Armazenamento / Código de Referência"
                    placeholder="Ex: Arquivo Físico Pasta 12 / Servidor de Arquivos Interno"
                    fullWidth
                    size="small"
                    error={!!errors.locationReference}
                    helperText={errors.locationReference?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>

            {occurrences.length > 0 && (
              <Grid size={{ xs: 12 }}>
                <Controller
                  name="occurrenceId"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth size="small">
                      <InputLabel id="occ-link-label">Vincular a Ocorrência (Opcional)</InputLabel>
                      <Select
                        {...field}
                        labelId="occ-link-label"
                        label="Vincular a Ocorrência (Opcional)"
                        disabled={loading}
                      >
                        <MenuItem value="">Nenhuma / Processo Geral</MenuItem>
                        {occurrences.map((occ) => (
                          <MenuItem key={occ.id} value={occ.id}>
                            {occ.description.slice(0, 40)}...
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Grid>
            )}

            <Grid size={{ xs: 12 }}>
              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Descrição do Conteúdo da Evidência *"
                    placeholder="Detalhe o conteúdo e a relevância desta evidência para o processo..."
                    fullWidth
                    multiline
                    rows={3}
                    size="small"
                    error={!!errors.description}
                    helperText={errors.description?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>
          </Grid>
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={onClose} disabled={loading} color="inherit">
            Cancelar
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={loading}
            startIcon={loading ? <CircularProgress size={18} color="inherit" /> : null}
          >
            {loading ? 'Registrando...' : 'Registrar Evidência'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
