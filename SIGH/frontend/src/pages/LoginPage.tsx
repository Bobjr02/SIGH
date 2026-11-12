import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link as RouterLink, Navigate, useLocation, useNavigate, type Location } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Link, Paper, TextField, Typography } from '@mui/material';
import { Shield } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { authErrorMessage } from '../auth/apiErrorMessage';
import { CHANGE_PASSWORD_PATH } from '../auth/ProtectedRoute';
import { FullScreenLoader } from '../components/common/FullScreenLoader';

const loginSchema = z.object({
  email: z.string().trim().min(1, 'Informe o e-mail.').email('Informe um e-mail válido.'),
  password: z.string().min(1, 'Informe a senha.'),
});

type LoginForm = z.infer<typeof loginSchema>;

export const LoginPage: React.FC = () => {
  const { login, status, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [apiError, setApiError] = useState<string | null>(null);

  const from = (location.state as { from?: Location } | null)?.from;
  const target = from && from.pathname !== '/login' ? `${from.pathname}${from.search}` : '/';

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } });

  if (status === 'loading') return <FullScreenLoader label="Carregando sessão..." />;
  if (status === 'authenticated' && user && !isSubmitting) {
    return <Navigate to={user.mustChangePassword ? CHANGE_PASSWORD_PATH : target} replace />;
  }

  const onSubmit = async (values: LoginForm) => {
    setApiError(null);
    try {
      const me = await login(values.email, values.password);
      navigate(me.mustChangePassword ? CHANGE_PASSWORD_PATH : target, { replace: true });
    } catch (error) {
      setApiError(authErrorMessage(error, 'Não foi possível entrar. Tente novamente.'));
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Paper elevation={0} sx={{ p: 4, width: '100%', maxWidth: 400, border: '1px solid', borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3 }}>
          <Shield size={32} color="#d97706" />
          <Box>
            <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
              Entrar no SIGH
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Gestão de Histórico Disciplinar
            </Typography>
          </Box>
        </Box>

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
          <TextField
            label="Senha"
            type="password"
            autoComplete="current-password"
            fullWidth
            {...register('password')}
            error={!!errors.password}
            helperText={errors.password?.message}
          />
          <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
            {isSubmitting ? <CircularProgress size={22} color="inherit" /> : 'Entrar'}
          </Button>
          <Link component={RouterLink} to="/forgot-password" variant="body2" sx={{ alignSelf: 'center' }}>
            Esqueci minha senha
          </Link>
        </Box>
      </Paper>
    </Box>
  );
};

export default LoginPage;
