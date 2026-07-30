import React from 'react';
import { Box, TextField, MenuItem, Select, FormControl, InputLabel, Button } from '@mui/material';
import { Search, RotateCcw } from 'lucide-react';

interface InfractionTypeFilterProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  statusFilter: string;
  onStatusFilterChange: (value: string) => void;
  onReset: () => void;
}

export const InfractionTypeFilter: React.FC<InfractionTypeFilterProps> = ({
  searchTerm,
  onSearchChange,
  statusFilter,
  onStatusFilterChange,
  onReset,
}) => {
  return (
    <Box
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 2,
        alignItems: 'center',
        mb: 3,
        p: 2,
        backgroundColor: '#f8fafc',
        borderRadius: 2,
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      <TextField
        size="small"
        placeholder="Buscar por código ou nome..."
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        sx={{ minWidth: 280, flexGrow: 1 }}
        slotProps={{
          input: {
            startAdornment: <Search size={18} style={{ marginRight: 8, color: '#64748b' }} />,
          },
        }}
      />

      <FormControl size="small" sx={{ minWidth: 160 }}>
        <InputLabel id="status-filter-label">Status</InputLabel>
        <Select
          labelId="status-filter-label"
          label="Status"
          value={statusFilter}
          onChange={(e) => onStatusFilterChange(e.target.value)}
        >
          <MenuItem value="all">Todos</MenuItem>
          <MenuItem value="active">Ativos</MenuItem>
          <MenuItem value="inactive">Inativos</MenuItem>
        </Select>
      </FormControl>

      <Button
        variant="outlined"
        size="small"
        startIcon={<RotateCcw size={16} />}
        onClick={onReset}
        sx={{ color: 'text.secondary', borderColor: 'divider' }}
      >
        Limpar
      </Button>
    </Box>
  );
};
