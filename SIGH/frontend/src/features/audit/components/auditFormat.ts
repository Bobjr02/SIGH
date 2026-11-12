import type { AuditLogDto } from '../api/auditApi';

export const ACTION_LABELS: Record<string, string> = {
  Created: 'Criação',
  Updated: 'Alteração',
  Deleted: 'Exclusão',
  SoftDeleted: 'Remoção lógica',
  'Security.LoginSucceeded': 'Login',
  'Security.LoginFailed': 'Login com falha',
  'Security.AccountLocked': 'Conta bloqueada',
  'Security.PasswordChanged': 'Troca de senha',
  'Security.PasswordResetRequested': 'Pedido de redefinição de senha',
  'Security.PasswordReset': 'Redefinição de senha',
  'Security.Logout': 'Logout',
  'Security.RefreshTokenReuseDetected': 'Reuso de token detectado',
  'Security.RolesOrPermissionsChanged': 'Perfis/permissões alterados',
  'Security.EvidenceDownloaded': 'Download de evidência',
};

export const ENTITY_LABELS: Record<string, string> = {
  DisciplinaryCase: 'Processo',
  DisciplinaryOccurrence: 'Ocorrência',
  DisciplinaryCaseEmployee: 'Envolvido',
  DisciplinaryEvidence: 'Evidência',
  DisciplinaryDecision: 'Decisão',
  DisciplinaryMeasure: 'Medida',
  InfractionType: 'Tipo de infração',
  Employee: 'Funcionário',
  User: 'Usuário',
  UserRole: 'Perfil do usuário',
  Role: 'Perfil',
  Permission: 'Permissão',
  RolePermission: 'Permissão do perfil',
  AccessControl: 'Controle de acesso',
};

export const actionLabel = (action: string) => ACTION_LABELS[action] ?? action;
export const entityLabel = (entity: string) => ENTITY_LABELS[entity] ?? entity;

const parse = (json?: string | null): Record<string, unknown> => {
  if (!json) return {};
  try {
    const value = JSON.parse(json);
    return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : { valor: value };
  } catch {
    return { valor: json };
  }
};

const show = (value: unknown): string =>
  value === null || value === undefined ? '∅' : typeof value === 'object' ? JSON.stringify(value) : String(value);

/** Linhas "campo: antigo → novo" (ou só o valor, em criações e eventos). */
export const describeChanges = (log: AuditLogDto): { field: string; before?: string; after?: string }[] => {
  const before = parse(log.oldValues);
  const after = parse(log.newValues);
  const fields = Array.from(new Set([...Object.keys(before), ...Object.keys(after)]));
  return fields.map((field) => ({
    field,
    before: field in before ? show(before[field]) : undefined,
    after: field in after ? show(after[field]) : undefined,
  }));
};

export const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'medium' });
