import { z } from 'zod';
import {
  InfractionSeverity,
  CaseEmployeeRole,
  EvidenceType,
  DecisionType,
  DisciplinaryMeasureType,
} from '../types';

// Validação de UUID
const uuidSchema = z.string().refine(
  (val) => /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(val),
  { message: 'ID no formato UUID inválido.' }
);

// Schema para Tipo de Infração
export const createInfractionTypeSchema = z.object({
  code: z
    .string()
    .min(2, 'O código é obrigatório e deve conter no mínimo 2 caracteres.')
    .max(20, 'O código deve conter no máximo 20 caracteres.')
    .regex(/^[A-Za-z0-9_-]+$/, 'O código deve conter apenas letras, números, hífen ou underline.'),
  name: z
    .string()
    .min(3, 'O nome é obrigatório e deve conter no mínimo 3 caracteres.')
    .max(100, 'O nome deve conter no máximo 100 caracteres.'),
  defaultSeverity: z
    .nativeEnum(InfractionSeverity)
    .refine((val) => val !== InfractionSeverity.Undefined, { message: 'Selecione uma gravidade válida.' }),
  requiresFormalInvestigation: z.boolean().default(false),
  allowsTerminationRecommendation: z.boolean().default(false),
  description: z.string().max(500, 'A descrição deve ter no máximo 500 caracteres.').optional().or(z.literal('')),
  legalReference: z.string().max(200, 'A referência legal deve ter no máximo 200 caracteres.').optional().or(z.literal('')),
});

export const updateInfractionTypeSchema = z.object({
  name: z
    .string()
    .min(3, 'O nome é obrigatório e deve conter no mínimo 3 caracteres.')
    .max(100, 'O nome deve conter no máximo 100 caracteres.'),
  defaultSeverity: z
    .nativeEnum(InfractionSeverity)
    .refine((val) => val !== InfractionSeverity.Undefined, { message: 'Selecione uma gravidade válida.' }),
  requiresFormalInvestigation: z.boolean().default(false),
  allowsTerminationRecommendation: z.boolean().default(false),
  description: z.string().max(500, 'A descrição deve ter no máximo 500 caracteres.').optional().or(z.literal('')),
  legalReference: z.string().max(200, 'A referência legal deve ter no máximo 200 caracteres.').optional().or(z.literal('')),
});

// Schema para Criação de Processo Disciplinar
export const createDisciplinaryCaseSchema = z.object({
  caseNumber: z
    .string()
    .max(50, 'O número deve conter no máximo 50 caracteres.')
    .optional()
    .or(z.literal('')),
  companyId: uuidSchema,
  title: z
    .string()
    .min(3, 'O título é obrigatório e deve conter no mínimo 3 caracteres.')
    .max(150, 'O título deve conter no máximo 150 caracteres.'),
  description: z.string().max(2000, 'A descrição deve ter no máximo 2000 caracteres.').optional().or(z.literal('')),
  infractionTypeId: z.string().optional().or(z.literal('')),
  severity: z.nativeEnum(InfractionSeverity).optional(),
  responsibleId: z.string().optional().or(z.literal('')),
  occurredAt: z.string().optional().or(z.literal('')),
});

// Schema para Adicionar Ocorrência
export const addOccurrenceSchema = z.object({
  description: z
    .string()
    .min(5, 'A descrição da ocorrência é obrigatória (mínimo 5 caracteres).')
    .max(1000, 'A descrição deve ter no máximo 1000 caracteres.'),
  occurredAt: z.string().min(1, 'A data/hora da ocorrência é obrigatória.'),
  location: z.string().max(200, 'O local deve ter no máximo 200 caracteres.').optional().or(z.literal('')),
  infractionTypeId: z.string().optional().or(z.literal('')),
  severity: z.nativeEnum(InfractionSeverity).optional(),
});

// Schema para Adicionar Funcionário
export const addEmployeeSchema = z.object({
  employeeId: uuidSchema,
  involvementRole: z
    .nativeEnum(CaseEmployeeRole)
    .refine((val) => val !== CaseEmployeeRole.Undefined, { message: 'Selecione um papel válido.' }),
  isPrimaryAccused: z.boolean().default(false),
  notes: z.string().max(500, 'As observações devem ter no máximo 500 caracteres.').optional().or(z.literal('')),
});

// Schema para Adicionar Evidência (Apenas Metadados)
export const addEvidenceSchema = z.object({
  title: z
    .string()
    .min(3, 'O título da evidência é obrigatório (mínimo 3 caracteres).')
    .max(100, 'O título deve ter no máximo 100 caracteres.'),
  description: z
    .string()
    .min(5, 'A descrição é obrigatória (mínimo 5 caracteres).')
    .max(1000, 'A descrição deve ter no máximo 1000 caracteres.'),
  evidenceType: z
    .nativeEnum(EvidenceType)
    .refine((val) => val !== EvidenceType.Undefined, { message: 'Selecione um tipo de evidência válido.' }),
  locationReference: z.string().max(300, 'A referência/localização deve ter no máximo 300 caracteres.').optional().or(z.literal('')),
  collectedAt: z.string().min(1, 'A data de coleta é obrigatória.'),
  occurrenceId: z.string().optional().or(z.literal('')),
});

// Schema para Registrar Decisão
export const recordDecisionSchema = z.object({
  decisionType: z
    .nativeEnum(DecisionType)
    .refine((val) => val !== DecisionType.Undefined, { message: 'Selecione um tipo de decisão válido.' }),
  description: z
    .string()
    .min(10, 'A justificativa/fundamentação da decisão é obrigatória (mínimo 10 caracteres).')
    .max(2000, 'A justificativa deve ter no máximo 2000 caracteres.'),
});

// Schema para Rejeitar Decisão
export const rejectDecisionSchema = z.object({
  justification: z
    .string()
    .min(10, 'A justificativa para a rejeição é obrigatória (mínimo 10 caracteres).')
    .max(1000, 'A justificativa deve ter no máximo 1000 caracteres.'),
});

// Schema para Aplicar Medida Disciplinar
export const applyMeasureSchema = z.object({
  measureType: z
    .nativeEnum(DisciplinaryMeasureType)
    .refine((val) => val !== DisciplinaryMeasureType.Undefined, { message: 'Selecione um tipo de medida válido.' }),
  description: z
    .string()
    .min(5, 'A descrição da medida é obrigatória (mínimo 5 caracteres).')
    .max(1000, 'A descrição deve ter no máximo 1000 caracteres.'),
  employeeId: uuidSchema,
  startDate: z.string().min(1, 'A data de início é obrigatória.'),
  endDate: z.string().optional().or(z.literal('')),
});

// Schema para Cancelar Processo
export const cancelCaseSchema = z.object({
  justification: z
    .string()
    .min(10, 'A justificativa para o cancelamento é obrigatória (mínimo 10 caracteres).')
    .max(1000, 'A justificativa deve ter no máximo 1000 caracteres.'),
});

// Schema para Concluir Processo
export const concludeCaseSchema = z.object({
  summaryNotes: z
    .string()
    .min(10, 'O resumo de conclusão é obrigatório (mínimo 10 caracteres).')
    .max(2000, 'O resumo deve ter no máximo 2000 caracteres.'),
});

// Schema para Validação de Paginação e Filtros
export const paginationQuerySchema = z.object({
  page: z.number().int().positive().default(1),
  pageSize: z.number().int().min(1).max(100).default(10),
  searchTerm: z.string().optional(),
});
