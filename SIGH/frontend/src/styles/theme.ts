import { createTheme, type Theme } from '@mui/material/styles';
import { colors, radii, typography, type ThemeMode } from './tokens';

/**
 * Tema do MUI a partir dos tokens (styles/tokens.ts). A paleta recebe os valores (o MUI calcula
 * contrastes e hovers com eles) e os componentes usam var(--sigh-*), as mesmas variáveis do Tailwind:
 * todas as telas, MUI ou Tailwind, parecem o mesmo sistema.
 */
export const createSighTheme = (mode: ThemeMode): Theme => {
  const c = colors[mode];
  return createTheme({
    palette: {
      mode,
      primary: { main: c.primary, dark: c.primaryHover, light: c.primaryText, contrastText: c.onPrimary },
      secondary: { main: c.infoSolid, contrastText: '#ffffff' },
      success: { main: c.successSolid, light: c.success, contrastText: '#ffffff' },
      warning: { main: c.warningSolid, dark: c.warning, contrastText: c.onWarningSolid },
      error: { main: c.dangerSolid, light: c.danger, contrastText: '#ffffff' },
      info: { main: c.infoSolid, light: c.info, contrastText: '#ffffff' },
      background: { default: c.bg, paper: c.surface },
      text: { primary: c.text, secondary: c.textMuted },
      divider: c.border,
      action: { hover: c.surface2, selected: c.primarySoft },
    },
    typography: {
      fontFamily: typography.fontFamily,
      h1: { fontWeight: 700, fontSize: '2rem', letterSpacing: '-0.02em' },
      h2: { fontWeight: 700, fontSize: '1.5rem' },
      h3: { fontWeight: 700, fontSize: '1.25rem' },
      h4: { fontWeight: 700, fontSize: '1.75rem', letterSpacing: '-0.01em' },
      h5: { fontWeight: 700, fontSize: '1.25rem' },
      h6: { fontWeight: 600, fontSize: '1rem' },
      body1: { fontSize: '0.9375rem' },
      body2: { fontSize: '0.875rem' },
      button: { textTransform: 'none', fontWeight: 600 },
    },
    shape: { borderRadius: 10 },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: { backgroundColor: 'var(--sigh-bg)', color: 'var(--sigh-text)' },
          '*:focus-visible': { outline: '2px solid var(--sigh-focus)', outlineOffset: '2px' },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: { backgroundImage: 'none', backgroundColor: 'var(--sigh-surface)' },
          outlined: { borderColor: 'var(--sigh-border)' },
          elevation0: { border: '1px solid var(--sigh-border)', borderRadius: radii.lg },
          elevation8: { border: '1px solid var(--sigh-border)', boxShadow: 'var(--sigh-shadow-popover)' },
        },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { borderRadius: radii.md, padding: '8px 18px' },
          sizeSmall: { padding: '5px 12px' },
          outlined: { borderColor: 'var(--sigh-border-strong)', color: 'var(--sigh-text)' },
          text: { color: 'var(--sigh-primary-text)' },
        },
      },
      MuiIconButton: { styleOverrides: { root: { color: 'var(--sigh-text-muted)', borderRadius: radii.md } } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            backgroundColor: 'var(--sigh-surface2)',
            borderRadius: radii.md,
            '& .MuiOutlinedInput-notchedOutline': { borderColor: 'var(--sigh-border-strong)' },
            '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: 'var(--sigh-text-muted)' },
          },
        },
      },
      MuiInputLabel: { styleOverrides: { root: { color: 'var(--sigh-text-muted)' } } },
      MuiFormHelperText: { styleOverrides: { root: { color: 'var(--sigh-text-muted)' } } },
      MuiTableContainer: { styleOverrides: { root: { backgroundColor: 'var(--sigh-surface)' } } },
      MuiTableHead: {
        styleOverrides: {
          root: {
            '& .MuiTableCell-head': {
              backgroundColor: 'var(--sigh-surface2)',
              color: 'var(--sigh-text-muted)',
              fontWeight: 600,
              fontSize: '0.8125rem',
              whiteSpace: 'nowrap',
            },
          },
        },
      },
      MuiTableCell: { styleOverrides: { root: { borderColor: 'var(--sigh-border)', padding: '12px 16px' } } },
      MuiTableRow: { styleOverrides: { hover: { '&:hover': { backgroundColor: 'var(--sigh-surface2) !important' } } } },
      MuiChip: {
        styleOverrides: {
          root: { fontWeight: 600, borderRadius: radii.sm },
          sizeSmall: { height: 22, fontSize: '0.75rem' },
          colorDefault: { backgroundColor: 'var(--sigh-neutral-solid)', color: '#ffffff' },
          outlined: { borderColor: 'var(--sigh-border-strong)', color: 'var(--sigh-text)' },
        },
      },
      MuiTabs: {
        styleOverrides: {
          root: { borderBottom: '1px solid var(--sigh-border)', minHeight: 44 },
          indicator: { backgroundColor: 'var(--sigh-primary-text)', height: 2 },
        },
      },
      MuiTab: {
        styleOverrides: {
          root: {
            textTransform: 'none',
            fontWeight: 600,
            minHeight: 44,
            color: 'var(--sigh-text-muted)',
            '&.Mui-selected': { color: 'var(--sigh-primary-text)' },
          },
        },
      },
      MuiTooltip: {
        styleOverrides: {
          tooltip: { backgroundColor: 'var(--sigh-surface3)', color: 'var(--sigh-text)', fontSize: '0.75rem', border: '1px solid var(--sigh-border)' },
        },
      },
      MuiLink: { styleOverrides: { root: { color: 'var(--sigh-primary-text)', textDecorationColor: 'currentColor' } } },
      MuiAlert: {
        styleOverrides: {
          root: ({ ownerState }) => {
            const tone = { success: 'success', error: 'danger', warning: 'warning', info: 'info' }[ownerState.severity ?? 'info'] ?? 'info';
            return ownerState.variant === 'standard' || !ownerState.variant
              ? { borderRadius: radii.md, border: '1px solid var(--sigh-border)', backgroundColor: `var(--sigh-${tone}-soft)`, color: `var(--sigh-${tone})` }
              : { borderRadius: radii.md };
          },
        },
      },
      MuiDialog: { styleOverrides: { paper: { borderRadius: radii.lg, border: '1px solid var(--sigh-border)' } } },
      MuiMenu: { styleOverrides: { paper: { backgroundColor: 'var(--sigh-surface)' } } },
      MuiPaginationItem: {
        styleOverrides: {
          root: {
            color: 'var(--sigh-text)',
            borderRadius: radii.sm,
            '&.Mui-selected': { backgroundColor: 'var(--sigh-primary)', color: '#ffffff' },
            '&.Mui-selected:hover': { backgroundColor: 'var(--sigh-primary-hover)' },
          },
        },
      },
      MuiDivider: { styleOverrides: { root: { borderColor: 'var(--sigh-border)' } } },
      MuiSkeleton: { styleOverrides: { root: { backgroundColor: 'var(--sigh-surface3)' } } },
    },
  });
};

/** Compatibilidade com testes e imports antigos: o tema escuro padrão. */
export const sighTheme = createSighTheme('dark');
