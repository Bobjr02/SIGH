import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Button,
  Paper,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Alert,
  CircularProgress,
  Breadcrumbs,
  Link,
  FormHelperText,
} from '@mui/material';
import { ArrowLeft, CheckCircle2, ShieldAlert } from 'lucide-react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createDisciplinaryCaseSchema } from '../schemas';
import { useDisciplinaryCases } from '../hooks/useDisciplinaryCases';
import { useInfractionTypes } from '../hooks/useInfractionTypes';
import { useDisciplinaryPermissions } from '../hooks/useDisciplinaryPermissions';
import { InfractionSeverity } from '../types';

export const CreateDisciplinaryCasePage: React.FC = () => {
  const navigate = useNavigate();
  const permissions = useDisciplinaryPermissions();
  const { createCase } = useDisciplinaryCases();
  const { infractionTypes } = useInfractionTypes({ page: 1, pageSize: 100 });

  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(createDisciplinaryCaseSchema),
    defaultValues: {
      caseNumber: '',
      companyId: '11111111-1111-1111-1111-111111111111',
      title: '',
      description: '',
      infractionTypeId: '',
      severity: InfractionSeverity.Moderate,
      responsibleId: '',
      occurredAt: new Date().toISOString().slice(0, 16),
    },
  });

  if (!permissions.canCreateCases) {
    return (
      <Paper elevation={0} sx={{ p: 4, textAlign: 'center', border: '1px solid', borderColor: 'divider', m: 3 }}>
        <ShieldAlert size={48} color="#ef4444" style={{ marginBottom: 16 }} />
        <Typography variant="h6" color="error" gutterBottom>
          Acesso Negado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Você não possui permissão para autuar novos processos disciplinares.
        </Typography>
      </Paper>
    );
  }

  const handleInfractionTypeChange = (typeId: string) => {
    setValue('infractionTypeId', typeId);
    const selected = infractionTypes.find((t) => t.id === typeId);
    if (selected && selected.defaultSeverity) {
      setValue('severity', selected.defaultSeverity);
    }
  };

  const handleFormSubmit = async (data: any) => {
    setFormError(null);
    try {
      const payload: any = {
        companyId: data.companyId,
        title: data.title,
        description: data.description || undefined,
      };

      if (data.caseNumber && data.caseNumber.trim()) {
        payload.caseNumber = data.caseNumber.trim();
      }

      if (data.responsibleId) {
        payload.responsibleEmployeeId = data.responsibleId;
        payload.responsibleId = data.responsibleId;
      }

      const newCase = await createCase.mutateAsync(payload);
      const createdId = newCase?.id;
      if (createdId) {
        navigate(`/disciplinary/cases/${createdId}`);
      } else {
        navigate('/disciplinary/cases');
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || err.message || 'Erro ao autuar o processo.';
      setFormError(msg);
    }
  };

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 900, mx: 'auto' }}>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 2, fontSize: '0.85rem' }}>
        <Link color="inherit" underline="hover" href="/">
          SIGH
        </Link>
        <Link color="inherit" underline="hover" href="/disciplinary/cases">
          Processos Disciplinares
        </Link>
        <Typography color="text.primary" sx={{ fontWeight: 600 }}>
          Autuar Novo Processo
        </Typography>
      </Breadcrumbs>

      {/* Header */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box>
            <Typography variant="h5" sx={{ fontWeight: 800, color: '#1e293b' }}>
              Autuar Novo Processo Disciplinar
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Preencha os dados iniciais do processo em estado de Rascunho.
            </Typography>
          </Box>
          <Button
            variant="outlined"
            size="small"
            startIcon={<ArrowLeft size={16} />}
            onClick={() => navigate('/disciplinary/cases')}
          >
            Voltar
          </Button>
        </Box>
      </Paper>

      {/* Form Card */}
      <Paper elevation={0} sx={{ p: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        {formError && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {formError}
          </Alert>
        )}

        <form onSubmit={handleSubmit(handleFormSubmit)}>
          <Grid container spacing={2.5}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="caseNumber"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Número do Processo *"
                    placeholder="Ex: PROC-2026-0001"
                    fullWidth
                    size="small"
                    error={!!errors.caseNumber}
                    helperText={errors.caseNumber?.message as string}
                    disabled={createCase.isPending}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="companyId"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="ID da Empresa (UUID) *"
                    placeholder="Ex: 11111111-1111-1111-1111-111111111111"
                    fullWidth
                    size="small"
                    error={!!errors.companyId}
                    helperText={errors.companyId?.message as string}
                    disabled={createCase.isPending}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }}>
              <Controller
                name="title"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Título Resumido do Processo *"
                    placeholder="Ex: Apuração de Faltas Repetidas Sem Justificativa"
                    fullWidth
                    size="small"
                    error={!!errors.title}
                    helperText={errors.title?.message as string}
                    disabled={createCase.isPending}
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
                    <InputLabel id="type-select-label">Tipo de Infração Enquadrada</InputLabel>
                    <Select
                      {...field}
                      labelId="type-select-label"
                      label="Tipo de Infração Enquadrada"
                      onChange={(e) => handleInfractionTypeChange(e.target.value)}
                      disabled={createCase.isPending}
                    >
                      <MenuItem value="">Nenhum / Definir na Ocorrência</MenuItem>
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
                    <InputLabel id="sev-select-label">Gravidade Estimada</InputLabel>
                    <Select
                      {...field}
                      labelId="sev-select-label"
                      label="Gravidade Estimada"
                      disabled={createCase.isPending}
                    >
                      <MenuItem value={InfractionSeverity.Low}>Baixa</MenuItem>
                      <MenuItem value={InfractionSeverity.Moderate}>Média</MenuItem>
                      <MenuItem value={InfractionSeverity.High}>Alta</MenuItem>
                      <MenuItem value={InfractionSeverity.Critical}>Crítica</MenuItem>
                    </Select>
                    {errors.severity && (
                      <FormHelperText>{errors.severity.message as string}</FormHelperText>
                    )}
                  </FormControl>
                )}
              />
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Controller
                name="responsibleId"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="ID do Responsável (UUID)"
                    placeholder="UUID do funcionário do RH / Jurídico"
                    fullWidth
                    size="small"
                    error={!!errors.responsibleId}
                    helperText={errors.responsibleId?.message as string}
                    disabled={createCase.isPending}
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
                    label="Data e Hora do Fato"
                    fullWidth
                    size="small"
                    disabled={createCase.isPending}
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
                    label="Descrição Detalhada do Processo"
                    placeholder="Descreva o contexto inicial, relato do ocorrido e observações primárias..."
                    fullWidth
                    multiline
                    rows={4}
                    size="small"
                    error={!!errors.description}
                    helperText={errors.description?.message as string}
                    disabled={createCase.isPending}
                  />
                )}
              />
            </Grid>

            <Grid size={{ xs: 12 }} sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2, mt: 1 }}>
              <Button
                variant="outlined"
                color="inherit"
                onClick={() => navigate('/disciplinary/cases')}
                disabled={createCase.isPending}
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                disabled={createCase.isPending}
                startIcon={
                  createCase.isPending ? (
                    <CircularProgress size={18} color="inherit" />
                  ) : (
                    <CheckCircle2 size={18} />
                  )
                }
              >
                {createCase.isPending ? 'Autuando...' : 'Autuar Processo'}
              </Button>
            </Grid>
          </Grid>
        </form>
      </Paper>
    </Box>
  );
};
