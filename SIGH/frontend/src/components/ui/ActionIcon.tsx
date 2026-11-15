import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { IconButton, Tooltip } from '@mui/material';

/** Ação de linha com ícone (olho = ver, lápis = editar) e dica em português. */
export const ActionIcon: React.FC<{ title: string; icon: React.ReactNode; to?: string; onClick?: () => void; testId?: string }> = ({
  title,
  icon,
  to,
  onClick,
  testId,
}) => (
  <Tooltip title={title}>
    {to ? (
      <IconButton component={RouterLink} to={to} aria-label={title} size="small" data-testid={testId}>
        {icon}
      </IconButton>
    ) : (
      <IconButton onClick={onClick} aria-label={title} size="small" data-testid={testId}>
        {icon}
      </IconButton>
    )}
  </Tooltip>
);
