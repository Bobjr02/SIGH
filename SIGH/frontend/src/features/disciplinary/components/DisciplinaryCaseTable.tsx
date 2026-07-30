import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  TablePagination,
  Box,
  Typography,
  Skeleton,
  Chip,
} from '@mui/material';
import { Eye, ArrowRight } from 'lucide-react';
import { DisciplinaryCaseSummaryDto } from '../types';
import { CaseStatusChip } from './CaseStatusChip';
import { CaseSeverityChip } from './CaseSeverityChip';

interface DisciplinaryCaseTableProps {
  data: DisciplinaryCaseSummaryDto[];
  loading: boolean;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (newPage: number) => void;
  onPageSizeChange: (newPageSize: number) => void;
  onViewCase: (id: string) => void;
}

export const DisciplinaryCaseTable: React.FC<DisciplinaryCaseTableProps> = ({
  data,
  loading,
  pageNumber,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  onViewCase,
}) => {
  if (loading) {
    return (
      <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider', p: 2 }}>
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} height={50} sx={{ my: 1 }} />
        ))}
      </Paper>
    );
  }

  if (data.length === 0) {
    return (
      <Paper
        elevation={0}
        sx={{
          p: 6,
          textAlign: 'center',
          border: '1px solid',
          borderColor: 'divider',
          backgroundColor: '#fafafa',
          borderRadius: 2,
        }}
      >
        <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
          Nenhum processo disciplinar encontrado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Ajuste os filtros de pesquisa ou crie um novo processo.
        </Typography>
      </Paper>
    );
  }

  return (
    <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      <TableContainer>
        <Table sx={{ minWidth: 800 }}>
          <TableHead sx={{ backgroundColor: '#f8fafc' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>Número</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Título / Assunto</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Prioridade</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Ocorrências</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Envolvidos</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Abertura</TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                Ações
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.map((row) => (
              <TableRow key={row.id} hover sx={{ cursor: 'pointer' }} onClick={() => onViewCase(row.id)}>
                <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700, color: 'primary.main' }}>
                  {row.caseNumber}
                </TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{row.title}</TableCell>
                <TableCell>
                  <CaseStatusChip status={row.status} />
                </TableCell>
                <TableCell>
                  <CaseSeverityChip severity={row.priority} />
                </TableCell>
                <TableCell>
                  <Chip label={`${row.occurrencesCount} ocorrência(s)`} size="small" variant="outlined" />
                </TableCell>
                <TableCell>
                  <Chip label={`${row.employeesCount} funcionário(s)`} size="small" variant="outlined" />
                </TableCell>
                <TableCell color="text.secondary">
                  {row.openedAt ? new Date(row.openedAt).toLocaleDateString('pt-BR') : '-'}
                </TableCell>
                <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                  <Tooltip title="Abrir Detalhes do Processo">
                    <IconButton size="small" color="primary" onClick={() => onViewCase(row.id)}>
                      <ArrowRight size={18} />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        component="div"
        count={totalCount}
        page={pageNumber - 1}
        onPageChange={(_, newPage) => onPageChange(newPage + 1)}
        rowsPerPage={pageSize}
        onRowsPerPageChange={(e) => onPageSizeChange(parseInt(e.target.value, 10))}
        rowsPerPageOptions={[5, 10, 25, 50]}
        labelRowsPerPage="Itens por página:"
        labelDisplayedRows={({ from, to, count }) => `${from}-${to} de ${count !== -1 ? count : `mais de ${to}`}`}
      />
    </Paper>
  );
};
