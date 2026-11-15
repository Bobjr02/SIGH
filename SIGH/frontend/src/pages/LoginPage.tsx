import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link as RouterLink, Navigate, useLocation, useNavigate, type Location } from 'react-router-dom';
import { Alert, CircularProgress } from '@mui/material';
import { ArrowRight, Eye, EyeOff, Lock, Mail, ScrollText, ShieldCheck, Sparkles } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { authErrorMessage } from '../auth/apiErrorMessage';
import { CHANGE_PASSWORD_PATH } from '../auth/ProtectedRoute';
import { FullScreenLoader } from '../components/common/FullScreenLoader';
import { SighLogo } from '../components/brand/SighLogo';

const loginSchema = z.object({
  email: z.string().trim().min(1, 'Informe o e-mail.').email('Informe um e-mail válido.'),
  password: z.string().min(1, 'Informe a senha.'),
  rememberMe: z.boolean(),
});

type LoginForm = z.infer<typeof loginSchema>;

const HIGHLIGHTS = [
  { icon: <ShieldCheck size={22} />, title: 'Segurança avançada' },
  { icon: <ScrollText size={22} />, title: 'Auditoria completa' },
  { icon: <Sparkles size={22} />, title: 'Apoio à decisão' },
];

/** Fundo abstrato do painel esquerdo (SVG próprio: nada de foto de banco de imagens). */
const AbstractBackdrop: React.FC = () => (
  <svg className="absolute inset-0 h-full w-full" viewBox="0 0 600 800" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
    <defs>
      <linearGradient id="login-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stopColor="#0b2a5c" />
        <stop offset="0.55" stopColor="#071733" />
        <stop offset="1" stopColor="#030a17" />
      </linearGradient>
      <radialGradient id="login-glow" cx="0.25" cy="0.3" r="0.6">
        <stop offset="0" stopColor="#1f6fe8" stopOpacity="0.55" />
        <stop offset="1" stopColor="#1f6fe8" stopOpacity="0" />
      </radialGradient>
    </defs>
    <rect width="600" height="800" fill="url(#login-bg)" />
    <rect width="600" height="800" fill="url(#login-glow)" />
    {/* Facetas em perspectiva, lembrando as linhas de um prédio, em azul translúcido. */}
    <g fill="#3f8cff">
      <polygon points="380,0 600,0 600,520 470,620" opacity="0.10" />
      <polygon points="430,0 600,0 600,300" opacity="0.10" />
      <polygon points="300,800 600,520 600,800" opacity="0.08" />
    </g>
    <g stroke="#5aa0ff" strokeOpacity="0.16" strokeWidth="1">
      {Array.from({ length: 12 }, (_, i) => (
        <line key={i} x1={360 + i * 22} y1="0" x2={250 + i * 30} y2="800" />
      ))}
      {Array.from({ length: 10 }, (_, i) => (
        <line key={`h${i}`} x1="360" y1={80 + i * 70} x2="600" y2={40 + i * 64} />
      ))}
    </g>
  </svg>
);

