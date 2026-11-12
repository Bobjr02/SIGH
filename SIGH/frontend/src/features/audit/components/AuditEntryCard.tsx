import React from 'react';
import { Box, Chip, Paper, Typography } from '@mui/material';
import type { AuditLogDto } from '../api/auditApi';
import { actionLabel, describeChanges, entityLabel, formatDateTime } from './auditFormat';

const actionColor = (action: string): 'success' | 'info' | 'warning' | 'error' | 'default' => {
  if (action === 'Created') return 'success';
  if (action === 'Updated') return 'info';
  if (action === 'SoftDeleted' || action === 'Deleted') return 'warning';
  if (action.includes('Failed') || action.includes('Reuse') || action.includes('Locked')) return 'error';
  return 'default';
};

export const AuditEntryCard: React.FC<{ log: AuditLogDto }> = ({ log }) => {
  const changes = describeChanges(log);
  return (
    <Paper elevation={0} sx={{ p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 2 }} data-testid="audit-entry">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap', mb: changes.length ? 1 : 0 }}>
        <Chip size="small" color={actionColor(log.action)} label={actionLabel(log.action)} />
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {entityLabel(log.entity)}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {formatDateTime(log.createdAt)} · {log.userName ?? 'Sistema'}
          {log.ipAddress ? ` · IP ${log.ipAddress}` : ''}
        </Typography>
      </Box>
      {changes.length > 0 && (
        <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
          {changes.map((c) => (
            <Typography component="li" variant="caption" key={c.field} sx={{ display: 'list-item' }}>
              <strong>{c.field}</strong>:{' '}
              {c.before !== undefined && c.after !== undefined ? `${c.before} → ${c.after}` : (c.after ?? c.before)}
            </Typography>
          ))}
        </Box>
      )}
    </Paper>
  );
};
