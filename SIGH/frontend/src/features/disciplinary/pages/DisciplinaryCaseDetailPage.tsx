import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Button,
  Paper,
  Breadcrumbs,
  Link,
  Grid,
  Chip,
  Alert,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Snackbar,
  Card,
  CardContent,
} from '@mui/material';
import {
  ArrowLeft,
  Plus,
  Play,
  Search,
  FileCheck,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Users,
  FileText,
  Gavel,
  ShieldAlert,
  Calendar,
  Building,
} from 'lucide-react';
import { useDisciplinaryCaseDetail } from '../hooks/useDisciplinaryCases';
import { useInfractionTypes } from '../hooks/useInfractionTypes';
import { useDisciplinaryPermissions } from '../hooks/useDisciplinaryPermissions';
import { CaseStatusChip } from '../components/CaseStatusChip';
import { CaseSeverityChip } from '../components/CaseSeverityChip';
import { AddOccurrenceModal } from '../components/AddOccurrenceModal';
import { AddEmployeeModal } from '../components/AddEmployeeModal';
import { AddEvidenceModal } from '../components/AddEvidenceModal';
import { RecordDecisionModal } from '../components/RecordDecisionModal';
import { ApplyMeasureModal } from '../components/ApplyMeasureModal';
import { ConfirmActionModal, ActionType } from '../components/ConfirmActionModal';
import {
  DisciplinaryCaseStatus,
  CaseEmployeeRole,
  EvidenceType,
  DecisionType,
  DecisionStatus,
  DisciplinaryMeasureType,
} from '../types';

