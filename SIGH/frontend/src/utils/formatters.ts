/**
 * Formatação brasileira em um só lugar: números com ponto no milhar e vírgula decimal (1.248; 12,5%),
 * datas dd/MM/aaaa e data/hora dd/MM/aaaa HH:mm no horário de Maceió. Vazio vira "—".
 */
export const EMPTY = '—';
export const TIME_ZONE = 'America/Maceio';

const integer = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 });
const decimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 1 });
const dateTime = new Intl.DateTimeFormat('pt-BR', {
  timeZone: TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});
const dateOnly = new Intl.DateTimeFormat('pt-BR', { timeZone: TIME_ZONE, day: '2-digit', month: '2-digit', year: 'numeric' });

const isEmpty = (value: unknown) => value === null || value === undefined || value === '';

/** 1248 → "1.248"; 12.5 → "12,5". */
export const formatNumber = (value: number | null | undefined, fractionDigits?: number): string => {
  if (isEmpty(value) || Number.isNaN(value)) return EMPTY;
  if (fractionDigits !== undefined)
    return new Intl.NumberFormat('pt-BR', { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits }).format(value!);
  return Number.isInteger(value) ? integer.format(value!) : decimal.format(value!);
};

/** 12.5 → "12,5%" (o valor já é percentual, não fração). */
export const formatPercent = (value: number | null | undefined): string =>
  isEmpty(value) || Number.isNaN(value) ? EMPTY : `${decimal.format(value!)}%`;

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

/** "2026-11-13" (data de negócio, sem fuso) ou instante ISO → "13/11/2026". */
export const formatDate = (value: string | Date | null | undefined): string => {
  if (isEmpty(value)) return EMPTY;
  if (typeof value === 'string' && DATE_ONLY.test(value.slice(0, 10)) && value.length <= 10) {
    const [y, m, d] = value.split('-');
    return `${d}/${m}/${y}`;
  }
  const date = value instanceof Date ? value : new Date(value!);
  return Number.isNaN(date.getTime()) ? EMPTY : dateOnly.format(date);
};

/** Instante → "13/11/2026 14:20" no horário de Maceió. */
export const formatDateTime = (value: string | Date | null | undefined): string => {
  if (isEmpty(value)) return EMPTY;
  const date = value instanceof Date ? value : new Date(value!);
  return Number.isNaN(date.getTime()) ? EMPTY : dateTime.format(date).replace(',', '');
};

/** "Sim" / "Não". */
export const formatBoolean = (value: boolean | null | undefined): string => (isEmpty(value) ? EMPTY : value ? 'Sim' : 'Não');

/** "1 dia" / "3 dias". */
export const formatDays = (days: number | null | undefined): string =>
  isEmpty(days) ? EMPTY : `${formatNumber(days)} ${days === 1 ? 'dia' : 'dias'}`;

/** Iniciais para o avatar: "Maria da Silva" → "MS". */
export const initialsOf = (name?: string | null): string => {
  const parts = (name ?? '')
    .trim()
    .split(/\s+/)
    .filter((p) => p.length > 2 || /^[A-Z]/.test(p))
    .filter((p) => !['da', 'de', 'do', 'das', 'dos', 'e'].includes(p.toLowerCase()));
  if (parts.length === 0) return '?';
  const first = parts[0][0];
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return (first + last).toUpperCase();
};
