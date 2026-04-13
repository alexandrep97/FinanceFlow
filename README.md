# FinanceFlow Desktop

Aplicação desktop de gestão financeira pessoal, construída em .NET 8.0 WinForms com interface web via WebView2.

---

## Funcionalidades

- **Dashboard** — Visão geral do saldo total, receitas vs despesas, gráficos por categoria e evolução mensal
- **Movimentos** — Listagem filtrável de todos os movimentos, com edição e remoção
- **Contas** — Gestão de contas bancárias, poupanças, mealheiros, carteiras cripto e carteiras de ações
- **Importação** — Importa ficheiros CSV/Excel das contas bancárias com mapeamento visual de colunas
- **Exportação** — Exporta movimentos para Excel (.xlsx) com formatação
- **Categorias** — Cria, edita e remove categorias personalizadas (receita/despesa/ambos)
- **Moedas** — Suporte multi-moeda (EUR, USD, GBP, BTC, ETH, USDC e personalizadas)
- **Temas** — Tema claro e escuro

---

## Requisitos

### Para compilar
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (x64)
- Windows 10 versão 1903 ou superior (para WebView2)
- Visual Studio 2022 (recomendado) **ou** linha de comandos

### Para executar
- Windows 10/11 (x64)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) — normalmente já instalado no Windows 11 e em Windows 10 atualizado
- Não é necessário .NET instalado na máquina do utilizador final (publicação self-contained)

---

## Compilação

### Opção 1 — Script automático (recomendado)

```
build.bat
```

Gera o executável em `publish\FinanceFlow.exe` (single-file, self-contained, ~150–200 MB).

### Opção 2 — Visual Studio 2022

1. Abre `FinanceFlow.csproj` no Visual Studio
2. Seleciona configuração **Release | x64**
3. Menu **Build → Publish** → perfil "FolderProfile" (ou cria um novo)
4. Copia a pasta `wwwroot\` para junto do executável publicado

### Opção 3 — Linha de comandos (dotnet CLI)

```bat
:: Build simples (para teste)
dotnet build -c Release

:: Publicação single-file self-contained
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o publish\

:: Copiar recursos web
xcopy /e /i /y wwwroot\ publish\wwwroot\
```

---

## Estrutura do Projeto

```
FinanceFlow/
├── FinanceFlow.csproj          # Definição do projeto (.NET 8.0 WinForms)
├── Program.cs                  # Ponto de entrada, STAThread
├── MainForm.cs                 # Janela principal, inicialização WebView2
├── build.bat                   # Script de build e publicação
├── build-debug.bat             # Execução rápida em modo debug
│
├── Models/
│   └── Models.cs               # Modelos de dados (Currency, Account, Transaction, …)
│
├── Data/
│   └── Database.cs             # Camada de dados SQLite (migrations, CRUD, stats)
│
├── Bridge/
│   └── BridgeHandler.cs        # Bridge C# ↔ JavaScript (WebMessageReceived)
│
├── Properties/
│   └── app.manifest            # Manifesto (DPI awareness, Windows 10/11)
│
└── wwwroot/
    └── index.html              # Interface completa (HTML + CSS + JS, single-file)
