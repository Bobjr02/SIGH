import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { ClipboardCheck } from 'lucide-react';
import { PageHeader } from '../../../components/ui/PageHeader';
import { recordErrorMessage, recordKeys, recordsApi, RecordStatus, type RecordFilters } from '../api/recordsApi';
import { RecordsTable } from '../components/RecordsTable';

/** Fila do aprovador: medidas aguardando aprovação. */
export const PendingApprovalsPage: React.FC = () => {
  const [filters, setFilters] = useState<RecordFilters>({ status: RecordStatus.PendingApproval, pageNumber: 1, pageSize: 20 });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });

  return (
    <div>
      <PageHeader
        icon={<ClipboardCheck size={22} />}
        title="Aprovações"
        subtitle="Abra a medida para aprovar ou rejeitar. Medidas registradas por você não podem ser aprovadas por você."
      />
      <RecordsTable
        ariaLabel="Medidas aguardando aprovação"
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar a fila.') : null}
        emptyMessage="Nenhuma medida aguardando aprovação."
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
      />
    </div>
  );
};

export default PendingApprovalsPage;
