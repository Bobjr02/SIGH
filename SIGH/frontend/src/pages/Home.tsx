import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Paper, Typography, Box, Button, Card, CardContent } from '@mui/material';
import { Shield, BookOpen, Gavel, PlusCircle } from 'lucide-react';

export const Home: React.FC = () => {
  const navigate = useNavigate();

  return (
    <Box sx={{ p: { xs: 2, md: 4 }, maxWidth: 1000, mx: 'auto' }}>
      <Paper
        elevation={0}
        sx={{
          p: 4,
          mb: 4,
          borderRadius: 3,
          border: '1px solid',
          borderColor: 'divider',
          backgroundColor: '#ffffff',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
          <Shield size={36} color="#d97706" />
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 800, color: '#1e293b' }}>
              SIGH - Módulo de Gestão Disciplinar
            </Typography>

            <Typography variant="body2" color="text.secondary">
              Sistema Inteligente de Gestão de Histórico Disciplinar (Sprint 6.4)
            </Typography>
          </Box>
        </Box>
      </Paper>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
          gap: 3,
        }}
      >
        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Box sx={{ mb: 2 }}>
              <Gavel size={32} color="#2563eb" />
            </Box>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              Processos Disciplinares
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ flexGrow: 1, mb: 2 }}>
              Consulte, acompanhe, abra investigações, registre decisões e aplique medidas disciplinares.
            </Typography>
            <Button
              variant="contained"
              color="primary"
              fullWidth
              onClick={() => navigate('/disciplinary/cases')}
            >
              Ver Processos
            </Button>
          </CardContent>
        </Card>

        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Box sx={{ mb: 2 }}>
              <BookOpen size={32} color="#0284c7" />
            </Box>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              Tipos de Infração
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ flexGrow: 1, mb: 2 }}>
              Gerencie os tipos de infração, gravidades padrão, exigências de investigação e enquadramento.
            </Typography>
            <Button
              variant="outlined"
              color="primary"
              fullWidth
              onClick={() => navigate('/disciplinary/infraction-types')}
            >
              Ver Tipos de Infração
            </Button>
          </CardContent>
        </Card>

        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
          <CardContent sx={{ p: 3, display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Box sx={{ mb: 2 }}>
              <PlusCircle size={32} color="#d97706" />
            </Box>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              Novo Processo
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ flexGrow: 1, mb: 2 }}>
              Autue um novo processo disciplinar informando a empresa, número e dados primários.
            </Typography>
            <Button
              variant="contained"
              color="secondary"
              fullWidth
              onClick={() => navigate('/disciplinary/cases/new')}
            >
              Autuar Processo
            </Button>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
};