```

---

## Arquitetura Bridge C# ↔ JavaScript

A comunicação entre a interface web e o código C# é feita via WebView2:

**JavaScript → C# (ação/pedido):**
```javascript
window.chrome.webview.postMessage(JSON.stringify({
    action: 'getTransactions',
    payload: { accountId: 1, startDate: '2025-01-01' }
}));
```

**C# → JavaScript (resposta):**
```csharp
CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new {
    action = "getTransactions",
    success = true,
    data = transactions
}, camelCaseSettings));
```

**JavaScript — escutar respostas:**
```javascript
window.chrome.webview.addEventListener('message', event => {
    const msg = JSON.parse(event.data);
    if (msg.action === 'getTransactions' && msg.success) {
        renderTransactions(msg.data);
    }
});
```

### Ações disponíveis

| Ação | Payload | Descrição |
|------|---------|-----------|
| `getCurrencies` | — | Lista todas as moedas |
| `createCurrency` | `{name, symbol, code}` | Cria moeda |
| `updateCurrency` | `{id, name, symbol, code}` | Atualiza moeda |
| `deleteCurrency` | `{id}` | Remove moeda |
| `getCategories` | — | Lista categorias |
| `createCategory` | `{name, type, color, icon}` | Cria categoria |
| `updateCategory` | `{id, …}` | Atualiza categoria |
| `deleteCategory` | `{id}` | Remove categoria |
| `getAccounts` | — | Lista contas com saldo calculado |
| `createAccount` | `{name, type, currencyId, initialBalance}` | Cria conta |
| `updateAccount` | `{id, …}` | Atualiza conta |
| `deleteAccount` | `{id}` | Remove conta |
| `getTransactions` | `{accountId?, categoryId?, startDate?, endDate?, search?, page?, pageSize?}` | Lista movimentos (paginado) |
| `createTransaction` | `{accountId, categoryId, amount, date, description, type}` | Cria movimento |
| `updateTransaction` | `{id, …}` | Atualiza movimento |
| `deleteTransaction` | `{id}` | Remove movimento |
| `bulkCreate` | `{transactions: […]}` | Importação em lote |
| `getDashboard` | `{startDate?, endDate?}` | Estatísticas do dashboard |
| `exportExcel` | `{accountId?, startDate?, endDate?}` | Abre SaveFileDialog e exporta |
| `openImportFile` | — | Abre OpenFileDialog e devolve preview |
| `processImport` | `{filePath, mapping, accountId}` | Processa importação |

---

## Base de Dados

**Ficheiro:** `financeflow.db` (criado automaticamente na pasta do executável)

**Tabelas:**
- `Currencies` — Moedas (EUR, USD, GBP, BTC, ETH, USDC por omissão)
- `Categories` — Categorias com tipo (income/expense/both), cor e ícone
- `Accounts` — Contas com tipo, saldo inicial e moeda associada
- `Transactions` — Movimentos com conta, categoria, valor, data e descrição

As migrations são aplicadas automaticamente na primeira execução.

---

## Distribuição

### Opção A — Pasta portátil (sem instalador)

1. Corre `build.bat` para gerar `publish\`
2. Copia toda a pasta `publish\` para o destino:
   - `FinanceFlow.exe` (self-contained, não requer .NET instalado)
   - `wwwroot\` (interface web)
   - `app.ico` (ícone da aplicação)
3. O utilizador precisa do **WebView2 Runtime** (incluído no Windows 11 e Windows 10 atualizado)

### Opção B — Instalador profissional (InnoSetup)

O ficheiro `setup.iss` gera um instalador completo com:
- Wizard de instalação em Português
- Atalho no Menu Iniciar e opcionalmente no Ambiente de Trabalho
- Registo correto no "Adicionar/Remover Programas" com ícone
- Verificação automática e instalação do WebView2 Runtime
- Opção de arranque automático com o Windows
- Desinstalador completo (dados do utilizador são preservados)

**Pré-requisitos:**
1. Instala o [InnoSetup 6](https://jrsoftware.org/isdl.php)
2. Corre `build.bat` (publica a aplicação E compila o instalador automaticamente se o InnoSetup estiver instalado)

**Compilar o instalador manualmente:**
```bat
:: Via linha de comandos
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```
Ou abre `setup.iss` no **Inno Setup Compiler** (GUI) e clica Build → Compile.

O instalador gerado fica em `installer\FinanceFlow_Setup_1.0.0.exe`.

**Personalizar o instalador:**

Edita as primeiras linhas do `setup.iss` para alterar nome, versão, publisher e URL:
```pascal
#define AppName      "FinanceFlow"
#define AppVersion   "1.0.0"
#define AppPublisher "O Teu Nome"
#define AppURL       "https://o-teu-site.pt"
```

> **Nota:** O campo `AppId` em `setup.iss` contém um GUID único para esta aplicação.
> Nunca o alteres entre versões — é o que o Windows usa para reconhecer que é a mesma aplicação e fazer upgrade em vez de instalação paralela.

---

## Resolução de Problemas

| Problema | Solução |
|----------|---------|
| "WebView2 não encontrado" | Instala o [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) |
| Ecrã em branco ao arrancar | Verifica que a pasta `wwwroot\` está junto ao `.exe` |
| Erro de acesso à BD | Garante que a pasta do `.exe` tem permissões de escrita |
| Texto desfocado em ecrã 4K | O manifesto DPI já está configurado — verifica se o Windows tem escalonamento ≤ 150% |
| Dev Tools (F12) | Disponível em modo debug (`AreDevToolsEnabled = true` em `MainForm.cs`) |

---

## Tecnologias

| Componente | Tecnologia |
|------------|-----------|
| Framework | .NET 8.0 WinForms (Windows) |
| Interface | HTML5 + CSS3 + JavaScript (WebView2) |
| Gráficos | Chart.js 4.4.3 |
| Base de dados | SQLite via Microsoft.Data.Sqlite 8.0.0 |
| Exportação Excel | ClosedXML 0.102.2 |
| Importação CSV | CsvHelper 33.0.1 |
| Importação Excel | ExcelDataReader 3.6.0 |
| JSON | Newtonsoft.Json 13.0.3 |
| WebView2 | Microsoft.Web.WebView2 1.0.2849.39 |
