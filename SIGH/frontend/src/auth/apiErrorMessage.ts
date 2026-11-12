import { isAxiosError } from 'axios';

interface ProblemBody {
  detail?: string;
  title?: string;
  message?: string;
  validationErrors?: Record<string, string[]>;
}

/** Converte o erro de uma chamada de autenticação numa mensagem para o usuário. */
export const authErrorMessage = (error: unknown, fallback: string): string => {
  if (!isAxiosError<ProblemBody>(error)) return fallback;

  const status = error.response?.status;
  if (!error.response) return 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';
  if (status === 429) return 'Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente.';

  const body = error.response.data;
  const firstValidationError = body?.validationErrors ? Object.values(body.validationErrors).flat()[0] : undefined;
  return body?.detail || firstValidationError || body?.message || fallback;
};
