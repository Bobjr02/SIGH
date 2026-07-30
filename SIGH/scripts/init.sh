#!/bin/bash
# Inicialização da Infraestrutura Docker do SIGH
echo "Iniciando os serviços Docker do SIGH..."
docker-compose up -d
echo "Aguardando o SQL Server ficar saudável..."
