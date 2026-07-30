// TypeScript DTOs e Tipos do Módulo Disciplinar (Sprint 6.4)

export interface Result<T = unknown> {
  data?: T | null;
  success: boolean;
  message?: string | null;
  errorCode?: string | null;
  errors?: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  validationErrors?: Record<string, string[]>;
  errors?: Record<string, string[]>;
}

// Enums Domain
export enum InfractionSeverity {
  Undefined = 0,
  Low = 1,
  Moderate = 2,
  High = 3,
  Critical = 4,
}

export enum DisciplinaryCaseStatus {
  Undefined = 0,
  Draft = 1,
  Open = 2,
  UnderInvestigation = 3,
  AwaitingDecision = 4,
  Decided = 5,
  Completed = 6,
  Cancelled = 7,
}

export enum DisciplinaryCasePriority {
  Undefined = 0,
  Low = 1,
  Normal = 2,
  High = 3,
  Critical = 4,
}

export enum CaseEmployeeRole {
  Undefined = 0,
  Accused = 1,
  Victim = 2,
  Witness = 3,
  Reporter = 4,
  Other = 5,
}

export enum EvidenceType {
  Undefined = 0,
  Document = 1,
  Image = 2,
  Video = 3,
  Audio = 4,
  Text = 5,
  Other = 6,
}

export enum EvidenceStatus {
  Undefined = 0,
  Draft = 1,
  Verified = 2,
  Rejected = 3,
}

export enum DecisionType {
  Undefined = 0,
  NoViolation = 1,
  InformalGuidance = 2,
  FormalWarning = 3,
  Suspension = 4,
  TerminationRecommendation = 5,
  Other = 6,
}

export enum DecisionStatus {
  Undefined = 0,
  PendingApproval = 1,
  Approved = 2,
  Rejected = 3,
}

export enum DisciplinaryMeasureType {
  Undefined = 0,
  Guidance = 1,
  VerbalWarning = 2,
  WrittenWarning = 3,
  Suspension = 4,
  TerminationRecommendation = 5,
  Other = 6,
}

export enum DisciplinaryMeasureStatus {
  Undefined = 0,
  Pending = 1,
  Applied = 2,
  Cancelled = 3,
}

export enum OccurrenceStatus {
  Undefined = 0,
  Draft = 1,
  Verified = 2,
  Investigating = 3,
  Closed = 4,
}

// DTOs para Tipos de Infrações
export interface InfractionTypeDto {
  id: string;
  code: string;
  name: string;
  defaultSeverity: InfractionSeverity;
  requiresFormalInvestigation: boolean;
  allowsTerminationRecommendation: boolean;
  description?: string;
  legalReference?: string;
  isActive: boolean;
}

export interface CreateInfractionTypeRequest {
  code: string;
  name: string;
  defaultSeverity: InfractionSeverity;
  requiresFormalInvestigation?: boolean;
  allowsTerminationRecommendation?: boolean;
  description?: string;
  legalReference?: string;
}

export interface CreateInfractionTypeResponse {
  id: string;
  code: string;
  name: string;
}

export interface UpdateInfractionTypeRequest {
  name: string;
  defaultSeverity: InfractionSeverity;
  requiresFormalInvestigation?: boolean;
  allowsTerminationRecommendation?: boolean;
  description?: string;
  legalReference?: string;
}

export interface GetInfractionTypesQuery {
  page?: number;
  pageNumber?: number;
  pageSize?: number;
  companyId?: string;
  searchTerm?: string;
  isActive?: boolean;
}

// DTOs para Processos Disciplinares
export interface OccurrenceDto {
  id: string;
  occurrenceDate: string;
  reportedAt: string;
  description: string;
  reportedByUserId: string;
  infractionTypeId: string;
  severity: InfractionSeverity;
  location?: string;
  status: OccurrenceStatus;
}

export interface CaseEmployeeDto {
  id: string;
  employeeId: string;
  role: CaseEmployeeRole;
  isPrimaryAccused: boolean;
  notes?: string;
}

export interface EvidenceDto {
  id: string;
  type: EvidenceType;
  description: string;
  collectedByUserId: string;
  collectedAt: string;
  occurrenceId?: string;
  referenceCode?: string;
  location?: string;
  status: EvidenceStatus;
}

