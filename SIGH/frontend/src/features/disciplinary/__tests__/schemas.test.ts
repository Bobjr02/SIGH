import { describe, it, expect } from 'vitest';
import {
  createInfractionTypeSchema,
  createDisciplinaryCaseSchema,
  addOccurrenceSchema,
  addEmployeeSchema,
  addEvidenceSchema,
  recordDecisionSchema,
  applyMeasureSchema,
  cancelCaseSchema,
  concludeCaseSchema,
} from '../schemas';
import {
  InfractionSeverity,
  CaseEmployeeRole,
  EvidenceType,
  DecisionType,
  DisciplinaryMeasureType,
} from '../types';

describe('Disciplinary Schemas', () => {
  it('validates InfractionType schema', () => {
    const validInfraction = createInfractionTypeSchema.safeParse({
      code: 'LEVE-01',
      name: 'Atraso Injustificado',
      defaultSeverity: InfractionSeverity.Low,
      requiresFormalInvestigation: false,
      allowsTerminationRecommendation: false,
      description: 'Atrasos repetidos sem justificativa.',
      legalReference: 'Artigo 482 CLT',
    });
    expect(validInfraction.success).toBe(true);

    const invalidInfraction = createInfractionTypeSchema.safeParse({
      code: 'LEVE@01!',
      name: 'Atraso',
      defaultSeverity: InfractionSeverity.Low,
    });
    expect(invalidInfraction.success).toBe(false);
  });

  it('validates DisciplinaryCase schema', () => {
    const validCase = createDisciplinaryCaseSchema.safeParse({
      caseNumber: 'PROC-2026-001',
      companyId: '11111111-1111-1111-1111-111111111111',
      title: 'Apuração de Faltas Não Justificadas',
      description: 'Funcionário acumulou 5 faltas não justificadas no mês.',
    });
    expect(validCase.success).toBe(true);
  });

  it('validates Occurrence schema', () => {
    const validOccurrence = addOccurrenceSchema.safeParse({
      description: 'Recusa em assinar advertência em reunião.',
      occurredAt: '2026-07-26T10:00',
      location: 'Sala de RH',
      severity: InfractionSeverity.Moderate,
    });
    expect(validOccurrence.success).toBe(true);
  });

  it('validates Employee binding schema', () => {
    const validEmployee = addEmployeeSchema.safeParse({
      employeeId: '22222222-2222-2222-2222-222222222222',
      involvementRole: CaseEmployeeRole.Accused,
      isPrimaryAccused: true,
      notes: 'Acusado principal no ato de indisciplina.',
    });
    expect(validEmployee.success).toBe(true);
  });

  it('validates Evidence schema', () => {
    const validEvidence = addEvidenceSchema.safeParse({
      title: 'Registro de Ponto Digital',
      description: 'Espelho de ponto referente ao mês de Julho.',
      evidenceType: EvidenceType.Document,
      locationReference: 'Pasta de Arquivo Físico RH-2026',
      collectedAt: '2026-07-26T11:00',
    });
    expect(validEvidence.success).toBe(true);
  });

  it('validates Decision schema', () => {
    const validDecision = recordDecisionSchema.safeParse({
      decisionType: DecisionType.FormalWarning,
      description: 'Comprovada a infração leve, aplica-se a pena de advertência escrita fundamentada.',
    });
    expect(validDecision.success).toBe(true);
  });

  it('validates Measure schema', () => {
    const validMeasure = applyMeasureSchema.safeParse({
      measureType: DisciplinaryMeasureType.WrittenWarning,
      description: 'Aplicação de advertência por descumprimento de horário.',
      employeeId: '22222222-2222-2222-2222-222222222222',
      startDate: '2026-07-26',
    });
    expect(validMeasure.success).toBe(true);
  });

  it('validates cancel and conclude schemas', () => {
    expect(cancelCaseSchema.safeParse({ justification: 'Curto' }).success).toBe(false);
    expect(
      cancelCaseSchema.safeParse({ justification: 'Processo instaurado por equívoco administrativo.' }).success
    ).toBe(true);

    expect(concludeCaseSchema.safeParse({ summaryNotes: 'OK' }).success).toBe(false);
    expect(
      concludeCaseSchema.safeParse({
        summaryNotes: 'Instrução concluída com aplicação de penalidade ao infror.',
      }).success
    ).toBe(true);
  });
});
