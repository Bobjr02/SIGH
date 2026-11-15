import type { AuditLogDto } from '../api/auditApi';
import { humanize, label, LABELS, type EnumGroup } from '../../../i18n/labels';
import { EMPTY, formatBoolean, formatDate, formatDateTime as formatInstant } from '../../../utils/formatters';

/** Rótulos de ações e entidades: o mapa central (i18n/labels.ts). */
export const ACTION_LABELS = LABELS.AuditAction as Record<string, string>;
export const ENTITY_LABELS = LABELS.AuditEntity as Record<string, string>;

export const actionLabel = (action: string) => label('AuditAction', action);
export const entityLabel = (entity: string) => label('AuditEntity', entity);

/** Nomes dos campos gravados na auditoria, em português. Os que faltarem viram texto legível (humanize). */
export const FIELD_LABELS: Record<string, string> = {
  AdmissionDate: 'Data de admissão',
  AllowsTerminationRecommendation: 'Permite recomendação de desligamento',
  AppliedAt: 'Aplicada em',
  AppliedByUserId: 'Aplicada por (usuário)',
  ApprovedAt: 'Aprovada em',
  ApprovedByUserId: 'Aprovada por (usuário)',
  CancellationReason: 'Motivo do cancelamento',
  CancelledAt: 'Cancelada em',
  CaseNumber: 'Número da medida',
  ClosedAt: 'Encerrada em',
  Code: 'Código',
  CollectedAt: 'Coletado em',
  CollectedByUserId: 'Coletado por (usuário)',
  CompanyId: 'Empresa',
  ConclusionSummary: 'Resumo da conclusão',
  ConfidentialityLevel: 'Nível de sigilo',
  ContentType: 'Tipo do arquivo',
  Cpf: 'CPF',
  DecidedAt: 'Decidida em',
  DecidedByUserId: 'Decidida por (usuário)',
  DecisionType: 'Tipo de decisão',
  DefaultSeverity: 'Gravidade padrão',
  DepartmentId: 'Setor',
  Description: 'Descrição',
  DisciplinaryCaseId: 'Medida',
  DisciplinaryDecisionId: 'Decisão',
  EffectiveFrom: 'Início da vigência',
  EffectiveUntil: 'Fim da vigência',
  Email: 'E-mail',
  EmployeeId: 'Funcionário',
  EmployeeNumber: 'Matrícula',
  EvidenceType: 'Tipo de anexo',
  FileSize: 'Tamanho do arquivo',
  FileUploadedAt: 'Arquivo enviado em',
  FileUploadedByUserId: 'Arquivo enviado por (usuário)',
  FullName: 'Nome completo',
  Group: 'Grupo',
  InfractionTypeId: 'Motivo',
  IntegrityHash: 'Código de integridade',
  IsActive: 'Ativo',
  IsDeleted: 'Removido',
  IsPrimarySubject: 'Funcionário principal',
  JobTitleId: 'Cargo',
  Kind: 'Tipo de registro',
  ManagementUnitId: 'Gerência',
  ManagementUnitIdAtTime: 'Gerência na época',
  MeasureDate: 'Data da medida',
  MeasureType: 'Tipo de medida',
  MustChangePassword: 'Troca de senha obrigatória',
  Name: 'Nome',
  OccurrenceDate: 'Data da ocorrência',
  OpenedAt: 'Registrada em',
  OpenedByUserId: 'Registrada por (usuário)',
  OriginalFileName: 'Nome do arquivo',
  PasswordHash: 'Senha',
  PermissionId: 'Permissão',
  Priority: 'Prioridade',
  ProposedMeasureType: 'Medida proposta',
  Reason: 'Motivo',
  Reasoning: 'Fundamentação',
  RejectedAt: 'Rejeitada em',
  RejectedByUserId: 'Rejeitada por (usuário)',
  RejectionReason: 'Motivo da rejeição',
  ReportedAt: 'Comunicada em',
  ReportedByUserId: 'Comunicada por (usuário)',
  RequiresFormalInvestigation: 'Exige apuração formal',
  Role: 'Papel',
  RoleId: 'Perfil',
  SecurityStamp: 'Carimbo de segurança',
  Severity: 'Gravidade',
  Status: 'Situação',
  StorageReference: 'Referência do arquivo',
  Summary: 'Resumo',
  SuspensionDays: 'Dias de suspensão',
  SuspensionEnd: 'Fim da suspensão',
  SuspensionStart: 'Início da suspensão',
  Team: 'Turma',
  Title: 'Título',
  UserId: 'Usuário',
  ValidFrom: 'Início da vigência',
  ValidUntil: 'Fim da vigência',
  LockoutEnd: 'Bloqueado até',
  FailedLoginAttempts: 'Tentativas de entrada com falha',
  LastLoginAt: 'Última entrada',
  TerminationDate: 'Data de desligamento',
  TerminationReason: 'Motivo do desligamento',
  SocialName: 'Nome social',
  CorporateEmail: 'E-mail corporativo',
  // Eventos de segurança (detalhes em camelCase).
  changes: 'Alterações',
  mustChangePassword: 'Troca de senha obrigatória',
  revokedRefreshTokens: 'Sessões encerradas',
  revokedTokens: 'Sessões encerradas',
  revokedSessions: 'Sessões revogadas',
  sessionId: 'Sessão',
  reason: 'Motivo',
  email: 'E-mail',
  attempts: 'Tentativas',
  role: 'Perfil',
  managementUnit: 'Gerência',
  fileName: 'Nome do arquivo',
  valor: 'Valor',
};

