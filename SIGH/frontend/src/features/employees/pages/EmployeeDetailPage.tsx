import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { Alert, Box, Button, Chip, CircularProgress, Paper, Typography } from '@mui/material';
import { ArrowLeft, Gavel, Scale, ShieldAlert } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';
import { disciplinaryReportsApi } from '../../disciplinary-reports/api/disciplinaryReportsApi';
import {
  GROUP_LABELS,
  STATUS_LABELS,
  employeeKeys,
  employeesApi,
  formatCpf,
  formatDate,
  type DecisionSupport,
} from '../api/employeesApi';

const Field: React.FC<{ label: string; value?: React.ReactNode }> = ({ label, value }) => (
  <Box>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" sx={{ fontWeight: 600 }}>
      {value ?? '-'}
    </Typography>
  </Box>
);

const Stat: React.FC<{ label: string; value: React.ReactNode; hint?: string }> = ({ label, value, hint }) => (
  <Paper elevation={0} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="h5" sx={{ fontWeight: 800 }}>
      {value}
    </Typography>
    {hint && (
      <Typography variant="caption" color="text.secondary">
        {hint}
      </Typography>
    )}
  </Paper>
);

/** Quadro de apoio à decisão: só informativo, nunca sugere medida. */
export const DecisionSupportPanel: React.FC<{ data: DecisionSupport }> = ({ data }) => (
  <Paper
    component="section"
    aria-label="Apoio à decisão"
    elevation={0}
    sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', borderRadius: 2, display: 'flex', flexDirection: 'column', gap: 2 }}
  >
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <Scale size={20} />
      <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
        Apoio à decisão
      </Typography>
      <Chip size="small" label="Informativo" variant="outlined" />
    </Box>

    {data.totalMeasures === 0 ? (
      <Typography variant="body2" color="text.secondary">
        Nenhuma medida disciplinar aplicada.
      </Typography>
    ) : (
      <>
        <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' } }}>
          <Stat label="Medidas aplicadas" value={data.totalMeasures} />
          <Stat
            label="Advertências"
            value={data.warnings}
            hint={`${data.verbalWarnings} verbal(is), ${data.writtenWarnings} escrita(s)`}
          />
          <Stat
            label="Suspensões"
            value={data.suspensions}
            hint={`${data.suspensionDays} dia(s) no total${
              data.suspensionsWithoutPeriod
                ? ` +${data.suspensionsWithoutPeriod} suspensão${data.suspensionsWithoutPeriod > 1 ? 'ões' : ''} sem dias informados`
                : ''
            }`}
          />
          <Stat label="Última medida" value={formatDate(data.lastMeasureAt)} />
          <Stat label="Últimos 6 meses" value={data.measuresLast6Months} />
          <Stat label="Últimos 12 meses" value={data.measuresLast12Months} />
        </Box>

        {(data.historicalMeasures ?? 0) > 0 && data.byOrigin && (
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
              Por origem
            </Typography>
            <Box component="ul" aria-label="Medidas por origem" sx={{ m: 0, pl: 2.5 }}>
              {data.byOrigin.map((o) => (
                <li key={o.label} data-testid="origin-breakdown">
                  <Typography variant="body2">
                    <strong>
                      {o.total} {o.label.toLowerCase()}
                    </strong>{' '}
                    — {o.historical} histórica{o.historical === 1 ? '' : 's'}, {o.sigh} no SIGH
                  </Typography>
                </li>
              ))}
            </Box>
          </Box>
        )}

        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' } }}>
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
              Por tipo
            </Typography>
            <Box component="ul" aria-label="Medidas por tipo" sx={{ m: 0, pl: 2.5 }}>
              {data.byType.map((t) => (
                <li key={t.label}>
                  <Typography variant="body2">
                    {t.label}: <strong>{t.count}</strong>
                  </Typography>
                </li>
              ))}
            </Box>
          </Box>
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
              Por motivo (tipo de infração)
            </Typography>
            <Box component="ul" aria-label="Medidas por motivo" sx={{ m: 0, pl: 2.5 }}>
              {data.byInfractionType.map((t) => (
                <li key={t.label}>
                  <Typography variant="body2">
                    {t.label}: <strong>{t.count}</strong>
                  </Typography>
                </li>
              ))}
            </Box>
          </Box>
        </Box>
      </>
    )}
  </Paper>
);

interface TimelineItem {
  key: string;
  date: string;
  kind: 'case' | 'measure';
  title: string;
  subtitle: string;
  link?: string;
  /** "Histórico importado — lote de dd/MM/yyyy". */
  seal?: string;
}

const historySeal = (importedAt?: string | null) => `Histórico importado — lote de ${importedAt ? formatDate(importedAt) : '-'}`;

