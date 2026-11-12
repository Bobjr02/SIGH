import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Button, Paper, Typography } from '@mui/material';
import { ShieldOff } from 'lucide-react';

export const ForbiddenPage: React.FC = () => {
  const navigate = useNavigate();

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
      <Paper elevation={0} sx={{ p: 5, maxWidth: 480, textAlign: 'center', border: '1px solid', borderColor: 'divider' }}>
        <ShieldOff size={48} color="#e11d48" style={{ marginBottom: 12 }} />
        <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
          403 — Acesso negado
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Você não tem permissão para acessar esta página. Se precisar de acesso, fale com o administrador do sistema.
        </Typography>
        <Button variant="contained" onClick={() => navigate('/')}>
          Voltar ao início
        </Button>
      </Paper>
    </Box>
  );
};
