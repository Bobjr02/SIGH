import { describe, it, expect } from 'vitest';

function sanitizeCsvField(field: string | null | undefined, separator: string = ';'): string {
  if (!field) return '""';
  let trimmed = field.trim();
  const dangerousPrefixes = ['=', '+', '-', '@'];
  if (trimmed.length > 0 && dangerousPrefixes.includes(trimmed[0])) {
    trimmed = "'" + trimmed;
  }
  if (trimmed.includes('"')) {
    trimmed = trimmed.replace(/"/g, '""');
  }
  return `"${trimmed}"`;
}

describe('CSV Sanitizer (CSV Injection Protection)', () => {
  it('should neutralize = formula prefix', () => {
    const result = sanitizeCsvField('=SUM(A1:A2)');
    expect(result).toBe('"' + "'=SUM(A1:A2)" + '"');
  });

  it('should neutralize + formula prefix', () => {
    const result = sanitizeCsvField("+cmd|' /C calc'!A0");
    expect(result).toBe('"' + "'+cmd|' /C calc'!A0" + '"');
  });

  it('should neutralize - formula prefix', () => {
    const result = sanitizeCsvField('-10+20');
    expect(result).toBe('"' + "'-10+20" + '"');
  });

  it('should neutralize @ formula prefix', () => {
    const result = sanitizeCsvField('@SUM(A1:A2)');
    expect(result).toBe('"' + "'@SUM(A1:A2)" + '"');
  });

  it('should neutralize dangerous characters preceded by leading spaces or tabs', () => {
    const resultSpace = sanitizeCsvField('   =SUM(A1:A2)');
    expect(resultSpace).toBe('"' + "'=SUM(A1:A2)" + '"');

    const resultTab = sanitizeCsvField('\t=SUM(A1:A2)');
    expect(resultTab).toBe('"' + "'=SUM(A1:A2)" + '"');
  });

  it('should escape double quotes correctly', () => {
    const result = sanitizeCsvField('Empresa "Alfa" Ltda');
    expect(result).toBe('"Empresa ""Alfa"" Ltda"');
  });

  it('should format normal safe strings properly', () => {
    const result = sanitizeCsvField('Processo Disciplinar 2026/001');
    expect(result).toBe('"Processo Disciplinar 2026/001"');
  });

  it('should handle custom separator and line breaks correctly', () => {
    const resultWithBreak = sanitizeCsvField('Linha 1\nLinha 2', ',');
    expect(resultWithBreak).toBe('"Linha 1\nLinha 2"');
  });
});
