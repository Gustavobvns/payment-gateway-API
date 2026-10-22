# 💳 Payment Gateway API

API REST de carteira digital simulada, desenvolvida com **.NET 10**, **ASP.NET Core Minimal APIs** e **PostgreSQL**. O projeto foi criado como portfólio para demonstrar fundamentos de backend: autenticação JWT, persistência relacional, operações transacionais, idempotência, testes automatizados e execução reproduzível com Docker.

> **esta é uma API educacional que simula operações entre contas. Não integra bancos, adquirentes ou provedores de pagamento e não processa transações reais de cartão ou Pix.**

## 📌 Visão geral

A API permite criar usuários e contas digitais, transferir valores entre contas, gerar e pagar cobranças e consultar o extrato financeiro. O escopo é intencionalmente reduzido: o foco está na estrutura e nos fundamentos técnicos de uma API, não na implementação de regras de negócio complexas ou integrações financeiras externas.

### Práticas técnicas demonstradas

- **Autenticação JWT:** emissão de tokens Bearer e validação de assinatura, emissor, audiência e expiração.
- **Hash de senhas:** armazenamento de senhas com BCrypt, em vez de texto puro.
- **Persistência relacional:** PostgreSQL com Entity Framework Core, configurações de entidades, relacionamentos, restrições de unicidade e migrações.
- **Consistência transacional:** operações financeiras agrupam alterações relacionadas de saldo e registros de transação em transações do banco.
- **Idempotência:** uso do header `Idempotency-Key` nas operações financeiras para recuperar uma resposta previamente armazenada em novas tentativas com a mesma chave, usuário e operação.
- **Organização por funcionalidades:** estrutura inspirada em Vertical Slice Architecture, mantendo endpoints, serviços e contratos próximos da funcionalidade correspondente.
- **Tratamento padronizado de erros:** tratamento global de exceções e respostas no formato `ProblemDetails`.
- **Health check:** endpoint `/health` para verificar a disponibilidade da conexão com o PostgreSQL.
- **Containerização:** Dockerfile com estágios separados de build e runtime e ambiente local composto por API e PostgreSQL via Docker Compose.
- **Testes automatizados:** xUnit, Moq, FluentAssertions, SQLite em memória e Testcontainers para testes de integração com PostgreSQL.

## 🧱 Stack tecnológica

| Área | Tecnologias |
|---|---|
| Linguagem e plataforma | C#, .NET 10 |
| API HTTP | ASP.NET Core Minimal APIs |
| Autenticação | JWT Bearer, BCrypt |
| Acesso a dados | Entity Framework Core, Npgsql |
| Banco de dados | PostgreSQL 16 |
| Documentação interativa | Swagger / OpenAPI |
| Testes | xUnit, Moq, FluentAssertions |
| Testes de integração | ASP.NET Core `WebApplicationFactory`, Testcontainers |
| Ambiente local | Docker, Docker Compose |

## 🧭 Arquitetura e organização

A aplicação utiliza uma organização inspirada em **Vertical Slice Architecture**. `Program.cs` é responsável principalmente pela composição da aplicação — registro das dependências, configuração de autenticação e middleware e mapeamento dos endpoints — enquanto cada funcionalidade concentra seu próprio fluxo.

```text
payment-gateway-API/
├── src/
│   ├── Auth/
│   │   ├── Configuration/
│   │   ├── Contracts/
│   │   ├── CurrentUser/
│   │   ├── Infrastructure/
│   │   ├── Login/
│   │   ├── Register/
│   │   ├── Services/
│   │   └── Users/
│   ├── Data/
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Features/
│   │   ├── Financial/
│   │   ├── Ledger/
│   │   ├── Payments/
│   │   └── Transfers/
│   ├── Infrastructure/
│   └── Models/
├── tests/
│   ├── Auth/
│   ├── ApiIntegrationTests.cs
│   └── FinancialServicesTests.cs
├── Dockerfile
├── docker-compose.yml
├── payment-gateway-API.csproj
├── payment-gateway-API.slnx
├── payment-gateway-API.http
└── financial-api.http
```

### Fluxo simplificado de uma requisição

```text
Cliente HTTP
    │
    ▼
Minimal API endpoint
    │
    ▼
Serviço da funcionalidade
    │
    ├── Validações e regras da operação
    ├── Contexto do usuário autenticado, quando necessário
    └── Entity Framework Core
             │
             ▼
         PostgreSQL
```

## 🚀 Como executar localmente

A maneira mais simples de executar o projeto é usando Docker Compose. A configuração de desenvolvimento já está no repositório para que qualquer pessoa consiga clonar e executar a API sem depender de chaves privadas ou configurações locais do autor.

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) com Docker Compose disponível.

