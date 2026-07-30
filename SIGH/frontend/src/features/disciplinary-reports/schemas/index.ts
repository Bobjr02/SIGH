import { z } from 'zod';

const guidSchema = z.string().refine(
  (val) => !val || /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(val),
  { message: 'GUID em formato inválido.' }
);

export const dashboardFiltersSchema = z.object({
  companyId: guidSchema.optional().or(z.literal('')),
  dateFrom: z.string().optional().or(z.literal('')),
  dateTo: z.string().optional().or(z.literal('')),
  departmentId: guidSchema.optional().or(z.literal('')),
  status: z.string().optional().or(z.literal('')),
  priority: z.string().optional().or(z.literal('')),
  responsibleEmployeeId: guidSchema.optional().or(z.literal('')),
});

export const caseReportFiltersSchema = z.object({
  companyId: guidSchema.optional().or(z.literal('')),
  departmentId: guidSchema.optional().or(z.literal('')),
  employeeId: guidSchema.optional().or(z.literal('')),
  responsibleEmployeeId: guidSchema.optional().or(z.literal('')),
  infractionTypeId: guidSchema.optional().or(z.literal('')),
  status: z.string().optional().or(z.literal('')),
  priority: z.string().optional().or(z.literal('')),
  dateFrom: z.string().optional().or(z.literal('')),
  dateTo: z.string().optional().or(z.literal('')),
  searchTerm: z.string().optional().or(z.literal('')),
  pageNumber: z.number().min(1).default(1),
  pageSize: z.number().min(1).max(100).default(10),
  sortBy: z.string().optional(),
  sortDirection: z.enum(['asc', 'desc']).optional(),
});

export const measureReportFiltersSchema = z.object({
  companyId: guidSchema.optional().or(z.literal('')),
  employeeId: guidSchema.optional().or(z.literal('')),
  caseId: guidSchema.optional().or(z.literal('')),
  measureType: z.string().optional().or(z.literal('')),
  appliedFrom: z.string().optional().or(z.literal('')),
  appliedTo: z.string().optional().or(z.literal('')),
  searchTerm: z.string().optional().or(z.literal('')),
  pageNumber: z.number().min(1).default(1),
  pageSize: z.number().min(1).max(100).default(10),
});

export const employeeHistoryFiltersSchema = z.object({
  companyId: guidSchema.optional().or(z.literal('')),
  dateFrom: z.string().optional().or(z.literal('')),
  dateTo: z.string().optional().or(z.literal('')),
  status: z.string().optional().or(z.literal('')),
  pageNumber: z.number().min(1).default(1),
  pageSize: z.number().min(1).max(100).default(10),
});
