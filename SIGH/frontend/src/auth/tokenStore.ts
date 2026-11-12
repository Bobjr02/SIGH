// O access token fica somente em memória (nunca em localStorage/sessionStorage).
// Ao recarregar a página ele se perde e a sessão é restaurada via POST /auth/refresh,
// que usa o cookie HttpOnly sigh_rt.
let accessToken: string | null = null;

export const tokenStore = {
  get: (): string | null => accessToken,
  set: (token: string): void => {
    accessToken = token;
  },
  clear: (): void => {
    accessToken = null;
  },
};
