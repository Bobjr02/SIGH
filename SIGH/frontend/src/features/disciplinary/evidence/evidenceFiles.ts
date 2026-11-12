import { EvidenceType } from '../types';

/** Mesmo limite padrão do servidor (Storage:MaxUploadSizeBytes). A validação real é feita no servidor. */
export const MAX_EVIDENCE_FILE_BYTES = 25 * 1024 * 1024;

/** Extensões aceitas e o tipo de evidência a que pertencem (o servidor confere a assinatura do conteúdo). */
export const EVIDENCE_FILE_EXTENSIONS: Record<string, EvidenceType> = {
  pdf: EvidenceType.Document,
  docx: EvidenceType.Document,
  xlsx: EvidenceType.Document,
  jpg: EvidenceType.Image,
  jpeg: EvidenceType.Image,
  png: EvidenceType.Image,
  webp: EvidenceType.Image,
  mp4: EvidenceType.Video,
  mp3: EvidenceType.Audio,
  m4a: EvidenceType.Audio,
};

export const EVIDENCE_FILE_ACCEPT = Object.keys(EVIDENCE_FILE_EXTENSIONS)
  .map((ext) => `.${ext}`)
  .join(',');

export const EVIDENCE_TYPE_LABELS: Record<EvidenceType, string> = {
  [EvidenceType.Undefined]: 'Indefinido',
  [EvidenceType.Document]: 'Documento',
  [EvidenceType.Image]: 'Imagem',
  [EvidenceType.Video]: 'Vídeo',
  [EvidenceType.Audio]: 'Áudio',
  [EvidenceType.Text]: 'Texto',
  [EvidenceType.Other]: 'Outro',
};

const extensionOf = (fileName: string) => {
  const dot = fileName.lastIndexOf('.');
  return dot >= 0 ? fileName.slice(dot + 1).toLowerCase() : '';
};

/**
 * O arquivo (pelo tipo da extensão) serve para este tipo de evidência? Mesma regra do servidor:
 * Document aceita documentos e imagens (o RH fotografa documentos com o celular); Image, Video e
 * Audio só o próprio tipo; Other qualquer formato permitido; Text nunca recebe arquivo.
 */
export const evidenceAcceptsKind = (evidenceType: EvidenceType, kind: EvidenceType): boolean => {
  switch (evidenceType) {
    case EvidenceType.Text:
    case EvidenceType.Undefined:
      return false;
    case EvidenceType.Other:
      return true;
    case EvidenceType.Document:
      return kind === EvidenceType.Document || kind === EvidenceType.Image;
    default:
      return kind === evidenceType;
  }
};

/** Tipos de evidência que recebem arquivo (Text é só texto digitado). */
export const evidenceTypeAcceptsFile = (evidenceType: EvidenceType) =>
  evidenceType !== EvidenceType.Text && evidenceType !== EvidenceType.Undefined;

/** Validação no cliente (extensão, tamanho e coerência com o tipo da evidência). Devolve a mensagem de erro ou null. */
export const validateEvidenceFile = (file: File, evidenceType: EvidenceType): string | null => {
  if (!evidenceTypeAcceptsFile(evidenceType)) {
    return 'Evidências do tipo Texto não recebem arquivo: o conteúdo delas é o próprio texto registrado.';
  }
  const kind = EVIDENCE_FILE_EXTENSIONS[extensionOf(file.name)];
  if (kind === undefined) {
    return `Formato não permitido. Envie: ${Object.keys(EVIDENCE_FILE_EXTENSIONS).join(', ')}.`;
  }
  if (file.size === 0) return 'O arquivo está vazio.';
  if (file.size > MAX_EVIDENCE_FILE_BYTES) {
    return `O arquivo tem ${formatBytes(file.size)}; o máximo é ${formatBytes(MAX_EVIDENCE_FILE_BYTES)}.`;
  }
  if (!evidenceAcceptsKind(evidenceType, kind)) {
    return evidenceType === EvidenceType.Document
      ? 'Evidências do tipo Documento aceitam PDF, DOCX, XLSX ou imagens (JPG, PNG, WEBP).'
      : `Esta evidência é do tipo ${EVIDENCE_TYPE_LABELS[evidenceType]}; o arquivo selecionado é ${EVIDENCE_TYPE_LABELS[kind]}.`;
  }
  return null;
};

export const formatBytes = (bytes?: number | null): string => {
  if (bytes == null) return '-';
  if (bytes < 1024) return `${bytes} B`;
  const units = ['KB', 'MB', 'GB'];
  let value = bytes / 1024;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  return `${value.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} ${units[unit]}`;
};

/** Hash abreviado para exibição: 8 primeiros e 4 últimos caracteres. */
export const shortHash = (hash?: string | null): string =>
  !hash ? '-' : hash.length <= 16 ? hash : `${hash.slice(0, 8)}…${hash.slice(-4)}`;
