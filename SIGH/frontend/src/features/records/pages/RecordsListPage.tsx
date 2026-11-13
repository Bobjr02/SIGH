import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Box, Button, Typography } from '@mui/material';
import { PlusCircle } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { recordErrorMessage, recordKeys, recordsApi, type RecordFilters as Filters } from '../api/recordsApi';
import { RecordFilters } from '../components/RecordFilters';
import { RecordsTable } from '../components/RecordsTable';

/** Lista de medidas com os filtros da Parte A (o servidor aplica o escopo por gerência). */
export const RecordsListPage: React.FC<{ title?: string; initialFilters?: Filters; highlightRejected?: boolean }> = ({
  title = 'Medidas',
  initialFilters,
  highlightRejected,
}) => {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [filters, setFilters] = useState<Filters>({ pageNumber: 1, pageSize: 20, ...initialFilters });
  const options = useQuery({ queryKey: recordKeys.options, queryFn: recordsApi.options });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
          {title}
        </Typography>
        {hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER) && (
          <Button variant="contained" startIcon={<PlusCircle size={16} />} onClick={() => navigate('/medidas/registrar')}>
            Registrar medida
          </Button>
        )}
      </Box>
      <RecordFilters value={filters} options={options.data} onChange={setFilters} />
      <RecordsTable
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao listar as medidas.') : null}
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
        highlightRejected={highlightRejected}
      />
    </Box>
  );
};

export default RecordsListPage;
