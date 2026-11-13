import React, { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Autocomplete, TextField } from '@mui/material';
import { employeeKeys, employeesApi, EmployeeStatus, type EmployeeListItem } from '../../employees/api/employeesApi';

interface EmployeePickerProps {
  value: EmployeeListItem | null;
  onChange: (employee: EmployeeListItem | null) => void;
  disabled?: boolean;
}

/** Busca por nome ou matrícula (no servidor, dentro do escopo do usuário). Desligados não aparecem. */
export const EmployeePicker: React.FC<EmployeePickerProps> = ({ value, onChange, disabled }) => {
  const [input, setInput] = useState('');
  const [term, setTerm] = useState('');

  useEffect(() => {
    const handle = setTimeout(() => setTerm(input.trim()), 300);
    return () => clearTimeout(handle);
  }, [input]);

  const filters = { searchTerm: term, pageSize: 20 };
  const { data, isFetching } = useQuery({
    queryKey: employeeKeys.list(filters),
    queryFn: () => employeesApi.list(filters),
    enabled: term.length >= 2,
  });

  const options = (data?.items ?? []).filter((e) => e.status !== EmployeeStatus.Terminated);

  return (
    <Autocomplete
      value={value}
      onChange={(_, next) => onChange(next)}
      inputValue={input}
      onInputChange={(_, next) => setInput(next)}
      options={value && !options.some((o) => o.id === value.id) ? [value, ...options] : options}
      getOptionLabel={(e) => `${e.employeeNumber} – ${e.fullName}`}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      filterOptions={(x) => x}
      loading={isFetching}
      disabled={disabled}
      noOptionsText={term.length < 2 ? 'Digite ao menos 2 caracteres' : 'Nenhum funcionário encontrado'}
      renderInput={(params) => <TextField {...params} label="Funcionário" required placeholder="Nome ou matrícula" />}
    />
  );
};
