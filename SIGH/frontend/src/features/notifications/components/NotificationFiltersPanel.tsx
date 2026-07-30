import React from 'react';
import {
  Paper,
  Box,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
} from '@mui/material';
import { FilterX } from 'lucide-react';
import { NotificationFilters } from '../types';

interface NotificationFiltersPanelProps {
  filters: NotificationFilters;
  onChange: (newFilters: Partial<NotificationFilters>) => void;
  onReset: () => void;
}

export const NotificationFiltersPanel: React.FC<NotificationFiltersPanelProps> = ({
  filters,
  onChange,
  onReset,
}) => {
  return (
    <Paper sx={{ p: 2, mb: 3, borderRadius: 2 }}>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: 'repeat(4, 1fr)' }, gap: 2, alignItems: 'center' }}>
        <FormControl fullWidth size="small">
          <InputLabel>Status</InputLabel>
          <Select
            value={filters.isRead === undefined || filters.isRead === null ? 'all' : filters.isRead ? 'read' : 'unread'}
            label="Status"
            onChange={(e) => {
              const val = e.target.value;
              onChange({
                isRead: val === 'all' ? null : val === 'read' ? true : false,
                pageNumber: 1,
              });
            }}
          >
            <MenuItem value="all">Todas</MenuItem>
            <MenuItem value="unread">Apenas Não Lidas</MenuItem>
            <MenuItem value="read">Apenas Lidas</MenuItem>
          </Select>
        </FormControl>

        <FormControl fullWidth size="small">
          <InputLabel>Tipo</InputLabel>
          <Select
            value={filters.type || ''}
            label="Tipo"
            onChange={(e) => onChange({ type: e.target.value || null, pageNumber: 1 })}
          >
            <MenuItem value="">Todos os tipos</MenuItem>
            <MenuItem value="Info">Info</MenuItem>
            <MenuItem value="Reminder">Reminder (Lembrete)</MenuItem>
            <MenuItem value="Escalation">Escalation (Escalonamento)</MenuItem>
            <MenuItem value="Warning">Warning (Alerta)</MenuItem>
          </Select>
        </FormControl>

        <FormControl fullWidth size="small">
          <InputLabel>Prioridade</InputLabel>
          <Select
            value={filters.priority || ''}
            label="Prioridade"
            onChange={(e) => onChange({ priority: e.target.value || null, pageNumber: 1 })}
          >
            <MenuItem value="">Todas as prioridades</MenuItem>
            <MenuItem value="Low">Baixa</MenuItem>
            <MenuItem value="Medium">Média</MenuItem>
            <MenuItem value="High">Alta</MenuItem>
            <MenuItem value="Critical">Crítica</MenuItem>
          </Select>
        </FormControl>

        <FormControl fullWidth size="small">
          <InputLabel>Ordenação</InputLabel>
          <Select
            value={`${filters.sortBy || 'CreatedAt'}_${filters.sortDirection || 'desc'}`}
            label="Ordenação"
            onChange={(e) => {
              const [sortBy, sortDirection] = e.target.value.split('_');
              onChange({ sortBy, sortDirection: sortDirection as 'asc' | 'desc', pageNumber: 1 });
            }}
          >
            <MenuItem value="CreatedAt_desc">Mais recentes primeiro</MenuItem>
            <MenuItem value="CreatedAt_asc">Mais antigas primeiro</MenuItem>
            <MenuItem value="Priority_desc">Prioridade (Maior primeiro)</MenuItem>
            <MenuItem value="DueDate_asc">Vencimento (Mais próximo)</MenuItem>
          </Select>
        </FormControl>
      </Box>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
        <Button
          size="small"
          variant="text"
          startIcon={<FilterX size={16} />}
          onClick={onReset}
          color="inherit"
        >
          Limpar Filtros
        </Button>
      </Box>
    </Paper>
  );
};