### 1. Clonar o repositório

```bash
git clone https://github.com/Gustavobvns/payment-gateway-API.git
cd payment-gateway-API
```

### 2. Construir e iniciar os serviços

```bash
docker compose up -d --build
```

O Compose inicia a API e o PostgreSQL. O banco utiliza um volume nomeado para persistir os dados entre reinicializações. As migrações do Entity Framework Core são aplicadas pela aplicação na inicialização.

### 3. Acessar a documentação interativa

- **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **Health check:** [http://localhost:8080/health](http://localhost:8080/health)

Para acompanhar os logs:

```bash
docker compose logs -f payment-gateway-api
```

Para parar os serviços:

```bash
docker compose down
```

Para apagar também os dados persistidos no volume local do PostgreSQL:

```bash
docker compose down -v
```

> **Configuração de desenvolvimento:** o repositório inclui configurações e credenciais demonstrativas para tornar a execução local reproduzível. Elas não devem ser reutilizadas em um ambiente de produção.

### Executar sem Docker (opcional)

Para iniciar a aplicação diretamente, é necessário ter o **.NET SDK 10** e um servidor PostgreSQL acessível. A connection string de desenvolvimento está em `appsettings.Development.json` e aponta para `localhost:5432`.

```bash
dotnet restore
dotnet run
```

No perfil de desenvolvimento, a API utiliza a porta `5201`. Os arquivos `.http` do repositório usam essa porta; no Docker Compose, a porta publicada é `8080`.

## 🔐 Autenticação

As rotas protegidas utilizam JWT Bearer. Após o login, envie o token no cabeçalho `Authorization`:

```http
Authorization: Bearer SEU_TOKEN_JWT
```

A validação do token considera assinatura, emissor, audiência e expiração. As senhas dos usuários são armazenadas como hashes BCrypt.

O token é stateless e permanece válido até expirar. Na versão atual, a desativação lógica de um usuário não revoga imediatamente tokens já emitidos; um novo login para um usuário desativado deve ser recusado.

## 📡 Endpoints principais

Os caminhos abaixo são relativos à raiz da API (`http://localhost:8080` com Docker Compose, ou `http://localhost:5201` com o perfil local de desenvolvimento).

| Área | Método | Endpoint | Descrição | Autenticação |
|---|---|---|---|---|
| Auth | `POST` | `/auth/register` | Cadastro e criação automática de conta | Não |
| Auth | `POST` | `/auth/login` | Login e emissão de JWT | Não |
| Users | `GET` | `/users/me` | Consulta do perfil e saldo | Sim |
| Users | `PUT` | `/users/me` | Atualização do perfil | Sim |
| Users | `PATCH` | `/users/change-password` | Alteração de senha | Sim |
| Users | `DELETE` | `/users/me` | Desativação lógica do usuário | Sim |
| Transfers | `POST` | `/transfers/{destinationAccountId}` | Transferência entre contas | Sim |
| Payments | `POST` | `/payments` | Criação de cobrança | Sim |
| Payments | `GET` | `/payments/{codigo}` | Consulta de cobrança | Não exige token segundo o fluxo atual |
| Payments | `POST` | `/payments/{codigo}/pay` | Pagamento de cobrança | Sim |
| Ledger | `GET` | `/ledger` | Extrato da conta autenticada | Sim |
| Health | `GET` | `/health` | Verificação da conexão com o PostgreSQL | Não |

### Idempotência nas operações financeiras

As operações abaixo recebem o header `Idempotency-Key`:

- `POST /transfers/{destinationAccountId}`
- `POST /payments`
- `POST /payments/{codigo}/pay`

Exemplo:

```http
Idempotency-Key: transferencia-exemplo-001
```

A chave deve ser reutilizada somente em novas tentativas da **mesma operação lógica**. A implementação procura uma resposta previamente armazenada por usuário, operação e chave. O comportamento sob concorrência e falhas em momentos específicos ainda exige validação adicional com PostgreSQL real; veja as limitações conhecidas ao final deste documento.

## 🧪 Exemplos de requisições

Os exemplos usam `http://localhost:8080`. Se executar a aplicação sem Docker, substitua a base por `http://localhost:5201`.

### 1. Cadastrar usuário

```http
POST /auth/register
Content-Type: application/json

{
  "nome": "Usuário Exemplo",
  "documento": "12345678900",
  "email": "usuario@example.com",
  "senha": "Senha-Forte-123"
}
```

Resposta `201 Created` (exemplo):

```json
{
  "accessToken": "SEU_TOKEN_JWT",
  "expiresAt": "AAAA-MM-DDTHH:mm:ssZ"
}
```

### 2. Fazer login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "usuario@example.com",
  "senha": "Senha-Forte-123"
}
```

Utilize o `accessToken` retornado nas requisições autenticadas.

### 3. Transferir entre contas

`destinationAccountId` deve ser o GUID da conta de outro usuário, e a conta de origem precisa ter saldo suficiente.

```http
POST /transfers/{destinationAccountId}
Authorization: Bearer SEU_TOKEN_JWT
Idempotency-Key: transferencia-exemplo-001
Content-Type: application/json

