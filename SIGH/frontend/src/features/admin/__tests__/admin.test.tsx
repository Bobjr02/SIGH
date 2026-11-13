import { describe, expect, it } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { API, makeUser, meReturns, ok, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { formatValidity, ValidityState, validityFromDates, type UserAccess } from '../api/adminApi';

const ADMIN = makeUser({
  id: 'admin-1',
  fullName: 'Ada Admin',
  roles: ['Administrator'],
  permissions: [
    PERMISSIONS.USERS_VIEW,
    PERMISSIONS.USERS_CREATE,
    PERMISSIONS.USERS_CHANGE_STATUS,
    PERMISSIONS.USERS_UNLOCK,
    PERMISSIONS.USERS_MANAGE_ROLES,
    PERMISSIONS.MANAGEMENT_UNITS_MANAGE,
  ],
});

const ROLES = [
  { id: 'r-admin', name: 'Administrator', label: 'Administrador', description: '', permissions: [] },
  { id: 'r-reg', name: 'HrRegistrar', label: 'RH – Registro', description: '', permissions: [] },
  { id: 'r-apr', name: 'HrApprover', label: 'RH – Aprovador', description: '', permissions: [] },
  { id: 'r-man', name: 'Manager', label: 'Gestor', description: '', permissions: [] },
];

const UNITS = [
  { id: 'mu-131', code: 'USGA131', name: 'Industrial', isActive: true },
  { id: 'mu-132', code: 'USGA132', name: 'Manutenção', isActive: true },
];

const userDetail = (id: string, fullName: string) => ({
  id,
  fullName,
  email: `${id}@sigh.test`,
  cpf: '***',
  status: 'Active',
  mustChangePassword: false,
  failedLoginAttempts: 0,
  roles: [],
  permissions: [],
  createdAt: '2026-11-01T00:00:00Z',
});

describe('administração de perfis', () => {
  it('shows expired and scheduled roles, and assigns a role with validity (until is inclusive)', async () => {
    let access: UserAccess = {
      userId: 'u-2',
      roles: [
        { id: 'l-1', roleId: 'r-reg', roleName: 'HrRegistrar', roleLabel: 'RH – Registro', validFrom: null, validUntil: null, state: ValidityState.Active },
        { id: 'l-2', roleId: 'r-apr', roleName: 'HrApprover', roleLabel: 'RH – Aprovador', ...validityFromDates('2026-10-01', '2026-10-20'), state: ValidityState.Expired },
      ],
      managementUnits: [],
    };
    let assigned: Record<string, unknown> | null = null;

    server.use(
      refreshSucceeds(),
      meReturns(ADMIN),
      http.get(`${API}/users/u-2`, () => ok(userDetail('u-2', 'Rui Substituto'))),
      http.get(`${API}/users/u-2/access`, () => ok(access)),
      http.get(`${API}/roles`, () => ok(ROLES)),
      http.get(`${API}/management-units`, () => ok(UNITS)),
      http.post(`${API}/users/u-2/roles`, async ({ request }) => {
        assigned = (await request.json()) as Record<string, unknown>;
        access = {
          ...access,
          roles: [
            ...access.roles,
            { id: 'l-3', roleId: 'r-apr', roleName: 'HrApprover', roleLabel: 'RH – Aprovador', validFrom: assigned.validFrom as string, validUntil: assigned.validUntil as string, state: ValidityState.Scheduled },
          ],
        };
        return ok(access);
      }),
    );

    renderApp('/admin/usuarios/u-2');
    const user = userEvent.setup();

    const roles = await screen.findByRole('region', { name: 'Perfis' });
    const rows = await within(roles).findAllByTestId('role-row');
    expect(within(rows[1]).getByTestId('validity-state')).toHaveTextContent('Vencido');
    expect(within(rows[1]).getByText('de 01/10/2026 até 20/10/2026')).toBeInTheDocument();

    await user.click(within(roles).getByRole('combobox', { name: 'Perfil' }));
    await user.click(await screen.findByRole('option', { name: 'RH – Aprovador' }));
    fireEvent.change(within(roles).getByLabelText('Válido de'), { target: { value: '2026-12-01' } });
    fireEvent.change(within(roles).getByLabelText('Válido até'), { target: { value: '2026-12-20' } });
    await user.click(within(roles).getByRole('button', { name: 'Atribuir' }));

    expect(await screen.findByText('Perfil atribuído.')).toBeInTheDocument();
    expect(assigned).toEqual({ roleId: 'r-apr', ...validityFromDates('2026-12-01', '2026-12-20') });
    const scheduled = (await within(roles).findAllByTestId('role-row'))[2];
    expect(within(scheduled).getByTestId('validity-state')).toHaveTextContent('Ainda vai começar');
    expect(within(scheduled).getByText('de 01/12/2026 até 20/12/2026')).toBeInTheDocument();
  });

  it('nobody manages their own access: the buttons are not shown for the logged user', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(ADMIN),
      http.get(`${API}/users/admin-1`, () => ok(userDetail('admin-1', 'Ada Admin'))),
      http.get(`${API}/users/admin-1/access`, () =>
        ok({ userId: 'admin-1', roles: [{ id: 'l-a', roleId: 'r-admin', roleName: 'Administrator', roleLabel: 'Administrador', state: ValidityState.Active }], managementUnits: [] }),
      ),
      http.get(`${API}/roles`, () => ok(ROLES)),
      http.get(`${API}/management-units`, () => ok(UNITS)),
    );

    renderApp('/admin/usuarios/admin-1');

    expect(await screen.findByText(/só podem ser alterados por outro administrador/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Atribuir' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /retirar administrador/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Desativar' })).not.toBeInTheDocument();
  });

  it('shows the server message when the rule blocks (manager without unit)', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(ADMIN),
      http.get(`${API}/users/u-3`, () => ok(userDetail('u-3', 'Gil Gestor'))),
      http.get(`${API}/users/u-3/access`, () => ok({ userId: 'u-3', roles: [], managementUnits: [] })),
      http.get(`${API}/roles`, () => ok(ROLES)),
      http.get(`${API}/management-units`, () => ok(UNITS)),
      http.post(`${API}/users/u-3/roles`, () =>
        HttpResponse.json({ status: 400, detail: 'O perfil Gestor exige pelo menos uma gerência vinculada ao usuário.' }, { status: 400 }),
      ),
    );

    renderApp('/admin/usuarios/u-3');
    const user = userEvent.setup();
    const roles = await screen.findByRole('region', { name: 'Perfis' });
    await user.click(within(roles).getByRole('combobox', { name: 'Perfil' }));
    await user.click(await screen.findByRole('option', { name: 'Gestor' }));
    await user.click(within(roles).getByRole('button', { name: 'Atribuir' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('pelo menos uma gerência');
  });

  it('the creation of a manager requires at least one management unit', async () => {
    let created: Record<string, unknown> | null = null;
    server.use(
      refreshSucceeds(),
      meReturns(ADMIN),
      http.get(`${API}/users`, () => ok({ items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      http.get(`${API}/roles`, () => ok(ROLES)),
      http.get(`${API}/management-units`, () => ok(UNITS)),
      http.post(`${API}/users`, async ({ request }) => {
        created = (await request.json()) as Record<string, unknown>;
        return ok({ id: 'new-1' }, { status: 201 });
      }),
      http.get(`${API}/users/new-1`, () => ok(userDetail('new-1', 'Gil Gestor'))),
      http.get(`${API}/users/new-1/access`, () => ok({ userId: 'new-1', roles: [], managementUnits: [] })),
    );

    renderApp('/admin/usuarios');
    const user = userEvent.setup();
    await user.click(await screen.findByRole('button', { name: 'Novo usuário' }));
    const dialog = await screen.findByRole('dialog', { name: 'Novo usuário' });
    await user.type(within(dialog).getByRole('textbox', { name: /nome completo/i }), 'Gil Gestor');
    await user.type(within(dialog).getByRole('textbox', { name: /e-mail/i }), 'gil@sigh.test');
    await user.type(within(dialog).getByRole('textbox', { name: /cpf/i }), '52998224725');
    await user.click(await within(dialog).findByRole('checkbox', { name: 'Gestor' }));

    const submit = within(dialog).getByRole('button', { name: 'Criar usuário' });
    expect(submit).toBeDisabled();
    await user.click(within(dialog).getByRole('checkbox', { name: 'USGA131' }));
    expect(submit).toBeEnabled();
    await user.click(submit);

    await waitFor(() => expect(created).toMatchObject({ roleIds: ['r-man'], managementUnitIds: ['mu-131'], initialPassword: null }));
    expect(await screen.findByText(/e-mail de definição de senha foi enviado/)).toBeInTheDocument();
  });

  it('management units: the 9 are listed and the name is editable', async () => {
    let renamed: string | null = null;
    server.use(
      refreshSucceeds(),
      meReturns(ADMIN),
      http.get(`${API}/management-units`, () => ok(renamed ? [{ ...UNITS[0], name: renamed }, UNITS[1]] : UNITS)),
      http.put(`${API}/management-units/mu-131`, async ({ request }) => {
        renamed = ((await request.json()) as { name: string }).name;
        return ok({ ...UNITS[0], name: renamed });
      }),
    );

    renderApp('/admin/gerencias');
    const user = userEvent.setup();
    const input = await screen.findByRole('textbox', { name: 'Nome da USGA131' });
    await user.clear(input);
    await user.type(input, 'Industrial e Moagem');
    await user.click(within(screen.getAllByTestId('management-unit-row')[0]).getByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(renamed).toBe('Industrial e Moagem'));
  });
});

describe('validade', () => {
  it('converts inclusive calendar dates to [from, until) instants and back', () => {
    const v = validityFromDates('2026-12-01', '2026-12-20');
    expect(formatValidity(v.validFrom, v.validUntil)).toBe('de 01/12/2026 até 20/12/2026');
    expect(formatValidity(null, null)).toBe('sem prazo');
    expect(formatValidity(v.validFrom, null)).toBe('a partir de 01/12/2026');
  });
});
