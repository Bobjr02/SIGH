import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  Box,
} from '@mui/material';

export type ActionType =
  | 'open'
  | 'startInvestigation'
  | 'submitForDecision'
  | 'approveDecision'
  | 'rejectDecision'
  | 'cancel'
  | 'conclude';

interface ConfirmActionModalProps {
  open: boolean;
  actionType: ActionType | null;
  onClose: () => void;
  onConfirm: (payload?: any) => Promise<void>;
  loading: boolean;
  errorMessage?: string | null;
}

export const ConfirmActionModal: React.FC<ConfirmActionModalProps> = ({
  open,
  actionType,
  onClose,
  onConfirm,
  loading,
  errorMessage,
}) => {
  const [inputText, setInputText] = useState('');
  const [fieldError, setFieldError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setInputText('');
      setFieldError(null);
    }
  }, [open]);

  if (!actionType) return null;

  const getActionConfig = () => {
    switch (actionType) {
      case 'open':
        return {
          title: 'Abrir Processo Disciplinar',
          description: 'Deseja alterar o status deste processo para "Aberto"?',
          requiresInput: false,
          inputLabel: 'Justificativa (Opcional)',
          buttonText: 'Abrir Processo',
          color: 'info' as const,
        };
      case 'startInvestigation':
        return {
          title: 'Iniciar Investigação Formal',
          description: 'Deseja iniciar a fase de investigação formal para este processo?',
          requiresInput: false,
          inputLabel: 'Observações / Investigador (Opcional)',
          buttonText: 'Iniciar Investigação',
          color: 'warning' as const,
        };
      case 'submitForDecision':
        return {
          title: 'Submeter para Decisão',
          description: 'Concluiu a instrução do processo e deseja encaminhá-lo para deliberação final?',
          requiresInput: false,
          inputLabel: 'Resumo / Observações (Opcional)',
          buttonText: 'Submeter para Decisão',
          color: 'secondary' as const,
        };
      case 'approveDecision':
        return {
          title: 'Aprovar Decisão Proferida',
          description: 'Confirma a aprovação da decisão proferida para este processo?',
          requiresInput: false,
          inputLabel: 'Observações do Aprovador (Opcional)',
          buttonText: 'Aprovar Decisão',
          color: 'success' as const,
        };
      case 'rejectDecision':
        return {
          title: 'Rejeitar Decisão Proferida',
          description: 'Por favor, informe a fundamentação para a rejeição da decisão.',
          requiresInput: true,
          minLength: 10,
          inputLabel: 'Justificativa da Rejeição *',
          buttonText: 'Rejeitar Decisão',
          color: 'error' as const,
        };
      case 'cancel':
        return {
          title: 'Cancelar Processo Disciplinar',
          description: 'Atenção: Esta ação cancelará o processo disciplinar. Informe o motivo do cancelamento.',
          requiresInput: true,
          minLength: 10,
          inputLabel: 'Motivo do Cancelamento *',
          buttonText: 'Cancelar Processo',
          color: 'error' as const,
        };
      case 'conclude':
        return {
          title: 'Concluir Processo Disciplinar',
          description: 'Confirma a conclusão do processo? Forneça um resumo do encerramento.',
          requiresInput: true,
          minLength: 10,
          inputLabel: 'Resumo da Conclusão *',
          buttonText: 'Concluir Processo',
          color: 'success' as const,
        };
    }
  };

  const config = getActionConfig();

  const handleConfirmClick = async () => {
    if (config.requiresInput) {
      if (!inputText || inputText.trim().length < (config.minLength || 5)) {
        setFieldError(`Por favor, preencha o campo com no mínimo ${config.minLength || 5} caracteres.`);
        return;
      }
    }

    setFieldError(null);

    let payload: any = {};
    if (actionType === 'cancel') payload = { justification: inputText };
    else if (actionType === 'conclude') payload = { summaryNotes: inputText };
    else if (actionType === 'rejectDecision') payload = { justification: inputText };
    else if (actionType === 'open') payload = { justification: inputText || undefined };
    else if (actionType === 'startInvestigation') payload = { notes: inputText || undefined };
    else if (actionType === 'submitForDecision') payload = { summaryNotes: inputText || undefined };
    else if (actionType === 'approveDecision') payload = { notes: inputText || undefined };

    await onConfirm(payload);
  };

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700 }}>{config.title}</DialogTitle>
      <DialogContent dividers>
        {errorMessage && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {errorMessage}
          </Alert>
        )}

        <Typography variant="body1" sx={{ mb: 2.5 }}>
          {config.description}
        </Typography>

        {(config.requiresInput || actionType === 'open' || actionType === 'startInvestigation' || actionType === 'submitForDecision' || actionType === 'approveDecision') && (
          <Box sx={{ mt: 1 }}>
            <TextField
              label={config.inputLabel}
              placeholder="Digite aqui..."
              fullWidth
              multiline
              rows={3}
              size="small"
              value={inputText}
              onChange={(e) => {
                setInputText(e.target.value);
                if (fieldError) setFieldError(null);
              }}
              error={!!fieldError}
              helperText={fieldError}
              disabled={loading}
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={loading} color="inherit">
          Cancelar
        </Button>
        <Button
          onClick={handleConfirmClick}
          variant="contained"
          color={config.color}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={18} color="inherit" /> : null}
        >
          {loading ? 'Processando...' : config.buttonText}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