{
  "valor": 25.00
}
```

### 4. Criar uma cobrança

A conta recebedora deve pertencer ao usuário autenticado.

```http
POST /payments
Authorization: Bearer SEU_TOKEN_JWT
Idempotency-Key: cobranca-exemplo-001
Content-Type: application/json

{
  "contaRecebedoraId": "GUID-DA-CONTA-RECEBEDORA",
  "valorOriginal": 50.00,
  "dataVencimento": "AAAA-MM-DD",
  "jurosDiario": 1.50
}
```

A resposta contém o código que será utilizado para consultar e pagar a cobrança. Use uma data de vencimento válida, de acordo com as regras da API.

### 5. Pagar uma cobrança

```http
POST /payments/{codigo}/pay
Authorization: Bearer SEU_TOKEN_JWT
Idempotency-Key: pagamento-exemplo-001
```

Os arquivos [`payment-gateway-API.http`](./payment-gateway-API.http) e [`financial-api.http`](./financial-api.http) contêm requisições adicionais que podem ser executadas por clientes compatíveis com o formato `.http`.

## 🗄️ Modelo de dados

| Entidade | Responsabilidade | Campos relevantes |
|---|---|---|
| `Usuarios` | Identidade e estado do usuário | `Id`, `Nome`, `Documento`, `Email`, `SenhaHash`, `Ativo` |
| `Contas` | Conta digital e saldo | `Id`, `UsuarioId`, `Saldo`, `Version` |
| `CodigosPagamento` | Cobranças emitidas | `Id`, `CodigoPagamentoHash`, `ContaGeradoraId`, `ValorOriginal`, `DataVencimento`, `JurosDiario`, `Status` |
| `Transacoes` | Registro financeiro de entrada/saída | `Id`, `ContaOrigemId`, `ContaDestinoId`, `Valor`, `CodigoPagamentoId`, `DataTransacao` |
| `IdempotencyRecords` | Respostas associadas a chaves idempotentes | `Id`, `UserId`, `Operation`, `Key`, `ResponseJson`, `CreatedAt` |

O banco utiliza chaves estrangeiras e restrições de unicidade para campos como e-mail, documento, hash do código de cobrança e combinação de chave idempotente, usuário e operação.

## ✅ Regras de negócio implementadas

- Valores de operação devem ser positivos; juros não podem ser negativos.
- Transferências exigem saldo suficiente e contas de origem/destino válidas e ativas.
- Uma transferência registra débito, crédito e lançamento financeiro relacionados.
- Uma cobrança possui valor original, vencimento e juros diários.
- A cobrança não deve ser paga mais de uma vez.
- O extrato consulta os lançamentos associados à conta autenticada.
- A remoção de usuário é lógica, preservando conta e histórico.

## 🧪 Testes

A solução contém testes com **xUnit**, **Moq** e **FluentAssertions**. Os testes de serviço utilizam SQLite em memória. Há também um teste de integração com `WebApplicationFactory` e Testcontainers, que utiliza uma instância real de PostgreSQL para verificar o health check e o cadastro.

### Executar os testes

```bash
dotnet test
```

Para executar o teste de integração com PostgreSQL via Testcontainers, mantenha o Docker em execução e habilite explicitamente esse cenário.

PowerShell:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS = "true"
dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"
```

Bash:

```bash
RUN_POSTGRES_INTEGRATION_TESTS=true dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"
```

## ⚠️ Escopo e limitações conhecidas

Este projeto prioriza clareza e fundamentos técnicos para fins educacionais; não é uma plataforma financeira de produção.

- **Revogação de JWT:** tokens emitidos não são invalidados imediatamente quando o usuário é desativado; continuam sujeitos à expiração configurada.
- **Concorrência:** os cenários de concorrência e falha entre etapas ainda precisam ser validados com PostgreSQL real. Os testes com SQLite em memória não substituem esses testes.
- **Testes PostgresSQL:** os testes de uso cobrem casos com SQLite, mas ainda não cobrem casos de integração com PostgreSQL.

## 👨‍💻 Autor

**Gustavo Beirão**  
- GitHub: [@Gustavobvns](https://github.com/Gustavobvns)
- Linkedin: https://linkedin.com/in/gustavo-beirão
- Email: gustavobvns@gmail.com
- Celular: (21) 99476-4457
