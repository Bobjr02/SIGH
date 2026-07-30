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
import { applyMeasureSchema } from '../schemas';
import { DisciplinaryMeasureType, CaseEmployeeDto } from '../types';

interface ApplyMeasureModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
  employees?: CaseEmployeeDto[];
}

export const ApplyMeasureModal: React.FC<ApplyMeasureModalProps> = ({
  open,
  onClose,
  onSubmit,
  loading,
  errorMessage,
  employees = [],
}) => {
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(applyMeasureSchema),
    defaultValues: {
      measureType: DisciplinaryMeasureType.WrittenWarning,
      description: '',
      employeeId: '',
      startDate: new Date().toISOString().slice(0, 10),
      endDate: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        measureType: DisciplinaryMeasureType.WrittenWarning,
        description: '',
        employeeId: employees.length > 0 ? employees[0].employeeId : '',
        startDate: new Date().toISOString().slice(0, 10),
        endDate: '',
      });
    }
  }, [open, reset, employees]);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Aplicar Medida Disciplinar</DialogTitle>
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
                name="employeeId"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.employeeId}>
                    <InputLabel id="emp-measure-label">Funcionário Alvo *</InputLabel>

                    {employees.length > 0 ? (
                      <Select
                        {...field}
                        labelId="emp-measure-label"
                        label="Funcionário Alvo *"
                        disabled={loading}
                      >
                        {employees.map((emp) => (
                          <MenuItem key={emp.id} value={emp.employeeId}>
                            {emp.employeeId} ({emp.isPrimaryAccused ? 'Acusado Principal' : 'Envolvido'})
                          </MenuItem>
                        ))}
                      </Select>
                    ) : (
                      <TextField
                        {...field}
                        label="ID do Funcionário (UUID) *"
                        placeholder="22222222-2222-2222-2222-222222222222"
                        size="small"
                        disabled={loading}
                      />
                    )}
                    {errors.employeeId && (
                      <FormHelperText>{errors.employeeId.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="measureType"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.measureType}>
                    <InputLabel id="measure-type-label">Tipo de Medida *</InputLabel>
                    <Select
                      {...field}
                      labelId="measure-type-label"
                      label="Tipo de Medida *"
                      disabled={loading}
                    >
                      <MenuItem value={DisciplinaryMeasureType.VerbalWarning}>Advertência Verbal</MenuItem>
                      <MenuItem value={DisciplinaryMeasureType.WrittenWarning}>Advertência Escrita</MenuItem>
                      <MenuItem value={DisciplinaryMeasureType.Suspension}>Suspensão Disciplinar</MenuItem>
                      <MenuItem value={DisciplinaryMeasureType.TerminationRecommendation}>
                        Recomendação de Rescisão Por Justa Causa
                      </MenuItem>
                      <MenuItem value={DisciplinaryMeasureType.Other}>Outro</MenuItem>
                    </Select>
                    {errors.measureType && (
                      <FormHelperText>{errors.measureType.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 3 }}>
              <Controller
                name="startDate"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="date"
                    label="Data Início *"
                    fullWidth
                    size="small"
                    error={!!errors.startDate}
                    helperText={errors.startDate?.message as string}
                    disabled={loading}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 3 }}>
              <Controller
                name="endDate"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="date"
                    label="Data Fim"
                    fullWidth
                    size="small"
                    error={!!errors.endDate}
                    helperText={errors.endDate?.message as string}
                    disabled={loading}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
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
                    label="Descrição da Medida Aplicada *"
                    placeholder="Especifique os detalhes da penalidade ou orientação..."
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
            color="primary"
            disabled={loading}
            startIcon={loading ? <CircularProgress size={18} color="inherit" /> : null}
          >
            {loading ? 'Aplicando...' : 'Aplicar Medida'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
