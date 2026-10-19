# 💳 Payment Gateway API

API REST de carteira digital desenvolvida em .NET para demonstrar autenticação, transferências P2P, cobrança, pagamento com juros por atraso e histórico financeiro, com foco em boas práticas de backend, testes, consistência transacional e execução em Docker.

---

## 📌 Visão Geral
Este projeto é uma API educacional que simula o core de uma carteira digital. Não integra provedores de pagamento nem processa transações reais de cartão ou Pix.

- Cadastro e autenticação de usuários
- Conta digital vinculada ao usuário
- Transferência P2P entre contas
- Geração de código de cobrança
- Consulta de cobrança com atualização dinâmica por juros
- Pagamento de cobrança
- Histórico (extrato) de transações

---

## 🧱 Stack Tecnológica

- **.NET 10 (C#)**
- **ASP.NET Core Minimal APIs**
- **Entity Framework Core**
- **xUnit**
- **Mocks**
- **FluentAssertions**
- **PostgreSQL**
- **JWT (JSON Web Token)**
- **Docker / Docker Compose**
- Arquitetura **Vertical Slice**

---

## ✅ Funcionalidades

### 1) Auth (Identidade e Segurança)
- Cadastro de usuário com:
  - dados pessoais
  - criação automática de conta digital com saldo inicial zero
  - armazenamento seguro da senha via hash
- Login com geração de token JWT
- Proteção de endpoints financeiros com autenticação
- CRUD do perfil do usuário autenticado:
  - consulta dos dados e saldo
  - atualização de nome, documento e e-mail
  - alteração de senha
  - desativação lógica da conta

### 2) Transfers (Transferências Diretas)
- Transferência P2P entre contas
- Validações:
  - valor > 0
  - conta de destino existente
  - saldo suficiente na conta de origem
- Operação transacional (ACID)
- Registro no Ledger

### 3) Payments (Cobranças)
- Geração de código de cobrança com:
  - conta recebedora
  - valor original
  - data de vencimento
  - juros diário
- Consulta de cobrança:
  - valida existência
  - valida status de pagamento
  - calcula valor atualizado em caso de atraso
- Pagamento de cobrança usando o valor atualizado da consulta

### 4) Ledger (Extrato)
- Consulta do histórico completo e imutável de transações da conta autenticada

---

## 🗄️ Modelo de Dados

```text
Usuarios:
- Id (GUID v7)
- Nome
- Documento (Unique)
- Email (Unique)
- SenhaHash
- ativo

Contas:
- Id (GUID v7)
- UsuarioId (FK)
- Saldo (Numeric)

CodigosPagamento:
- Id (GUID v7)
- CodigoPagamentoHash (Unique)
- ContaGeradoraId (FK)
- ValorOriginal
- DataVencimento
- JurosDiario
- Status

Transacoes:
- Id (GUID v7)
- ContaOrigemId (FK)
- ContaDestinoId (FK)
- Valor
- CodigoPagamentoId (FK, Nullable)
- DataTransacao
```

---

## 🧭 Arquitetura e Organização (Vertical Slice)

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

---

## 🚀 Como executar localmente

### Pré-requisitos
- Docker Desktop instalado e em execução

### 1) Clonar o repositório
```bash
git clone https://github.com/Gustavobvns/payment-gateway-API.git
cd payment-gateway-API
```

### 2) Subir infraestrutura
```bash
docker compose up -d --build
```

### 3) Acessar Swagger
- **http://localhost:8080/swagger**

O `docker-compose.yml` publica a API na porta `8080`. Ao executar a aplicação
diretamente com o perfil de desenvolvimento (`dotnet run`), a porta HTTP
configurada em `Properties/launchSettings.json` é `5201`; essa é a porta usada
pelos arquivos `payment-gateway-API.http` e `financial-api.http`.

O Swagger possui autenticação Bearer configurada. Use o botão **Authorize** e informe:

```http
Bearer SEU_TOKEN_JWT
```

---

## 🔐 Autenticação

Os tokens JWT são stateless e permanecem válidos até sua data de expiração.
A desativação lógica de um usuário não invalida imediatamente tokens emitidos
anteriormente, pois a API não mantém uma lista de revogação nem consulta o
status do usuário a cada requisição autenticada. Um novo login deve ser
impedido para usuários desativados, e o token deixa de ser aceito
automaticamente quando expirar.

Após login, utilize o token JWT no cabeçalho:

```http
Authorization: Bearer SEU_TOKEN_JWT
```

---

## 📡 Endpoints principais

| Feature   | Método | Rota                       | Descrição |
|-----------|--------|----------------------------|-----------|
| Auth      | POST   | `/auth/register`           | Cadastro de usuário e abertura automática de conta |
| Auth      | POST   | `/auth/login`              | Login e geração de JWT |
| Users     | GET    | `/users/me`                | Consulta do perfil e saldo da conta autenticada |
| Users     | PUT    | `/users/me`                | Atualização do perfil do usuário autenticado |
| Users     | PATCH  | `/users/change-password`   | Alteração de senha do usuário logado |
| Users     | DELETE | `/users/me`                | Desativação lógica do usuário autenticado |
| Transfers | POST   | `/transfers/{destinationAccountId}` | Transferência P2P |
| Payments  | POST   | `/payments`                | Geração de cobrança |
| Payments  | GET    | `/payments/{codigo}`       | Consulta de cobrança |
| Payments  | POST   | `/payments/{codigo}/pay`   | Pagamento de cobrança |
| Ledger    | GET    | `/ledger`                  | Extrato completo da conta autenticada |
| Health    | GET    | `/health`                  | Verificação de disponibilidade do PostgreSQL |

As operações `POST /transfers/{destinationAccountId}`, `POST /payments` e
`POST /payments/{codigo}/pay` exigem o header:

```http
Idempotency-Key: uma-chave-unica-da-operacao
```

Uma repetição da mesma chave para o mesmo usuário e operação reutiliza o
resultado persistido, evitando duplicidade em retries.

### Exemplos de requisições e respostas

Os exemplos abaixo usam `http://localhost:8080`, quando a API foi iniciada
com Docker Compose. Para execução local sem Docker, substitua a base por
`http://localhost:5201`, como nos arquivos `.http` do repositório.

#### 1. Cadastro

```http
POST /auth/register
Content-Type: application/json

{
  "nome": "Gustavo",
  "documento": "12345678900",
  "email": "gustavo@example.com",
  "senha": "Senha-Forte-123"
}
```

Resposta `201 Created`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-10-19T17:02:57Z"
}
```

#### 2. Login e obtenção do token

```http
POST /auth/login
Content-Type: application/json

