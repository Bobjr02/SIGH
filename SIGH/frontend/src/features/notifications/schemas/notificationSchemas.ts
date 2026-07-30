import { z } from 'zod';

export const notificationFiltersSchema = z.object({
  pageNumber: z.number().int().min(1).default(1),
  pageSize: z.number().int().min(1).max(100).default(10),
  isRead: z.boolean().nullable().optional(),
  type: z.string().nullable().optional(),
  priority: z.string().nullable().optional(),
  startDate: z.string().nullable().optional(),
  endDate: z.string().nullable().optional(),
  sortBy: z.enum(['CreatedAt', 'Priority', 'Type', 'IsRead', 'DueDate']).default('CreatedAt'),
  sortDirection: z.enum(['asc', 'desc']).default('desc'),
  companyId: z.string().uuid().optional(),
});

export type NotificationFiltersSchemaType = z.infer<typeof notificationFiltersSchema>;
