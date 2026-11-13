import { api } from '../../../services/api';

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
}

const unwrap = <T,>(result: Result<T>, fallback: string): T => {
  if (!result.success || result.data === undefined) throw new Error(result.message || fallback);
  return result.data;
};

/** Espelha UserStatus. */
export enum UserStatus {
  Active = 1,
  Inactive = 2,
  Locked = 3,
  PendingFirstAccess = 4,
  Suspended = 5,
}

export const USER_STATUS_LABELS: Record<string, string> = {
  Active: 'Ativo',
  Inactive: 'Inativo',
  Locked: 'Bloqueado',
  PendingFirstAccess: 'Primeiro acesso pendente',
  Suspended: 'Suspenso',
};

/** Espelha ValidityState. */
export enum ValidityState {
  Active = 1,
  Scheduled = 2,
  Expired = 3,
}

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  cpf: string;
  status: string;
  mustChangePassword: boolean;
  roles: string[];
  createdAt: string;
}

export interface UserDetail extends UserSummary {
  failedLoginAttempts: number;
  lockoutEnd?: string | null;
  lastLoginAt?: string | null;
  permissions: string[];
}

export interface PagedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RoleOption {
  id: string;
  name: string;
  label: string;
  description: string;
  permissions: string[];
}

export interface UserRoleLink {
  id: string;
  roleId: string;
  roleName: string;
  roleLabel: string;
  validFrom?: string | null;
  validUntil?: string | null;
  state: ValidityState;
}

export interface UserUnitLink {
  id: string;
  managementUnitId: string;
  code?: string | null;
  name: string;
  validFrom?: string | null;
  validUntil?: string | null;
  state: ValidityState;
}

export interface UserAccess {
  userId: string;
  roles: UserRoleLink[];
  managementUnits: UserUnitLink[];
}

export interface ManagementUnit {
  id: string;
  code?: string | null;
  name: string;
  isActive: boolean;
}

export interface CreateUserInput {
  fullName: string;
  email: string;
  cpf: string;
  roleIds: string[];
  managementUnitIds: string[];
}

export interface Validity {
  validFrom?: string | null;
  validUntil?: string | null;
}

export const adminKeys = {
  users: (search: string, page: number) => ['admin', 'users', search, page] as const,
  user: (id: string) => ['admin', 'user', id] as const,
  access: (id: string) => ['admin', 'access', id] as const,
  roles: ['admin', 'roles'] as const,
  units: ['admin', 'management-units'] as const,
};

export const adminApi = {
  async users(searchTerm: string, pageNumber: number): Promise<PagedList<UserSummary>> {
    const response = await api.get<Result<PagedList<UserSummary>>>('/v1/users', {
      params: { searchTerm: searchTerm || undefined, pageNumber, pageSize: 20 },
    });
    return unwrap(response.data, 'Erro ao listar usuários.');
  },

  async user(id: string): Promise<UserDetail> {
    const response = await api.get<Result<UserDetail>>(`/v1/users/${id}`);
    return unwrap(response.data, 'Erro ao carregar o usuário.');
  },

  async createUser(input: CreateUserInput): Promise<{ id: string }> {
    const response = await api.post<Result<{ id: string }>>('/v1/users', { ...input, initialPassword: null });
    return unwrap(response.data, 'Erro ao criar o usuário.');
  },

  async changeStatus(id: string, newStatus: UserStatus, reason: string): Promise<void> {
    const response = await api.patch<Result<unknown>>(`/v1/users/${id}/status`, { userId: id, newStatus, reason });
    unwrap(response.data, 'Erro ao alterar o status.');
  },

  async unlock(id: string): Promise<void> {
    const response = await api.post<Result<unknown>>(`/v1/users/${id}/unlock`);
    unwrap(response.data, 'Erro ao desbloquear.');
  },

  async roles(): Promise<RoleOption[]> {
    const response = await api.get<Result<RoleOption[]>>('/v1/roles');
    return unwrap(response.data, 'Erro ao carregar os perfis.');
  },

  async access(id: string): Promise<UserAccess> {
    const response = await api.get<Result<UserAccess>>(`/v1/users/${id}/access`);
    return unwrap(response.data, 'Erro ao carregar os perfis do usuário.');
  },

  async assignRole(userId: string, roleId: string, validity: Validity): Promise<UserAccess> {
    const response = await api.post<Result<UserAccess>>(`/v1/users/${userId}/roles`, { roleId, ...validity });
    return unwrap(response.data, 'Erro ao atribuir o perfil.');
  },

  async revokeRole(userId: string, linkId: string): Promise<UserAccess> {
    const response = await api.delete<Result<UserAccess>>(`/v1/users/${userId}/roles/${linkId}`);
    return unwrap(response.data, 'Erro ao retirar o perfil.');
  },

  async assignUnit(userId: string, managementUnitId: string, validity: Validity): Promise<UserAccess> {
    const response = await api.post<Result<UserAccess>>(`/v1/users/${userId}/management-units`, { managementUnitId, ...validity });
    return unwrap(response.data, 'Erro ao vincular a gerência.');
  },

  async revokeUnit(userId: string, linkId: string): Promise<UserAccess> {
    const response = await api.delete<Result<UserAccess>>(`/v1/users/${userId}/management-units/${linkId}`);
    return unwrap(response.data, 'Erro ao retirar a gerência.');
  },

  async units(): Promise<ManagementUnit[]> {
    const response = await api.get<Result<ManagementUnit[]>>('/v1/management-units');
    return unwrap(response.data, 'Erro ao carregar as gerências.');
  },

  async renameUnit(id: string, name: string): Promise<ManagementUnit> {
    const response = await api.put<Result<ManagementUnit>>(`/v1/management-units/${id}`, { name });
    return unwrap(response.data, 'Erro ao salvar a gerência.');
  },
};

/**
 * Validade em datas de calendário (inclusivas) ↔ instantes do servidor [ValidFrom, ValidUntil).
 * "Até 20/12" vira ValidUntil = 21/12 00:00 local; na exibição, volta a "20/12".
 */
export const validityFromDates = (from: string, until: string): Validity => ({
  validFrom: from ? new Date(`${from}T00:00:00`).toISOString() : null,
  validUntil: until ? new Date(new Date(`${until}T00:00:00`).getTime() + 86_400_000).toISOString() : null,
});

const pad = (n: number) => String(n).padStart(2, '0');
const localDate = (d: Date) => `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;

export const formatValidity = (validFrom?: string | null, validUntil?: string | null): string => {
  const from = validFrom ? localDate(new Date(validFrom)) : null;
  const until = validUntil ? localDate(new Date(new Date(validUntil).getTime() - 1)) : null;
  if (from && until) return `de ${from} até ${until}`;
  if (from) return `a partir de ${from}`;
  if (until) return `até ${until}`;
  return 'sem prazo';
};

export const VALIDITY_LABELS: Record<ValidityState, string> = {
  [ValidityState.Active]: 'Vigente',
  [ValidityState.Scheduled]: 'Ainda vai começar',
  [ValidityState.Expired]: 'Vencido',
};
