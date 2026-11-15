/**
 * FONTE ÚNICA de design do SIGH (paleta extraída de docs/design/referencia-tema.png).
 *
 * Os tokens viram variáveis CSS (--sigh-*) em :root[data-theme="dark" | "light"] (ThemeContext), e são
 * consumidos:
 * - pelo Tailwind (index.css, @theme inline → classes bg-surface, text-muted…);
 * - pelo tema do MUI (styles/theme.ts usa os mesmos valores na paleta e var(--sigh-*) nos componentes).
 *
 * Contraste WCAG AA conferido para texto (≥ 4,5:1) e bordas de campos (≥ 3:1) nos dois temas
 * (e validado com axe-core no Playwright: e2e-local/accessibility.spec.ts).
 */
export type ThemeMode = 'dark' | 'light';

export interface ColorTokens {
  /** Fundo da página (área de conteúdo). */
  bg: string;
  /** Menu lateral e barra superior. */
  sidebar: string;
  /** Cartões, tabelas, painéis. */
  surface: string;
  /** Campos, hover de linhas, cabeçalho de tabela. */
  surface2: string;
  /** Hover sobre surface2. */
  surface3: string;
  border: string;
  /** Borda de campos e componentes interativos (contraste ≥ 3:1). */
  borderStrong: string;
  text: string;
  textMuted: string;
  /** Azul de destaque: botões e item ativo. */
  primary: string;
  primaryHover: string;
  /** Azul para texto/ícones sobre o fundo (links, abas ativas). */
  primaryText: string;
  /** Fundo do item ativo do menu e dos ícones dos cartões. */
  primarySoft: string;
  onPrimary: string;
  success: string;
  successSolid: string;
  successSoft: string;
  warning: string;
  warningSolid: string;
  warningSoft: string;
  onWarningSolid: string;
  danger: string;
  dangerSolid: string;
  dangerSoft: string;
  neutralSolid: string;
  info: string;
  infoSolid: string;
  infoSoft: string;
  /** Linha e área do gráfico. */
  chartLine: string;
  chartArea: string;
  chartGrid: string;
  focus: string;
}

export const colors: Record<ThemeMode, ColorTokens> = {
  dark: {
    bg: '#0b1324',
    sidebar: '#0e1729',
    surface: '#131c31',
    surface2: '#1a2438',
    surface3: '#222e46',
    border: '#243049',
    borderStrong: '#64748f',
    text: '#e8edf7',
    textMuted: '#a1aac3',
    // Destaque = azul do escudo do logo oficial (#0BB4FD → #0062FE). Branco sobre #0062FE: 5,03:1.
    primary: '#0062FE',
    primaryHover: '#0055dd',
    primaryText: '#3fb2fd',
    primarySoft: '#123067',
    onPrimary: '#ffffff',
    success: '#3fd19c',
    successSolid: '#0b7a52',
    successSoft: '#0e3b33',
    warning: '#f5c15a',
    warningSolid: '#f5b544',
    warningSoft: '#3b2f14',
    onWarningSolid: '#1a1300',
    danger: '#ff8a93',
    dangerSolid: '#c42b3a',
    dangerSoft: '#3a1b27',
    neutralSolid: '#5a6478',
    info: '#7cc4ff',
    infoSolid: '#2b5cab',
    infoSoft: '#11325a',
    chartLine: '#2f7bf5',
    chartArea: 'rgba(47, 123, 245, 0.28)',
    chartGrid: '#243049',
    focus: '#3fb2fd',
  },
  light: {
    bg: '#f3f6fb',
    sidebar: '#ffffff',
    surface: '#ffffff',
    surface2: '#eef2f8',
    surface3: '#e3e9f3',
    border: '#dbe2ee',
    borderStrong: '#7b879c',
    text: '#0f1a2b',
    textMuted: '#4a5670',
    primary: '#0062FE',
    primaryHover: '#0055dd',
    primaryText: '#0055dd',
    primarySoft: '#dce8fd',
    onPrimary: '#ffffff',
    success: '#0b7a52',
    successSolid: '#0b7a52',
    successSoft: '#dcf3ea',
    warning: '#8a5a00',
    warningSolid: '#f5b544',
    warningSoft: '#fdf0d5',
    onWarningSolid: '#1a1300',
    danger: '#b42335',
    dangerSolid: '#c42b3a',
    dangerSoft: '#fbe3e6',
    neutralSolid: '#5a6478',
    info: '#1d5fa8',
    infoSolid: '#2b5cab',
    infoSoft: '#dde9f8',
    chartLine: '#0d55c8',
    chartArea: 'rgba(13, 85, 200, 0.16)',
    chartGrid: '#dbe2ee',
    focus: '#0055dd',
  },
};

export const typography = {
  fontFamily: "'Inter Variable', Inter, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif",
  /** Tamanhos em rem (base 16 px). */
  size: { xs: '0.75rem', sm: '0.8125rem', md: '0.875rem', lg: '1rem', xl: '1.25rem', '2xl': '1.75rem', '3xl': '2.25rem' },
  weight: { regular: 400, medium: 500, semibold: 600, bold: 700 },
};

export const radii = { sm: '6px', md: '10px', lg: '14px', pill: '999px' };

export const shadows: Record<ThemeMode, { card: string; popover: string }> = {
  dark: { card: '0 1px 2px rgba(0, 0, 0, 0.35)', popover: '0 12px 32px rgba(0, 0, 0, 0.55)' },
  light: { card: '0 1px 3px rgba(15, 26, 43, 0.08)', popover: '0 12px 32px rgba(15, 26, 43, 0.16)' },
};

/** Espaçamentos (múltiplos de 4 px; o MUI usa theme.spacing(n) = 4n px... aqui 8n, como no padrão). */
export const spacing = { unit: 8, sidebarWidth: 248, sidebarCollapsed: 76, topbarHeight: 68 };

const kebab = (key: string) => key.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

/** Variáveis CSS de um tema: --sigh-bg, --sigh-text-muted, --sigh-radius-md… */
export const cssVariables = (mode: ThemeMode): Record<string, string> => ({
  ...Object.fromEntries(Object.entries(colors[mode]).map(([k, v]) => [`--sigh-${kebab(k)}`, v])),
  ...Object.fromEntries(Object.entries(radii).map(([k, v]) => [`--sigh-radius-${k}`, v])),
  '--sigh-shadow-card': shadows[mode].card,
  '--sigh-shadow-popover': shadows[mode].popover,
  '--sigh-font': typography.fontFamily,
  '--sigh-sidebar-width': `${spacing.sidebarWidth}px`,
  '--sigh-sidebar-collapsed': `${spacing.sidebarCollapsed}px`,
  '--sigh-topbar-height': `${spacing.topbarHeight}px`,
  'color-scheme': mode,
});

/** CSS completo dos dois temas (injetado uma vez pelo ThemeContext). */
export const themeStylesheet = (): string =>
  (['dark', 'light'] as ThemeMode[])
    .map(
      (mode) =>
        `:root[data-theme="${mode}"]{${Object.entries(cssVariables(mode))
          .map(([k, v]) => `${k}:${v}`)
          .join(';')}}`,
    )
    .join('\n');