export interface DecisionDto {
  id: string;
  type: DecisionType;
  justification: string;
  decidedByUserId: string;
  decidedAt: string;
  approvedByUserId?: string;
  approvedAt?: string;
  status: DecisionStatus;
}

export interface MeasureDto {
  id: string;
  decisionId: string;
  employeeId: string;
  type: DisciplinaryMeasureType;
  description: string;
  effectiveFrom: string;
  effectiveUntil?: string;
  appliedByUserId: string;
  appliedAt: string;
  status: DisciplinaryMeasureStatus;
}

export interface DisciplinaryCaseSummaryDto {
  id: string;
  caseNumber: string;
  companyId: string;
  title: string;
  status: DisciplinaryCaseStatus;
  priority: DisciplinaryCasePriority;
  openedAt: string;
  openedByUserId: string;
  responsibleEmployeeId?: string;
  dueDate?: string;
  closedAt?: string;
  occurrencesCount: number;
  employeesCount: number;
}

export interface DisciplinaryCaseDetailDto {
  id: string;
  caseNumber: string;
  companyId: string;
  title: string;
  description: string;
  status: DisciplinaryCaseStatus;
  priority: DisciplinaryCasePriority;
  openedAt: string;
  openedByUserId: string;
  responsibleEmployeeId?: string;
  dueDate?: string;
  closedAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  conclusionSummary?: string;
  occurrences: OccurrenceDto[];
  employees: CaseEmployeeDto[];
  evidences: EvidenceDto[];
  decisions: DecisionDto[];
  measures: MeasureDto[];
}

export interface GetDisciplinaryCaseByIdResponse {
  caseDetail: DisciplinaryCaseDetailDto;
}

export interface CreateDisciplinaryCaseRequest {
  caseNumber?: string;
  companyId: string;
  title: string;
  description?: string;
  infractionTypeId?: string;
  severity?: InfractionSeverity;
  priority?: DisciplinaryCasePriority;
  responsibleId?: string;
  responsibleEmployeeId?: string;
  occurredAt?: string;
}

export interface CreateDisciplinaryCaseResponse {
  id: string;
  caseNumber: string;
}

export interface GetDisciplinaryCasesQuery {
  page?: number;
  pageNumber?: number;
  pageSize?: number;
  companyId?: string;
  status?: DisciplinaryCaseStatus;
  severity?: InfractionSeverity;
  priority?: DisciplinaryCasePriority | InfractionSeverity;
  searchTerm?: string;
  startDate?: string;
  endDate?: string;
  dateFrom?: string;
  dateTo?: string;
  employeeId?: string;
  responsibleId?: string;
  responsibleEmployeeId?: string;
}

// Request Payload DTOs das Ações de Processo
export interface OpenCaseRequest {
  openedByUserId?: string;
  justification?: string;
}

export interface StartInvestigationRequest {
  investigatorUserId?: string;
  notes?: string;
}

export interface SubmitCaseForDecisionRequest {
  submittedByUserId?: string;
  summaryNotes?: string;
}

export interface RecordDecisionRequest {
  decisionType: DecisionType;
  description: string;
  decidedByUserId?: string;
}

export interface ApproveDecisionRequest {
  approvedByUserId?: string;
  notes?: string;
}

export interface RejectDecisionRequest {
  rejectedByUserId?: string;
  justification: string;
}

export interface AddOccurrenceRequest {
  description: string;
  occurredAt: string;
  location?: string;
  infractionTypeId?: string;
  severity?: InfractionSeverity;
  reportedByUserId?: string;
}

export interface AddEmployeeRequest {
  employeeId: string;
  involvementRole: CaseEmployeeRole;
  isPrimaryAccused?: boolean;
  notes?: string;
}

export interface AddEvidenceRequest {
  title: string;
  description: string;
  evidenceType: EvidenceType;
  locationReference?: string;
  collectedAt: string;
  occurrenceId?: string;
  collectedByUserId?: string;
}

export interface ApplyMeasureRequest {
  measureType: DisciplinaryMeasureType;
  description: string;
  employeeId: string;
  decisionId?: string;
  startDate: string;
  endDate?: string;
  appliedByUserId?: string;
}

export interface CancelCaseRequest {
  cancelledByUserId?: string;
  justification: string;
}

export interface ConcludeCaseRequest {
  concludedByUserId?: string;
  summaryNotes: string;
}
