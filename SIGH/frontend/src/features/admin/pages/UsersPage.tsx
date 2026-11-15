import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { Eye, FilterX, Pencil, UserPlus, Users as UsersIcon } from 'lucide-react';
import { PageHeader } from '../../../components/ui/PageHeader';
import { DataTable } from '../../../components/ui/DataTable';
import { InitialsAvatar } from '../../../components/ui/InitialsAvatar';
import { UserStatusChip } from '../../../components/ui/StatusChip';
import { ActionIcon } from '../../../components/ui/ActionIcon';
import { label } from '../../../i18n/labels';
import { formatDateTime } from '../../../utils/formatters';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { recordErrorMessage } from '../../records/api/recordsApi';
import { adminApi, adminKeys, USER_STATUS_LABELS, type RoleOption } from '../api/adminApi';

const MANAGER = 'Manager';

const CreateUserDialog: React.FC<{ open: boolean; onClose: () => void }> = ({ open, onClose }) => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const roles = useQuery({ queryKey: adminKeys.roles, queryFn: adminApi.roles, enabled: open });
  const units = useQuery({ queryKey: adminKeys.units, queryFn: adminApi.units, enabled: open });
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [cpf, setCpf] = useState('');
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [unitIds, setUnitIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  const managerRole = roles.data?.find((r) => r.name === MANAGER);
  const needsUnit = managerRole !== undefined && roleIds.includes(managerRole.id);

  const create = useMutation({
    mutationFn: () => adminApi.createUser({ fullName: fullName.trim(), email: email.trim(), cpf: cpf.replace(/\D/g, ''), roleIds, managementUnitIds: unitIds }),
    onSuccess: async (user) => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
      onClose();
      navigate(`/admin/usuarios/${user.id}`, { state: { created: true } });
    },
    onError: (e) => setError(recordErrorMessage(e, 'Não foi possível criar o usuário.')),
  });

  const toggle = (list: string[], id: string) => (list.includes(id) ? list.filter((x) => x !== id) : [...list, id]);
  const valid = fullName.trim() && email.trim() && cpf.replace(/\D/g, '').length === 11 && roleIds.length > 0 && (!needsUnit || unitIds.length > 0);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm" aria-labelledby="create-user-title">
      <DialogTitle id="create-user-title">Novo usuário</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '8px !important' }}>
        <TextField label="Nome completo" required value={fullName} onChange={(e) => setFullName(e.target.value)} />
        <TextField label="E-mail" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <TextField label="CPF" required value={cpf} onChange={(e) => setCpf(e.target.value)} helperText="Somente números (11 dígitos)." />
        <Typography variant="subtitle2">Perfis</Typography>
        <FormGroup>
          {roles.data?.map((r: RoleOption) => (
            <FormControlLabel
              key={r.id}
              control={<Checkbox checked={roleIds.includes(r.id)} onChange={() => setRoleIds((ids) => toggle(ids, r.id))} />}
              label={r.label}
            />
          ))}
        </FormGroup>
        {needsUnit && (
          <>
            <Typography variant="subtitle2">Gerências do gestor (pelo menos uma)</Typography>
            <FormGroup row>
              {units.data?.map((u) => (
                <FormControlLabel
                  key={u.id}
                  control={<Checkbox checked={unitIds.includes(u.id)} onChange={() => setUnitIds((ids) => toggle(ids, u.id))} />}
                  label={u.code ?? u.name}
                />
              ))}
            </FormGroup>
          </>
        )}
        <Alert severity="info">O usuário recebe por e-mail um link para definir a própria senha.</Alert>
        {error && <Alert severity="error">{error}</Alert>}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="contained" disabled={!valid || create.isPending} onClick={() => create.mutate()}>
          Criar usuário
        </Button>
      </DialogActions>
    </Dialog>
  );
};

/** Administração: lista de usuários (sem CPF: a coluna da referência virou e-mail), criação e acesso ao detalhe. */
export const UsersPage: React.FC = () => {
  const { hasPermission } = useAuth();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const users = useQuery({
    queryKey: adminKeys.users(search, page, status),
    queryFn: () => adminApi.users(search, page, status),
    placeholderData: keepPreviousData,
  });

  return (
    <div>
      <PageHeader
        icon={<UsersIcon size={22} />}
        title="Usuários"
        subtitle="Gerencie os usuários do sistema"
        actions={
          hasPermission(PERMISSIONS.USERS_CREATE) &&
          hasPermission(PERMISSIONS.USERS_MANAGE_ROLES) && (
            <Button variant="contained" startIcon={<UserPlus size={16} />} onClick={() => setCreating(true)}>
              Novo usuário
            </Button>
          )
        }
      />
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <TextField
          size="small"
          label="Buscar por nome ou e-mail"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          sx={{ minWidth: 280 }}
        />
        <TextField
          select
          size="small"
          label="Situação"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todas as situações</MenuItem>
          {Object.keys(USER_STATUS_LABELS).map((k) => (
            <MenuItem key={k} value={k}>
              {label('UserStatus', k)}
            </MenuItem>
          ))}
        </TextField>
        <Button
          variant="outlined"
          startIcon={<FilterX size={16} />}
          onClick={() => {
            setSearch('');
            setStatus('');
            setPage(1);
          }}
        >
          Limpar filtros
        </Button>
      </div>
      <DataTable
        ariaLabel="Usuários"
        rows={users.data?.items}
        rowKey={(u) => u.id}
        rowTestId="user-row"
        isLoading={users.isLoading}
        error={users.isError ? recordErrorMessage(users.error, 'Erro ao listar usuários.') : null}
        onRetry={() => users.refetch()}
        emptyMessage="Nenhum usuário encontrado."
        emptyIcon={<UsersIcon size={32} />}
        columns={[
          {
            key: 'name',
            header: 'Nome',
            render: (u) => (
              <span className="flex items-center gap-2.5">
                <InitialsAvatar name={u.fullName} />
                <RouterLink to={`/admin/usuarios/${u.id}`} className="font-medium text-ink hover:underline">
                  {u.fullName}
                </RouterLink>
              </span>
            ),
          },
          { key: 'email', header: 'E-mail', render: (u) => u.email, hideOnMobile: true },
          { key: 'roles', header: 'Perfil', render: (u) => (u.roles.length ? u.roles.map((r) => label('Role', r)).join(', ') : '—') },
          { key: 'status', header: 'Situação', render: (u) => <UserStatusChip status={u.status} /> },
          { key: 'last', header: 'Último acesso', render: (u) => formatDateTime(u.lastLoginAt), className: 'whitespace-nowrap', hideOnMobile: true },
          {
            key: 'actions',
            header: 'Ações',
            align: 'right',
            render: (u) => (
              <span className="inline-flex gap-1">
                <ActionIcon title="Ver usuário" icon={<Eye size={17} />} to={`/admin/usuarios/${u.id}`} />
                {hasPermission(PERMISSIONS.USERS_MANAGE_ROLES) && (
                  <ActionIcon title="Editar perfis e gerências" icon={<Pencil size={16} />} to={`/admin/usuarios/${u.id}?aba=perfis`} />
                )}
              </span>
            ),
          },
        ]}
        paging={
          users.data
            ? {
                pageNumber: users.data.pageNumber,
                pageSize: users.data.pageSize,
                totalCount: users.data.totalCount,
                onPageChange: setPage,
                noun: users.data.totalCount === 1 ? 'usuário' : 'usuários',
              }
            : undefined
        }
      />
      {creating && <CreateUserDialog open={creating} onClose={() => setCreating(false)} />}
    </div>
  );
};

export default UsersPage;
