import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Chip,
  MenuItem,
  Pagination,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { Upload, Users } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import {
  EmployeeGroup,
  EmployeeStatus,
  GROUP_LABELS,
  STATUS_LABELS,
  employeeKeys,
  employeesApi,
  formatDate,
  maskCpf,
  type EmployeeListFilters,
} from '../api/employeesApi';

const PAGE_SIZE = 20;

const statusColor = (status: EmployeeStatus): 'success' | 'warning' | 'default' =>
  status === EmployeeStatus.Active ? 'success' : status === EmployeeStatus.OnLeave ? 'warning' : 'default';

export const EmployeesListPage: React.FC = () => {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [draft, setDraft] = useState({ searchTerm: '', status: '', group: '', managementUnitId: '', departmentId: '' });
  const [filters, setFilters] = useState<EmployeeListFilters>({ pageNumber: 1, pageSize: PAGE_SIZE });

  const options = useQuery({ queryKey: employeeKeys.filterOptions, queryFn: employeesApi.filterOptions, staleTime: 5 * 60_000 });
  const list = useQuery({ queryKey: employeeKeys.list(filters), queryFn: () => employeesApi.list(filters), placeholderData: keepPreviousData });

  const set = (field: keyof typeof draft) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setDraft((d) => ({ ...d, [field]: e.target.value, ...(field === 'managementUnitId' ? { departmentId: '' } : {}) }));

  const apply = (event: React.FormEvent) => {
    event.preventDefault();
    setFilters({
      searchTerm: draft.searchTerm.trim() || undefined,
      status: draft.status ? (Number(draft.status) as EmployeeStatus) : undefined,
      group: draft.group ? (Number(draft.group) as EmployeeGroup) : undefined,
      managementUnitId: draft.managementUnitId || undefined,
      departmentId: draft.departmentId || undefined,
      pageNumber: 1,
      pageSize: PAGE_SIZE,
    });
  };

  const departments = (options.data?.departments ?? []).filter(
    (d) => !draft.managementUnitId || d.managementUnitId === draft.managementUnitId,
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800, display: 'flex', alignItems: 'center', gap: 1 }}>
          <Users size={28} /> Funcionários
        </Typography>
        {hasPermission(PERMISSIONS.EMPLOYEES_IMPORT) && (
          <Button variant="outlined" startIcon={<Upload size={16} />} onClick={() => navigate('/employees/import')}>
            Importar do SAP
          </Button>
        )}
      </Box>

      <Paper
        component="form"
        aria-label="Filtros de funcionários"
        onSubmit={apply}
        elevation={0}
        sx={{ p: 2, border: '1px solid', borderColor: 'divider', display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' } }}
      >
        <TextField size="small" label="Nome ou matrícula" value={draft.searchTerm} onChange={set('searchTerm')} />
        <TextField select size="small" label="Situação" value={draft.status} onChange={set('status')}>
          <MenuItem value="">Todas</MenuItem>
          {[EmployeeStatus.Active, EmployeeStatus.OnLeave, EmployeeStatus.Terminated].map((s) => (
            <MenuItem key={s} value={String(s)}>
              {STATUS_LABELS[s]}
            </MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="Grupo" value={draft.group} onChange={set('group')}>
          <MenuItem value="">Todos</MenuItem>
          {[EmployeeGroup.Field, EmployeeGroup.Industry].map((g) => (
            <MenuItem key={g} value={String(g)}>
              {GROUP_LABELS[g]}
            </MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="Gerência" value={draft.managementUnitId} onChange={set('managementUnitId')}>
          <MenuItem value="">Todas</MenuItem>
          {(options.data?.managementUnits ?? []).map((m) => (
            <MenuItem key={m.id} value={m.id}>
              {m.code ? `${m.code} - ${m.name}` : m.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="Setor" value={draft.departmentId} onChange={set('departmentId')}>
          <MenuItem value="">Todos</MenuItem>
          {departments.map((d) => (
            <MenuItem key={d.id} value={d.id}>
              {d.name}
            </MenuItem>
          ))}
        </TextField>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end' }}>
          <Button type="submit" variant="contained">
            Filtrar
          </Button>
        </Box>
      </Paper>

      {list.isError && <Alert severity="error">{(list.error as Error).message}</Alert>}

      {list.data && (
        <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider', overflowX: 'auto' }}>
          <Table size="small" aria-label="Funcionários">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Matrícula</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Nome</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>CPF</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Situação</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Grupo</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Gerência</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Setor</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Admissão</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {list.data.items.map((e) => (
                <TableRow
                  key={e.id}
                  hover
                  sx={{ cursor: 'pointer' }}
                  onClick={() => navigate(`/employees/${e.id}`)}
                  data-testid={`employee-row-${e.employeeNumber}`}
                >
                  <TableCell sx={{ fontFamily: 'monospace' }}>{e.employeeNumber}</TableCell>
                  <TableCell>{e.fullName}</TableCell>
                  <TableCell sx={{ fontFamily: 'monospace' }}>{maskCpf(e.cpfMasked)}</TableCell>
                  <TableCell>
                    <Chip size="small" color={statusColor(e.status)} label={STATUS_LABELS[e.status] ?? e.status} />
                  </TableCell>
                  <TableCell>{e.group ? GROUP_LABELS[e.group] : '-'}</TableCell>
                  <TableCell>{e.managementUnitName ?? '-'}</TableCell>
                  <TableCell>{e.departmentName ?? '-'}</TableCell>
                  <TableCell>{formatDate(e.admissionDate)}</TableCell>
                </TableRow>
              ))}
              {list.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} align="center">
                    Nenhum funcionário encontrado.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 1.5 }}>
            <Typography variant="body2" color="text.secondary">
              {list.data.totalCount} funcionário(s)
            </Typography>
            {list.data.totalPages > 1 && (
              <Pagination
                count={list.data.totalPages}
                page={list.data.pageNumber}
                onChange={(_, page) => setFilters((f) => ({ ...f, pageNumber: page }))}
              />
            )}
          </Box>
        </Paper>
      )}
    </Box>
  );
};

export default EmployeesListPage;
