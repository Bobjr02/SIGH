import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Box, Typography } from '@mui/material';
import { recordErrorMessage, recordKeys, recordsApi, RecordStatus, type RecordFilters } from '../api/recordsApi';
import { RecordsTable } from '../components/RecordsTable';

/** Fila do aprovador: medidas aguardando aprovação, mais antigas primeiro na leitura do RH. */
export const PendingApprovalsPage: React.FC = () => {
  const [filters, setFilters] = useState<RecordFilters>({ status: RecordStatus.PendingApproval, pageNumber: 1, pageSize: 20 });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        Aprovações pendentes
      </Typography>
      <Typography color="text.secondary">Abra a medida para aprovar ou rejeitar. Medidas registradas por você não podem ser aprovadas por você.</Typography>
      <RecordsTable
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar a fila.') : null}
        emptyMessage="Nenhuma medida aguardando aprovação."
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
      />
    </Box>
  );
};

export default PendingApprovalsPage;
