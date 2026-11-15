import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Button } from '@mui/material';
import { Gavel, Plus } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { PageHeader } from '../../../components/ui/PageHeader';
import { recordErrorMessage, recordKeys, recordsApi, type RecordFilters as Filters } from '../api/recordsApi';
import { RecordFilters } from '../components/RecordFilters';
import { RecordsTable } from '../components/RecordsTable';

/** Lista de medidas com filtros (o servidor aplica o escopo por gerência). */
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
    <div>
      <PageHeader
        icon={<Gavel size={22} />}
        title={title}
        subtitle="Medidas disciplinares registradas no SIGH e o histórico importado"
        actions={
          hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER) && (
            <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => navigate('/medidas/registrar')}>
              Registrar medida
            </Button>
          )
        }
      />
      <RecordFilters value={filters} options={options.data} onChange={setFilters} />
      <RecordsTable
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao listar as medidas.') : null}
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
        highlightRejected={highlightRejected}
      />
    </div>
  );
};

export default RecordsListPage;
