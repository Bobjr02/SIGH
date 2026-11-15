import { describe, expect, it } from 'vitest';
import { formatBoolean, formatDate, formatDateTime, formatDays, formatNumber, formatPercent, initialsOf } from './formatters';

describe('formatters (pt-BR)', () => {
  it('números com ponto no milhar e vírgula decimal', () => {
    expect(formatNumber(1248)).toBe('1.248');
    expect(formatNumber(12.5)).toBe('12,5');
    expect(formatPercent(12.5)).toBe('12,5%');
    expect(formatPercent(66.7)).toBe('66,7%');
    expect(formatNumber(null)).toBe('—');
  });

  it('datas dd/MM/aaaa; data de negócio não muda de dia', () => {
    expect(formatDate('2026-11-13')).toBe('13/11/2026');
    expect(formatDate('2026-11-14T01:30:00Z')).toBe('13/11/2026'); // 22:30 em Maceió
    expect(formatDate(undefined)).toBe('—');
  });

  it('data e hora no horário de Maceió', () => {
    expect(formatDateTime('2026-11-14T17:20:00Z')).toBe('14/11/2026 14:20');
  });

  it('booleanos, dias e iniciais', () => {
    expect(formatBoolean(true)).toBe('Sim');
    expect(formatBoolean(false)).toBe('Não');
    expect(formatDays(1)).toBe('1 dia');
    expect(formatDays(3)).toBe('3 dias');
    expect(initialsOf('Maria da Silva')).toBe('MS');
    expect(initialsOf('admin')).toBe('A');
  });
});
