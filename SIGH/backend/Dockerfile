# Build da imagem da API. Contexto = raiz do repositório:
#   docker build -f SIGH/backend/Dockerfile .
# Os testes rodam no CI (os de integração precisam de Docker/Testcontainers),
# não durante o build da imagem.

# Stage 1: restore + publish com o SDK .NET 9
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Arquivos de projeto primeiro, para cache do restore
COPY SIGH/backend/global.json SIGH/backend/Directory.Build.props ./SIGH/backend/
COPY SIGH/backend/src/SIGH.Domain/SIGH.Domain.csproj ./SIGH/backend/src/SIGH.Domain/
COPY SIGH/backend/src/SIGH.Application/SIGH.Application.csproj ./SIGH/backend/src/SIGH.Application/
COPY SIGH/backend/src/SIGH.Persistence/SIGH.Persistence.csproj ./SIGH/backend/src/SIGH.Persistence/
COPY SIGH/backend/src/SIGH.Infrastructure/SIGH.Infrastructure.csproj ./SIGH/backend/src/SIGH.Infrastructure/
COPY SIGH/backend/src/SIGH.Api/SIGH.Api.csproj ./SIGH/backend/src/SIGH.Api/
RUN dotnet restore ./SIGH/backend/src/SIGH.Api/SIGH.Api.csproj

COPY SIGH/backend/src/ ./SIGH/backend/src/
RUN dotnet publish ./SIGH/backend/src/SIGH.Api/SIGH.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

# Stage 2: runtime, sem root
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configuração obrigatória (a API não sobe sem ela), sempre por variável de ambiente:
#   ConnectionStrings__DefaultConnection, JwtOptions__SecretKey, JwtOptions__Issuer, JwtOptions__Audience
ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000

# Storage de evidências fora do diretório da aplicação (/app), gravável pelo usuário sem root.
# Em produção, monte um volume nomeado aqui: o Docker copia o dono (APP_UID) do diretório da
# imagem para o volume vazio na primeira montagem, então a API consegue gravar sem root.
RUN mkdir -p /var/lib/sigh/evidences && chown -R $APP_UID:$APP_UID /var/lib/sigh && chmod 750 /var/lib/sigh/evidences
ENV Storage__Local__RootPath=/var/lib/sigh/evidences
VOLUME ["/var/lib/sigh/evidences"]

USER $APP_UID
ENTRYPOINT ["dotnet", "SIGH.Api.dll"]
