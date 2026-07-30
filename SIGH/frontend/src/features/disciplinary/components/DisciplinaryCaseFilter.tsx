import React from 'react';
import {
  Box,
  TextField,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Button,
  Grid,
  Paper,
} from '@mui/material';
import { Search, RotateCcw, Filter } from 'lucide-react';
import { DisciplinaryCaseStatus, DisciplinaryCasePriority } from '../types';

interface DisciplinaryCaseFilterProps {
  companyId: string;
  onCompanyIdChange: (val: string) => void;
  status: string;
  onStatusChange: (val: string) => void;
  severity: string;
  onSeverityChange: (val: string) => void;
  searchTerm: string;
  onSearchTermChange: (val: string) => void;
  startDate: string;
  onStartDateChange: (val: string) => void;
  endDate: string;
  onEndDateChange: (val: string) => void;
  responsibleId?: string;
  onResponsibleIdChange?: (val: string) => void;
  employeeId?: string;
  onEmployeeIdChange?: (val: string) => void;
  onReset: () => void;
}

export const DisciplinaryCaseFilter: React.FC<DisciplinaryCaseFilterProps> = ({
  companyId,
  onCompanyIdChange,
  status,
  onStatusChange,
  severity,
  onSeverityChange,
  searchTerm,
  onSearchTermChange,
  startDate,
  onStartDateChange,
  endDate,
  onEndDateChange,
  responsibleId = '',
  onResponsibleIdChange,
  employeeId = '',
  onEmployeeIdChange,
  onReset,
}) => {
  return (
    <Paper
      elevation={0}
      sx={{
        p: 2.5,
        mb: 3,
        backgroundColor: '#f8fafc',
        borderRadius: 2,
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Filter size={18} color="#475569" />
        <Box component="span" sx={{ fontWeight: 700, color: 'text.secondary', fontSize: '0.9rem' }}>
          Filtros de Pesquisa
        </Box>
      </Box>

      <Grid container spacing={2} sx={{ alignItems: 'center' }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <TextField
            size="small"
            label="Termo de Busca"
            placeholder="Número ou Título..."
            value={searchTerm}
            onChange={(e) => onSearchTermChange(e.target.value)}
            fullWidth
            slotProps={{
              input: {
                startAdornment: <Search size={16} style={{ marginRight: 6, color: '#94a3b8' }} />,
              },
            }}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 2 }}>
          <TextField
            size="small"
            label="ID da Empresa"
            placeholder="UUID da empresa"
            value={companyId}
            onChange={(e) => onCompanyIdChange(e.target.value)}
            fullWidth
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 2 }}>
          <FormControl size="small" fullWidth>
            <InputLabel id="case-status-label">Status</InputLabel>
            <Select
              labelId="case-status-label"
              label="Status"
              value={status}
              onChange={(e) => onStatusChange(e.target.value)}
            >
              <MenuItem value="all">Todos os Status</MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.Draft.toString()}>Rascunho</MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.Open.toString()}>Aberto</MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.UnderInvestigation.toString()}>
                Em Investigação
              </MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.AwaitingDecision.toString()}>
                Aguardando Decisão
              </MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.Decided.toString()}>Decidido</MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.Completed.toString()}>Concluído</MenuItem>
              <MenuItem value={DisciplinaryCaseStatus.Cancelled.toString()}>Cancelado</MenuItem>
            </Select>
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 2 }}>
          <FormControl size="small" fullWidth>
            <InputLabel id="case-priority-label">Prioridade</InputLabel>
            <Select
              labelId="case-priority-label"
              label="Prioridade"
              value={severity}
              onChange={(e) => onSeverityChange(e.target.value)}
            >
              <MenuItem value="all">Todas</MenuItem>
              <MenuItem value={DisciplinaryCasePriority.Low.toString()}>Baixa</MenuItem>
              <MenuItem value={DisciplinaryCasePriority.Normal.toString()}>Média/Normal</MenuItem>
              <MenuItem value={DisciplinaryCasePriority.High.toString()}>Alta</MenuItem>
              <MenuItem value={DisciplinaryCasePriority.Critical.toString()}>Crítica</MenuItem>
            </Select>
          </FormControl>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 1.5 }}>
          <TextField
            size="small"
            type="date"
            label="Data Inicial"
            value={startDate}
            onChange={(e) => onStartDateChange(e.target.value)}
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 1.5 }}>
          <TextField
            size="small"
            type="date"
            label="Data Final"
            value={endDate}
            onChange={(e) => onEndDateChange(e.target.value)}
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <TextField
            size="small"
            label="ID do Responsável (ResponsibleEmployeeId)"
            placeholder="UUID do responsável..."
            value={responsibleId}
            onChange={(e) => onResponsibleIdChange && onResponsibleIdChange(e.target.value)}
            fullWidth
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <TextField
            size="small"
            label="ID do Envolvido (EmployeeId)"
            placeholder="UUID do funcionário envolvido..."
            value={employeeId}
            onChange={(e) => onEmployeeIdChange && onEmployeeIdChange(e.target.value)}
            fullWidth
          />
        </Grid>

        <Grid size={{ xs: 12, md: 6 }} sx={{ display: 'flex', justifyContent: 'flex-end', ml: 'auto' }}>
          <Button
            variant="outlined"
            size="small"
            startIcon={<RotateCcw size={16} />}
            onClick={onReset}
            sx={{ color: 'text.secondary', borderColor: 'divider' }}
          >
            Limpar Filtros
          </Button>
        </Grid>
      </Grid>
    </Paper>
  );
};
