#!/bin/bash
# Execução dos Testes da Solução SIGH
echo "Executando testes unitários e de integração..."
dotnet test SIGH/backend/SIGH.sln --configuration Release
