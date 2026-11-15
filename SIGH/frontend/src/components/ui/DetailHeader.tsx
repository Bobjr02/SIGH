import React from 'react';
import { Tab, Tabs } from '@mui/material';
import { InitialsAvatar } from './InitialsAvatar';

/** Cabeçalho grande das telas de detalhe: avatar de iniciais, nome, chip de situação e subtítulo. */
export const DetailHeader: React.FC<{
  name: string;
  avatarName?: string;
  icon?: React.ReactNode;
  chip?: React.ReactNode;
  subtitle?: React.ReactNode;
  actions?: React.ReactNode;
}> = ({ name, avatarName, icon, chip, subtitle, actions }) => (
  <div className="mb-4 flex flex-wrap items-center justify-between gap-4">
    <div className="flex items-center gap-4">
      {icon ? (
        <span className="inline-flex h-16 w-16 items-center justify-center rounded-full bg-primary text-white" aria-hidden="true">
          {icon}
        </span>
      ) : (
        <InitialsAvatar name={avatarName ?? name} size={64} />
      )}
      <div>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold leading-tight text-ink">{name}</h1>
          {chip}
        </div>
        {subtitle && <div className="mt-1 text-sm text-muted">{subtitle}</div>}
      </div>
    </div>
    {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
  </div>
);

/** Abas das telas de detalhe (Informações | … ), acessíveis (tablist/tabpanel). */
export function DetailTabs<K extends string>({
  value,
  onChange,
  tabs,
  idPrefix,
}: {
  value: K;
  onChange: (key: K) => void;
  tabs: { key: K; label: string }[];
  idPrefix: string;
}) {
  return (
    <Tabs value={value} onChange={(_, v: K) => onChange(v)} aria-label="Seções" variant="scrollable" allowScrollButtonsMobile sx={{ mb: 2.5 }}>
      {tabs.map((t) => (
        <Tab key={t.key} value={t.key} label={t.label} id={`${idPrefix}-tab-${t.key}`} aria-controls={`${idPrefix}-panel-${t.key}`} />
      ))}
    </Tabs>
  );
}

export const TabPanel: React.FC<{ active: boolean; idPrefix: string; tabKey: string; children: React.ReactNode }> = ({
  active,
  idPrefix,
  tabKey,
  children,
}) =>
  active ? (
    <div role="tabpanel" id={`${idPrefix}-panel-${tabKey}`} aria-labelledby={`${idPrefix}-tab-${tabKey}`} className="flex flex-col gap-4">
      {children}
    </div>
  ) : null;
