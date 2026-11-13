import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  Link,
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
import { UserPlus } from 'lucide-react';
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

/** Administração: lista de usuários, criação (com e-mail de definição de senha) e acesso ao detalhe. */
export const UsersPage: React.FC = () => {
  const { hasPermission } = useAuth();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const users = useQuery({ queryKey: adminKeys.users(search, page), queryFn: () => adminApi.users(search, page), placeholderData: keepPreviousData });
  const roles = useQuery({ queryKey: adminKeys.roles, queryFn: adminApi.roles });
  const labelOf = (name: string) => roles.data?.find((r) => r.name === name)?.label ?? name;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
          Usuários
        </Typography>
        {hasPermission(PERMISSIONS.USERS_CREATE) && hasPermission(PERMISSIONS.USERS_MANAGE_ROLES) && (
          <Button variant="contained" startIcon={<UserPlus size={16} />} onClick={() => setCreating(true)}>
            Novo usuário
          </Button>
        )}
      </Box>
      <TextField size="small" label="Buscar por nome ou e-mail" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} sx={{ maxWidth: 360 }} />
      {users.isError && <Alert severity="error">{recordErrorMessage(users.error, 'Erro ao listar usuários.')}</Alert>}
      <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider' }}>
        <Table size="small" aria-label="Usuários">
          <TableHead>
            <TableRow>
              <TableCell>Nome</TableCell>
              <TableCell>E-mail</TableCell>
              <TableCell>Perfis</TableCell>
              <TableCell>Situação</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.data?.items.map((u) => (
              <TableRow key={u.id} hover data-testid="user-row">
                <TableCell>
                  <Link component={RouterLink} to={`/admin/usuarios/${u.id}`}>
                    {u.fullName}
                  </Link>
                </TableCell>
                <TableCell>{u.email}</TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                    {u.roles.map((r) => (
                      <Chip key={r} size="small" label={labelOf(r)} />
                    ))}
                  </Box>
                </TableCell>
                <TableCell>{USER_STATUS_LABELS[u.status] ?? u.status}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {users.data && users.data.totalPages > 1 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 1.5 }}>
            <Pagination count={users.data.totalPages} page={page} onChange={(_, p) => setPage(p)} />
          </Box>
        )}
      </Paper>
      {creating && <CreateUserDialog open={creating} onClose={() => setCreating(false)} />}
    </Box>
  );
};

export default UsersPage;
