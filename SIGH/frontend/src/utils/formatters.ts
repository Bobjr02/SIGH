import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';

dayjs.locale('pt-br');

export const formatDate = (date: string | Date | null | undefined): string => {
  if (!date) return '-';
  return dayjs(date).format('DD/MM/YYYY');
};

export const formatDateTime = (date: string | Date | null | undefined): string => {
  if (!date) return '-';
  return dayjs(date).format('DD/MM/YYYY HH:mm');
};
