import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Tooltip,
  TablePagination,
  Box,
  Typography,
  Skeleton,
} from '@mui/material';
import { Eye, Edit3, CheckCircle, XCircle } from 'lucide-react';
import { InfractionTypeDto } from '../types';
import { CaseSeverityChip } from './CaseSeverityChip';
import { useDisciplinaryPermissions } from '../hooks/useDisciplinaryPermissions';

interface InfractionTypeTableProps {
  data: InfractionTypeDto[];
  loading: boolean;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (newPage: number) => void;
  onPageSizeChange: (newPageSize: number) => void;
  onViewDetails: (type: InfractionTypeDto) => void;
  onEdit: (type: InfractionTypeDto) => void;
  onToggleStatus: (type: InfractionTypeDto) => void;
}

export const InfractionTypeTable: React.FC<InfractionTypeTableProps> = ({
  data,
  loading,
  pageNumber,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  onViewDetails,
  onEdit,
  onToggleStatus,
}) => {
  const { canUpdateInfractionTypes, canActivateInfractionTypes, canDeactivateInfractionTypes } =
    useDisciplinaryPermissions();

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
          Nenhum tipo de infração encontrado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Tente ajustar os filtros de busca ou crie um novo tipo de infração.
        </Typography>
      </Paper>
    );
  }

  return (
    <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      <TableContainer>
        <Table sx={{ minWidth: 700 }}>
          <TableHead sx={{ backgroundColor: '#f8fafc' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>Código</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Nome</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Gravidade Padrão</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Investigação Formal?</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Rescisão Permitida?</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                Ações
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.map((row) => (
              <TableRow key={row.id} hover>
                <TableCell sx={{ fontFamily: 'monospace', fontWeight: 600 }}>{row.code}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{row.name}</TableCell>
                <TableCell>
                  <CaseSeverityChip severity={row.defaultSeverity} />
                </TableCell>
                <TableCell>
                  {row.requiresFormalInvestigation ? (
                    <Chip label="Sim" color="warning" size="small" variant="outlined" />
                  ) : (
                    <Chip label="Não" color="default" size="small" variant="outlined" />
                  )}
                </TableCell>
                <TableCell>
                  {row.allowsTerminationRecommendation ? (
                    <Chip label="Sim" color="error" size="small" variant="outlined" />
                  ) : (
                    <Chip label="Não" color="default" size="small" variant="outlined" />
                  )}
                </TableCell>
                <TableCell>
                  <Chip
                    label={row.isActive ? 'Ativo' : 'Inativo'}
                    color={row.isActive ? 'success' : 'default'}
                    size="small"
                    sx={{ fontWeight: 600 }}
                  />
                </TableCell>
                <TableCell align="right">
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5 }}>
                    <Tooltip title="Visualizar Detalhes">
                      <IconButton size="small" onClick={() => onViewDetails(row)}>
                        <Eye size={18} />
                      </IconButton>
                    </Tooltip>

                    {canUpdateInfractionTypes && (
                      <Tooltip title="Editar">
                        <IconButton size="small" color="primary" onClick={() => onEdit(row)}>
                          <Edit3 size={18} />
                        </IconButton>
                      </Tooltip>
                    )}

                    {row.isActive && canDeactivateInfractionTypes && (
                      <Tooltip title="Desativar Tipo de Infração">
                        <IconButton size="small" color="error" onClick={() => onToggleStatus(row)}>
                          <XCircle size={18} />
                        </IconButton>
                      </Tooltip>
                    )}

                    {!row.isActive && canActivateInfractionTypes && (
                      <Tooltip title="Ativar Tipo de Infração">
                        <IconButton size="small" color="success" onClick={() => onToggleStatus(row)}>
                          <CheckCircle size={18} />
                        </IconButton>
                      </Tooltip>
                    )}
                  </Box>
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
