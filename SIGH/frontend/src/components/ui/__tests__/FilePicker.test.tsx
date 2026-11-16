import React, { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FilePicker, type FilePickerProps } from '../FilePicker';
import { ImportSteps } from '../ImportSteps';

const xlsx = (name = 'base.xlsx', bytes = 2048) => new File([new Uint8Array(bytes)], name, { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

/** O FilePicker é controlado: o teste guarda o arquivo como as telas fazem. */
const Harness: React.FC<Partial<FilePickerProps> & { onPicked?: (f: File | null) => void }> = ({ onPicked, ...props }) => {
  const [file, setFile] = useState<File | null>(null);
  return (
    <FilePicker
      label="Planilha do SAP"
      buttonText="Selecionar arquivo .xlsx"
      extensions={['xlsx']}
      hint="Exportação do SAP em .xlsx."
      {...props}
      file={file}
      onChange={(f) => {
        setFile(f);
        onPicked?.(f);
      }}
    />
  );
};

const input = () => screen.getByLabelText('Planilha do SAP') as HTMLInputElement;

describe('FilePicker', () => {
  it('shows a real button with the upload text and the drop area; the native input is labelled but out of the tab order', () => {
    render(<Harness />);
    expect(screen.getByRole('button', { name: 'Selecionar arquivo .xlsx' })).toBeVisible();
    expect(screen.getByText('ou arraste o arquivo aqui')).toBeInTheDocument();
    expect(input()).toHaveAttribute('type', 'file');
    expect(input()).toHaveAttribute('tabindex', '-1');
    expect(input()).toHaveAttribute('accept', '.xlsx');
    expect(input()).toHaveAccessibleDescription('Exportação do SAP em .xlsx.');
  });

  it('clicking the button opens the native dialog; the chosen file shows name and size', async () => {
    const user = userEvent.setup();
    const picked = vi.fn();
    render(<Harness onPicked={picked} />);
    const open = vi.spyOn(input(), 'click');

    await user.click(screen.getByRole('button', { name: 'Selecionar arquivo .xlsx' }));
    expect(open).toHaveBeenCalledTimes(1);

    await user.upload(input(), xlsx('funcionarios.xlsx', 2048));
    expect(picked).toHaveBeenLastCalledWith(expect.objectContaining({ name: 'funcionarios.xlsx' }));
    expect(screen.getByTestId('file-picker-name')).toHaveTextContent('funcionarios.xlsx');
    expect(screen.getByTestId('file-picker-size')).toHaveTextContent('2 KB');
    expect(screen.getByRole('button', { name: 'Trocar arquivo (funcionarios.xlsx)' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remover arquivo (funcionarios.xlsx)' })).toBeInTheDocument();
  });

  it.each([['{Enter}'], [' ']])('keyboard: Tab reaches the button and %s opens the native dialog', async (key) => {
    const user = userEvent.setup();
    render(<Harness />);
    const open = vi.spyOn(input(), 'click');

    await user.tab();
    expect(screen.getByRole('button', { name: 'Selecionar arquivo .xlsx' })).toHaveFocus();
    await user.keyboard(key);
    expect(open).toHaveBeenCalledTimes(1);
  });

  it('rejects a file with the wrong extension, in pt-BR, and keeps nothing selected', async () => {
    const user = userEvent.setup({ applyAccept: false });
    const picked = vi.fn();
    render(<Harness onPicked={picked} />);

    await user.upload(input(), new File(['a;b'], 'base.csv', { type: 'text/csv' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Formato não aceito. Envie um arquivo .xlsx.');
    expect(picked).toHaveBeenLastCalledWith(null);
    expect(screen.queryByTestId('file-picker-name')).not.toBeInTheDocument();
    expect(input()).toHaveAccessibleDescription(/Formato não aceito/);

    // Um arquivo certo limpa a mensagem.
    await user.upload(input(), xlsx());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('uses the screen-specific message and the extra validation (size)', async () => {
    const user = userEvent.setup({ applyAccept: false });
    render(<Harness extensionMessage="Envie a planilha exportada do SAP no formato .xlsx." validate={(f) => (f.size > 10 ? 'Grande demais.' : null)} />);

    await user.upload(input(), new File(['x'], 'base.xls'));
    expect(screen.getByRole('alert')).toHaveTextContent('Envie a planilha exportada do SAP no formato .xlsx.');
    await user.upload(input(), xlsx('base.xlsx', 11));
    expect(screen.getByRole('alert')).toHaveTextContent('Grande demais.');
  });

  it('swaps the file ("Trocar") and removes it ("Remover")', async () => {
    const user = userEvent.setup();
    const picked = vi.fn();
    render(<Harness onPicked={picked} />);
    await user.upload(input(), xlsx('primeira.xlsx'));

    const open = vi.spyOn(input(), 'click');
    await user.click(screen.getByRole('button', { name: 'Trocar arquivo (primeira.xlsx)' }));
    expect(open).toHaveBeenCalledTimes(1);
    await user.upload(input(), xlsx('segunda.xlsx'));
    expect(screen.getByTestId('file-picker-name')).toHaveTextContent('segunda.xlsx');
    expect(screen.queryByText('primeira.xlsx')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Remover arquivo (segunda.xlsx)' }));
    expect(picked).toHaveBeenLastCalledWith(null);
    expect(screen.getByRole('button', { name: 'Selecionar arquivo .xlsx' })).toBeInTheDocument();
  });

  it('accepts a dropped file and validates it the same way', () => {
    const picked = vi.fn();
    render(<Harness onPicked={picked} />);
    const area = screen.getByTestId('file-picker');

    fireEvent.dragOver(area, { dataTransfer: { files: [] } });
    expect(area).toHaveAttribute('data-dragging', 'true');
    fireEvent.drop(area, { dataTransfer: { files: [xlsx('arrastada.xlsx')] } });
    expect(area).not.toHaveAttribute('data-dragging');
    expect(screen.getByTestId('file-picker-name')).toHaveTextContent('arrastada.xlsx');

    fireEvent.drop(area, { dataTransfer: { files: [new File(['x'], 'foto.png')] } });
    expect(screen.getByRole('alert')).toHaveTextContent('Formato não aceito');
    expect(picked).toHaveBeenLastCalledWith(null);
  });

  it('disabled: no button action and drops are ignored', async () => {
    const picked = vi.fn();
    render(<Harness onPicked={picked} disabled />);
    expect(screen.getByRole('button', { name: 'Selecionar arquivo .xlsx' })).toBeDisabled();
    fireEvent.drop(screen.getByTestId('file-picker'), { dataTransfer: { files: [xlsx()] } });
    expect(picked).not.toHaveBeenCalled();
  });
});

describe('ImportSteps', () => {
  it('marks the current step and the completed ones', () => {
    render(<ImportSteps active={1} />);
    const steps = screen.getAllByRole('listitem');
    expect(screen.getByRole('list', { name: 'Etapas da importação' })).toBeInTheDocument();
    expect(steps.map((s) => s.getAttribute('aria-label'))).toEqual([
      'Passo 1: Selecionar arquivo (concluído)',
      'Passo 2: Pré-visualizar (atual)',
      'Passo 3: Confirmar importação',
    ]);
    expect(steps[1]).toHaveAttribute('aria-current', 'step');
  });
});
