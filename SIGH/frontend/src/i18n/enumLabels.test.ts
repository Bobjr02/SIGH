import { describe, expect, it } from 'vitest';
import enumValues from './enum-values.json';
import { LABELS, label, type EnumGroup } from './labels';

// enum-values.json é gerado pelo backend (EnumValuesFrontendSyncTests). Todo valor precisa de rótulo pt-BR.
describe('rótulos dos enums', () => {
  const groups = Object.keys(enumValues) as EnumGroup[];

  it.each(groups)('%s: todo valor tem rótulo em português', (group) => {
    const labels = LABELS[group] as Record<string, string>;
    const missing = Object.keys(enumValues[group]).filter((name) => !labels?.[name]?.trim());
    expect(missing, `sem rótulo em ${group}`).toEqual([]);
  });

  it.each(groups)('%s: não há rótulo sobrando (valor que não existe mais)', (group) => {
    const extra = Object.keys(LABELS[group]).filter((name) => !(name in enumValues[group]));
    expect(extra).toEqual([]);
  });

  it('nenhum rótulo é o próprio nome técnico em inglês', () => {
    for (const group of groups) {
      for (const [name, text] of Object.entries(LABELS[group] as Record<string, string>)) {
        if (name === 'Sigh' || name === 'Normal') continue;
        expect(text, `${group}.${name}`).not.toBe(name);
      }
    }
  });

  it('aceita o número ou o nome', () => {
    expect(label('RecordStatus', 3)).toBe('Aprovada');
    expect(label('RecordStatus', 'Approved')).toBe('Aprovada');
    expect(label('UserStatus', 'PendingFirstAccess')).toBe('Aguardando primeiro acesso');
    expect(label('DisciplinaryCaseStatus', 'Completed')).toBe('Concluída');
    expect(label('DisciplinaryCaseStatus', 'AwaitingDecision')).toBe('Aguardando aprovação');
    expect(label('DisciplinaryMeasureType', 'JustCauseTermination')).toBe('Demissão por justa causa');
    expect(label('RecordStatus', null)).toBe('—');
  });
});
