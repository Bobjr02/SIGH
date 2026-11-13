import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Box, Button, Paper, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { recordErrorMessage } from '../../records/api/recordsApi';
import { adminApi, adminKeys, type ManagementUnit } from '../api/adminApi';

const UnitRow: React.FC<{ unit: ManagementUnit; canEdit: boolean }> = ({ unit, canEdit }) => {
  const queryClient = useQueryClient();
  const [name, setName] = useState(unit.name);
  const [error, setError] = useState<string | null>(null);
  const save = useMutation({
    mutationFn: () => adminApi.renameUnit(unit.id, name.trim()),
    onSuccess: () => {
      setError(null);
      return queryClient.invalidateQueries({ queryKey: adminKeys.units });
    },
    onError: (e) => setError(recordErrorMessage(e, 'Não foi possível salvar.')),
  });

  return (
    <TableRow data-testid="management-unit-row">
      <TableCell sx={{ fontWeight: 600 }}>{unit.code}</TableCell>
      <TableCell>
        {canEdit ? (
          <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
            <TextField size="small" value={name} onChange={(e) => setName(e.target.value)} slotProps={{ htmlInput: { 'aria-label': `Nome da ${unit.code}` } }} />
            <Button size="small" disabled={!name.trim() || name.trim() === unit.name || save.isPending} onClick={() => save.mutate()}>
              Salvar
            </Button>
          </Box>
        ) : (
          unit.name
        )}
        {error && <Alert severity="error">{error}</Alert>}
      </TableCell>
    </TableRow>
  );
};

/** As 9 gerências (Chave Organização do SAP), com edição do nome. */
export const ManagementUnitsPage: React.FC = () => {
  const { hasPermission } = useAuth();
  const units = useQuery({ queryKey: adminKeys.units, queryFn: adminApi.units });
  const canEdit = hasPermission(PERMISSIONS.MANAGEMENT_UNITS_MANAGE);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        Gerências
      </Typography>
      {units.isError && <Alert severity="error">{recordErrorMessage(units.error, 'Erro ao carregar as gerências.')}</Alert>}
      <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider' }}>
        <Table size="small" aria-label="Gerências">
          <TableHead>
            <TableRow>
              <TableCell>Chave Organização</TableCell>
              <TableCell>Nome</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {units.data?.map((u) => (
              <UnitRow key={`${u.id}-${u.name}`} unit={u} canEdit={canEdit} />
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
};

export default ManagementUnitsPage;
