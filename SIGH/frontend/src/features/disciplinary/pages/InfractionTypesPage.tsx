import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Snackbar,
  Alert,
  Paper,
  Breadcrumbs,
  Link,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
} from '@mui/material';
import { Plus, ShieldAlert } from 'lucide-react';
import { useInfractionTypes } from '../hooks/useInfractionTypes';
import { useDisciplinaryPermissions } from '../hooks/useDisciplinaryPermissions';
import { InfractionTypeFilter } from '../components/InfractionTypeFilter';
import { InfractionTypeTable } from '../components/InfractionTypeTable';
import { InfractionTypeFormModal } from '../components/InfractionTypeFormModal';
import { InfractionTypeDetailModal } from '../components/InfractionTypeDetailModal';
import { InfractionTypeDto } from '../types';

export const InfractionTypesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Modais state
  const [formModalOpen, setFormModalOpen] = useState(false);
  const [detailModalOpen, setDetailModalOpen] = useState(false);
  const [confirmDeactivateType, setConfirmDeactivateType] = useState<InfractionTypeDto | null>(null);
  const [deactivatingLoading, setDeactivatingLoading] = useState(false);
  const [deactivateError, setDeactivateError] = useState<string | null>(null);
  const [selectedType, setSelectedType] = useState<InfractionTypeDto | null>(null);

  // Notifications
  const [toast, setToast] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });
  const [actionError, setActionError] = useState<string | null>(null);

  const { canViewInfractionTypes, canCreateInfractionTypes } = useDisciplinaryPermissions();

  const queryParams = {
    page,
    pageSize,
    searchTerm: searchTerm || undefined,
    isActive: statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined,
  };

  const {
    infractionTypes,
    pagination,
    isLoading,
    isFetching,
    createInfractionType,
    updateInfractionType,
    activateInfractionType,
    deactivateInfractionType,
    extractErrorMessage,
  } = useInfractionTypes(queryParams);

  const handleOpenCreateModal = () => {
    setSelectedType(null);
    setActionError(null);
    setFormModalOpen(true);
  };

  const handleOpenEditModal = (type: InfractionTypeDto) => {
    setSelectedType(type);
    setActionError(null);
    setFormModalOpen(true);
  };

  const handleOpenDetailModal = (type: InfractionTypeDto) => {
    setSelectedType(type);
    setDetailModalOpen(true);
  };

  const handleFormSubmit = async (data: any) => {
    setActionError(null);
    try {
      if (selectedType) {
        await updateInfractionType.mutateAsync({ id: selectedType.id, data });
        setToast({ open: true, message: 'Tipo de infração atualizado com sucesso!', severity: 'success' });
      } else {
        await createInfractionType.mutateAsync(data);
        setToast({ open: true, message: 'Tipo de infração criado com sucesso!', severity: 'success' });
      }
      setFormModalOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const handleToggleStatus = async (type: InfractionTypeDto) => {
    if (type.isActive) {
      // Exige confirmação explícita para desativação
      setConfirmDeactivateType(type);
      setDeactivateError(null);
    } else {
      try {
        await activateInfractionType.mutateAsync(type.id);
        setToast({ open: true, message: 'Tipo de infração ativado.', severity: 'success' });
      } catch (err: any) {
        setToast({ open: true, message: extractErrorMessage(err), severity: 'error' });
      }
    }
  };

  const handleConfirmDeactivate = async () => {
    if (!confirmDeactivateType) return;
    setDeactivatingLoading(true);
    setDeactivateError(null);
    try {
      await deactivateInfractionType.mutateAsync(confirmDeactivateType.id);
      setToast({ open: true, message: 'Tipo de infração desativado com sucesso.', severity: 'success' });
      setConfirmDeactivateType(null);
    } catch (err: any) {
      setDeactivateError(extractErrorMessage(err));
    } finally {
      setDeactivatingLoading(false);
    }
  };

  const handleResetFilters = () => {
    setSearchTerm('');
    setStatusFilter('all');
    setPage(1);
  };

  if (!canViewInfractionTypes) {
    return (
      <Paper elevation={0} sx={{ p: 4, textAlign: 'center', border: '1px solid', borderColor: 'divider' }}>
        <ShieldAlert size={48} color="#ef4444" style={{ marginBottom: 16 }} />
        <Typography variant="h6" color="error" gutterBottom>
          Acesso Negado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Você não possui permissão para visualizar os tipos de infrações disciplinares.
        </Typography>
      </Paper>
    );
  }

  return (
    <Box sx={{ p: { xs: 2, md: 3 } }}>
      {/* Breadcrumb e Header */}
      <Breadcrumbs sx={{ mb: 1.5, fontSize: '0.85rem' }}>
        <Link color="inherit" underline="hover" href="/">
          SIGH
        </Link>
        <Typography color="text.primary">Módulo Disciplinar</Typography>
        <Typography color="text.primary" sx={{ fontWeight: 600 }}>
          Tipos de Infração
        </Typography>
      </Breadcrumbs>

      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          justifyContent: 'space-between',
          alignItems: { xs: 'flex-start', sm: 'center' },
          gap: 2,
          mb: 3,
        }}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800, color: '#1e293b' }}>
            Tipos de Infração
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Cadastro e parametrização das tipificações de infrações disciplinares
          </Typography>
        </Box>

        {canCreateInfractionTypes && (
          <Button
            variant="contained"
            color="primary"
            startIcon={<Plus size={18} />}
            onClick={handleOpenCreateModal}
            sx={{ fontWeight: 700, borderRadius: 2 }}
          >
            Novo Tipo de Infração
          </Button>
        )}
      </Box>

      {/* Painel de Filtros */}
      <InfractionTypeFilter
        searchTerm={searchTerm}
        onSearchChange={(val) => {
          setSearchTerm(val);
          setPage(1);
        }}
        statusFilter={statusFilter}
        onStatusFilterChange={(val) => {
          setStatusFilter(val);
          setPage(1);
        }}
        onReset={handleResetFilters}
      />

      {/* Tabela de Dados */}
      <InfractionTypeTable
        data={infractionTypes}
        loading={isLoading || isFetching}
        pageNumber={pagination.pageNumber}
        pageSize={pagination.pageSize}
        totalCount={pagination.totalCount}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onViewDetails={handleOpenDetailModal}
        onEdit={handleOpenEditModal}
        onToggleStatus={handleToggleStatus}
      />

      {/* Modal de Formulário (Criação/Edição) */}
      <InfractionTypeFormModal
        open={formModalOpen}
        onClose={() => setFormModalOpen(false)}
        onSubmit={handleFormSubmit}
        editingType={selectedType}
        loading={createInfractionType.isPending || updateInfractionType.isPending}
        errorMessage={actionError}
      />

      {/* Modal de Detalhes */}
      <InfractionTypeDetailModal
        open={detailModalOpen}
        onClose={() => setDetailModalOpen(false)}
        type={selectedType}
      />

      {/* Modal de Confirmação para Desativação */}
      <Dialog
        open={!!confirmDeactivateType}
        onClose={deactivatingLoading ? undefined : () => setConfirmDeactivateType(null)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle sx={{ fontWeight: 700 }}>Desativar Tipo de Infração</DialogTitle>
        <DialogContent dividers>
          {deactivateError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {deactivateError}
            </Alert>
          )}
          <Typography variant="body1">
            Tem certeza que deseja desativar o tipo de infração{' '}
            <strong>{confirmDeactivateType?.name}</strong>?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Esta ação impedirá que este tipo de infração seja selecionado em novos processos disciplinares.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button
            onClick={() => setConfirmDeactivateType(null)}
            disabled={deactivatingLoading}
            color="inherit"
          >
            Cancelar
          </Button>
          <Button
            onClick={handleConfirmDeactivate}
            variant="contained"
            color="error"
            disabled={deactivatingLoading}
            startIcon={deactivatingLoading ? <CircularProgress size={18} color="inherit" /> : null}
          >
            {deactivatingLoading ? 'Desativando...' : 'Confirmar Desativação'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Toast Feedback */}
      <Snackbar
        open={toast.open}
        autoHideDuration={5000}
        onClose={() => setToast((prev) => ({ ...prev, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          onClose={() => setToast((prev) => ({ ...prev, open: false }))}
          severity={toast.severity}
          variant="filled"
          sx={{ width: '100%' }}
        >
          {toast.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};
