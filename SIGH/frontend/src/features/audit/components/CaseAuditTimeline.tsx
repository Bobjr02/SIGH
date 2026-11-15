import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { ScrollText } from 'lucide-react';
import { auditApi, auditKeys } from '../api/auditApi';
import { EmptyState, ErrorState } from '../../../components/ui/States';
import { formatNumber } from '../../../utils/formatters';
import { AuditEntryCard } from './AuditEntryCard';

/** Histórico (auditoria) da medida e das subentidades (exige Audit.View). */
export const CaseAuditTimeline: React.FC<{ caseId: string }> = ({ caseId }) => {
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: auditKeys.caseTrail(caseId, 1),
    queryFn: () => auditApi.caseTrail(caseId),
  });

  if (isLoading) {
    return (
      <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando histórico">
        {[0, 1, 2].map((i) => (
          <div key={i} className="h-20 animate-pulse rounded-lg bg-surface-2" />
        ))}
      </div>
    );
  }
  if (isError) return <ErrorState message={(error as Error).message} onRetry={() => refetch()} />;

  const items = data?.items ?? [];
  return (
    <section aria-label="Histórico de auditoria" className="flex flex-col gap-3">
      <h2 className="text-base font-semibold text-ink">Histórico ({formatNumber(data?.totalCount ?? 0)})</h2>
      {items.length === 0 ? (
        <EmptyState message="Nenhum registro de auditoria para esta medida." icon={<ScrollText size={32} />} />
      ) : (
        items.map((log) => <AuditEntryCard key={log.id} log={log} />)
      )}
    </section>
  );
};
