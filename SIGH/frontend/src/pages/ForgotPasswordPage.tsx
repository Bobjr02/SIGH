import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link as RouterLink } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Link, Paper, TextField, Typography } from '@mui/material';
import { MailQuestion } from 'lucide-react';
import { authApi } from '../auth/authApi';
import { authErrorMessage } from '../auth/apiErrorMessage';

const schema = z.object({
  email: z.string().trim().min(1, 'Informe o e-mail.').email('Informe um e-mail válido.'),
});

type FormValues = z.infer<typeof schema>;

export const ForgotPasswordPage: React.FC = () => {
  const [done, setDone] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { email: '' } });

  const onSubmit = async ({ email }: FormValues) => {
    setApiError(null);
    try {
      const message = await authApi.forgotPassword(email);
      // Mesma resposta para qualquer e-mail: a tela não revela se a conta existe.
      setDone(message || 'Se o e-mail informado estiver cadastrado, você receberá as instruções para redefinir a senha.');
    } catch (error) {
      setApiError(authErrorMessage(error, 'Não foi possível enviar o pedido. Tente novamente.'));
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Paper elevation={0} sx={{ p: 4, width: '100%', maxWidth: 420, border: '1px solid', borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
          <MailQuestion size={30} color="#2563eb" />
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
            Esqueci minha senha
          </Typography>
        </Box>

        {done ? (
          <Alert severity="success" sx={{ mb: 2 }}>
            {done}
          </Alert>
        ) : (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Informe o e-mail da sua conta. Enviaremos um link para você criar uma nova senha.
            </Typography>
            {apiError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {apiError}
              </Alert>
            )}
            <Box component="form" noValidate onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="E-mail"
                type="email"
                autoComplete="username"
                autoFocus
                fullWidth
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
              />
              <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
                {isSubmitting ? <CircularProgress size={22} color="inherit" /> : 'Enviar link'}
              </Button>
            </Box>
          </>
        )}

        <Link component={RouterLink} to="/login" variant="body2" sx={{ display: 'block', mt: 2, textAlign: 'center' }}>
          Voltar para o login
        </Link>
      </Paper>
    </Box>
  );
};

export default ForgotPasswordPage;