export const DisciplinaryCaseDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const permissions = useDisciplinaryPermissions();
  const { infractionTypes } = useInfractionTypes({ page: 1, pageSize: 100 });

  const {
    caseDetail,
    isLoading,
    isError,
    error,
    openCase,
    startInvestigation,
    submitForDecision,
    recordDecision,
    approveDecision,
    rejectDecision,
    addOccurrence,
    addEmployee,
    addEvidence,
    applyMeasure,
    cancelCase,
    concludeCase,
    extractErrorMessage,
  } = useDisciplinaryCaseDetail(id);

  // Modais state
  const [confirmModalOpen, setConfirmModalOpen] = useState(false);
  const [currentActionType, setCurrentActionType] = useState<ActionType | null>(null);

  const [addOccurrenceOpen, setAddOccurrenceOpen] = useState(false);
  const [addEmployeeOpen, setAddEmployeeOpen] = useState(false);
  const [addEvidenceOpen, setAddEvidenceOpen] = useState(false);
  const [recordDecisionOpen, setRecordDecisionOpen] = useState(false);
  const [applyMeasureOpen, setApplyMeasureOpen] = useState(false);

  // Feedback State
  const [actionError, setActionError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });

  if (!permissions.canViewCases) {
    return (
      <Paper elevation={0} sx={{ p: 4, textAlign: 'center', border: '1px solid', borderColor: 'divider', m: 3 }}>
        <ShieldAlert size={48} color="#ef4444" style={{ marginBottom: 16 }} />
        <Typography variant="h6" color="error" gutterBottom>
          Acesso Negado
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Você não possui permissão para visualizar este processo disciplinar.
        </Typography>
      </Paper>
    );
  }

  if (isLoading) {
    return (
      <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
        <Skeleton height={40} width="30%" sx={{ mb: 2 }} />
        <Skeleton height={150} sx={{ mb: 3 }} />
        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 6 }}>
            <Skeleton height={250} />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <Skeleton height={250} />
          </Grid>
        </Grid>
      </Box>
    );
  }

  if (isError || !caseDetail) {
    return (
      <Box sx={{ p: 3, maxWidth: 800, mx: 'auto', textAlign: 'center' }}>
        <Alert severity="error" sx={{ mb: 3 }}>
          {error ? extractErrorMessage(error) : 'Processo não encontrado.'}
        </Alert>
        <Button variant="outlined" startIcon={<ArrowLeft size={16} />} onClick={() => navigate('/disciplinary/cases')}>
          Voltar para Lista
        </Button>
      </Box>
    );
  }

  // Modais trigger helpers
  const handleOpenConfirm = (action: ActionType) => {
    setActionError(null);
    setCurrentActionType(action);
    setConfirmModalOpen(true);
  };

  const handleConfirmAction = async (payload?: any) => {
    setActionError(null);
    try {
      if (currentActionType === 'open') {
        await openCase.mutateAsync(payload);
        setToast({ open: true, message: 'Processo aberto com sucesso!', severity: 'success' });
      } else if (currentActionType === 'startInvestigation') {
        await startInvestigation.mutateAsync(payload);
        setToast({ open: true, message: 'Investigação iniciada!', severity: 'success' });
      } else if (currentActionType === 'submitForDecision') {
        await submitForDecision.mutateAsync(payload);
        setToast({ open: true, message: 'Processo submetido para decisão!', severity: 'success' });
      } else if (currentActionType === 'approveDecision') {
        await approveDecision.mutateAsync(payload);
        setToast({ open: true, message: 'Decisão aprovada com sucesso!', severity: 'success' });
      } else if (currentActionType === 'rejectDecision') {
        await rejectDecision.mutateAsync(payload);
        setToast({ open: true, message: 'Decisão rejeitada.', severity: 'success' });
      } else if (currentActionType === 'cancel') {
        await cancelCase.mutateAsync(payload);
        setToast({ open: true, message: 'Processo disciplinar cancelado.', severity: 'success' });
      } else if (currentActionType === 'conclude') {
        await concludeCase.mutateAsync(payload);
        setToast({ open: true, message: 'Processo disciplinar concluído!', severity: 'success' });
      }
      setConfirmModalOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  // Helper additions
  const handleAddOccurrence = async (data: any) => {
    setActionError(null);
    try {
      await addOccurrence.mutateAsync(data);
      setToast({ open: true, message: 'Ocorrência registrada no processo.', severity: 'success' });
      setAddOccurrenceOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const handleAddEmployee = async (data: any) => {
    setActionError(null);
    try {
      await addEmployee.mutateAsync(data);
      setToast({ open: true, message: 'Funcionário vinculado ao processo.', severity: 'success' });
      setAddEmployeeOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const handleAddEvidence = async (data: any) => {
    setActionError(null);
    try {
      await addEvidence.mutateAsync(data);
      setToast({ open: true, message: 'Metadados da evidência vinculados.', severity: 'success' });
      setAddEvidenceOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const handleRecordDecision = async (data: any) => {
    setActionError(null);
    try {
      await recordDecision.mutateAsync(data);
      setToast({ open: true, message: 'Decisão registrada no processo.', severity: 'success' });
      setRecordDecisionOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const handleApplyMeasure = async (data: any) => {
    setActionError(null);
    try {
      await applyMeasure.mutateAsync(data);
      setToast({ open: true, message: 'Medida disciplinar aplicada.', severity: 'success' });
      setApplyMeasureOpen(false);
    } catch (err: any) {
      setActionError(extractErrorMessage(err));
    }
  };

  const isPendingDecision = caseDetail.decisions?.some((d) => d.status === DecisionStatus.PendingApproval);

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1200, mx: 'auto' }}>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 1.5, fontSize: '0.85rem' }}>
        <Link color="inherit" underline="hover" href="/">
          SIGH
        </Link>
        <Link color="inherit" underline="hover" href="/disciplinary/cases">
          Processos Disciplinares
        </Link>
        <Typography color="text.primary" sx={{ fontWeight: 600 }}>
          {caseDetail.caseNumber}
        </Typography>
      </Breadcrumbs>

      {/* Header Bar */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 2, mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Button
              variant="outlined"
              size="small"
              onClick={() => navigate('/disciplinary/cases')}
              startIcon={<ArrowLeft size={16} />}
            >
              Voltar
            </Button>
            <Typography variant="h5" sx={{ fontWeight: 800, color: '#1e293b' }}>
              Processo {caseDetail.caseNumber}
            </Typography>
            <CaseStatusChip status={caseDetail.status} size="medium" />
            <CaseSeverityChip severity={caseDetail.priority} size="medium" />
          </Box>

          {/* Action Buttons Bar */}
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {caseDetail.status === DisciplinaryCaseStatus.Draft && permissions.canOpenCases && (
              <Button
                variant="contained"
                color="info"
                size="small"
                startIcon={<Play size={16} />}
                onClick={() => handleOpenConfirm('open')}
              >
                Abrir Processo
              </Button>
            )}

            {caseDetail.status === DisciplinaryCaseStatus.Open && permissions.canStartInvestigation && (
              <Button
                variant="contained"
                color="warning"
                size="small"
                startIcon={<Search size={16} />}
                onClick={() => handleOpenConfirm('startInvestigation')}
              >
                Iniciar Investigação
              </Button>
            )}

            {caseDetail.status === DisciplinaryCaseStatus.UnderInvestigation && permissions.canSubmitForDecision && (
              <Button
                variant="contained"
                color="secondary"
                size="small"
                startIcon={<FileCheck size={16} />}
                onClick={() => handleOpenConfirm('submitForDecision')}
              >
                Submeter p/ Decisão
              </Button>
            )}

            {(caseDetail.status === DisciplinaryCaseStatus.AwaitingDecision || caseDetail.status === DisciplinaryCaseStatus.UnderInvestigation) &&
              permissions.canRecordDecision && (
                <Button
                  variant="contained"
                  color="primary"
                  size="small"
                  startIcon={<Gavel size={16} />}
                  onClick={() => setRecordDecisionOpen(true)}
                >
                  Registrar Decisão
                </Button>
              )}

            {isPendingDecision && permissions.canApproveDecision && (
              <Button
                variant="contained"
                color="success"
                size="small"
                startIcon={<CheckCircle size={16} />}
                onClick={() => handleOpenConfirm('approveDecision')}
              >
                Aprovar Decisão
              </Button>
            )}

            {isPendingDecision && permissions.canRejectDecision && (
              <Button
                variant="outlined"
                color="error"
                size="small"
                startIcon={<XCircle size={16} />}
                onClick={() => handleOpenConfirm('rejectDecision')}
              >
                Rejeitar Decisão
              </Button>
            )}

            {(caseDetail.status === DisciplinaryCaseStatus.Decided || caseDetail.status === DisciplinaryCaseStatus.AwaitingDecision) &&
              permissions.canConcludeCases && (
                <Button
                  variant="contained"
                  color="success"
                  size="small"
                  startIcon={<CheckCircle size={16} />}
                  onClick={() => handleOpenConfirm('conclude')}
                >
                  Concluir Processo
                </Button>
              )}

            {caseDetail.status !== DisciplinaryCaseStatus.Completed &&
              caseDetail.status !== DisciplinaryCaseStatus.Cancelled &&
              permissions.canCancelCases && (
                <Button
                  variant="outlined"
                  color="error"
                  size="small"
                  startIcon={<AlertTriangle size={16} />}
                  onClick={() => handleOpenConfirm('cancel')}
                >
                  Cancelar
                </Button>
              )}
          </Box>
        </Box>

        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: '#334155' }}>
          {caseDetail.title}
        </Typography>

        {caseDetail.description && (
          <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap' }}>
            {caseDetail.description}
          </Typography>
        )}
      </Paper>

      {/* Case Overview & Metadata */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                <Building size={16} /> Empresa & Responsável
              </Typography>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Empresa ID:</strong> {caseDetail.companyId}
              </Typography>
              <Typography variant="body2">
                <strong>Responsável:</strong> {caseDetail.responsibleEmployeeId || 'Não atribuído'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                <Calendar size={16} /> Datas do Processo
              </Typography>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Aberto em:</strong> {caseDetail.openedAt ? new Date(caseDetail.openedAt).toLocaleString('pt-BR') : '-'}
              </Typography>
              <Typography variant="body2">
                <strong>Encerrado em:</strong> {caseDetail.closedAt ? new Date(caseDetail.closedAt).toLocaleString('pt-BR') : '-'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                <Gavel size={16} /> Resumo / Encerramento
              </Typography>
              {caseDetail.conclusionSummary && (
                <Typography variant="body2" color="success.main" sx={{ fontWeight: 600 }}>
                  <strong>Conclusão:</strong> {caseDetail.conclusionSummary}
                </Typography>
              )}
              {caseDetail.cancellationReason && (
                <Typography variant="body2" color="error.main" sx={{ fontWeight: 600 }}>
                  <strong>Motivo Cancelamento:</strong> {caseDetail.cancellationReason}
                </Typography>
              )}
              {!caseDetail.conclusionSummary && !caseDetail.cancellationReason && (
                <Typography variant="body2" color="text.secondary">
                  Processo em andamento regular.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Seção 1: Ocorrências */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
            <FileText size={20} color="#2563eb" /> Ocorrências do Processo ({caseDetail.occurrences?.length || 0})
          </Typography>
          {permissions.canAddOccurrence && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => setAddOccurrenceOpen(true)}
            >
              Adicionar Ocorrência
            </Button>
          )}
        </Box>

        {caseDetail.occurrences && caseDetail.occurrences.length > 0 ? (
          <Table size="small">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Data/Hora</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Descrição</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Local</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Gravidade</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {caseDetail.occurrences.map((occ) => (
                <TableRow key={occ.id}>
                  <TableCell>{new Date(occ.occurrenceDate).toLocaleString('pt-BR')}</TableCell>
                  <TableCell>{occ.description}</TableCell>
                  <TableCell>{occ.location || '-'}</TableCell>
                  <TableCell>
                    <CaseSeverityChip severity={occ.severity} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            Nenhuma ocorrência registrada neste processo.
          </Typography>
        )}
      </Paper>

      {/* Seção 2: Funcionários Envolvidos */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Users size={20} color="#0284c7" /> Funcionários Envolvidos ({caseDetail.employees?.length || 0})
          </Typography>
          {permissions.canAddEmployee && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => setAddEmployeeOpen(true)}
            >
              Vincular Funcionário
            </Button>
          )}
        </Box>

        {caseDetail.employees && caseDetail.employees.length > 0 ? (
          <Table size="small">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>ID Funcionário</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Papel no Processo</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Acusado Principal?</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Observações</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {caseDetail.employees.map((emp) => (
                <TableRow key={emp.id}>
                  <TableCell sx={{ fontFamily: 'monospace', fontWeight: 600 }}>{emp.employeeId}</TableCell>
                  <TableCell>
                    <Chip
                      label={
                        emp.role === CaseEmployeeRole.Accused
                          ? 'Acusado / Envolvido'
                          : emp.role === CaseEmployeeRole.Victim
                          ? 'Vítima'
                          : emp.role === CaseEmployeeRole.Witness
                          ? 'Testemunha'
                          : emp.role === CaseEmployeeRole.Reporter
                          ? 'Relator'
                          : 'Outro'
                      }
                      size="small"
                      color={emp.role === CaseEmployeeRole.Accused ? 'error' : 'default'}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>{emp.isPrimaryAccused ? <Chip label="Sim" color="error" size="small" /> : 'Não'}</TableCell>
                  <TableCell>{emp.notes || '-'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            Nenhum funcionário vinculado ao processo.
          </Typography>
        )}
      </Paper>

      {/* Seção 3: Metadados de Evidências */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
            <FileText size={20} color="#16a34a" /> Metadados de Evidências ({caseDetail.evidences?.length || 0})
          </Typography>
          {permissions.canAddEvidence && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => setAddEvidenceOpen(true)}
            >
              Registrar Evidência
            </Button>
          )}
        </Box>

        {caseDetail.evidences && caseDetail.evidences.length > 0 ? (
          <Table size="small">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Tipo</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Descrição</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Localização / Ref.</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Coletado em</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {caseDetail.evidences.map((ev) => (
                <TableRow key={ev.id}>
                  <TableCell>
                    <Chip
                      label={
                        ev.type === EvidenceType.Document
                          ? 'Documento'
                          : ev.type === EvidenceType.Image
                          ? 'Imagem'
                          : ev.type === EvidenceType.Video
                          ? 'Vídeo'
                          : ev.type === EvidenceType.Audio
                          ? 'Áudio'
                          : 'Texto'
                      }
                      size="small"
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>{ev.description}</TableCell>
                  <TableCell>{ev.location || ev.referenceCode || '-'}</TableCell>
                  <TableCell>{new Date(ev.collectedAt).toLocaleDateString('pt-BR')}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            Nenhum metadado de evidência vinculado.
          </Typography>
        )}
      </Paper>

      {/* Seção 4: Decisões Proferidas */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Gavel size={20} color="#d97706" /> Decisões do Processo ({caseDetail.decisions?.length || 0})
          </Typography>
          {permissions.canRecordDecision && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => setRecordDecisionOpen(true)}
            >
              Registrar Decisão
            </Button>
          )}
        </Box>

        {caseDetail.decisions && caseDetail.decisions.length > 0 ? (
          <Table size="small">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Tipo de Decisão</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Justificativa / Fundamentação</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Proferida em</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {caseDetail.decisions.map((dec) => (
                <TableRow key={dec.id}>
                  <TableCell sx={{ fontWeight: 600 }}>
                    {dec.type === DecisionType.FormalWarning
                      ? 'Advertência Escrita'
                      : dec.type === DecisionType.Suspension
                      ? 'Suspensão'
                      : dec.type === DecisionType.TerminationRecommendation
                      ? 'Recomendação de Rescisão'
                      : dec.type === DecisionType.InformalGuidance
                      ? 'Orientação Verbal'
                      : 'Sem Infração'}
                  </TableCell>
                  <TableCell>{dec.justification}</TableCell>
                  <TableCell>{new Date(dec.decidedAt).toLocaleString('pt-BR')}</TableCell>
                  <TableCell>
                    <Chip
                      label={
                        dec.status === DecisionStatus.Approved
                          ? 'Aprovada'
                          : dec.status === DecisionStatus.Rejected
                          ? 'Rejeitada'
                          : 'Aguardando Aprovação'
                      }
                      color={
                        dec.status === DecisionStatus.Approved
                          ? 'success'
                          : dec.status === DecisionStatus.Rejected
                          ? 'error'
                          : 'warning'
                      }
                      size="small"
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            Nenhuma decisão proferida até o momento.
          </Typography>
        )}
      </Paper>

      {/* Seção 5: Medidas Aplicadas */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1 }}>
            <ShieldAlert size={20} color="#dc2626" /> Medidas Disciplinares Aplicadas ({caseDetail.measures?.length || 0})
          </Typography>
          {permissions.canApplyMeasure && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<Plus size={16} />}
              onClick={() => setApplyMeasureOpen(true)}
            >
              Aplicar Medida
            </Button>
          )}
        </Box>

        {caseDetail.measures && caseDetail.measures.length > 0 ? (
          <Table size="small">
            <TableHead sx={{ backgroundColor: '#f8fafc' }}>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Tipo de Medida</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Funcionário Alvo</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Descrição</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Início</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Término</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {caseDetail.measures.map((m) => (
                <TableRow key={m.id}>
                  <TableCell sx={{ fontWeight: 600 }}>
                    {m.type === DisciplinaryMeasureType.WrittenWarning
                      ? 'Advertência Escrita'
                      : m.type === DisciplinaryMeasureType.Suspension
                      ? 'Suspensão'
                      : m.type === DisciplinaryMeasureType.TerminationRecommendation
                      ? 'Recomendação de Rescisão'
                      : m.type === DisciplinaryMeasureType.VerbalWarning
                      ? 'Advertência Verbal'
                      : 'Orientação'}
                  </TableCell>
                  <TableCell sx={{ fontFamily: 'monospace' }}>{m.employeeId}</TableCell>
                  <TableCell>{m.description}</TableCell>
                  <TableCell>{new Date(m.effectiveFrom).toLocaleDateString('pt-BR')}</TableCell>
                  <TableCell>{m.effectiveUntil ? new Date(m.effectiveUntil).toLocaleDateString('pt-BR') : '-'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            Nenhuma medida disciplinar aplicada.
          </Typography>
        )}
      </Paper>

      {/* Modais de Ações do Ciclo de Vida */}
      <ConfirmActionModal
        open={confirmModalOpen}
        actionType={currentActionType}
        onClose={() => setConfirmModalOpen(false)}
        onConfirm={handleConfirmAction}
        loading={
          openCase.isPending ||
          startInvestigation.isPending ||
          submitForDecision.isPending ||
          approveDecision.isPending ||
          rejectDecision.isPending ||
          cancelCase.isPending ||
          concludeCase.isPending
        }
        errorMessage={actionError}
      />

      {/* Modais de Cadastro */}
      <AddOccurrenceModal
        open={addOccurrenceOpen}
        onClose={() => setAddOccurrenceOpen(false)}
        onSubmit={handleAddOccurrence}
        loading={addOccurrence.isPending}
        errorMessage={actionError}
        infractionTypes={infractionTypes}
      />

      <AddEmployeeModal
        open={addEmployeeOpen}
        onClose={() => setAddEmployeeOpen(false)}
        onSubmit={handleAddEmployee}
        loading={addEmployee.isPending}
        errorMessage={actionError}
      />

      <AddEvidenceModal
        open={addEvidenceOpen}
        onClose={() => setAddEvidenceOpen(false)}
        onSubmit={handleAddEvidence}
        loading={addEvidence.isPending}
        errorMessage={actionError}
        occurrences={caseDetail.occurrences}
      />

      <RecordDecisionModal
        open={recordDecisionOpen}
        onClose={() => setRecordDecisionOpen(false)}
        onSubmit={handleRecordDecision}
        loading={recordDecision.isPending}
        errorMessage={actionError}
      />

      <ApplyMeasureModal
        open={applyMeasureOpen}
        onClose={() => setApplyMeasureOpen(false)}
        onSubmit={handleApplyMeasure}
        loading={applyMeasure.isPending}
        errorMessage={actionError}
        employees={caseDetail.employees}
      />

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
