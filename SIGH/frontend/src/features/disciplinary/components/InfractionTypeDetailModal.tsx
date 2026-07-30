import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Chip,
  Grid,
  Divider,
} from '@mui/material';
import { InfractionTypeDto } from '../types';
import { CaseSeverityChip } from './CaseSeverityChip';

interface InfractionTypeDetailModalProps {
  open: boolean;
  onClose: () => void;
  type: InfractionTypeDto | null;
}

export const InfractionTypeDetailModal: React.FC<InfractionTypeDetailModalProps> = ({
  open,
  onClose,
  type,
}) => {
  if (!type) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <span>Detalhes do Tipo de Infração</span>
        <Chip
          label={type.isActive ? 'Ativo' : 'Inativo'}
          color={type.isActive ? 'success' : 'default'}
          size="small"
        />
      </DialogTitle>
      <DialogContent dividers>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <Typography variant="caption" color="text.secondary">
              Código
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>
              {type.code}
            </Typography>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Typography variant="caption" color="text.secondary">
              Gravidade Padrão
            </Typography>
            <Box sx={{ mt: 0.5 }}>
              <CaseSeverityChip severity={type.defaultSeverity} />
            </Box>
          </Grid>

          <Grid size={{ xs: 12 }}>
            <Typography variant="caption" color="text.secondary">
              Nome
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 600 }}>
              {type.name}
            </Typography>
          </Grid>

          <Grid size={{ xs: 12 }}>
            <Divider sx={{ my: 1 }} />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Typography variant="caption" color="text.secondary">
              Exige Investigação Formal
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {type.requiresFormalInvestigation ? 'Sim' : 'Não'}
            </Typography>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Typography variant="caption" color="text.secondary">
              Permite Recomendação de Rescisão
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {type.allowsTerminationRecommendation ? 'Sim' : 'Não'}
            </Typography>
          </Grid>

          {type.legalReference && (
            <Grid size={{ xs: 12 }}>
              <Typography variant="caption" color="text.secondary">
                Referência Legal
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                {type.legalReference}
              </Typography>
            </Grid>
          )}

          {type.description && (
            <Grid size={{ xs: 12 }}>
              <Typography variant="caption" color="text.secondary">
                Descrição
              </Typography>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', color: 'text.primary' }}>
                {type.description}
              </Typography>
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 1.5 }}>
        <Button onClick={onClose} variant="contained">
          Fechar
        </Button>
      </DialogActions>
    </Dialog>
  );
};
