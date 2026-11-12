import React from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';

export const FullScreenLoader: React.FC<{ label?: string }> = ({ label = 'Carregando...' }) => (
  <Box
    role="status"
    aria-live="polite"
    sx={{ minHeight: '60vh', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 2 }}
  >
    <CircularProgress />
    <Typography variant="body2" color="text.secondary">
      {label}
    </Typography>
  </Box>
);
