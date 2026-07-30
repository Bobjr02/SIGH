import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import { InfractionSeverity, DisciplinaryCasePriority } from '../types';

interface CaseSeverityChipProps {
  severity: InfractionSeverity | DisciplinaryCasePriority | number;
  size?: ChipProps['size'];
}

export const getSeverityLabel = (sev: InfractionSeverity | DisciplinaryCasePriority | number): string => {
  switch (Number(sev)) {
    case InfractionSeverity.Low:
      return 'Baixa';
    case InfractionSeverity.Moderate:
      return 'Média';
    case InfractionSeverity.High:
      return 'Alta';
    case InfractionSeverity.Critical:
      return 'Crítica';
    default:
      return 'Não Definida';
  }
};

export const getSeverityColor = (sev: InfractionSeverity | DisciplinaryCasePriority | number): ChipProps['color'] => {
  switch (Number(sev)) {
    case InfractionSeverity.Low:
      return 'success';
    case InfractionSeverity.Moderate:
      return 'info';
    case InfractionSeverity.High:
      return 'warning';
    case InfractionSeverity.Critical:
      return 'error';
    default:
      return 'default';
  }
};

export const CaseSeverityChip: React.FC<CaseSeverityChipProps> = ({ severity, size = 'small' }) => {
  return (
    <Chip
      label={getSeverityLabel(severity)}
      color={getSeverityColor(severity)}
      size={size}
      variant="outlined"
      sx={{ fontWeight: 600 }}
    />
  );
};
