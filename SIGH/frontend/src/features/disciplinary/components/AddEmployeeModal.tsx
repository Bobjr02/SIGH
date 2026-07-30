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
  FormControlLabel,
  Switch,
  Grid,
  Alert,
  CircularProgress,
  FormHelperText,
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { addEmployeeSchema } from '../schemas';
import { CaseEmployeeRole } from '../types';

interface AddEmployeeModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
}

export const AddEmployeeModal: React.FC<AddEmployeeModalProps> = ({
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
    resolver: zodResolver(addEmployeeSchema),
    defaultValues: {
      employeeId: '',
      involvementRole: CaseEmployeeRole.Accused,
      isPrimaryAccused: false,
      notes: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        employeeId: '',
        involvementRole: CaseEmployeeRole.Accused,
        isPrimaryAccused: false,
        notes: '',
      });
    }
  }, [open, reset]);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>Vincular Funcionário ao Processo</DialogTitle>
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
                  <TextField
                    {...field}
                    label="ID do Funcionário (UUID) *"
                    placeholder="Ex: 22222222-2222-2222-2222-222222222222"
                    fullWidth
                    size="small"
                    error={!!errors.employeeId}
                    helperText={errors.employeeId?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="involvementRole"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.involvementRole}>
                    <InputLabel id="role-label">Papel no Processo *</InputLabel>
                    <Select {...field} labelId="role-label" label="Papel no Processo *" disabled={loading}>
                      <MenuItem value={CaseEmployeeRole.Accused}>Acusado / Envolvido</MenuItem>
                      <MenuItem value={CaseEmployeeRole.Victim}>Vítima / Noticiante</MenuItem>
                      <MenuItem value={CaseEmployeeRole.Witness}>Testemunha</MenuItem>
                      <MenuItem value={CaseEmployeeRole.Reporter}>Relator / Denunciante</MenuItem>
                      <MenuItem value={CaseEmployeeRole.Other}>Outro</MenuItem>
                    </Select>
                    {errors.involvementRole && (
                      <FormHelperText>{errors.involvementRole.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="isPrimaryAccused"
                control={control}
                render={({ field: { value, onChange } }) => (
                  <FormControlLabel
                    control={
                      <Switch
                        checked={!!value}
                        onChange={(e) => onChange(e.target.checked)}
                        disabled={loading}
                      />
                    }
                    label="Acusado Principal"
                    sx={{ mt: 1 }}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Controller
                name="notes"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Observações / Detalhes do Envolvimento"
                    placeholder="Informe detalhes sobre a participação do funcionário..."
                    fullWidth
                    multiline
                    rows={2}
                    size="small"
                    error={!!errors.notes}
                    helperText={errors.notes?.message as string}
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
            {loading ? 'Adicionando...' : 'Vincular Funcionário'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
