import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Button } from '@mui/material';
import { ArrowRight, Home as HomeIcon, Plus } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { PERMISSIONS } from '../auth/permissions';
import { PageHeader } from '../components/ui/PageHeader';
import { EmptyState } from '../components/ui/States';
import { recordErrorMessage, recordKeys, recordsApi, RecordStatus, type RecordFilters } from '../features/records/api/recordsApi';
import { RecordsTable } from '../features/records/components/RecordsTable';
import { DashboardPage } from '../features/dashboard/DashboardPage';

const Section: React.FC<{
  title: string;
  testId: string;
  filters: RecordFilters;
  emptyMessage: string;
  highlightRejected?: boolean;
  seeAll?: string;
}> = ({ title, testId, filters: initial, emptyMessage, highlightRejected, seeAll }) => {
  const [filters, setFilters] = useState<RecordFilters>({ pageNumber: 1, pageSize: 10, ...initial });
  const list = useQuery({ queryKey: recordKeys.list(filters), queryFn: () => recordsApi.list(filters), placeholderData: keepPreviousData });

  return (
    <section aria-label={title} data-testid={testId} className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-ink">{title}</h2>
        {seeAll && (
          <RouterLink to={seeAll} className="inline-flex items-center gap-1 text-sm font-semibold text-primary-text hover:underline">
            Ver todas <ArrowRight size={14} aria-hidden="true" />
          </RouterLink>
        )}
      </div>
      <RecordsTable
        ariaLabel={title}
        data={list.data}
        isLoading={list.isLoading}
        error={list.isError ? recordErrorMessage(list.error, 'Erro ao carregar as medidas.') : null}
        emptyMessage={emptyMessage}
        highlightRejected={highlightRejected}
        onPageChange={(pageNumber) => setFilters((f) => ({ ...f, pageNumber }))}
      />
    </section>
  );
};

/**
 * Página inicial por perfil:
 * - registrante e aprovador: as listas do dia a dia (as próprias medidas, a fila de aprovação);
 * - jurídico, gestor e administrador: o Painel.
 */
export const Home: React.FC = () => {
  const navigate = useNavigate();
  const { user, hasPermission } = useAuth();
  const canRegister = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER);
  const canApprove = hasPermission(PERMISSIONS.DISCIPLINARY_RECORDS_APPROVE);

  if (!canRegister && !canApprove) {
    if (hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD)) return <DashboardPage />;
    return (
      <div>
        <PageHeader icon={<HomeIcon size={22} />} title={`Olá, ${user?.fullName?.split(' ')[0] ?? ''}`} />
        <div className="rounded-lg border border-line bg-surface" data-testid="home-no-records">
          <EmptyState message="Use o menu para acessar as funções do seu perfil." />
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon={<HomeIcon size={22} />}
        title={`Olá, ${user?.fullName?.split(' ')[0] ?? ''}`}
        subtitle="Medidas disciplinares da USGA"
        actions={
          canRegister && (
            <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => navigate('/medidas/registrar')}>
              Registrar medida
            </Button>
          )
        }
      />
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
    </div>
  );
};