export const EmployeeDetailPage: React.FC = () => {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canSeeHistory = hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY);
  const canSeeMeasures = hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES);

  const employee = useQuery({ queryKey: employeeKeys.detail(id), queryFn: () => employeesApi.getById(id) });
  const support = useQuery({
    queryKey: employeeKeys.decisionSupport(id),
    queryFn: () => employeesApi.decisionSupport(id),
    enabled: canSeeHistory,
  });
  const history = useQuery({
    queryKey: ['employees', 'history', id],
    queryFn: () => disciplinaryReportsApi.getEmployeeHistory(id),
    enabled: canSeeHistory,
  });
  const measures = useQuery({
    queryKey: ['employees', 'measures', id],
    queryFn: () => disciplinaryReportsApi.getMeasuresReport({ employeeId: id, pageSize: 100 }),
    enabled: canSeeMeasures,
  });

  if (employee.isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress aria-label="Carregando funcionário" />
      </Box>
    );
  }
  if (employee.isError || !employee.data) {
    return <Alert severity="error">{(employee.error as Error | null)?.message ?? 'Funcionário não encontrado.'}</Alert>;
  }

  const e = employee.data;
  const timeline: TimelineItem[] = [
    ...(history.data?.cases ?? []).map((c) => ({
      key: `case-${c.caseId}`,
      date: c.occurredAt ?? c.openedAt,
      kind: 'case' as const,
      title: `Processo ${c.caseNumber} - ${c.title}`,
      subtitle: `${c.status} · ${c.measuresCount} medida(s)`,
      link: `/medidas/${c.caseId}`,
      seal: c.origin === 'Histórico' ? historySeal(c.importedAt) : undefined,
    })),
    ...(measures.data?.items ?? []).map((m) => ({
      key: `measure-${m.measureId}`,
      date: m.appliedAt,
      kind: 'measure' as const,
      title: m.measureType,
      subtitle: `Processo ${m.caseNumber}${m.effectiveTo ? ` · até ${formatDate(m.effectiveTo)}` : ''}${
        m.suspensionDaysUnknown ? ' · dias não informados' : ''
      }`,
      link: `/medidas/${m.caseId}`,
      seal: m.origin === 'Histórico' ? 'Histórico importado' : undefined,
    })),
  ].sort((a, b) => (a.date < b.date ? 1 : -1));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
      <Box>
        <Button size="small" startIcon={<ArrowLeft size={16} />} onClick={() => navigate('/employees')}>
          Funcionários
        </Button>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
          {e.fullName}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Matrícula {e.employeeNumber} · {STATUS_LABELS[e.status] ?? e.status}
        </Typography>
      </Box>

      {canSeeHistory && support.data && <DecisionSupportPanel data={support.data} />}
      {canSeeHistory && support.isError && <Alert severity="error">{(support.error as Error).message}</Alert>}

      <Paper component="section" aria-label="Dados cadastrais" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mb: 2 }}>
          Dados cadastrais
        </Typography>
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' } }}>
          <Field label="CPF" value={<span data-testid="employee-cpf">{formatCpf(e.cpf)}</span>} />
          <Field label="Admissão" value={formatDate(e.admissionDate)} />
          <Field label="Demissão" value={formatDate(e.terminationDate)} />
          <Field label="Situação" value={STATUS_LABELS[e.status] ?? e.status} />
          <Field label="Cargo" value={e.jobTitleCode ? `${e.jobTitleCode} - ${e.jobTitleName}` : e.jobTitleName} />
          <Field label="Setor" value={e.departmentCode ? `${e.departmentCode} - ${e.departmentName}` : e.departmentName} />
          <Field label="Gerência" value={e.managementUnitCode ? `${e.managementUnitCode} - ${e.managementUnitName}` : e.managementUnitName} />
          <Field label="Grupo / Turma" value={`${e.group ? GROUP_LABELS[e.group] : '-'} / ${e.team ?? '-'}`} />
        </Box>
      </Paper>

      {(canSeeHistory || canSeeMeasures) && (
        <Paper component="section" aria-label="Histórico disciplinar" elevation={0} sx={{ p: 2.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
          <Typography variant="h6" component="h2" sx={{ fontWeight: 700, mb: 2 }}>
            Histórico disciplinar
          </Typography>
          {timeline.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Nenhum processo ou medida registrado.
            </Typography>
          ) : (
            <Box component="ol" sx={{ m: 0, pl: 0, listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              {timeline.map((item) => (
                <Box component="li" key={item.key} data-testid="timeline-item" sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-start' }}>
                  {item.kind === 'case' ? <Gavel size={18} /> : <ShieldAlert size={18} color="#dc2626" />}
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {formatDate(item.date)} · {item.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {item.subtitle}
                    </Typography>
                    {item.seal && (
                      <Box>
                        <Chip size="small" variant="outlined" color="secondary" label={item.seal} data-testid="history-seal" />
                      </Box>
                    )}
                  </Box>
                </Box>
              ))}
            </Box>
          )}
        </Paper>
      )}
    </Box>
  );
};

export default EmployeeDetailPage;
