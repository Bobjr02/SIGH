import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Box, CircularProgress, Typography } from '@mui/material';
import { auditApi, auditKeys } from '../api/auditApi';
import { AuditEntryCard } from './AuditEntryCard';

/** Linha do tempo de auditoria do processo e das subentidades (exige Audit.View). */
export const CaseAuditTimeline: React.FC<{ caseId: string }> = ({ caseId }) => {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: auditKeys.caseTrail(caseId, 1),
    queryFn: () => auditApi.caseTrail(caseId),
  });

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress aria-label="Carregando histórico" />
      </Box>
    );
  }

  if (isError) return <Alert severity="error">{(error as Error).message}</Alert>;

  const items = data?.items ?? [];

  return (
    <Box component="section" aria-label="Histórico de auditoria" sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
      <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
        Histórico ({data?.totalCount ?? 0})
      </Typography>
      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Nenhum registro de auditoria para este processo.
        </Typography>
      ) : (
        items.map((log) => <AuditEntryCard key={log.id} log={log} />)
      )}
    </Box>
  );
};
