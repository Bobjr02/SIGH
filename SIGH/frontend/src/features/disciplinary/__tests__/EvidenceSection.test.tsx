import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { AxiosError } from 'axios';
import { server } from '../../../test/server';
import { api } from '../../../services/api';
import { EvidenceSection } from '../components/EvidenceSection';
import { evidenceApi, fileNameFromContentDisposition } from '../evidence/evidenceApi';
import { MAX_EVIDENCE_FILE_BYTES } from '../evidence/evidenceFiles';
import { DisciplinaryCaseStatus, EvidenceStatus, EvidenceType, type EvidenceDto } from '../types';

const API = '/api/v1/disciplinary-cases';
const CASE_ID = 'case-1';
const HASH = 'a3f1c2d4e5b60718293a4b5c6d7e8f90112233445566778899aabbccddeeff00';

const withFile: EvidenceDto = {
  id: 'ev-file',
  evidenceType: EvidenceType.Document,
  title: 'Relatório da catraca',
  hasFile: true,
  originalFileName: 'catraca.pdf',
  contentType: 'application/pdf',
  fileSize: 1536,
  collectedAt: '2026-11-10T10:00:00Z',
  collectedByUserId: 'u1',
  integrityHash: HASH,
  status: EvidenceStatus.Draft,
  fileUploadedByName: 'Maria Gestora',
};

const withoutFile: EvidenceDto = {
  ...withFile,
  id: 'ev-pending',
  title: 'Foto do local',
  evidenceType: EvidenceType.Image,
  hasFile: false,
  originalFileName: null,
  fileSize: null,
  integrityHash: null,
  fileUploadedByName: null,
};

const renderSection = (props: Partial<React.ComponentProps<typeof EvidenceSection>> = {}) => {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <EvidenceSection
        caseId={CASE_ID}
        caseStatus={DisciplinaryCaseStatus.UnderInvestigation}
        evidences={[withFile, withoutFile]}
        canAdd
        canRemove
        canDownload
        onRegisterClick={() => undefined}
        {...props}
      />
    </QueryClientProvider>,
  );
};

const pdf = (name = 'laudo.pdf', size = 10) => new File([new Uint8Array(size).fill(37)], name, { type: 'application/pdf' });

