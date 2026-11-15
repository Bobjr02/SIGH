import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeContextProvider, useThemeContext } from '../../../contexts/ThemeContext';
import { SighLogo } from '../SighLogo';

const Toggle = () => <button onClick={useThemeContext().toggleTheme}>alternar</button>;

describe('SighLogo (logo oficial)', () => {
  it('uses the package files as <img alt="SIGH">, switching dark/light with the theme', async () => {
    render(
      <ThemeContextProvider>
        <SighLogo variant="horizontal" height={20} />
        <SighLogo variant="completo" />
        <SighLogo variant="simbolo" />
        <Toggle />
      </ThemeContextProvider>,
    );
    const horizontal = screen.getByTestId('sigh-logo-horizontal');
    expect(horizontal).toHaveAttribute('alt', 'SIGH');
    expect(horizontal.getAttribute('src')).toMatch(/sigh-logo-para-fundo-escuro/);
    // Altura mínima do horizontal: 28 px.
    expect(horizontal).toHaveAttribute('height', '28');
    expect(screen.getByTestId('sigh-logo-completo').getAttribute('src')).toMatch(/sigh-logo-completo-para-fundo-escuro/);
    expect(screen.getByTestId('sigh-logo-simbolo').getAttribute('src')).toMatch(/sigh-simbolo/);
    expect(screen.getAllByAltText('SIGH')).toHaveLength(3);

    await userEvent.setup().click(screen.getByRole('button', { name: 'alternar' }));
    expect(screen.getByTestId('sigh-logo-horizontal').getAttribute('src')).toMatch(/sigh-logo-para-fundo-claro/);
    expect(screen.getByTestId('sigh-logo-completo').getAttribute('src')).toMatch(/sigh-logo-completo-para-fundo-claro/);
    localStorage.clear();
  });
});
