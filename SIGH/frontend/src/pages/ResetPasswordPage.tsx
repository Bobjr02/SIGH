import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Link, Paper, TextField, Typography } from '@mui/material';
import { KeyRound } from 'lucide-react';
import { authApi } from '../auth/authApi';
import { authErrorMessage } from '../auth/apiErrorMessage';

// A política de complexidade é validada no servidor; aqui só o básico.
const schema = z
  .object({
    newPassword: z.string().min(1, 'Informe a nova senha.'),
    confirmPassword: z.string().min(1, 'Confirme a nova senha.'),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'A confirmação não confere com a nova senha.',
  });

type FormValues = z.infer<typeof schema>;

/** /reset-password?token=... (esqueci a senha) e ?token=...&setup=1 (primeiro acesso). */
export const ResetPasswordPage: React.FC = () => {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const isSetup = params.get('setup') === '1';
  const [done, setDone] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { newPassword: '', confirmPassword: '' } });

  const onSubmit = async ({ newPassword }: FormValues) => {
    setApiError(null);
    try {
      await authApi.resetPassword(token, newPassword);
      setDone(true);
    } catch (error) {
      setApiError(authErrorMessage(error, 'Não foi possível redefinir a senha. Tente novamente.'));
    }
  };

  const title = isSetup ? 'Definir senha' : 'Redefinir senha';

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Paper elevation={0} sx={{ p: 4, width: '100%', maxWidth: 440, border: '1px solid', borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
          <KeyRound size={30} color="#2563eb" />
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
            {title}
          </Typography>
        </Box>

        {!token ? (
          <Alert severity="error">
            Link inválido. Abra o link recebido por e-mail ou peça um novo em &quot;Esqueci minha senha&quot;.
          </Alert>
        ) : done ? (
          <Alert severity="success">Senha {isSetup ? 'definida' : 'redefinida'} com sucesso. Entre com a nova senha.</Alert>
        ) : (
          <>
            {apiError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {apiError}{' '}
                <Link component={RouterLink} to="/forgot-password">
                  Pedir um novo link
                </Link>
              </Alert>
            )}
            <Box component="form" noValidate onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="Nova senha"
                type="password"
                autoComplete="new-password"
                autoFocus
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
                {isSubmitting ? <CircularProgress size={22} color="inherit" /> : 'Salvar senha'}
              </Button>
            </Box>
          </>
        )}

        <Link component={RouterLink} to="/login" variant="body2" sx={{ display: 'block', mt: 2, textAlign: 'center' }}>
          Ir para o login
        </Link>
      </Paper>
    </Box>
  );
};

export default ResetPasswordPage;
