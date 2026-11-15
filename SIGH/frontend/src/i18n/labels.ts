import enumValues from './enum-values.json';

/**
 * Rótulos em português de TODOS os enums que chegam do backend. Um único lugar: telas, chips, filtros,
 * gráficos e exportações usam `label(grupo, valor)`. O teste enumLabels.test.ts falha se algum valor de
 * enum-values.json (gerado a partir do backend) ficar sem rótulo.
 */
export type EnumGroup = keyof typeof enumValues;

export const LABELS: { [G in EnumGroup]: Record<keyof (typeof enumValues)[G] & string, string> } = {
  RecordStatus: {
    PendingApproval: 'Aguardando aprovação',
    Rejected: 'Rejeitada',
    Approved: 'Aprovada',
    Cancelled: 'Cancelada',
    Historical: 'Histórico importado',
  },
  RecordOrigin: {
    Sigh: 'SIGH',
    Historical: 'Histórico',
  },
  RecordActivityKind: {
    Registered: 'Medida registrada',
    Approved: 'Medida aprovada',
    Rejected: 'Medida rejeitada',
  },
  OverviewPeriod: {
    Last30Days: 'Últimos 30 dias',
    Last90Days: 'Últimos 90 dias',
    Last12Months: 'Últimos 12 meses',
    CurrentYear: 'Ano atual',
  },
  DisciplinaryMeasureType: {
    Undefined: 'Não definida',
    Guidance: 'Orientação',
    VerbalWarning: 'Advertência verbal',
    WrittenWarning: 'Advertência escrita',
    Suspension: 'Suspensão',
    TerminationRecommendation: 'Recomendação de desligamento',
    Other: 'Outra',
    JustCauseTermination: 'Demissão por justa causa',
  },
  DisciplinaryMeasureStatus: {
    Undefined: 'Não definida',
    Pending: 'Pendente',
    Applied: 'Aplicada',
    Completed: 'Concluída',
    Cancelled: 'Cancelada',
  },
  DisciplinaryCaseStatus: {
    Undefined: 'Não definida',
    Draft: 'Rascunho',
    Open: 'Aberta',
    UnderInvestigation: 'Em apuração',
    AwaitingDecision: 'Aguardando aprovação',
    Decided: 'Decidida',
    Completed: 'Concluída',
    Cancelled: 'Cancelada',
  },
  DecisionStatus: {
    Undefined: 'Não definida',
    Draft: 'Rascunho',
    PendingApproval: 'Aguardando aprovação',
    Approved: 'Aprovada',
    Rejected: 'Rejeitada',
    Invalidated: 'Invalidada',
  },
  EvidenceType: {
    Undefined: 'Não definido',
    Document: 'Documento',
    Image: 'Imagem',
    Video: 'Vídeo',
    Audio: 'Áudio',
    Text: 'Texto',
    Other: 'Outro',
  },
  UserStatus: {
    Active: 'Ativo',
    Inactive: 'Inativo',
    Locked: 'Bloqueado',
    PendingFirstAccess: 'Aguardando primeiro acesso',
    Suspended: 'Suspenso',
  },
  ValidityState: {
    Active: 'Vigente',
    Scheduled: 'Agendado',
    Expired: 'Expirado',
  },
  EmployeeStatus: {
    PendingAdmission: 'Admissão pendente',
    Active: 'Ativo',
    OnLeave: 'Afastado',
    Inactive: 'Inativo',
    Terminated: 'Desligado',
  },
  EmployeeGroup: {
    Field: 'Campo',
    Industry: 'Indústria',
  },
  Role: {
    Administrator: 'Administrador',
    HrRegistrar: 'RH – Registro',
    HrApprover: 'RH – Aprovador',
    Legal: 'Jurídico',
    Manager: 'Gestor',
  },
  AuditAction: {
    Created: 'Criação',
    Updated: 'Alteração',
    Deleted: 'Exclusão',
    SoftDeleted: 'Remoção',
    'Security.LoginSucceeded': 'Entrada no sistema',
    'Security.LoginFailed': 'Tentativa de entrada recusada',
    'Security.AccountLocked': 'Conta bloqueada',
    'Security.PasswordChanged': 'Troca de senha',
    'Security.PasswordResetRequested': 'Pedido de redefinição de senha',
    'Security.PasswordReset': 'Redefinição de senha',
    'Security.Logout': 'Saída do sistema',
    'Security.RefreshTokenReuseDetected': 'Reuso de sessão detectado',
    'Security.RolesOrPermissionsChanged': 'Perfis ou permissões alterados',
    'Security.EvidenceDownloaded': 'Download de anexo',
    'Security.RoleAssigned': 'Perfil atribuído',
    'Security.RoleRevoked': 'Perfil retirado',
    'Security.ManagementUnitAssigned': 'Gerência atribuída',
    'Security.ManagementUnitRevoked': 'Gerência retirada',
    'Security.RefreshTokensRevoked': 'Sessões encerradas',
    'History.ImportClosed': 'Importação do histórico encerrada',
    'History.ImportClosedByConfiguration': 'Importação do histórico encerrada pela configuração',
    'History.ImportReopenedByConfiguration': 'Importação do histórico reaberta pela configuração',
    'History.ImportApplied': 'Histórico importado',
  },
  AuditEntity: {
    AccessControl: 'Controle de acesso',
    DisciplinaryCase: 'Medida',
    DisciplinaryCaseEmployee: 'Funcionário da medida',
    DisciplinaryDecision: 'Decisão',
    DisciplinaryEvidence: 'Anexo',
    DisciplinaryMeasure: 'Aplicação da medida',
    DisciplinaryOccurrence: 'Ocorrência',
    Employee: 'Funcionário',
    HistoryImportState: 'Importação do histórico',
    InfractionType: 'Motivo',
    ManagementUnit: 'Gerência',
    Permission: 'Permissão',
    Role: 'Perfil',
    RolePermission: 'Permissão do perfil',
    User: 'Usuário',
    UserManagementUnit: 'Gerência do usuário',
    UserRole: 'Perfil do usuário',
  },
  NotificationType: {
    Info: 'Informação',
    Warning: 'Alerta',
    RecordRegistered: 'Medida registrada',
    RecordApproved: 'Medida aprovada',
    RecordRejected: 'Medida rejeitada',
    RecordPendingApproval: 'Medidas aguardando aprovação',
  },
  NotificationPriority: {
    Low: 'Baixa',
    Medium: 'Média',
    High: 'Alta',
    Urgent: 'Urgente',
  },
};

