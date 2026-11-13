import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Alert,
  Box,
  Link,
  Pagination,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { PagedResult } from '../../employees/api/employeesApi';
import { formatBusinessDate, RecordStatus, type RecordSummary } from '../api/recordsApi';
import { RecordStatusChip } from './RecordStatusChip';

interface RecordsTableProps {
  data?: PagedResult<RecordSummary>;
  isLoading: boolean;
  error?: string | null;
  emptyMessage?: string;
  onPageChange?: (page: number) => void;
  /** Destaca as rejeitadas (página inicial do registrante). */
  highlightRejected?: boolean;
}

export const RecordsTable: React.FC<RecordsTableProps> = ({ data, isLoading, error, emptyMessage, onPageChange, highlightRejected }) => {
  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider' }}>
      <TableContainer>
        <Table size="small" aria-label="Medidas">
          <TableHead>
            <TableRow>
              <TableCell>Número</TableCell>
              <TableCell>Funcionário</TableCell>
              <TableCell>Gerência</TableCell>
              <TableCell>Medida</TableCell>
              <TableCell>Motivo</TableCell>
              <TableCell>Data da medida</TableCell>
              <TableCell>Situação</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={7}>Carregando…</TableCell>
              </TableRow>
            )}
            {!isLoading && data?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Typography variant="body2" color="text.secondary" data-testid="records-empty">
                    {emptyMessage ?? 'Nenhuma medida encontrada.'}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
            {data?.items.map((r) => {
              const rejected = highlightRejected && r.status === RecordStatus.Rejected;
              return (
                <TableRow key={r.id} hover data-testid="record-row" sx={rejected ? { backgroundColor: 'error.50', outline: '2px solid', outlineColor: 'error.light' } : undefined}>
                  <TableCell>
                    <Link component={RouterLink} to={`/medidas/${r.id}`}>
                      {r.caseNumber}
                    </Link>
                  </TableCell>
                  <TableCell>
                    {r.employeeNumber} – {r.employeeName}
                  </TableCell>
                  <TableCell>{r.managementUnitCode ?? '—'}</TableCell>
                  <TableCell>
                    {r.measureTypeLabel}
                    {r.suspensionDays ? ` (${r.suspensionDays} dia${r.suspensionDays > 1 ? 's' : ''})` : ''}
                  </TableCell>
                  <TableCell>{r.infractionTypeName ?? '—'}</TableCell>
                  <TableCell>{formatBusinessDate(r.measureDate)}</TableCell>
                  <TableCell>
                    <RecordStatusChip status={r.status} />
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
      {data && data.totalPages > 1 && onPageChange && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 1.5 }}>
          <Pagination count={data.totalPages} page={data.pageNumber} onChange={(_, page) => onPageChange(page)} />
        </Box>
      )}
    </Paper>
  );
};
