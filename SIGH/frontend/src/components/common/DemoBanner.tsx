import React, { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Box, Typography } from '@mui/material';
import { api } from '../../services/api';

interface HealthResult {
  success: boolean;
  data?: { status: string; demo?: boolean };
}

/** GET /api/v1/health (público): demo = true em Development ou Demo. */
export const fetchDemoMode = async (): Promise<boolean> => {
  const response = await api.get<HealthResult>('/v1/health', { skipAuthRefresh: true });
  return response.data.data?.demo === true;
};

/**
 * Faixa fixa no topo de todas as telas (inclusive o login) quando a API informa ambiente de demonstração:
 * os dados fictícios nunca podem ser confundidos com reais.
 */
export const DemoBanner: React.FC = () => {
  const { data: demo } = useQuery({ queryKey: ['health', 'demo'], queryFn: fetchDemoMode, staleTime: Infinity, retry: false });
  // O menu lateral e a barra superior (sticky) descontam a altura da faixa.
  useEffect(() => {
    const root = document.documentElement;
    if (demo) root.style.setProperty('--sigh-banner-height', '28px');
    else root.style.removeProperty('--sigh-banner-height');
  }, [demo]);
  if (!demo) return null;

  return (
    <Box
      role="status"
      data-testid="demo-banner"
      sx={{ position: 'sticky', top: 0, zIndex: 2000, height: 28, boxSizing: 'border-box', py: 0.5, px: 2, textAlign: 'center', backgroundColor: '#b45309', color: '#ffffff' }}
    >
      <Typography variant="body2" sx={{ fontWeight: 800, letterSpacing: '0.08em' }}>
        AMBIENTE DE DEMONSTRAÇÃO — dados fictícios
      </Typography>
    </Box>
  );
};
