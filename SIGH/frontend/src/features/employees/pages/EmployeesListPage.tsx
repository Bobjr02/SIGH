import React, { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Button, MenuItem, TextField } from '@mui/material';
import { Eye, Search, Upload, Users } from 'lucide-react';
import { PageHeader } from '../../../components/ui/PageHeader';
import { DataTable } from '../../../components/ui/DataTable';
import { InitialsAvatar } from '../../../components/ui/InitialsAvatar';
import { EmployeeStatusChip } from '../../../components/ui/StatusChip';
import { ActionIcon } from '../../../components/ui/ActionIcon';
import { label } from '../../../i18n/labels';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import {
  EmployeeGroup,
  EmployeeStatus,
  employeeKeys,
  employeesApi,
  formatDate,
  maskCpf,
  type EmployeeListFilters,
} from '../api/employeesApi';

const PAGE_SIZE = 20;

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
    <div>
      <PageHeader
        icon={<Users size={22} />}
        title="Funcionários"
        subtitle="Funcionários importados do SAP, no escopo do seu perfil"
        actions={
          hasPermission(PERMISSIONS.EMPLOYEES_IMPORT) && (
            <Button variant="outlined" startIcon={<Upload size={16} />} onClick={() => navigate('/employees/import')}>
              Importar do SAP
            </Button>
          )
        }
      />

      <form
        aria-label="Filtros de funcionários"
        onSubmit={apply}
        className="mb-4 grid grid-cols-1 gap-3 rounded-lg border border-line bg-surface p-4 shadow-card sm:grid-cols-2 lg:grid-cols-3"
      >
        <TextField size="small" label="Nome ou matrícula" value={draft.searchTerm} onChange={set('searchTerm')} />
        <TextField select size="small" label="Situação" value={draft.status} onChange={set('status')}>
          <MenuItem value="">Todas</MenuItem>
          {[EmployeeStatus.Active, EmployeeStatus.OnLeave, EmployeeStatus.Terminated].map((s) => (
            <MenuItem key={s} value={String(s)}>
              {label('EmployeeStatus', s)}
            </MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="Grupo" value={draft.group} onChange={set('group')}>
          <MenuItem value="">Todos</MenuItem>
          {[EmployeeGroup.Field, EmployeeGroup.Industry].map((g) => (
            <MenuItem key={g} value={String(g)}>
              {label('EmployeeGroup', g)}
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
        <div className="flex items-center justify-end">
          <Button type="submit" variant="contained" startIcon={<Search size={16} />}>
            Filtrar
          </Button>
        </div>
      </form>

      <DataTable
        ariaLabel="Funcionários"
        rows={list.data?.items}
        rowKey={(e) => e.id}
        rowTestIdOf={(e) => `employee-row-${e.employeeNumber}`}
        isLoading={list.isLoading}
        error={list.isError ? (list.error as Error).message : null}
        onRetry={() => list.refetch()}
        emptyMessage="Nenhum funcionário encontrado."
        emptyIcon={<Users size={32} />}
        columns={[
          {
            key: 'name',
            header: 'Nome',
            render: (e) => (
              <span className="flex items-center gap-2.5">
                <InitialsAvatar name={e.fullName} />
                <span>
                  <button type="button" onClick={() => navigate(`/employees/${e.id}`)} className="text-left font-medium text-ink hover:underline">
                    {e.fullName}
                  </button>
                  <span className="block text-xs text-muted">Matrícula {e.employeeNumber}</span>
                </span>
              </span>
            ),
          },
          { key: 'cpf', header: 'CPF', render: (e) => <span className="whitespace-nowrap font-mono text-xs">{maskCpf(e.cpfMasked)}</span>, hideOnMobile: true },
          { key: 'status', header: 'Situação', render: (e) => <EmployeeStatusChip status={e.status} /> },
          { key: 'group', header: 'Grupo', render: (e) => (e.group ? label('EmployeeGroup', e.group) : '—'), hideOnMobile: true },
          { key: 'unit', header: 'Gerência', render: (e) => e.managementUnitName ?? '—', hideOnMobile: true },
          { key: 'dept', header: 'Setor', render: (e) => e.departmentName ?? '—', hideOnMobile: true },
          { key: 'admission', header: 'Admissão', render: (e) => formatDate(e.admissionDate), hideOnMobile: true },
          {
            key: 'actions',
            header: 'Ações',
            align: 'right',
            render: (e) => <ActionIcon title="Ver funcionário" icon={<Eye size={17} />} to={`/employees/${e.id}`} />,
          },
        ]}
        paging={
          list.data
            ? {
                pageNumber: list.data.pageNumber,
                pageSize: list.data.pageSize,
                totalCount: list.data.totalCount,
                onPageChange: (page) => setFilters((f) => ({ ...f, pageNumber: page })),
                noun: list.data.totalCount === 1 ? 'funcionário' : 'funcionários',
              }
            : undefined
        }
      />
    </div>
  );
};

export default EmployeesListPage;
