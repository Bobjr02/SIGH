import React, { useId } from 'react';

export type SighLogoVariant = 'full' | 'mark';

interface SighLogoProps {
  /** full: escudo + "SIGH"; mark: só o escudo (menu recolhido, favicon). */
  variant?: SighLogoVariant;
  /** Altura em px. */
  size?: number;
  className?: string;
  /** Texto alternativo; omita quando houver texto visível ao lado (o logo fica decorativo). */
  title?: string;
}

/** Caminho do escudo (viewBox 0 0 48 56). */
export const SHIELD_PATH = 'M24 2 L44 9 V27 C44 40 35.5 49.5 24 54 C12.5 49.5 4 40 4 27 V9 Z';
/** "S" como traço único (stroke), espessura constante. */
export const S_PATH =
  'M33.5 16.5 H20.5 C16.6 16.5 14.5 18.9 14.5 21.9 C14.5 24.9 16.6 27.2 20.5 27.2 H27.5 C31.4 27.2 33.5 29.5 33.5 32.5 C33.5 35.5 31.4 37.9 27.5 37.9 H14.5';

/**
 * Logo do SIGH: escudo azul com o "S" (recriado em SVG a partir da referência) e o nome. As cores do
 * escudo funcionam nos dois temas; o texto "SIGH" usa a cor do texto do tema (currentColor).
 */
export const SighLogo: React.FC<SighLogoProps> = ({ variant = 'full', size = 32, className, title }) => {
  const id = useId().replace(/:/g, '');
  const mark = (
    <svg
      viewBox="0 0 48 56"
      height={size}
      width={(size * 48) / 56}
      role={title ? 'img' : undefined}
      aria-label={title}
      aria-hidden={title ? undefined : true}
      focusable="false"
    >
      <defs>
        <linearGradient id={`sigh-shield-${id}`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#3fa2ff" />
          <stop offset="1" stopColor="#0d4fc4" />
        </linearGradient>
      </defs>
      <path d={SHIELD_PATH} fill={`url(#sigh-shield-${id})`} />
      <path d="M24 6.5 L40 12.2 V27 C40 37.6 33.2 45.5 24 49.6 C14.8 45.5 8 37.6 8 27 V12.2 Z" fill="#0a2a66" opacity="0.35" />
      <path d={S_PATH} fill="none" stroke="#ffffff" strokeWidth={5.2} strokeLinejoin="round" />
    </svg>
  );

  if (variant === 'mark') return <span className={className} style={{ display: 'inline-flex' }}>{mark}</span>;

  return (
    <span className={className} style={{ display: 'inline-flex', alignItems: 'center', gap: size * 0.32 }}>
      {mark}
      <span
        aria-hidden={title ? true : undefined}
        style={{ fontWeight: 800, fontSize: size * 0.72, letterSpacing: '0.04em', lineHeight: 1, color: 'currentColor' }}
      >
        SIGH
      </span>
    </span>
  );
};

export default SighLogo;
