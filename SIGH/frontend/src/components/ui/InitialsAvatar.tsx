import React from 'react';
import { initialsOf } from '../../utils/formatters';

/** Avatar com as iniciais do nome (linhas de pessoa nas tabelas, cabeçalho de detalhe, rodapé do menu). */
export const InitialsAvatar: React.FC<{ name?: string | null; size?: number }> = ({ name, size = 28 }) => (
  <span
    aria-hidden="true"
    className="inline-flex shrink-0 items-center justify-center rounded-full font-bold text-white"
    style={{ width: size, height: size, fontSize: Math.max(10, size * 0.38), background: 'var(--sigh-primary)' }}
  >
    {initialsOf(name)}
  </span>
);