const VALUES = enumValues as Record<EnumGroup, Record<string, number | string>>;

/** Nome do valor (ex.: 3 → "Approved"), a partir do número ou do próprio nome. */
export const enumName = (group: EnumGroup, value: number | string | null | undefined): string | undefined => {
  if (value === null || value === undefined || value === '') return undefined;
  const entries = Object.entries(VALUES[group]);
  const byValue = entries.find(([, v]) => v === value || String(v) === String(value));
  if (byValue) return byValue[0];
  const byName = entries.find(([name]) => name.toLowerCase() === String(value).toLowerCase());
  return byName?.[0];
};

/** Rótulo em português. Valor desconhecido: o texto legível, nunca o código cru. */
export const label = (group: EnumGroup, value: number | string | null | undefined): string => {
  if (value === null || value === undefined || value === '') return '—';
  const name = enumName(group, value);
  const labels = LABELS[group] as Record<string, string>;
  if (name && labels[name]) return labels[name];
  return typeof value === 'number' ? '—' : humanize(String(value));
};

/** Opções de filtro (valor numérico + rótulo), na ordem do backend. */
export const options = (group: EnumGroup, exclude: string[] = []): { value: number | string; label: string }[] =>
  Object.entries(VALUES[group])
    .filter(([name]) => !exclude.includes(name))
    .map(([name, value]) => ({ value, label: (LABELS[group] as Record<string, string>)[name] }));

/** "MustChangePassword" → "Must change password" (fallback: nunca exibir o nome técnico colado). */
export const humanize = (technical: string): string => {
  const words = technical
    .replace(/^.*\./, '')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ')
    .trim()
    .toLowerCase();
  return words ? words.charAt(0).toUpperCase() + words.slice(1) : technical;
};
