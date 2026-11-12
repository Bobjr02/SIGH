import { api } from '../../../services/api';
import type { EvidenceFileDto } from '../types';

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
}

const base = (caseId: string, evidenceId: string) => `/v1/disciplinary-cases/${caseId}/evidences/${evidenceId}`;

/** "filename*=UTF-8''..." tem prioridade sobre "filename=". */
export const fileNameFromContentDisposition = (header?: string | null): string | null => {
  if (!header) return null;
  const star = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header);
  if (star) {
    try {
      return decodeURIComponent(star[1].trim());
    } catch {
      /* cai no filename= */
    }
  }
  const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
  return plain ? plain[1].trim() : null;
};

export const evidenceApi = {
  /** Upload multipart (campo "file"), com progresso de 0 a 100. */
  async uploadFile(caseId: string, evidenceId: string, file: File, onProgress?: (percent: number) => void): Promise<EvidenceFileDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    const response = await api.post<Result<EvidenceFileDto>>(`${base(caseId, evidenceId)}/file`, form, {
      // Deixa o navegador definir o boundary do multipart.
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 0,
      onUploadProgress: (event) => {
        if (onProgress && event.total) onProgress(Math.round((event.loaded / event.total) * 100));
      },
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Não foi possível enviar o arquivo.');
    }
    return response.data.data;
  },

  /** Baixa pelo axios (leva o Bearer) e entrega ao navegador como download. */
  async downloadFile(caseId: string, evidenceId: string, fallbackName: string): Promise<void> {
    const response = await api.get<Blob>(`${base(caseId, evidenceId)}/file`, { responseType: 'blob', timeout: 0 });
    const name = fileNameFromContentDisposition(response.headers['content-disposition'] as string | undefined) ?? fallbackName;

    const url = URL.createObjectURL(response.data);
    try {
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = name;
      anchor.rel = 'noopener';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
    } finally {
      URL.revokeObjectURL(url);
    }
  },

  /** Remoção lógica, com motivo obrigatório. */
  async remove(caseId: string, evidenceId: string, reason: string): Promise<void> {
    const response = await api.delete<Result<boolean>>(base(caseId, evidenceId), { data: { reason } });
    if (!response.data.success) throw new Error(response.data.message || 'Não foi possível remover a evidência.');
  },
};
