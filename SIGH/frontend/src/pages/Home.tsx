import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Box, Button, Paper, Typography } from '@mui/material';
import { PlusCircle } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { PERMISSIONS } from '../auth/permissions';
import { recordErrorMessage, recordKeys, recordsApi, RecordStatus, type RecordFilters } from '../features/records/api/recordsApi';
import { RecordsTable } from '../features/records/components/RecordsTable';

const Section: React.FC<{
  title: string;
  testId: string;
  filters: RecordFilters;
  emptyMessage: string;
  highlightRejected?: boolean;
  seeAll?: string;
}> = ({ title, testId, filters: initial, emptyMessage, highlightRejected, seeAll }) => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<RecordFilters>({ pageNumber: 1, pageSize: 10, ...initial });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });

  return (
    <Box component="section" aria-label={title} data-testid={testId} sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
        {seeAll && (
          <Button size="small" onClick={() => navigate(seeAll)}>
            Ver todas
          </Button>
        )}
      </Box>
      <RecordsTable
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar as medidas.') : null}
        emptyMessage={emptyMessage}
        highlightRejected={highlightRejected}
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
      />
    </Box>
  );
};

/**
 * Página inicial por perfil:
 * - aprovador: a fila de pendentes;
 * - registrante: as suas medidas, com destaque para as rejeitadas;
 * - gestor: as medidas das suas gerências;
 * - jurídico e administrador: a lista geral.
 */
export const Home: React.FC = () => {
  const navigate = useNavigate();
  const { user, hasPermission } = useAuth();
  const canView = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_VIEW);
  const canRegister = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER);
  const canApprove = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_APPROVE);
  const fullScope = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_VIEW_ALL);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <Paper elevation={0} sx={{ p: 3, border: '1px solid', borderColor: 'divider', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
            Olá, {user?.fullName?.split(' ')[0]}
          </Typography>
          <Typography color="text.secondary">SIGH — medidas disciplinares da USGA</Typography>
        </Box>
        {canRegister && (
          <Button variant="contained" startIcon={<PlusCircle size={16} />} onClick={() => navigate('/medidas/registrar')}>
            Registrar medida
          </Button>
        )}
      </Paper>

      {canApprove && (
        <Section
          title="Aguardando a sua aprovação"
          testId="home-pending"
          filters={{ status: RecordStatus.PendingApproval }}
          emptyMessage="Nenhuma medida aguardando aprovação."
          seeAll="/medidas/aprovacoes"
        />
      )}
      {canRegister && (
        <Section title="Minhas medidas" testId="home-mine" filters={{ mine: true }} emptyMessage="Você ainda não registrou medidas." highlightRejected seeAll="/medidas" />
      )}
      {canView && !canApprove && !canRegister && (
        <Section
          title={fullScope ? 'Medidas' : 'Medidas das suas gerências'}
          testId="home-all"
          filters={{}}
          emptyMessage="Nenhuma medida encontrada."
          seeAll="/medidas"
        />
      )}
      {!canView && (
        <Typography color="text.secondary" data-testid="home-no-records">
          Use o menu para acessar as funções do seu perfil.
        </Typography>
      )}
    </Box>
  );
};
