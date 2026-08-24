# 💳 Payment Gateway API

API REST de carteira digital desenvolvida em .NET para demonstrar autenticação, transferências P2P, cobrança, pagamento com juros por atraso e histórico financeiro, com foco em boas práticas de backend, testes, consistência transacional e execução em Docker.

---

# 📍 Status do Projeto e Estratégia de Desenvolvimento

> **Status Atual:** 🛠️ Em Desenvolvimento (Fase de Implementação das Slices)

A evolução do projeto segue a arquitetura **Vertical Slice**, onde cada funcionalidade é construída de forma isolada, contendo seus próprios contratos (DTOs), regras de negócio (Services), rotas (Endpoints) e testes.

---

## 📌 Visão Geral

Este projeto representa o *core* de movimentações financeiras de uma carteira digital, incluindo:

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
- CRUD de todos os dados de usuario autenticado 

### 2) Transfers (Transferências Diretas)
- Transferência P2P entre contas
- Validações:
  - valor > 0
  - conta de destino existente
  - saldo suficiente na conta de origem
- Operação transacional (ACID)

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
- Consulta do histórico imutável de transações da conta autenticada

---

## 🗄️ Modelo de Dados

```text
Usuarios:
- Id (UUID)
- Nome
- Documento (Unique)
- Email (Unique)
- SenhaHash
- ativo

Contas:
- Id (UUID)
- UsuarioId (FK)
- Saldo (Numeric)

CodigosPagamento:
- Id (UUID)
- CodigoPagamentoHash (Unique)
- ContaGeradoraId (FK)
- ValorOriginal
- DataVencimento
- JurosDiario
- Status

Transacoes:
- Id (UUID)
- ContaOrigemId (FK)
- ContaDestinoId (FK)
- Valor
- CodigoPagamentoId (FK, Nullable)
- DataHora
```

---

## 🧭 Arquitetura e Organização (Vertical Slice)

```text
payment-gateway-API/
└── FinPay.API/
    ├── Data/
    ├── Models/
    │
    ├── Features/
    │   ├── Auth/
    │   ├── Transfers/
    │   ├── Payments/
    │   └── Ledger/
    │
    ├── Program.cs
    ├── appsettings.json
    ├── Dockerfile
    └── docker-compose.yml
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

---

## 🔐 Autenticação

Após login, utilize o token JWT no cabeçalho:

```http
Authorization: Bearer {seu_token}
```

---

## 📡 Endpoints principais

| Feature   | Método | Rota                    | Descrição |
|-----------|--------|-------------------------|-----------|
| Auth      | POST   | `/auth/register`        | Cadastro de usuário |
| Auth      | POST   | `/auth/login`           | Login e geração de JWT |
| Transfers | POST   | `/transfers`            | Transferência P2P |
| Payments  | POST   | `/payments`             | Geração de cobrança |
| Payments  | GET    | `/payments/{codigo}`    | Consulta de cobrança |
| Payments  | POST   | `/payments/{codigo}/pay`| Pagamento de cobrança |
| Ledger    | GET    | `/ledger`               | Extrato da conta autenticada |

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

---

## 🧪 Testes e Qualidade de Código

A suíte de testes foi projetada com foco na integridade de dados e na validação das regras de negócio financeiras mais críticas (como transações ACID e cálculos de juros).

### 🛠️ Stack de Testes
* **Framework Principal:** `xUnit`
* **Mocking & Mocks:** `Moq` (para isolamento de dependências e regras de serviço)
* **Asserções:** `FluentAssertions` (para leitura declarativa e expressiva das validações)
* **Testes de Integração:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)

### 🎯 Matriz de principais Cobertura e Cenários Críticos

| Nível | Componente / Feature | Cenários Validados |
| :--- | :--- | :--- |
| **Unitário** | `PaymentService` | Cálculo exato de juros diários por atraso, pagamento sem acréscimo dentro do prazo e rejeição de cobranças já pagas. |
| **Unitário** | `TransferService` | Débito/crédito simultâneo em transferência P2P, falha imediata por saldo insuficiente e *Rollback* em exceções. |
| **Unitário** | `AuthService` | Verificação de hash de senha (`BCrypt`), geração do JWT Token com *claims* e expiração. |
| **Integração** | Endpoints API | Fluxo E2E: Cadastro → Login (Extração do Bearer Token) → Transferência P2P Autenticada → Atualização do Saldo. |

---

### 🚀 Como Executar os Testes

Executar toda a suíte de testes unitários e de integração localmente:

```bash
# Executar todos os testes da solução
dotnet test

# Executar exibindo o detalhamento de cada cenário testado
dotnet test --logger "console;verbosity=detailed"
```

---

## ⚠️ Limitações atuais (projeto em evolução)

- Projeto em fase de concepção e desenvolvimento
- Endpoints e contratos podem sofrer ajustes durante evolução do domínio
- Foco atual em robustez de regras financeiras e qualidade de código

---

## 👨‍💻 Autor

**Gustavo**  
GitHub: [@Gustavobvns](https://github.com/Gustavobvns)