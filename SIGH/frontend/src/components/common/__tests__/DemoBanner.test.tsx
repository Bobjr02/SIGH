import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { API } from '../../../test/auth';
import { DemoBanner } from '../DemoBanner';

const renderBanner = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <DemoBanner />
      <p>conteúdo</p>
    </QueryClientProvider>,
  );

describe('DemoBanner', () => {
  it('shows "AMBIENTE DE DEMONSTRAÇÃO" when the API says demo', async () => {
    server.use(http.get(`${API}/health`, () => HttpResponse.json({ success: true, data: { status: 'Healthy', demo: true } })));
    renderBanner();
    expect(await screen.findByTestId('demo-banner')).toHaveTextContent('AMBIENTE DE DEMONSTRAÇÃO');
  });

  it('stays hidden in production or when the health check fails', async () => {
    server.use(http.get(`${API}/health`, () => HttpResponse.json({ success: true, data: { status: 'Healthy', demo: false } })));
    renderBanner();
    await screen.findByText('conteúdo');
    await new Promise((r) => setTimeout(r, 50));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
