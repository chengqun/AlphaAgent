# AlphaAgent

> Chinese A-share market data platform with social communication features and AI Agent system

[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)

AlphaAgent combines real-time stock data acquisition, technical analysis, and an AI Agent framework with a social platform for managing financial relationships. It provides failover-capable quote fetching from multiple Chinese financial data sources, technical indicator calculation, an AI Agent system with streaming tool-use capabilities for stock analysis, a unified relationship system for friends, devices, groups, and stock watchlists, and a multi-type social feed (Moments) supporting user, stock, device, and group posts.

The system is built as three cooperating subsystems: a standalone core library (AlphaAgent.Core) for market data, business logic, and AI agents following Clean Architecture principles, an ABP Framework backend providing social features via auto-generated REST APIs, and a cross-platform .NET MAUI client that connects the two.

## Table of Contents

- [Background](#background)
- [Key Features](#key-features)
- [Installation](#installation)
- [Usage](#usage)
- [Development](#development)
- [Architecture](#architecture)
- [Contributing](#contributing)
- [Maintainers](#maintainers)
- [License](#license)
- [Documentation](#documentation)

## Background

AlphaAgent serves Chinese A-share market participants who need both real-time market data and social coordination. The platform addresses three problems: reliable access to market quotes (single-source APIs are fragile), organizing investment-related social connections (friends, stock watchlists, discussion groups, device integrations) in one place, and providing intelligent stock analysis through an AI Agent system with streaming tool-use capabilities.

## Key Features

- **AI Agent System** — `IAgent`/`IAgentFactory` abstraction with `LlmAgent` wrapping `Microsoft.Agents.AI.ChatClientAgent`. Three memory modes: `Stateful` (full history), `SlidingWindow` (last N, default), `Stateless` (no LLM history). Streaming via `IAsyncEnumerable<AgentResponseChunk>` with interleaved content tracking (`ContentPart`). Context-based session isolation enables per-stock Agent sessions. `StockAnalystAgent` and `StockAnalystNoMemoryAgent` factory classes with `AIFunctionFactory.Create()` tools.
- **Failover Quote Provider** — Randomizes and iterates through Sina, Baidu, and EastMoney APIs, falling back automatically on failure. Distributes load across providers via Fisher-Yates shuffle.
- **8 Technical Indicators** — SMA, EMA, RSI, MACD, Bollinger Bands, SAR, KDJ, ADX with configurable parameters. Outputs CSV with OHLCV data for charting.
- **Unified Relationship System** — Single `AppRelationship` entity with four polymorphic managers: bidirectional friendships, owner-authorized device/group memberships, and auto-accepted stock watchlists.
- **Real-Time Chat** — SignalR-based messaging supporting user-to-user, user-to-device, and group conversations. REST API for history/CRUD, SignalR for real-time delivery. Dual authentication: JWT for users, authorization codes for devices.
- **Cache-First Loading** — MAUI ViewModels load from local SQLite cache first for instant display, then sync from server in the background with 30-second throttle. `IConversationSyncService` and `IContactSyncService` manage local caches. Incremental updates prevent UI flicker.
- **Hybrid Message Cache** — Memory + SQLite caching for chat messages with configurable expiration (5 minutes) and message limit (50 per conversation).
- **Moments Social Feed** — Multi-type social feed supporting user posts, stock-related moments, device moments, and group moments. Deterministic GUID conversion packs non-Guid IDs into the `UserId` Guid column.
- **Device Management** — Device registration with auto-generated authorization codes. Devices connect via SignalR using auth codes.
- **Group Management** — First-class entity with admin-only creation, auto-join for owners, member management, and group disbanding.
- **Video Feed** — Video browsing with vertical swiping, paginated loading, and deduplication. Uses `CommunityToolkit.Maui.MediaElement` for playback.
- **Auto Update** — MAUI client checks for updates on startup via server API. New versions are registered automatically during CI/CD build. APK deployed to IIS for public download. Supports force-update mode.
- **CI/CD** — GitHub Actions workflows for automatic deployment: Web backend to IIS via msdeploy with AppOffline rule, MAUI APK build + deploy + version registration. Secrets managed via GitHub Secrets with placeholder replacement pattern.
- **Console Device Client** — Standalone .NET console app connecting via SignalR using device authorization codes. Also Agent-enabled for LLM-powered command processing.
- **Clean Architecture** — AlphaAgent.Core follows Clean Architecture with Domain.Abstractions, Domain, Application, and Infrastructure layers. UI layer (MAUI) only references Application layer.

## Installation

### Prerequisites

- .NET 10 SDK
- SQL Server (for the Web backend)

### Setup Steps

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AlphaAgent
   ```

2. **Restore and build**
   ```bash
   dotnet build AlphaAgent.sln
   ```

3. **Configure the database**

   For local development, create `appsettings.Development.json` alongside `appsettings.json` with real values (gitignored):
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=<your-server>;Database=AlphaAgent;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
     }
   }
   ```
   For production deployment, `appsettings.json` uses `${PLACEHOLDER}` tokens replaced by GitHub Actions secrets at deploy time.

4. **Run database migrations**
   ```bash
   dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator
   ```
   This creates the database schema and seeds initial data including the OpenIddict clients. The HttpApi.Host also auto-migrates on startup, so running the migrator separately is optional during development.

## Usage

### Starting the Application

**Web backend (HTTP API Host + SignalR):**
```bash
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host
```
Access Swagger UI at `https://localhost:44319/swagger`

**MAUI desktop/mobile client:**
```bash
dotnet run --project src/AlphaAgent.Maui
```
The client authenticates against the backend using OAuth2 password flow (client: `alphaagent_chat`).

**Console device client:**
```bash
dotnet run --project src/AlphaAgent.ConsoleDevice
```
Connects via SignalR using device authorization code configured in `appsettings.json`. Also supports Agent-enabled command processing via the configured LLM endpoint.

### Basic Workflows

- **AI Agent chat**: Start a session with StockAnalyst → send Chinese stock queries like "分析浦发银行的技术指标" → agent streams results via `LlmAgent` → invokes technical analysis tools → returns interleaved text and tool results
- **Stock watchlist**: Add securities to your watchlist — relationships are auto-accepted (no approval needed)
- **Friend requests**: Send friendship requests — the target user must accept, which creates a bidirectional relationship
- **Real-time chat**: Open conversations with friends, devices, or groups — messages delivered via SignalR in real-time. Message history cached locally in SQLite for faster access.
- **Group membership**: Request to join groups — the group owner approves
- **Device registration**: Register devices with auto-generated authorization codes — devices connect via SignalR
- **Moments feed**: View friend moments and stock moments — create user, stock, device, or group moments
- **Technical analysis**: Search for a security and calculate indicators with configurable parameters (e.g., `SMA(20),RSI(14),MACD`)

## Development

### Project Structure

```
src/
├── AlphaAgent.Core/                        # Core library with Clean Architecture layers
│   ├── AlphaAgent.Domain.Abstractions/     # Pure abstractions (IAgent, IAgentFactory, infrastructure interfaces)
│   ├── AlphaAgent.Domain/                  # Domain entities, services, repository interfaces
│   ├── AlphaAgent.Application/             # Application services, DTOs, sync services
│   └── AlphaAgent.Infrastructure/          # SQLite, HTTP, Quotes, LlmAgent, AgentFactory
├── AlphaAgent.Web/                         # ABP Framework backend (layered DDD)
│   ├── AlphaAgent.Abp.Domain.Shared/       # Enums, error codes, multi-tenancy consts
│   ├── AlphaAgent.Abp.Domain/              # Entities, domain services
│   ├── AlphaAgent.Abp.Application.Contracts/ # DTOs, app service interfaces, permissions
│   ├── AlphaAgent.Abp.Application/         # App service implementations, Mapperly mappers
│   ├── AlphaAgent.Abp.HttpApi/             # Auto API controllers + ChatHub (SignalR)
│   ├── AlphaAgent.Abp.HttpApi.Host/        # API host entry point, Swagger, OpenIddict
│   ├── AlphaAgent.Abp.HttpApi.Client/      # HTTP client module
│   ├── AlphaAgent.Abp.EntityFrameworkCore/ # EF Core DbContext, migrations (SQL Server)
│   └── AlphaAgent.Abp.DbMigrator/          # Console tool for database migrations
├── AlphaAgent.Maui/                        # Cross-platform MAUI client
│   ├── ViewModels/                         # CommunityToolkit.Mvvm ViewModels with IPageLifecycleAware
│   ├── Views/                              # XAML pages
│   ├── Services/                           # EventBusService, GlobalMessageHandler, CustomCertificateHandler, AppSettings
│   └── Events/                             # NewMessageEvent, NewConversationEvent, etc.
├── AlphaAgent.ConsoleDevice/               # Console device client (SignalR + Agent-enabled)
└── common.props                            # Shared build properties (LangVersion=latest, CS1591 suppressed)
```

### AlphaAgent.Core Clean Architecture Layers

| Layer | Responsibility | Dependencies |
|-------|----------------|-------------|
| **Domain.Abstractions** | Pure abstractions: IAgent, IAgentFactory, Agent models, infrastructure interfaces | None |
| **Domain** | Entities, domain services, repository interfaces | Domain.Abstractions |
| **Application** | Use cases, DTOs, service interfaces and implementations (including sync services) | Domain, Domain.Abstractions |
| **Infrastructure** | Database, external services, LlmAgent, AgentFactory, quote providers | Domain, Domain.Abstractions |

### Key Development Commands

```bash
# Build
dotnet build AlphaAgent.sln

# Run backend (API + SignalR + Swagger)
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Add EF Core migration
dotnet ef migrations add <Name> --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Apply migrations
dotnet ef database update --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Run database migrator
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator

# Run console device client
dotnet run --project src/AlphaAgent.ConsoleDevice

# Clear NuGet cache (if ABP packages changed after pull)
dotnet nuget locals all --clear
```

### Conventions

- Code in English; user-facing strings and comments in Chinese (中文)
- Custom entities and tables prefixed with `App` (e.g., `AppSecurity`, `AppSecurities`) to avoid ABP conflicts
- Nullable reference types enabled across all projects
- Object mapping uses ABP's `MapperBase<TSource, TDestination>` with hand-written mappers (not Riok.Mapperly source generators)
- Business exceptions use `BusinessException` with codes like `"AlphaAgent:UserNotFound"`
- Clean Architecture: UI layer only references Application layer
- ViewModels implement `IPageLifecycleAware` for event management during navigation
- DTOs and interfaces organized by domain subdirectory (e.g., `Dtos/Agent/`, `Interfaces/Chat/`)

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for comprehensive development workflows and [CLAUDE.md](CLAUDE.md) for critical runtime conventions.

## Architecture

The system follows Clean Architecture principles with three cooperating subsystems:

```
┌─────────────────────┐  OAuth2 + REST + SignalR  ┌──────────────────────────┐
│  AlphaAgent.Maui     │ ──────────────────────── │    AlphaAgent.Web         │
│  (Mobile/Desktop)    │                           │    (ABP + API + SignalR)  │
│                      │                           │    SQL Server             │
│  AlphaAgent.Core     │                           └──────────────────────────┘
│  (Application/Domain)│
│  (SQLite cache)      │
│  (AI Agent system)   │
├─────────────────────┤
│  ConsoleDevice       │ ──── SignalR (auth code + Agent) ──►
└─────────────────────┘
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed system design, data flow diagrams, and technology decisions.

## Contributing

1. Check existing issues or create a new one describing the change
2. Create a feature branch from `main`
3. Make changes with clear commit messages
4. Ensure the solution builds: `dotnet build AlphaAgent.sln`
5. Submit a pull request with a clear description

### Code Quality

- Follow existing naming conventions (`App` prefix for custom entities)
- Use ABP Mapperly for object mapping (`MapperBase<TSource, TDestination>` with hand-written mappers)
- Register domain services as `Transient` in module `ConfigureServices`
- Generic interface implementations (like `IRelationshipManager<,,>`) must be registered individually by their specific generic type parameters
- New agents are added by creating a static factory class and registering in `RegisterAgentServices()`, not by implementing `IAgent` directly
- Adhere to Clean Architecture: UI layer should not directly reference Infrastructure

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for detailed development workflows.

## Maintainers

[@chengqun](https://github.com/chengqun)

## License

Proprietary. All rights reserved.

## Documentation

- **[README.md](README.md)** — Project overview and getting started (you are here)
- **[CLAUDE.md](CLAUDE.md)** — Development commands, architecture, and conventions
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** — System design and technical decisions
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** — Detailed development workflows
