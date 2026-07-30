import React from 'react';
import { Box, Container, Paper, Typography } from '@mui/material';
import { Header } from '../components/common/Header';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', backgroundColor: 'background.default' }}>
      <Header />
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4, flexGrow: 1 }}>
        {children}
      </Container>
      <Box component="footer" sx={{ py: 2.5, px: 2, mt: 'auto', backgroundColor: 'background.paper', borderTop: 1, borderColor: 'divider' }}>
        <Container maxWidth="lg">
          <Typography variant="body2" color="text.secondary" align="center">
            © {new Date().getFullYear()} SIGH - Sistema Inteligente de Gestão de Histórico Disciplinar. Todos os direitos reservados.
          </Typography>
        </Container>
      </Box>
    </Box>
  );
};