export const LoginPage: React.FC = () => {
  const { login, status, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [apiError, setApiError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);

  const from = (location.state as { from?: Location } | null)?.from;
  const target = from && from.pathname !== '/login' ? `${from.pathname}${from.search}` : '/';

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '', rememberMe: false } });

  if (status === 'loading') return <FullScreenLoader label="Carregando sessão..." />;
  if (status === 'authenticated' && user && !isSubmitting) {
    return <Navigate to={user.mustChangePassword ? CHANGE_PASSWORD_PATH : target} replace />;
  }

  const onSubmit = async (values: LoginForm) => {
    setApiError(null);
    try {
      const me = await login(values.email, values.password, values.rememberMe);
      navigate(me.mustChangePassword ? CHANGE_PASSWORD_PATH : target, { replace: true });
    } catch (error) {
      setApiError(authErrorMessage(error, 'Não foi possível entrar. Tente novamente.'));
    }
  };

  const input =
    'h-12 w-full rounded-md border border-line-strong bg-surface-2 pl-11 pr-11 text-sm text-ink placeholder:text-muted focus:border-primary-text focus:outline-none';

  return (
    <div className="flex min-h-screen bg-page text-ink">
      {/* Painel esquerdo (some no celular). */}
      <aside className="relative hidden w-[46%] max-w-[720px] overflow-hidden text-white lg:flex" aria-label="Sobre o SIGH">
        <AbstractBackdrop />
        <div className="relative z-10 flex w-full flex-col justify-between px-14 py-16">
          <div className="mt-[8vh]">
            <SighLogo variant="full" size={72} className="text-white" />
            <p className="mt-5 max-w-sm text-lg leading-snug text-[#dbe6ff]">Sistema Inteligente de Gestão de Histórico Disciplinar</p>
            <p className="mt-14 max-w-sm text-xl leading-snug text-white">Transparência, controle e inteligência na gestão disciplinar.</p>
            <span className="mt-6 block h-[3px] w-14 rounded bg-[#3f8cff]" aria-hidden="true" />
          </div>
          <ul className="grid grid-cols-3 divide-x divide-white/15 text-center">
            {HIGHLIGHTS.map((h) => (
              <li key={h.title} className="flex flex-col items-center gap-2 px-3">
                <span className="text-[#bcd3ff]" aria-hidden="true">
                  {h.icon}
                </span>
                <span className="text-sm text-[#dbe6ff]">{h.title}</span>
              </li>
            ))}
          </ul>
        </div>
      </aside>

      {/* Painel direito: formulário. */}
      <main className="flex flex-1 flex-col items-center justify-center bg-sidebar px-6 py-10">
        <div className="w-full max-w-[400px]">
          <div className="mb-8 flex justify-center lg:hidden">
            <SighLogo variant="full" size={48} />
          </div>
          <h1 className="text-[1.75rem] font-bold leading-tight">Bem-vindo de volta!</h1>
          <p className="mt-1 text-sm text-muted">Faça login para acessar o sistema</p>

          {apiError && (
            <Alert severity="error" sx={{ mt: 3 }}>
              {apiError}
            </Alert>
          )}

          <form noValidate onSubmit={handleSubmit(onSubmit)} className="mt-7 flex flex-col gap-4">
            <div>
              <label htmlFor="login-email" className="sr-only">
                E-mail
              </label>
              <div className="relative">
                <Mail size={18} className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted" aria-hidden="true" />
                <input
                  id="login-email"
                  type="email"
                  autoComplete="username"
                  autoFocus
                  placeholder="E-mail"
                  aria-invalid={!!errors.email}
                  aria-describedby={errors.email ? 'login-email-error' : undefined}
                  className={input}
                  {...register('email')}
                />
              </div>
              {errors.email && (
                <p id="login-email-error" className="mt-1 text-xs text-danger">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="login-password" className="sr-only">
                Senha
              </label>
              <div className="relative">
                <Lock size={18} className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted" aria-hidden="true" />
                <input
                  id="login-password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="Senha"
                  aria-invalid={!!errors.password}
                  aria-describedby={errors.password ? 'login-password-error' : undefined}
                  className={input}
                  {...register('password')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((s) => !s)}
                  aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                  aria-pressed={showPassword}
                  className="absolute right-2 top-1/2 inline-flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-md text-muted hover:text-ink"
                >
                  {showPassword ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
                </button>
              </div>
              {errors.password && (
                <p id="login-password-error" className="mt-1 text-xs text-danger">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div className="flex items-center justify-between gap-3 text-sm">
              <label className="inline-flex cursor-pointer items-center gap-2 text-ink">
                <input type="checkbox" className="h-4 w-4 accent-[var(--sigh-primary)]" {...register('rememberMe')} />
                Lembrar-me
              </label>
              <RouterLink to="/forgot-password" className="font-medium text-primary-text hover:underline">
                Esqueceu sua senha?
              </RouterLink>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="mt-2 inline-flex h-12 items-center justify-center gap-2 rounded-md bg-primary text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-70"
            >
              {isSubmitting ? (
                <CircularProgress size={20} color="inherit" aria-label="Entrando" />
              ) : (
                <>
                  <ArrowRight size={18} aria-hidden="true" /> Entrar
                </>
              )}
            </button>

            {/*
              Etapa futura: "Entrar com Google" (SSO). O espaço fica reservado aqui, com o divisor "ou" e o
              botão secundário de largura total, como na referência; até lá NÃO é renderizado.
              <div className="my-1 flex items-center gap-3 text-xs text-muted"><span className="h-px flex-1 bg-line" />ou<span className="h-px flex-1 bg-line" /></div>
              <button type="button" className="h-12 rounded-md border border-line-strong">Entrar com Google</button>
            */}
          </form>

          <p className="mt-12 text-center text-xs text-muted" data-testid="login-footer">
            SIGH v{__APP_VERSION__} • Usina Serra Grande
          </p>
        </div>
      </main>
    </div>
  );
};

export default LoginPage;
