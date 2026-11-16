import React from 'react';
import { Step, StepLabel, Stepper } from '@mui/material';

export const IMPORT_STEPS = ['Selecionar arquivo', 'Pré-visualizar', 'Confirmar importação'] as const;

/**
 * Indicador de etapas das telas de importação (passo 1 escolher, 2 pré-visualizar, 3 confirmar).
 * `active` vai de 0 a 2; 3 marca as três como concluídas (importação aplicada).
 */
export const ImportSteps: React.FC<{ active: number; label?: string }> = ({ active, label = 'Etapas da importação' }) => (
  <Stepper
    activeStep={active}
    aria-label={label}
    role="list"
    data-testid="import-steps"
    sx={{
      '& .MuiStepLabel-label': { color: 'var(--sigh-text-muted)', fontWeight: 500 },
      '& .MuiStepLabel-label.Mui-active, & .MuiStepLabel-label.Mui-completed': { color: 'var(--sigh-text)', fontWeight: 600 },
      '& .MuiStepIcon-root': { color: 'var(--sigh-surface3)' },
      '& .MuiStepIcon-root .MuiStepIcon-text': { fill: 'var(--sigh-text)' },
      '& .MuiStepIcon-root.Mui-active, & .MuiStepIcon-root.Mui-completed': { color: 'var(--sigh-primary)' },
      '& .MuiStepIcon-root.Mui-active .MuiStepIcon-text': { fill: 'var(--sigh-on-primary)' },
      '& .MuiStepConnector-line': { borderColor: 'var(--sigh-border-strong)' },
    }}
  >
    {IMPORT_STEPS.map((step, index) => (
      <Step
        key={step}
        role="listitem"
        aria-current={index === active ? 'step' : undefined}
        aria-label={`Passo ${index + 1}: ${step}${index < active ? ' (concluído)' : index === active ? ' (atual)' : ''}`}
      >
        <StepLabel>{step}</StepLabel>
      </Step>
    ))}
  </Stepper>
);

export default ImportSteps;
