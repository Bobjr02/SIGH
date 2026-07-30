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
import { recordDecisionSchema } from '../schemas';
import { DecisionType } from '../types';

interface RecordDecisionModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
}

export const RecordDecisionModal: React.FC<RecordDecisionModalProps> = ({
  open,
  onClose,
  onSubmit,
  loading,
  errorMessage,
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(recordDecisionSchema),
    defaultValues: {
      decisionType: DecisionType.FormalWarning,
      description: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        decisionType: DecisionType.FormalWarning,
        description: '',
      });
    }
  }, [open, reset]);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Registrar Decisão no Processo</DialogTitle>
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
                name="decisionType"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.decisionType}>
                    <InputLabel id="decision-type-label">Tipo de Decisão *</InputLabel>
                    <Select
                      {...field}
                      labelId="decision-type-label"
                      label="Tipo de Decisão *"
                      disabled={loading}
                    >
                      <MenuItem value={DecisionType.NoViolation}>Sem Infração / Arquivamento</MenuItem>
                      <MenuItem value={DecisionType.InformalGuidance}>Orientação Verbal / Informal</MenuItem>
                      <MenuItem value={DecisionType.FormalWarning}>Advertência Formal Escrita</MenuItem>
                      <MenuItem value={DecisionType.Suspension}>Suspensão Disciplinar</MenuItem>
                      <MenuItem value={DecisionType.TerminationRecommendation}>Recomendação de Rescisão / Demissão por Justa Causa</MenuItem>
                      <MenuItem value={DecisionType.Other}>Outro</MenuItem>
                    </Select>
                    {errors.decisionType && (
                      <FormHelperText>{errors.decisionType.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Fundamentação / Justificativa da Decisão *"
                    placeholder="Descreva a fundamentação legal, análise dos fatos e a decisão proferida..."
                    fullWidth
                    multiline
                    rows={4}
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
            color="primary"
            disabled={loading}
            startIcon={loading ? <CircularProgress size={18} color="inherit" /> : null}
          >
            {loading ? 'Registrando...' : 'Registrar Decisão'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
