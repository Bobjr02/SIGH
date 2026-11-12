import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Paper, TextField, Typography } from '@mui/material';
import { KeyRound } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { authErrorMessage } from '../auth/apiErrorMessage';

// A complexidade da senha é validada no servidor (PasswordComplexityValidator); aqui só o básico.
const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Informe a senha atual.'),
    newPassword: z.string().min(1, 'Informe a nova senha.'),
    confirmPassword: z.string().min(1, 'Confirme a nova senha.'),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'A confirmação não confere com a nova senha.',
  })
  .refine((v) => v.newPassword !== v.currentPassword, {
    path: ['newPassword'],
    message: 'A nova senha deve ser diferente da atual.',
  });

type ChangePasswordForm = z.infer<typeof changePasswordSchema>;

export const ChangePasswordPage: React.FC = () => {
  const { user, changePassword, logout } = useAuth();
  const navigate = useNavigate();
  const [apiError, setApiError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const mandatory = user?.mustChangePassword ?? false;

  const onSubmit = async (values: ChangePasswordForm) => {
    setApiError(null);
    try {
      await changePassword(values.currentPassword, values.newPassword);
      navigate('/', { replace: true });
    } catch (error) {
      setApiError(authErrorMessage(error, 'Não foi possível trocar a senha. Tente novamente.'));
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Paper elevation={0} sx={{ p: 4, width: '100%', maxWidth: 440, border: '1px solid', borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
          <KeyRound size={30} color="#2563eb" />
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
            Trocar senha
          </Typography>
        </Box>

        {mandatory && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            Por segurança, defina uma nova senha antes de continuar.
          </Alert>
        )}

        {apiError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {apiError}
          </Alert>
        )}

        <Box component="form" noValidate onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            label="Senha atual"
            type="password"
            autoComplete="current-password"
            fullWidth
            {...register('currentPassword')}
            error={!!errors.currentPassword}
            helperText={errors.currentPassword?.message}
          />
          <TextField
            label="Nova senha"
            type="password"
            autoComplete="new-password"
            fullWidth
            {...register('newPassword')}
            error={!!errors.newPassword}
            helperText={errors.newPassword?.message}
          />
          <TextField
            label="Confirmar nova senha"
            type="password"
            autoComplete="new-password"
            fullWidth
            {...register('confirmPassword')}
            error={!!errors.confirmPassword}
            helperText={errors.confirmPassword?.message}
          />
          <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
            {isSubmitting ? <CircularProgress size={22} color="inherit" /> : 'Salvar nova senha'}
          </Button>
          {mandatory ? (
            <Button variant="text" color="inherit" onClick={handleLogout} disabled={isSubmitting}>
              Sair
            </Button>
          ) : (
            <Button variant="text" color="inherit" onClick={() => navigate(-1)} disabled={isSubmitting}>
              Cancelar
            </Button>
          )}
        </Box>
      </Paper>
    </Box>
  );
};

export default ChangePasswordPage;
