import React from 'react';
import { Chip } from '@mui/material';
import { label } from '../../i18n/labels';

export type Tone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

const STYLES: Record<Tone, { bg: string; fg: string }> = {
  success: { bg: 'var(--sigh-success-solid)', fg: '#ffffff' },
  warning: { bg: 'var(--sigh-warning-solid)', fg: 'var(--sigh-on-warning-solid)' },
  danger: { bg: 'var(--sigh-danger-solid)', fg: '#ffffff' },
  neutral: { bg: 'var(--sigh-neutral-solid)', fg: '#ffffff' },
  info: { bg: 'var(--sigh-info-solid)', fg: '#ffffff' },
};

/** Chip de situação colorido (verde Ativo/Aprovada, amarelo Aguardando, vermelho Bloqueado/Rejeitada, cinza Cancelada). */
export const StatusChip: React.FC<{ tone: Tone; label: string; testId?: string; size?: 'small' | 'medium' }> = ({
  tone,
  label: text,
  testId,
  size = 'small',
}) => (
  <Chip
    size={size}
    label={text}
    data-testid={testId}
    data-tone={tone}
    sx={{ backgroundColor: STYLES[tone].bg, color: STYLES[tone].fg, fontWeight: 600, borderRadius: '6px' }}
  />
);

/** Situação da medida (número do RecordStatus). */
export const recordTone = (status: number): Tone =>
  ({ 1: 'warning', 2: 'danger', 3: 'success', 4: 'neutral', 5: 'info' } as Record<number, Tone>)[status] ?? 'neutral';

export const RecordStatusChip: React.FC<{ status: number }> = ({ status }) => (
  <StatusChip tone={recordTone(status)} label={label('RecordStatus', status)} testId="record-status" />
);

/** Situação do usuário (nome do UserStatus). */
export const userTone = (status: string): Tone =>
  ({ Active: 'success', PendingFirstAccess: 'warning', Locked: 'danger', Suspended: 'danger', Inactive: 'neutral' } as Record<string, Tone>)[status] ??
  'neutral';

export const UserStatusChip: React.FC<{ status: string }> = ({ status }) => (
  <StatusChip tone={userTone(status)} label={label('UserStatus', status)} testId="user-status" />
);

/** Situação do funcionário (número do EmployeeStatus). */
export const employeeTone = (status: number): Tone =>
  ({ 1: 'warning', 2: 'success', 3: 'info', 4: 'neutral', 5: 'danger' } as Record<number, Tone>)[status] ?? 'neutral';

export const EmployeeStatusChip: React.FC<{ status: number }> = ({ status }) => (
  <StatusChip tone={employeeTone(status)} label={label('EmployeeStatus', status)} testId="employee-status" />
);
