import React, { useState } from 'react';
import { useLocation, useParams, useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
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
import { ScrollText, Trash2 } from 'lucide-react';
import { DetailNav } from '../../../components/ui/PageHeader';
import { DetailHeader, DetailTabs, TabPanel } from '../../../components/ui/DetailHeader';
import { Field, SectionCard } from '../../../components/ui/SectionCard';
import { EmptyState, ErrorState } from '../../../components/ui/States';
import { UserStatusChip } from '../../../components/ui/StatusChip';
import { PaginationBar } from '../../../components/ui/DataTable';
import { AuditEntryCard } from '../../audit/components/AuditEntryCard';
import { auditApi, auditKeys } from '../../audit/api/auditApi';
import { label } from '../../../i18n/labels';
import { formatBoolean, formatDateTime, formatNumber } from '../../../utils/formatters';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { recordErrorMessage } from '../../records/api/recordsApi';
import {
  adminApi,
  adminKeys,
  formatValidity,
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

/** Atividades do usuário: registros da auditoria feitos por ele (exige Audit.View). */
const UserActivities: React.FC<{ userId: string }> = ({ userId }) => {
  const [page, setPage] = useState(1);
  const filters = { userId, pageNumber: page, pageSize: 20 };
  const trail = useQuery({ queryKey: auditKeys.search(filters), queryFn: () => auditApi.search(filters) });
  if (trail.isLoading) return <div className="h-40 animate-pulse rounded-lg bg-surface-2" aria-busy="true" aria-label="Carregando atividades" />;
  if (trail.isError) return <ErrorState message={(trail.error as Error).message} onRetry={() => trail.refetch()} />;
  const data = trail.data!;
  return (
    <section aria-label="Atividades" className="flex flex-col gap-3">
      <h2 className="text-base font-semibold text-ink">Atividades ({formatNumber(data.totalCount)})</h2>
      {data.items.length === 0 ? (
        <EmptyState message="Nenhuma atividade registrada para este usuário." icon={<ScrollText size={32} />} />
      ) : (
        <>
          {data.items.map((log) => (
            <AuditEntryCard key={log.id} log={log} />
          ))}
          <div className="overflow-hidden rounded-lg border border-line bg-surface">
            <PaginationBar pageNumber={data.pageNumber} pageSize={data.pageSize} totalCount={data.totalCount} onPageChange={setPage} noun="registros" />
          </div>
        </>
      )}
    </section>
  );
};

type TabKey = 'informacoes' | 'perfis' | 'atividades';

/**
 * Detalhe do usuário: status (ativar, desativar, desbloquear), perfis e gerências com validade.
 * Ninguém altera o próprio acesso (o servidor recusa; a tela nem mostra os botões).
 */
export const UserDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { user: me, hasPermission } = useAuth();
  const [params] = useSearchParams();
  const [tab, setTab] = useState<TabKey>(params.get('aba') === 'perfis' ? 'perfis' : 'informacoes');
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

  if (user.isLoading || access.isLoading) {
    return (
      <div className="flex flex-col gap-4" aria-busy="true" aria-label="Carregando usuário">
        <div className="h-16 w-1/2 animate-pulse rounded-lg bg-surface-2" />
        <div className="h-64 animate-pulse rounded-lg bg-surface-2" />
      </div>
    );
  }
  if (user.isError || !user.data) {
    return (
      <div>
        <DetailNav crumbs={[{ label: 'Usuários', to: '/admin/usuarios' }, { label: 'Detalhes' }]} backTo="/admin/usuarios" />
        <ErrorState message={recordErrorMessage(user.error, 'Usuário não encontrado.')} />
      </div>
    );
  }

  const u = user.data;
  const busy = assignRole.isPending || revokeRole.isPending || assignUnit.isPending || revokeUnit.isPending;
  const roleText = u.roles.length ? u.roles.map((r) => label('Role', r)).join(', ') : '—';
  const tabs: { key: TabKey; label: string }[] = [
    { key: 'informacoes', label: 'Informações' },
    { key: 'perfis', label: 'Perfis e gerências' },
    ...(hasPermission(PERMISSIONS.AUDIT_VIEW) ? [{ key: 'atividades' as const, label: 'Atividades' }] : []),
  ];

  return (
    <div>
      <DetailNav crumbs={[{ label: 'Usuários', to: '/admin/usuarios' }, { label: 'Detalhes' }]} backTo="/admin/usuarios" />
      <DetailHeader
        name={u.fullName}
        chip={<UserStatusChip status={u.status} />}
        subtitle={
          <>
            {u.email} · Perfil: {roleText}
          </>
        }
        actions={
          canChangeStatus && (
            <>
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
            </>
          )
        }
      />
      <div className="mb-4 flex flex-col gap-2">
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
      </div>

      <DetailTabs<TabKey> idPrefix="usuario" value={tab} onChange={setTab} tabs={tabs} />

      <TabPanel active={tab === 'informacoes'} idPrefix="usuario" tabKey="informacoes">
        <SectionCard title="Dados do usuário">
          <dl className="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Nome completo" value={u.fullName} />
            <Field label="E-mail" value={u.email} />
            <Field label="Situação" value={label('UserStatus', u.status)} />
            <Field label="Perfis" value={roleText} />
            <Field label="Último acesso" value={formatDateTime(u.lastLoginAt)} />
            <Field label="Cadastrado em" value={formatDateTime(u.createdAt)} />
            <Field label="Troca de senha obrigatória" value={formatBoolean(u.mustChangePassword)} />
            <Field label="Tentativas de entrada com falha" value={formatNumber(u.failedLoginAttempts)} />
            <Field label="Bloqueado até" value={formatDateTime(u.lockoutEnd)} />
          </dl>
        </SectionCard>
      </TabPanel>

      <TabPanel active={tab === 'perfis'} idPrefix="usuario" tabKey="perfis">
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
      </TabPanel>

      {hasPermission(PERMISSIONS.AUDIT_VIEW) && (
        <TabPanel active={tab === 'atividades'} idPrefix="usuario" tabKey="atividades">
          <UserActivities userId={id} />
        </TabPanel>
      )}
    </div>
  );
};

export default UserDetailPage;
