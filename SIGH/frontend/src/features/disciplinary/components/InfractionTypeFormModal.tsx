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
import { z } from 'zod';
import { InfractionTypeDto, InfractionSeverity } from '../types';

interface InfractionTypeFormModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: any) => Promise<void>;
  editingType?: InfractionTypeDto | null;
  loading: boolean;
  errorMessage?: string | null;
}

const formSchema = z.object({
  code: z.string().optional(),
  name: z
    .string()
    .min(3, 'O nome é obrigatório e deve conter no mínimo 3 caracteres.')
    .max(100, 'O nome deve conter no máximo 100 caracteres.'),
  defaultSeverity: z
    .nativeEnum(InfractionSeverity)
    .refine((val) => val !== InfractionSeverity.Undefined, { message: 'Selecione uma gravidade válida.' }),
  requiresFormalInvestigation: z.boolean().default(false),
  allowsTerminationRecommendation: z.boolean().default(false),
  description: z.string().max(500, 'A descrição deve ter no máximo 500 caracteres.').optional().or(z.literal('')),
  legalReference: z.string().max(200, 'A referência legal deve ter no máximo 200 caracteres.').optional().or(z.literal('')),
});

export const InfractionTypeFormModal: React.FC<InfractionTypeFormModalProps> = ({
  open,
  onClose,
  onSubmit,
  editingType,
  loading,
  errorMessage,
}) => {
  const isEditing = !!editingType;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(formSchema),
    defaultValues: {
      code: '',
      name: '',
      defaultSeverity: InfractionSeverity.Moderate,
      requiresFormalInvestigation: false,
      allowsTerminationRecommendation: false,
      description: '',
      legalReference: '',
    },
  });

  useEffect(() => {
    if (editingType) {
      reset({
        code: editingType.code,
        name: editingType.name,
        defaultSeverity: editingType.defaultSeverity || InfractionSeverity.Moderate,
        requiresFormalInvestigation: editingType.requiresFormalInvestigation || false,
        allowsTerminationRecommendation: editingType.allowsTerminationRecommendation || false,
        description: editingType.description || '',
        legalReference: editingType.legalReference || '',
      });
    } else {
      reset({
        code: '',
        name: '',
        defaultSeverity: InfractionSeverity.Moderate,
        requiresFormalInvestigation: false,
        allowsTerminationRecommendation: false,
        description: '',
        legalReference: '',
      });
    }
  }, [editingType, open, reset]);

  const handleFormSubmit = async (data: any) => {
    await onSubmit(data);
  };

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>
        {isEditing ? 'Editar Tipo de Infração' : 'Novo Tipo de Infração'}
      </DialogTitle>
      <form onSubmit={handleSubmit(handleFormSubmit)}>
        <DialogContent dividers>
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {errorMessage}
            </Alert>
          )}

          <Grid container spacing={2.5}>
            {!isEditing && (
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name="code"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Código *"
                      placeholder="Ex: LEVE-01"
                      fullWidth
                      size="small"
                      error={!!errors.code}
                      helperText={errors.code?.message as string}
                      disabled={loading}
                    />
                  )}
                />
              </Grid>
            )}

            <Grid size={{ xs: 12, sm: isEditing ? 12 : 8 }}>
              <Controller
                name="name"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Nome do Tipo de Infração *"
                    placeholder="Ex: Atraso Injustificado Repetido"
                    fullWidth
                    size="small"
                    error={!!errors.name}
                    helperText={errors.name?.message as string}
                    disabled={loading}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="defaultSeverity"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth size="small" error={!!errors.defaultSeverity}>
                    <InputLabel id="severity-label">Gravidade Padrão *</InputLabel>
                    <Select
                      {...field}
                      labelId="severity-label"
                      label="Gravidade Padrão *"
                      disabled={loading}
                    >
                      <MenuItem value={InfractionSeverity.Low}>Baixa</MenuItem>
                      <MenuItem value={InfractionSeverity.Moderate}>Média</MenuItem>
                      <MenuItem value={InfractionSeverity.High}>Alta</MenuItem>
                      <MenuItem value={InfractionSeverity.Critical}>Crítica</MenuItem>
                    </Select>
                    {errors.defaultSeverity && (
                      <FormHelperText>{errors.defaultSeverity.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="requiresFormalInvestigation"
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
                    label="Exige Investigação Formal"
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="allowsTerminationRecommendation"
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
                    label="Permite Recomendação de Rescisão"
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Controller
                name="legalReference"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Referência Legal / Artigo da CLT"
                    placeholder="Ex: Artigo 482 da CLT, alínea 'b'"
                    fullWidth
                    size="small"
                    error={!!errors.legalReference}
                    helperText={errors.legalReference?.message as string}
                    disabled={loading}
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
                    label="Descrição Detalhada"
                    placeholder="Descreva as características e enquadramento deste tipo de infração..."
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
            {loading ? 'Salvando...' : isEditing ? 'Salvar Alterações' : 'Criar Tipo de Infração'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
