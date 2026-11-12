#!/usr/bin/env bash
# Executado uma vez quando o devcontainer/Codespace é criado.
set -euo pipefail

# Claude Code pelo instalador nativo (instala em ~/.local/bin).
curl -fsSL https://claude.ai/install.sh | bash

# Garante ~/.local/bin no PATH dos próximos shells.
if ! grep -q 'HOME/.local/bin' "$HOME/.bashrc" 2>/dev/null; then
  echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bashrc"
fi
if [ -f "$HOME/.zshrc" ] && ! grep -q 'HOME/.local/bin' "$HOME/.zshrc"; then
  echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.zshrc"
fi
export PATH="$HOME/.local/bin:$PATH"

# Ferramentas e dependências do projeto.
(cd SIGH/backend && dotnet tool restore)
(cd SIGH/frontend && npm ci)