{
  "email": "gustavo@example.com",
  "senha": "Senha-Forte-123"
}
```

Resposta `200 OK`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-10-19T17:02:57Z"
}
```

Use o valor de `accessToken` nas requisições protegidas:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### 3. Transferência entre duas contas

Considere que `destinationAccountId` é o GUID da conta de outro usuário e que
a conta de origem possui saldo suficiente.

```http
POST /transfers/{destinationAccountId}
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Idempotency-Key: transferencia-exemplo-001
Content-Type: application/json

{
  "valor": 25.00
}
```

Resposta `201 Created`:

```json
{
  "id": "0199f000-0000-7000-8000-000000000001",
  "contaOrigemId": "0199f000-0000-7000-8000-000000000002",
  "contaDestinoId": "0199f000-0000-7000-8000-000000000003",
  "valor": 25.00,
  "dataTransacao": "2026-10-19T16:05:00+00:00"
}
```

#### 4. Criação e pagamento de uma cobrança

Primeiro, o usuário recebedor cria a cobrança:

```http
POST /payments
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Idempotency-Key: cobranca-exemplo-001
Content-Type: application/json

{
  "contaRecebedoraId": "0199f000-0000-7000-8000-000000000003",
  "valorOriginal": 50.00,
  "dataVencimento": "2026-12-31",
  "jurosDiario": 1.50
}
```

Resposta `201 Created`:

```json
{
  "codigo": "a1b2c3d4e5f6...",
  "contaRecebedoraId": "0199f000-0000-7000-8000-000000000003",
  "valorOriginal": 50.00,
  "valorAtualizado": 50.00,
  "dataVencimento": "2026-12-31",
  "jurosDiario": 1.50,
  "paga": false
}
```

