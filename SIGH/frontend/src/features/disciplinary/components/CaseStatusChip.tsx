import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import { DisciplinaryCaseStatus } from '../types';

interface CaseStatusChipProps {
  status: DisciplinaryCaseStatus | number;
  size?: ChipProps['size'];
}

export const getStatusLabel = (status: DisciplinaryCaseStatus | number): string => {
  switch (Number(status)) {
    case DisciplinaryCaseStatus.Draft:
      return 'Rascunho';
    case DisciplinaryCaseStatus.Open:
      return 'Aberto';
    case DisciplinaryCaseStatus.UnderInvestigation:
      return 'Em Investigação';
    case DisciplinaryCaseStatus.AwaitingDecision:
      return 'Aguardando Decisão';
    case DisciplinaryCaseStatus.Decided:
      return 'Decidido';
    case DisciplinaryCaseStatus.Completed:
      return 'Concluído';
    case DisciplinaryCaseStatus.Cancelled:
      return 'Cancelado';
    default:
      return 'Não Definido';
  }
};

export const getStatusColor = (status: DisciplinaryCaseStatus | number): ChipProps['color'] => {
  switch (Number(status)) {
    case DisciplinaryCaseStatus.Draft:
      return 'default';
    case DisciplinaryCaseStatus.Open:
      return 'info';
    case DisciplinaryCaseStatus.UnderInvestigation:
      return 'warning';
    case DisciplinaryCaseStatus.AwaitingDecision:
      return 'secondary';
    case DisciplinaryCaseStatus.Decided:
      return 'primary';
    case DisciplinaryCaseStatus.Completed:
      return 'success';
    case DisciplinaryCaseStatus.Cancelled:
      return 'error';
    default:
      return 'default';
  }
};

export const CaseStatusChip: React.FC<CaseStatusChipProps> = ({ status, size = 'small' }) => {
  return (
    <Chip
      label={getStatusLabel(status)}
      color={getStatusColor(status)}
      size={size}
      sx={{ fontWeight: 600 }}
    />
  );
};