export const fieldLabel = (field: string) => FIELD_LABELS[field] ?? FIELD_LABELS[field.charAt(0).toUpperCase() + field.slice(1)] ?? humanize(field);

/** Enum de cada campo; "Status" depende da entidade. */
const enumGroupOf = (field: string, entity: string): EnumGroup | undefined => {
  switch (field) {
    case 'Status':
      return (
        {
          DisciplinaryCase: 'DisciplinaryCaseStatus',
          DisciplinaryDecision: 'DecisionStatus',
          DisciplinaryMeasure: 'DisciplinaryMeasureStatus',
          DisciplinaryEvidence: 'EvidenceStatus',
          DisciplinaryOccurrence: 'OccurrenceStatus',
          User: 'UserStatus',
          Employee: 'EmployeeStatus',
        } as Record<string, EnumGroup>
      )[entity];
    case 'MeasureType':
    case 'ProposedMeasureType':
      return 'DisciplinaryMeasureType';
    case 'DecisionType':
      return 'DecisionType';
    case 'EvidenceType':
      return 'EvidenceType';
    case 'Group':
      return 'EmployeeGroup';
    case 'Kind':
      return 'DisciplinaryCaseKind';
    case 'Priority':
      return 'DisciplinaryCasePriority';
    case 'ConfidentialityLevel':
      return 'ConfidentialityLevel';
    case 'Severity':
    case 'DefaultSeverity':
      return 'InfractionSeverity';
    case 'Role':
      return entity === 'DisciplinaryCaseEmployee' ? 'CaseEmployeeRole' : 'Role';
    case 'role':
      return 'Role';
    default:
      return undefined;
  }
};

const ISO_INSTANT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/;
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Valor legível: [redacted] → "(oculto)", vazio → "—", booleano → Sim/Não, datas dd/MM/aaaa HH:mm
 * (Maceió), enums com os rótulos pt-BR.
 */
export const formatAuditValue = (value: unknown, field = '', entity = ''): string => {
  if (value === null || value === undefined || value === '') return EMPTY;
  if (value === '[redacted]') return '(oculto)';
  if (typeof value === 'boolean') return formatBoolean(value);
  if (typeof value === 'number') {
    const group = enumGroupOf(field, entity);
    return group ? label(group, value) : value.toLocaleString('pt-BR');
  }
  if (typeof value === 'string') {
    const group = enumGroupOf(field, entity);
    if (group) return label(group, value);
    if (ISO_INSTANT.test(value)) return formatInstant(value);
    if (ISO_DATE.test(value)) return formatDate(value);
    if (value === 'true' || value === 'false') return formatBoolean(value === 'true');
    return value;
  }
  if (Array.isArray(value)) return value.length === 0 ? EMPTY : value.map((v) => formatAuditValue(v, field, entity)).join(', ');
  if (typeof value === 'object') {
    return Object.entries(value as Record<string, unknown>)
      .map(([k, v]) => `${fieldLabel(k)}: ${formatAuditValue(v, k, entity)}`)
      .join('; ');
  }
  return String(value);
};

const parse = (json?: string | null): Record<string, unknown> => {
  if (!json) return {};
  try {
    const value = JSON.parse(json);
    return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : { valor: value };
  } catch {
    return { valor: json };
  }
};

/** Linhas "campo: antigo → novo" (ou só o valor, em criações e eventos), já em português. */
export const describeChanges = (log: AuditLogDto): { key: string; field: string; before?: string; after?: string }[] => {
  const before = parse(log.oldValues);
  const after = parse(log.newValues);
  const fields = Array.from(new Set([...Object.keys(before), ...Object.keys(after)]));
  return fields.map((field) => ({
    key: field,
    field: fieldLabel(field),
    before: field in before ? formatAuditValue(before[field], field, log.entity) : undefined,
    after: field in after ? formatAuditValue(after[field], field, log.entity) : undefined,
  }));
};

/** dd/MM/aaaa HH:mm no horário de Maceió. */
export const formatDateTime = (iso: string) => formatInstant(iso);