Depois, o pagador usa o código retornado:

```http
POST /payments/{codigo}/pay
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Idempotency-Key: pagamento-exemplo-001
```

Resposta `200 OK`:

```json
{
  "codigo": "a1b2c3d4e5f6...",
  "valorPago": 50.00,
  "transacaoId": "0199f000-0000-7000-8000-000000000004",
  "dataPagamento": "2026-10-19T16:06:00+00:00"
}
```

---

## 🧪 Cenário de validação manual (E2E)

1. Cadastrar dois usuários  
2. Autenticar com o usuário A  
3. Realizar transferência para conta do usuário B  
4. Gerar código de cobrança para conta B  
5. Consultar código antes/depois do vencimento  
6. Pagar cobrança com usuário A  
7. Validar extrato dos dois usuários

---

## 🛡️ Regras de negócio implementadas

- Transferência só ocorre com saldo suficiente
- Valor de transação deve ser maior que zero
- Contas devem existir e estar ativas para operar
- Débito e crédito ocorrem na mesma transação de banco (ACID)
- Cobrança paga não pode ser liquidada novamente
- Juros de atraso são aplicados conforme regra de `JurosDiario`
- A exclusão de usuário é lógica: o usuário é desativado para preservar a conta e o histórico financeiro

---

## 🧪 Testes e Qualidade de Código

A suíte de testes foi projetada com foco na integridade de dados e na validação das regras de negócio financeiras mais críticas (como transações ACID e cálculos de juros).

### 🛠️ Stack de Testes
* **Framework Principal:** `xUnit`
* **Mocking & Mocks:** `Moq` (para isolamento de dependências e regras de serviço)
* **Asserções:** `FluentAssertions` (para leitura declarativa e expressiva das validações)
* **Persistência de teste:** SQLite em memória para isolar serviços e regras

### 🎯 Matriz de principais Cobertura e Cenários Críticos

| Nível | Componente / Feature | Cenários Validados |
| :--- | :--- | :--- |
| **Serviço** | `PaymentService` | Pagamento, rejeição de cobrança já paga, repetição idempotente, reutilização inconsistente de chave e congelamento do valor após a liquidação. |
| **Serviço** | `TransferService` | Débito/crédito atômico, saldo insuficiente sem alteração parcial, repetição idempotente, reutilização inconsistente de chave e registro no ledger. |
| **Serviço** | `LedgerService` | Consulta do histórico e distinção entre entradas e saídas. |
| **Autenticação** | `LoginService` e `PasswordHasher` | Login válido, senha inválida sem diferenciar e-mail inexistente, hash de senha e bloqueio de usuário inativo. |
| **JWT** | `JwtTokenGenerator` | Claims, emissor, audiência, expiração e rejeição de tokens expirados ou com assinatura inválida. |

Os testes de concorrência entre requisições e a confirmação da atomicidade
entre a operação financeira e o registro de idempotência dependem de
PostgreSQL real. Atualmente, o teste de integração versionado cobre saúde e
cadastro usando Testcontainers; os cenários de concorrência devem ser
executados nesse mesmo ambiente antes de serem tratados como garantia da
suíte SQLite em memória.

---

### 🚀 Como Executar os Testes

Executar toda a suíte de testes unitários localmente:

```bash
# Executar todos os testes da solução
dotnet test

# Executar exibindo o detalhamento de cada cenário testado
dotnet test --logger "console;verbosity=detailed"
```

Os testes HTTP com PostgreSQL usam Testcontainers. Para executá-los, inicie o
Docker e defina `RUN_POSTGRES_INTEGRATION_TESTS=true` antes de executar:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS = "true"
dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"
```

---

### Regras de entrada

- Nome, documento e email respeitam os limites definidos no modelo de dados
- Email é validado antes do cadastro ou atualização
- Senhas possuem entre 8 e 100 caracteres
- Valores financeiros e juros aceitam no máximo duas casas decimais

---

## 👨‍💻 Autor

**Gustavo**  
GitHub: [@Gustavobvns](https://github.com/Gustavobvns)