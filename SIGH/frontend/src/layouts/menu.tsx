import React from 'react';
import {
  BookOpen,
  Building2,
  ClipboardCheck,
  FilePlus2,
  FileSpreadsheet,
  Gavel,
  History,
  LayoutDashboard,
  ScrollText,
  BarChart3,
  UserCog,
  Users,
} from 'lucide-react';
import { PERMISSIONS, type PermissionCode } from '../auth/permissions';

export interface MenuItem {
  label: string;
  path: string;
  permission: PermissionCode;
  icon: React.ReactNode;
  /** Item ativo para estes caminhos (padrão: o próprio caminho e os filhos). */
  isActive?: (pathname: string) => boolean;
  /** Contador de aprovações pendentes. */
  badge?: 'pending-approvals';
  testId?: string;
}

const under = (prefix: string) => (p: string) => p === prefix || p.startsWith(`${prefix}/`);

/** Menu principal, na ordem da especificação. Só aparece o que o perfil pode ver. */
export const MAIN_MENU: MenuItem[] = [
  { label: 'Painel', path: '/painel', permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD, icon: <LayoutDashboard size={19} />, testId: 'menu-painel' },
  { label: 'Registrar medida', path: '/medidas/registrar', permission: PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER, icon: <FilePlus2 size={19} /> },
  {
    label: 'Aprovações',
    path: '/medidas/aprovacoes',
    permission: PERMISSIONS.DISCIPLINARY_RECORDS_APPROVE,
    icon: <ClipboardCheck size={19} />,
    badge: 'pending-approvals',
  },
  {
    label: 'Medidas',
    path: '/medidas',
    permission: PERMISSIONS.DISCIPLINARY_RECORDS_VIEW,
    icon: <Gavel size={19} />,
    isActive: (p) => under('/medidas')(p) && !p.startsWith('/medidas/registrar') && !p.startsWith('/medidas/aprovacoes'),
  },
  {
    label: 'Funcionários',
    path: '/employees',
    permission: PERMISSIONS.EMPLOYEES_VIEW,
    icon: <Users size={19} />,
    isActive: (p) => under('/employees')(p) && !p.startsWith('/employees/import'),
  },
  { label: 'Relatórios', path: '/relatorios', permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES, icon: <BarChart3 size={19} /> },
  { label: 'Auditoria', path: '/audit', permission: PERMISSIONS.AUDIT_VIEW, icon: <ScrollText size={19} /> },
];

/** Grupo "Administração". "Motivos" (catálogo de tipos de infração) fica aqui para não sumir do menu. */
export const ADMIN_MENU: MenuItem[] = [
  { label: 'Usuários', path: '/admin/usuarios', permission: PERMISSIONS.USERS_VIEW, icon: <UserCog size={19} /> },
  { label: 'Gerências', path: '/admin/gerencias', permission: PERMISSIONS.MANAGEMENT_UNITS_MANAGE, icon: <Building2 size={19} /> },
  { label: 'Importar funcionários', path: '/employees/import', permission: PERMISSIONS.EMPLOYEES_IMPORT, icon: <FileSpreadsheet size={19} /> },
  { label: 'Importar histórico', path: '/admin/history-import', permission: PERMISSIONS.DISCIPLINARY_HISTORY_IMPORT, icon: <History size={19} /> },
  { label: 'Motivos', path: '/disciplinary/infraction-types', permission: PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_VIEW, icon: <BookOpen size={19} /> },
];

export const isItemActive = (item: MenuItem, pathname: string) => (item.isActive ?? under(item.path))(pathname);
