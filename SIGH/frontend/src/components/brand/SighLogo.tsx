import React from 'react';
import { useThemeContext } from '../../contexts/ThemeContext';
import type { ThemeMode } from '../../styles/tokens';
// Logo oficial (SIGH/docs/design/logo, ver LOGO.md). Os arquivos são copiados SEM alteração e
// carregados como arquivo (<img src>): cada SVG usa os mesmos ids internos (a, b, c) nos degradês,
// que colidiriam se dois logos fossem embutidos na mesma página; como <img>, cada um é um documento
// isolado. De quebra, os SVGs (23 a 62 kB) não entram no bundle de JS.
import horizontalEscuro from '../../assets/logo/sigh-logo-para-fundo-escuro.svg';
import horizontalClaro from '../../assets/logo/sigh-logo-para-fundo-claro.svg';
import completoEscuro from '../../assets/logo/sigh-logo-completo-para-fundo-escuro.svg';
import completoClaro from '../../assets/logo/sigh-logo-completo-para-fundo-claro.svg';
import simbolo from '../../assets/logo/sigh-simbolo.svg';

export type SighLogoVariant = 'horizontal' | 'completo' | 'simbolo';

/** Proporções (largura/altura) do viewBox de cada arquivo: a largura acompanha a altura, sem distorcer. */
const RATIO: Record<SighLogoVariant, number> = {
  horizontal: 1275 / 449,
  completo: 1605 / 667,
  simbolo: 363 / 449,
};

const SOURCES: Record<SighLogoVariant, Record<ThemeMode, string>> = {
  horizontal: { dark: horizontalEscuro, light: horizontalClaro },
  completo: { dark: completoEscuro, light: completoClaro },
  simbolo: { dark: simbolo, light: simbolo },
};

/** Altura mínima do logo horizontal (LOGO.md). Abaixo disso, use o símbolo. */
export const MIN_HORIZONTAL_HEIGHT = 28;

interface SighLogoProps {
  /** horizontal: escudo + SIGH; completo: + subtítulo; simbolo: só o escudo. */
  variant?: SighLogoVariant;
  /** Altura em px (a largura acompanha). */
  height?: number;
  /**
   * Fundo onde o logo aparece. Padrão: o do tema atual (troca sozinho com o tema). Use "dark" quando o
   * fundo é sempre escuro, como o painel esquerdo do login.
   */
  background?: ThemeMode;
  className?: string;
}

export const SighLogo: React.FC<SighLogoProps> = ({ variant = 'horizontal', height = 32, background, className }) => {
  const { mode } = useThemeContext();
  const h = variant === 'horizontal' ? Math.max(height, MIN_HORIZONTAL_HEIGHT) : height;
  return (
    <img
      src={SOURCES[variant][background ?? mode]}
      alt="SIGH"
      height={h}
      width={Math.round(h * RATIO[variant])}
      className={className}
      style={{ height: h, width: 'auto', display: 'block' }}
      data-testid={`sigh-logo-${variant}`}
      draggable={false}
    />
  );
};

export default SighLogo;
