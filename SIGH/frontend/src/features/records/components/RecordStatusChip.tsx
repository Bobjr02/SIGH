import React from 'react';
import { Chip } from '@mui/material';
import { RECORD_STATUS_LABELS, RecordStatus } from '../api/recordsApi';

const COLORS: Record<RecordStatus, 'warning' | 'error' | 'success' | 'default' | 'info'> = {
  [RecordStatus.PendingApproval]: 'warning',
  [RecordStatus.Rejected]: 'error',
  [RecordStatus.Approved]: 'success',
  [RecordStatus.Cancelled]: 'default',
  [RecordStatus.Historical]: 'info',
};

export const RecordStatusChip: React.FC<{ status: RecordStatus }> = ({ status }) => (
  <Chip size="small" color={COLORS[status]} label={RECORD_STATUS_LABELS[status]} data-testid="record-status" />
);
