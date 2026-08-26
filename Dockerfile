# ETAPA 1: BUILD (O Construtor)
# Puxa uma imagem oficial da Microsoft que contém o SDK do .NET 10 (pesada, tem compilador)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Define a pasta de trabalho dentro do ambiente virtual
WORKDIR /src
# Copia o arquivo de projeto para a pasta de trabalho(/src)
COPY ["payment-gateway-API.csproj", "./"]

# Baixa os pacotes NuGet e restaura as dependências do projeto
RUN dotnet restore "payment-gateway-API.csproj"

# Copia todos os arquivos do projeto para a pasta de trabalho
COPY . .
# Compila o projeto e publica os arquivos de saída em uma pasta chamada /app/publish
RUN dotnet publish "payment-gateway-API.csproj" -c Release -o


# ETAPA 2: RUNTIME (O Executor)
# Puxa uma imagem oficial da Microsoft que contém apenas o runtime do .NET 10 
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# Copia os arquivos publicados da etapa de build para a pasta de trabalho do runtime
COPY --from=build /src/bin/Release/net10.0/publish .

# Define a porta que o container irá expor
EXPOSE 8080

# Define o comando que será executado quando o container for iniciado
ENTRYPOINT ["dotnet", "payment-gateway-API.dll"]
