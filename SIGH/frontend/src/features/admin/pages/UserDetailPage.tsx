import React, { useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { Trash2 } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { recordErrorMessage } from '../../records/api/recordsApi';
import {
  adminApi,
  adminKeys,
  formatValidity,
  USER_STATUS_LABELS,
  UserStatus,
  VALIDITY_LABELS,
  validityFromDates,
  ValidityState,
  type UserAccess,
} from '../api/adminApi';

const VALIDITY_COLORS: Record<ValidityState, 'success' | 'info' | 'default'> = {
  [ValidityState.Active]: 'success',
  [ValidityState.Scheduled]: 'info',
  [ValidityState.Expired]: 'default',
};

const ValidityChip: React.FC<{ state: ValidityState }> = ({ state }) => (
  <Chip size="small" variant={state === ValidityState.Active ? 'filled' : 'outlined'} color={VALIDITY_COLORS[state]} label={VALIDITY_LABELS[state]} data-testid="validity-state" />
);

/** Seletor + datas de validade (opcionais) + botão. "Até" é inclusivo (o último dia vale inteiro). */
const AssignRow: React.FC<{
  label: string;
  options: { id: string; label: string }[];
  onAssign: (id: string, from: string, until: string) => void;
  busy: boolean;
}> = ({ label, options, onAssign, busy }) => {
  const [id, setId] = useState('');
  const [from, setFrom] = useState('');
  const [until, setUntil] = useState('');
  return (
    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', alignItems: 'center', mt: 1.5 }}>
      <TextField select size="small" label={label} value={id} onChange={(e) => setId(e.target.value)} sx={{ minWidth: 220 }}>
        {options.map((o) => (
          <MenuItem key={o.id} value={o.id}>
            {o.label}
          </MenuItem>
        ))}
      </TextField>
      <TextField size="small" type="date" label="Válido de" value={from} onChange={(e) => setFrom(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
      <TextField size="small" type="date" label="Válido até" value={until} onChange={(e) => setUntil(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
      <Button
        variant="contained"
        disabled={!id || busy}
        onClick={() => {
          onAssign(id, from, until);
          setId('');
          setFrom('');
          setUntil('');
        }}
      >
        Atribuir
      </Button>
    </Box>
  );
};

/**
 * Detalhe do usuário: status (ativar, desativar, desbloquear), perfis e gerências com validade.
 * Ninguém altera o próprio acesso (o servidor recusa; a tela nem mostra os botões).
 */
export const UserDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { user: me, hasPermission } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>((location.state as { created?: boolean } | null)?.created ? 'Usuário criado. O e-mail de definição de senha foi enviado.' : null);

  const user = useQuery({ queryKey: adminKeys.user(id), queryFn: () => adminApi.user(id) });
  const access = useQuery({ queryKey: adminKeys.access(id), queryFn: () => adminApi.access(id) });
  const roles = useQuery({ queryKey: adminKeys.roles, queryFn: adminApi.roles });
  const units = useQuery({ queryKey: adminKeys.units, queryFn: adminApi.units });

  const self = me?.id === id;
  const canManage = hasPermission(PERMISSIONS.USERS_MANAGE_ROLES) && !self;
  const canChangeStatus = hasPermission(PERMISSIONS.USERS_CHANGE_STATUS) && !self;

  const onAccess = (next: UserAccess, text: string) => {
    queryClient.setQueryData(adminKeys.access(id), next);
    void queryClient.invalidateQueries({ queryKey: adminKeys.user(id) });
    setError(null);
    setMessage(text);
  };
  const onError = (e: unknown) => setError(recordErrorMessage(e, 'Não foi possível concluir a ação.'));

  const assignRole = useMutation({
    mutationFn: ({ roleId, from, until }: { roleId: string; from: string; until: string }) => adminApi.assignRole(id, roleId, validityFromDates(from, until)),
    onSuccess: (next) => onAccess(next, 'Perfil atribuído.'),
    onError,
  });
  const revokeRole = useMutation({ mutationFn: (linkId: string) => adminApi.revokeRole(id, linkId), onSuccess: (next) => onAccess(next, 'Perfil retirado.'), onError });
  const assignUnit = useMutation({
    mutationFn: ({ unitId, from, until }: { unitId: string; from: string; until: string }) => adminApi.assignUnit(id, unitId, validityFromDates(from, until)),
    onSuccess: (next) => onAccess(next, 'Gerência vinculada.'),
    onError,
  });
  const revokeUnit = useMutation({ mutationFn: (linkId: string) => adminApi.revokeUnit(id, linkId), onSuccess: (next) => onAccess(next, 'Gerência retirada.'), onError });

  const status = useMutation({
    mutationFn: async (action: 'activate' | 'deactivate' | 'unlock') => {
      if (action === 'unlock') return adminApi.unlock(id);
      const reason = action === 'deactivate' ? 'Desativado pelo administrador.' : 'Reativado pelo administrador.';
      return adminApi.changeStatus(id, action === 'deactivate' ? UserStatus.Inactive : UserStatus.Active, reason);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: adminKeys.user(id) });
      setError(null);
      setMessage('Situação do usuário atualizada.');
    },
    onError,
  });

  if (user.isLoading || access.isLoading) return <CircularProgress aria-label="Carregando" />;
  if (user.isError || !user.data) return <Alert severity="error">{recordErrorMessage(user.error, 'Usuário não encontrado.')}</Alert>;

  const u = user.data;
  const busy = assignRole.isPending || revokeRole.isPending || assignUnit.isPending || revokeUnit.isPending;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        {u.fullName}
      </Typography>
      <Typography color="text.secondary">
        {u.email} · {USER_STATUS_LABELS[u.status] ?? u.status}
      </Typography>
      {self && <Alert severity="info">Este é o seu usuário: perfis, gerências e situação só podem ser alterados por outro administrador.</Alert>}
      {message && (
        <Alert severity="success" onClose={() => setMessage(null)}>
          {message}
        </Alert>
      )}
      {error && (
        <Alert severity="error" role="alert" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {canChangeStatus && (
        <Box sx={{ display: 'flex', gap: 1 }}>
          {u.status === 'Inactive' || u.status === 'Suspended' ? (
            <Button variant="outlined" onClick={() => status.mutate('activate')}>
              Ativar
            </Button>
          ) : (
            <Button variant="outlined" color="error" onClick={() => status.mutate('deactivate')}>
              Desativar
            </Button>
          )}
          {u.status === 'Locked' && hasPermission(PERMISSIONS.USERS_UNLOCK) && (
            <Button variant="outlined" onClick={() => status.mutate('unlock')}>
              Desbloquear
            </Button>
          )}
        </Box>
      )}

      <Paper component="section" aria-label="Perfis" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          Perfis
        </Typography>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Perfil</TableCell>
              <TableCell>Validade</TableCell>
              <TableCell>Situação</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {access.data?.roles.map((r) => (
              <TableRow key={r.id} data-testid="role-row">
                <TableCell>{r.roleLabel}</TableCell>
                <TableCell>{formatValidity(r.validFrom, r.validUntil)}</TableCell>
                <TableCell>
                  <ValidityChip state={r.state} />
                </TableCell>
                <TableCell align="right">
                  {canManage && (
                    <Tooltip title="Retirar perfil">
                      <IconButton aria-label={`Retirar ${r.roleLabel}`} disabled={busy} onClick={() => revokeRole.mutate(r.id)}>
                        <Trash2 size={16} />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {canManage && (
          <AssignRow
            label="Perfil"
            busy={busy}
            options={(roles.data ?? []).map((r) => ({ id: r.id, label: r.label }))}
            onAssign={(roleId, from, until) => assignRole.mutate({ roleId, from, until })}
          />
        )}
      </Paper>

      <Paper component="section" aria-label="Gerências" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider' }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          Gerências (escopo do gestor)
        </Typography>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Gerência</TableCell>
              <TableCell>Validade</TableCell>
              <TableCell>Situação</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {access.data?.managementUnits.map((m) => (
              <TableRow key={m.id} data-testid="unit-row">
                <TableCell>
                  {m.code} – {m.name}
                </TableCell>
                <TableCell>{formatValidity(m.validFrom, m.validUntil)}</TableCell>
                <TableCell>
                  <ValidityChip state={m.state} />
                </TableCell>
                <TableCell align="right">
                  {canManage && (
                    <Tooltip title="Retirar gerência">
                      <IconButton aria-label={`Retirar ${m.code}`} disabled={busy} onClick={() => revokeUnit.mutate(m.id)}>
                        <Trash2 size={16} />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {canManage && (
          <AssignRow
            label="Gerência"
            busy={busy}
            options={(units.data ?? []).map((m) => ({ id: m.id, label: `${m.code} – ${m.name}` }))}
            onAssign={(unitId, from, until) => assignUnit.mutate({ unitId, from, until })}
          />
        )}
      </Paper>
    </Box>
  );
};

export default UserDetailPage;
