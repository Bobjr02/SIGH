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
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { addOccurrenceSchema } from '../schemas';
import { InfractionSeverity, InfractionTypeDto } from '../types';

interface AddOccurrenceModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
  infractionTypes?: InfractionTypeDto[];
}

export const AddOccurrenceModal: React.FC<AddOccurrenceModalProps> = ({
  open,
  onClose,
  onSubmit,
  loading,
  errorMessage,
  infractionTypes = [],
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(addOccurrenceSchema),
    defaultValues: {
      description: '',
      occurredAt: new Date().toISOString().slice(0, 16),
      location: '',
      infractionTypeId: '',
      severity: InfractionSeverity.Moderate,
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        description: '',
        occurredAt: new Date().toISOString().slice(0, 16),
        location: '',
        infractionTypeId: '',
        severity: InfractionSeverity.Moderate,
      });
    }
  }, [open, reset]);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Adicionar Ocorrência ao Processo</DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent dividers>
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2.5 }}>
              {errorMessage}
            </Alert>
          )}

          <Grid container spacing={2}>
            <Grid size={{ xs: 12 }}>
              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Descrição da Ocorrência *"
                    placeholder="Descreva o fato ocorrido..."
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

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="occurredAt"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="datetime-local"
                    label="Data e Hora do Fato *"
                    fullWidth
                    size="small"
                    error={!!errors.occurredAt}
                    helperText={errors.occurredAt?.message as string}
                    disabled={loading}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="location"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Local do Ocorrido"
                    placeholder="Ex: Setor de Produção, Galpão 2"
                    fullWidth
                    size="small"
                    error={!!errors.location}
                    helperText={errors.location?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="infractionTypeId"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small">
                    <InputLabel id="infraction-type-select-label">Tipo de Infração</InputLabel>
                    <Select
                      {...field}
                      labelId="infraction-type-select-label"
                      label="Tipo de Infração"
                      disabled={loading}
                    >
                      <MenuItem value="">Nenhum / Selecionar depois</MenuItem>
                      {infractionTypes.map((type) => (
                        <MenuItem key={type.id} value={type.id}>
                          {type.code} - {type.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="severity"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.severity}>
                    <InputLabel id="occ-severity-label">Gravidade</InputLabel>
                    <Select {...field} labelId="occ-severity-label" label="Gravidade" disabled={loading}>
                      <MenuItem value={InfractionSeverity.Low}>Baixa</MenuItem>
                      <MenuItem value={InfractionSeverity.Moderate}>Média</MenuItem>
                      <MenuItem value={InfractionSeverity.High}>Alta</MenuItem>
                      <MenuItem value={InfractionSeverity.Critical}>Crítica</MenuItem>
                    </Select>
                    {errors.severity && <FormHelperText>{errors.severity.message as string}</FormHelperText>}
                  </FormControl>
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
            {loading ? 'Adicionando...' : 'Adicionar Ocorrência'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
