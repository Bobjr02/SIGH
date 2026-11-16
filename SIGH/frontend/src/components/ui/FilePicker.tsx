import React, { useId, useRef, useState } from 'react';
import { Box, Button, Typography } from '@mui/material';
import { FileText, RefreshCw, Upload, X } from 'lucide-react';
import { formatBytes } from '../../features/disciplinary/evidence/evidenceFiles';

export interface FilePickerProps {
  /** Nome acessível do <input type="file"> (ex.: "Planilha do SAP"). */
  label: string;
  /** Texto do botão (ex.: "Selecionar arquivo .xlsx"). */
  buttonText?: string;
  /** Extensões aceitas, sem ponto e em minúsculas (ex.: ['xlsx']). Vazio = qualquer uma. */
  extensions: string[];
  /** Valor do atributo accept do input (padrão: as extensões com ponto). */
  accept?: string;
  /** Mensagem para extensão errada (padrão: "Formato não aceito. Envie um arquivo .xlsx."). */
  extensionMessage?: string;
  /** Validação extra no cliente (tamanho, tipo da evidência…): devolve a mensagem de erro ou null. */
  validate?: (file: File) => string | null;
  /** Texto de apoio abaixo da área (formatos, limite de tamanho). */
  hint?: React.ReactNode;
  file: File | null;
  onChange: (file: File | null) => void;
  disabled?: boolean;
  /** data-testid do input nativo (os testes de ponta a ponta enviam o arquivo direto nele). */
  inputTestId?: string;
}

const extensionOf = (name: string) => {
  const dot = name.lastIndexOf('.');
  return dot >= 0 ? name.slice(dot + 1).toLowerCase() : '';
};

const listOf = (extensions: string[]) => {
  const items = extensions.map((e) => `.${e}`);
  return items.length <= 1 ? (items[0] ?? '') : `${items.slice(0, -1).join(', ')} ou ${items[items.length - 1]}`;
};

/**
 * Seletor de arquivo do SIGH: botão com ícone, área de arrastar e soltar e, depois da escolha, nome,
 * tamanho e os botões "Trocar" e "Remover". O <input type="file"> nativo fica visualmente oculto
 * (fora da ordem de tabulação) e é aberto pelo botão, que é um <button> de verdade: foco visível do
 * tema e ativação por Enter/Espaço. A extensão é conferida aqui só para orientar o usuário; a
 * validação que vale continua no servidor.
 */
export const FilePicker: React.FC<FilePickerProps> = ({
  label,
  buttonText = 'Selecionar arquivo',
  extensions,
  accept,
  extensionMessage,
  validate,
  hint,
  file,
  onChange,
  disabled = false,
  inputTestId,
}) => {
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);

  const open = () => input.current?.click();

  const choose = (selected: File | null) => {
    if (!selected) return;
    const wrongExtension = extensions.length > 0 && !extensions.includes(extensionOf(selected.name));
    const message = wrongExtension
      ? (extensionMessage ?? `Formato não aceito. Envie um arquivo ${listOf(extensions)}.`)
      : (validate?.(selected) ?? null);
    setError(message);
    onChange(message ? null : selected);
  };

  const remove = () => {
    setError(null);
    onChange(null);
  };

  const onDrop = (event: React.DragEvent) => {
    event.preventDefault();
    setDragging(false);
    if (!disabled) choose(event.dataTransfer.files?.[0] ?? null);
  };

  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(' ') || undefined;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
      <input
        ref={input}
        id={id}
        type="file"
        className="sr-only"
        tabIndex={-1}
        accept={accept ?? extensions.map((e) => `.${e}`).join(',')}
        aria-label={label}
        aria-describedby={describedBy}
        disabled={disabled}
        data-testid={inputTestId}
        onChange={(e) => {
          choose(e.target.files?.[0] ?? null);
          // Permite escolher de novo o mesmo arquivo (ex.: depois de "Remover").
          e.target.value = '';
        }}
      />
      <Box
        data-testid="file-picker"
        data-dragging={dragging || undefined}
        onDragOver={(e) => {
          e.preventDefault();
          if (!disabled) setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        sx={{
          border: '2px dashed',
          borderColor: error ? 'var(--sigh-danger)' : dragging ? 'var(--sigh-primary-text)' : 'var(--sigh-border-strong)',
          borderRadius: 'var(--sigh-radius-md)',
          backgroundColor: dragging ? 'var(--sigh-primary-soft)' : 'var(--sigh-surface2)',
          p: 2.5,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          flexWrap: 'wrap',
          opacity: disabled ? 0.7 : 1,
          transition: 'background-color 120ms, border-color 120ms',
        }}
      >
        {file ? (
          <>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                flex: '1 1 240px',
                minWidth: 0,
              }}
            >
              <FileText size={28} color="var(--sigh-primary-text)" aria-hidden />
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, wordBreak: 'break-all' }} data-testid="file-picker-name">
                  {file.name}
                </Typography>
                <Typography variant="caption" color="text.secondary" data-testid="file-picker-size">
                  {formatBytes(file.size)}
                </Typography>
              </Box>
            </Box>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                variant="outlined"
                size="small"
                startIcon={<RefreshCw size={14} />}
                onClick={open}
                disabled={disabled}
                aria-label={`Trocar arquivo (${file.name})`}
              >
                Trocar
              </Button>
              <Button
                variant="outlined"
                size="small"
                color="error"
                startIcon={<X size={14} />}
                onClick={remove}
                disabled={disabled}
                aria-label={`Remover arquivo (${file.name})`}
                sx={{
                  color: 'var(--sigh-danger)',
                  borderColor: 'var(--sigh-danger)',
                }}
              >
                Remover
              </Button>
            </Box>
          </>
        ) : (
          <>
            <Button variant="contained" startIcon={<Upload size={16} />} onClick={open} disabled={disabled} aria-describedby={describedBy}>
              {buttonText}
            </Button>
            <Typography variant="body2" color="text.secondary">
              ou arraste o arquivo aqui
            </Typography>
          </>
        )}
      </Box>
      {hint && (
        <Typography id={hintId} variant="caption" color="text.secondary">
          {hint}
        </Typography>
      )}
      {error && (
        <Typography id={errorId} role="alert" variant="body2" sx={{ color: 'var(--sigh-danger)', fontWeight: 500 }}>
          {error}
        </Typography>
      )}
    </Box>
  );
};

export default FilePicker;
