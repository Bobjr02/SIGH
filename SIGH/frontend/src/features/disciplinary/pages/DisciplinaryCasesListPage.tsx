import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Button,
  Paper,
  Breadcrumbs,
  Link,
  Snackbar,
  Alert,
} from '@mui/material';
import { Plus, ShieldAlert } from 'lucide-react';
import { useDisciplinaryCases } from '../hooks/useDisciplinaryCases';
import { useDisciplinaryPermissions } from '../hooks/useDisciplinaryPermissions';
import { DisciplinaryCaseFilter } from '../components/DisciplinaryCaseFilter';
import { DisciplinaryCaseTable } from '../components/DisciplinaryCaseTable';

export const DisciplinaryCasesListPage: React.FC = () => {
  const navigate = useNavigate();

  const [companyId, setCompanyId] = useState('');
  const [status, setStatus] = useState('all');
  const [severity, setSeverity] = useState('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [responsibleId, setResponsibleId] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [toast, setToast] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });

  const { canViewCases, canCreateCases } = useDisciplinaryPermissions();

  const queryParams = {
    page,
    pageSize,
    companyId: companyId || undefined,
    status: status !== 'all' ? parseInt(status, 10) : undefined,
    severity: severity !== 'all' ? parseInt(severity, 10) : undefined,
    searchTerm: searchTerm || undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
    responsibleId: responsibleId || undefined,
    responsibleEmployeeId: responsibleId || undefined,
    employeeId: employeeId || undefined,
  };

  const { cases, pagination, isLoading, isFetching } = useDisciplinaryCases(queryParams);

  const handleResetFilters = () => {
    setCompanyId('');
    setStatus('all');
    setSeverity('all');
    setSearchTerm('');
    setStartDate('');
    setEndDate('');
    setResponsibleId('');
    setEmployeeId('');
    setPage(1);
  };

  const handleViewCase = (id: string) => {
    navigate(`/disciplinary/cases/${id}`);
  };

  if (!canViewCases) {
    return (
      <Paper elevation={0} sx={{ p: 4, textAlign: 'center', border: '1px solid', borderColor: 'divider' }}>
        <ShieldAlert size={48} color="#ef4444" style={{ marginBottom: 16 }} />
        <Typography variant="h6" color="error" gutterBottom>
          Acesso Negado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Você não possui permissão para visualizar os processos disciplinares.
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
          Processos Disciplinares
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
            Processos Disciplinares
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Gestão do ciclo de vida de investigações, pareceres e medidas disciplinares
          </Typography>
        </Box>

        {canCreateCases && (
          <Button
            variant="contained"
            color="primary"
            startIcon={<Plus size={18} />}
            onClick={() => navigate('/disciplinary/cases/new')}
            sx={{ fontWeight: 700, borderRadius: 2 }}
          >
            Novo Processo Disciplinar
          </Button>
        )}
      </Box>

      {/* Filtros */}
      <DisciplinaryCaseFilter
        companyId={companyId}
        onCompanyIdChange={(val) => {
          setCompanyId(val);
          setPage(1);
        }}
        status={status}
        onStatusChange={(val) => {
          setStatus(val);
          setPage(1);
        }}
        severity={severity}
        onSeverityChange={(val) => {
          setSeverity(val);
          setPage(1);
        }}
        searchTerm={searchTerm}
        onSearchTermChange={(val) => {
          setSearchTerm(val);
          setPage(1);
        }}
        startDate={startDate}
        onStartDateChange={(val) => {
          setStartDate(val);
          setPage(1);
        }}
        endDate={endDate}
        onEndDateChange={(val) => {
          setEndDate(val);
          setPage(1);
        }}
        responsibleId={responsibleId}
        onResponsibleIdChange={(val) => {
          setResponsibleId(val);
          setPage(1);
        }}
        employeeId={employeeId}
        onEmployeeIdChange={(val) => {
          setEmployeeId(val);
          setPage(1);
        }}
        onReset={handleResetFilters}
      />

      {/* Tabela */}
      <DisciplinaryCaseTable
        data={cases}
        loading={isLoading || isFetching}
        pageNumber={pagination.pageNumber}
        pageSize={pagination.pageSize}
        totalCount={pagination.totalCount}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onViewCase={handleViewCase}
      />

      {/* Feedback Toast */}
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