describe('EvidenceSection', () => {
  beforeEach(() => {
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:fake'), revokeObjectURL: vi.fn() }));
  });
  afterEach(() => vi.unstubAllGlobals());

  it('lists name, size, abbreviated hash and who uploaded', () => {
    renderSection();

    const row = within(screen.getByTestId('evidence-row-ev-file'));
    expect(row.getByText('catraca.pdf')).toBeInTheDocument();
    expect(row.getByText('1,5 KB')).toBeInTheDocument();
    expect(row.getByText('a3f1c2d4…ff00')).toBeInTheDocument();
    expect(row.getByText('Maria Gestora')).toBeInTheDocument();
    expect(within(screen.getByTestId('evidence-row-ev-pending')).getByText('Sem arquivo')).toBeInTheDocument();
  });

  it('downloads through the API with the bearer token and uses the server file name', async () => {
    let requested = '';
    server.use(
      http.get(`${API}/${CASE_ID}/evidences/ev-file/file`, ({ request }) => {
        requested = new URL(request.url).pathname;
        return new HttpResponse(new Blob(['%PDF-1.4']), {
          headers: {
            'Content-Type': 'application/pdf',
            'Content-Disposition': "attachment; filename=catraca.pdf; filename*=UTF-8''Relat%C3%B3rio%20cat.pdf",
          },
        });
      }),
    );
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    renderSection();
    await userEvent.setup().click(screen.getByRole('button', { name: 'Baixar catraca.pdf' }));

    await waitFor(() => expect(click).toHaveBeenCalledTimes(1));
    expect(requested).toBe(`/api/v1/disciplinary-cases/${CASE_ID}/evidences/ev-file/file`);
    expect((click.mock.contexts[0] as HTMLAnchorElement).download).toBe('Relatório cat.pdf');
    expect(URL.createObjectURL).toHaveBeenCalled();
  });

  // O interceptor XHR do msw não lê FormData do jsdom; o contrato HTTP do upload é verificado no
  // api.post (URL, campo "file", progresso) e, de ponta a ponta, no e2e do Playwright.
  it('uploads the chosen file as multipart (field "file") and reports progress', async () => {
    const post = vi.spyOn(api, 'post').mockImplementation(async (_url, _data, config) => {
      config?.onUploadProgress?.({ loaded: 5, total: 10 } as never);
      return { data: { success: true, data: { evidenceId: 'ev-pending', sha256Hash: HASH } } } as never;
    });
    const progress: number[] = [];

    await evidenceApi.uploadFile(CASE_ID, 'ev-pending', new File([new Uint8Array(20)], 'foto.png'), (p) => progress.push(p));

    const [url, form, config] = post.mock.calls[0];
    expect(url).toBe(`/v1/disciplinary-cases/${CASE_ID}/evidences/ev-pending/file`);
    expect((form as FormData).get('file')).toBeInstanceOf(File);
    expect(((form as FormData).get('file') as File).name).toBe('foto.png');
    expect(config?.timeout).toBe(0);
    expect(progress).toEqual([50]);
  });

  it('after a successful upload the dialog closes', async () => {
    vi.spyOn(evidenceApi, 'uploadFile').mockResolvedValue({} as never);
    const user = userEvent.setup();
    renderSection({ evidences: [withoutFile] });

    await user.click(screen.getByRole('button', { name: /anexar arquivo/i }));
    await user.upload(screen.getByLabelText('Arquivo da evidência'), new File([new Uint8Array(20)], 'foto.png', { type: 'image/png' }));
    await user.click(screen.getByRole('button', { name: 'Enviar arquivo' }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(evidenceApi.uploadFile).toHaveBeenCalledWith(CASE_ID, 'ev-pending', expect.any(File), expect.any(Function));
  });

  it('shows upload progress while the file is being sent', async () => {
    let finish!: () => void;
    vi.spyOn(evidenceApi, 'uploadFile').mockImplementation(async (_c, _e, _f, onProgress) => {
      onProgress?.(42);
      await new Promise<void>((resolve) => (finish = resolve));
      return {} as never;
    });
    const user = userEvent.setup();
    renderSection({ evidences: [withoutFile] });

    await user.click(screen.getByRole('button', { name: /anexar arquivo/i }));
    await user.upload(screen.getByLabelText('Arquivo da evidência'), new File([new Uint8Array(5)], 'foto.jpg'));
    await user.click(screen.getByRole('button', { name: 'Enviar arquivo' }));

    const bar = await screen.findByRole('progressbar', { name: 'Progresso do envio' });
    expect(bar).toHaveAttribute('aria-valuenow', '42');
    expect(screen.getByText('Enviando… 42%')).toBeInTheDocument();
    finish();
  });

  it.each([
    ['programa.exe', 10, EvidenceType.Document, /formato não permitido/i],
    ['grande.pdf', MAX_EVIDENCE_FILE_BYTES + 1, EvidenceType.Document, /o máximo é 25 MB/i],
    ['laudo.pdf', 10, EvidenceType.Image, /é do tipo imagem/i],
  ])('rejects %s on the client without calling the API', async (name, size, type, message) => {
    const upload = vi.spyOn(evidenceApi, 'uploadFile');
    const user = userEvent.setup({ applyAccept: false });
    renderSection({ evidences: [{ ...withoutFile, evidenceType: type }] });

    await user.click(screen.getByRole('button', { name: /anexar arquivo/i }));
    await user.upload(screen.getByLabelText('Arquivo da evidência'), pdf(name, size));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Enviar arquivo' })).toBeDisabled();
    expect(upload).not.toHaveBeenCalled();
  });

  it('shows the server error (e.g. signature rejected) and keeps the dialog open', async () => {
    vi.spyOn(api, 'post').mockRejectedValue(
      new AxiosError('415', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 415,
        data: { success: false, message: 'O conteúdo do arquivo não corresponde a nenhum formato permitido.' },
      } as never),
    );
    const user = userEvent.setup();
    renderSection({ evidences: [withoutFile] });

    await user.click(screen.getByRole('button', { name: /anexar arquivo/i }));
    await user.upload(screen.getByLabelText('Arquivo da evidência'), new File([new Uint8Array(5)], 'foto.png'));
    await user.click(screen.getByRole('button', { name: 'Enviar arquivo' }));

    expect(await screen.findByText('O conteúdo do arquivo não corresponde a nenhum formato permitido.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('removal requires a reason and sends it', async () => {
    let body: unknown;
    server.use(
      http.delete(`${API}/${CASE_ID}/evidences/ev-file`, async ({ request }) => {
        body = await request.json();
        return HttpResponse.json({ success: true, data: true });
      }),
    );
    const user = userEvent.setup();
    renderSection();

    await user.click(screen.getByRole('button', { name: 'Remover Relatório da catraca' }));
    await user.click(screen.getByRole('button', { name: 'Remover' }));
    expect(await screen.findByText(/informe o motivo/i)).toBeInTheDocument();
    expect(body).toBeUndefined();

    await user.type(screen.getByLabelText('Motivo da remoção'), 'Anexada ao processo errado');
    await user.click(screen.getByRole('button', { name: 'Remover' }));

    await waitFor(() => expect(body).toEqual({ reason: 'Anexada ao processo errado' }));
  });

  it('hides upload, removal and download without the permissions', () => {
    renderSection({ canAdd: false, canRemove: false, canDownload: false });

    expect(screen.queryByRole('button', { name: /registrar evidência/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /anexar arquivo/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /remover/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /baixar/i })).not.toBeInTheDocument();
  });

  it('a closed case keeps download but blocks new evidence, upload and removal', () => {
    renderSection({ caseStatus: DisciplinaryCaseStatus.Completed });

    expect(screen.getByText(/processo encerrado/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Baixar catraca.pdf' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /registrar evidência/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /anexar arquivo/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /remover/i })).not.toBeInTheDocument();
  });
});

describe('fileNameFromContentDisposition', () => {
  it('prefers filename* (UTF-8) over filename', () => {
    expect(fileNameFromContentDisposition("attachment; filename=a.pdf; filename*=UTF-8''Ata%20n%C2%BA1.pdf")).toBe('Ata nº1.pdf');
    expect(fileNameFromContentDisposition('attachment; filename="b.pdf"')).toBe('b.pdf');
    expect(fileNameFromContentDisposition(undefined)).toBeNull();
  });
});
