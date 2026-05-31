# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Documentation Structure

- **README.md** — Project overview and getting started
- **AGENTS.md** — Development commands, architecture, and conventions (you are here)
- **docs/ARCHITECTURE.md** — Detailed system design, data flow diagrams, and technology decisions
- **docs/DEVELOPMENT.md** — Step-by-step workflows, environment config, and troubleshooting

## Build & Run Commands

```bash
# Build the entire solution
dotnet build AlphaAgent.sln

# Run the web API host (API + SignalR + Swagger)
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Run the database migrator (HttpApi.Host also auto-migrates on startup)
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator

# Add an EF Core migration (ABP Web, SQL Server)
dotnet ef migrations add <MigrationName> --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Apply EF Core migrations
dotnet ef database update --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Run the console device client (SignalR device auth + Agent-enabled)
dotnet run --project src/AlphaAgent.ConsoleDevice

# Clear NuGet cache (if ABP packages changed after pull)
dotnet nuget locals all --clear
```

**Note**: There are no test projects in this repository. No `dotnet test` commands are available.

## Architecture

AlphaAgent has three subsystems with distinct tech stacks:

1. **AlphaAgent.Core** — Clean Architecture library (no ABP dependency): Chinese A-share market data, quote failover, technical indicators, AI Agent system, local SQLite cache. 4 layers: Domain.Abstractions → Domain → Application → Infrastructure.
2. **AlphaAgent.Web** — ABP Framework 10.3 DDD web app: social features (friends, devices, groups, moments, stock relationships), real-time chat via SignalR, agent config management, security sync, SQL Server via EF Core. 8 sub-projects following ABP DDD layering.
3. **AlphaAgent.Maui** — .NET MAUI cross-platform client: 4-tab Shell (Chat, Contacts, Discovery, Me), connects to Web backend through Core's Application services + SignalR. Cache-first loading with local SQLite. Theme support (light/dark/system).

### Critical Dependency Rules

- **Domain.Abstractions → nothing** (root of dependency graph)
- **Domain → Domain.Abstractions only**
- **Application → Domain + Domain.Abstractions only**
- **Infrastructure → Domain + Domain.Abstractions only** (never Application)
- **MAUI → Application only** (never Domain, Domain.Abstractions, or Infrastructure)

Infrastructure interfaces that Infrastructure implements (e.g., `ISignalRChatService`, `IHttpClientService`) must live in **Domain.Abstractions** — not Application — so Infrastructure can implement them without creating a circular dependency.

### Core DI Registration Pattern

Each Core sub-library uses extension methods on `IServiceCollection`:
- **Application**: `AddApplicationServices()` in `AlphaAgent.Application/Extensions/ServiceCollectionExtensions.cs` — registers domain services and application services as Scoped
- **Infrastructure**: `AddInfrastructureServices(connectionString, baseAddress, agentOptions, httpMessageHandlerFactory?)` in `AlphaAgent.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — registers SQLite DbContext, repositories (Scoped), quote providers (Singleton), agent services (IChatClient Singleton, AgentFactory Singleton with named registrations), SignalRChatService (Singleton with optional HttpMessageHandler for custom certificate validation)

MAUI wires them in `MauiProgram.cs`:
```csharp
CustomCertificateHandler.Initialize(); // Load embedded rootCA.crt for self-signed server
builder.Services.AddInfrastructureServices(sqliteConnectionString, baseAddress, agentOptions, CustomCertificateHandler.CreateHandler);
builder.Services.AddApplicationServices();
```

ABP Web registers domain services as **Transient** in `AbpDomainModule.ConfigureServices()`. Generic implementations like `IRelationshipManager<,,>` must be registered individually by specific type parameters.

### Cache-First Loading Pattern

MAUI ViewModels use a consistent pattern for fast UI rendering:
1. `OnAppearingAsync()` loads from local SQLite cache first (instant display)
2. `SyncInBackgroundAsync()` syncs from server with 30-second throttle (`_minSyncInterval`)
3. `SortConversations()` / `UpdateContactBookIfNeeded()` compare ID sets/order before updating `ObservableCollection` — prevents UI flicker from full collection rebuilds
4. Subscribe to events (`NewConversationEvent`, `ContactChangedEvent`) for real-time updates

Sync services (`IConversationSyncService`, `IContactSyncService`, `AgentConfigService`) manage local SQLite caches that mirror server data. `ConversationSyncService` filters out Agent conversations (Type 3/4) and deletes stale entries. `ContactSyncService` and `AgentConfigService` use full-replace strategy (delete all then upsert).

**SQLite**: App is always reinstalled (no in-place upgrade), so `DatabaseInitializer` uses `EnsureCreatedAsync` only — no migration logic needed.

### Agent System

The Agent system spans all four Core layers, using `Microsoft.Extensions.AI` + `Microsoft.Agents.AI`:

- **Domain.Abstractions**: `IAgent`, `IAgentFactory`, `AgentMemoryMode` (Stateful/SlidingWindow/Stateless), agent models, `AgentOptions` (LLM config)
- **Domain**: `AgentSession` (with `Context` field for per-stock isolation), `AgentMessage`, `IAgentRepository`
- **Application**: `IAgentService` orchestrates sessions — `StartSessionAsync(agentName, initialContext?)`, `SendMessageAsync`, `SendMessageStreamingAsync`. `AgentStreamEvent` hierarchy for typed stream discrimination. `ContentPart` for interleaved content order.
- **Infrastructure**: `LlmAgent` wraps `ChatClientAgent`, `AgentFactory` with named registrations, `StockAnalystAgent`/`StockAnalystNoMemoryAgent` static factory classes, `TechnicalAnalysisTool` + `SecurityQueryTool` via `AIFunctionFactory.Create()`. `AgentConfigCacheRepository` for local SQLite caching of agent config data. `BearerTokenDelegatingHandler` auto-injects JWT and refreshes on 401.

**AgentConfig Cache-First** — `AgentConfigService` uses local SQLite cache (`AgentConfigCacheItem`) via `IAgentConfigCacheRepository`:
- Server fetch → full-replace cache (delete all by userId, then upsert range)
- Server failure → fall back to local cache
- `GetCachedConfigsAsync(userId)` reads directly from local cache

**Agent Tool Selection** — `AgentOptions.EnabledTools` (Dictionary<string, List<string>>) controls which tools each agent uses:
- `null` or key not present → load all tools (default)
- Empty list → load no tools
- Non-empty list → load only named tools
- `AgentContactDetailViewModel` provides Switch toggles per tool; changes save immediately to both `AgentOptions` in-memory and local SQLite cache (`AgentConfigCacheItem.EnabledTools`). Server does not store `EnabledTools` — it is local-only.

**Agent Memory Modes** — all modes persist messages to SQLite for display; `BuildChatHistory` controls what's sent to the LLM:
- `Stateful`: all history sent to LLM
- `SlidingWindow` (default): last N messages sent (default 20)
- `Stateless`: no history sent (each call independent)

**Agent Session Isolation**:
- `GetActiveSessionAsync(userId, agentName)` — excludes sessions with non-empty `Context`
- `GetActiveSessionByContextAsync(userId, agentName, context)` — for per-stock sessions where Context = `"stock:{stockId}:{stockName}"`
- `StartSessionAsync` accepts optional `initialContext`

**New agent registration**: (1) create static factory class in Infrastructure/Services/AiAgent/Agents/, (2) register in `RegisterAgentServices()` in ServiceCollectionExtensions, (3) add tool class to DI if needed. No Application layer changes needed.

**Agent Tools** — `ToolNames` static class defines tool name constants. Currently registered tools: `TechnicalAnalysisTool` (`CalculateIndicators`), `SecurityQueryTool` (`QuerySecurity`).

### Post-Login Initialization

`IPostLoginInitializer`/`PostLoginInitializer` orchestrates the post-login flow with 3 steps tracked via `IProgress<PostLoginProgress>`:
1. Connect SignalR (`ISignalRChatService.ConnectAsync`)
2. Load Agent config (`IAgentConfigService.SyncFromServerAsync` + `EnsureDefaultConfigsAsync`)
3. Sync securities (`ISecurityClientSyncService.SyncFromServerAsync`)

MAUI `InitializingViewModel` displays step-by-step progress (spinner/checkmark/X).

### Security Client Sync

`ISecurityClientSyncService`/`SecurityClientSyncService` provides incremental sync of security data from server to local SQLite:
- Uses `ISyncMetadataStore` (keyed by sync type, e.g., `"SecurityLastSyncTime"`) to track last sync time
- `SyncFromServerAsync()` calls `api/app/security-client-sync/updates?after={lastSyncTime}` for incremental updates
- Falls back to full sync if no local data exists

### BearerTokenDelegatingHandler

`BearerTokenDelegatingHandler` is a `DelegatingHandler` that:
- Injects Bearer token from `ITokenManager` into all outgoing requests
- On 401 response, attempts token refresh via `ITokenManager.TryRefreshTokenAsync()` and retries once
- Skips auth for `connect/` and `api/account/register` paths (avoids circular dependency during token acquisition)
- Registered as Transient; `HttpClientService` uses it in its HTTP pipeline

### ABP Web Key Patterns

- **Conventional API Controllers**: ABP auto-generates HTTP endpoints from `IAppService` interfaces → `api/app/{service}/{method}`
- **Unified Relationship System**: Single `AppRelationship` entity with `RelationshipType` discriminator (Friendship/Device/Group/Stock). Each type has its own `IRelationshipManager<,,>` with type-specific authorization.
- **Object Mapping**: ABP `MapperBase<TSource, TDestination>` with hand-written mappers (not Riok.Mapperly source generators). Registered via `AddMapperlyObjectMapper<TModule>()`.
- **Auth**: OpenIddict, OAuth2 password+refresh token. Client: `alphaagent_chat`. Access 30 days, refresh 365 days.
- **Real-time Chat**: Dual-protocol (REST for history/CRUD, SignalR for delivery). `ChatHub` with dual authentication (JWT + authorization code for devices). `SignalRQueryTokenMiddleware` extracts tokens from query params for WebSocket auth.
- **Moments GUID conversion**: Stock integer IDs → hex-padded Guid. Device/group string IDs → SHA-256 first 16 bytes as Guid.
- **Agent Config Management**: `AppAgentConfig` entity (server-side) stores per-user LLM config (AgentName, ModelName, ApiKey, Endpoint, DefaultSystemPrompt, Temperature, IsActive). `AgentConfigAppService` provides CRUD + `GetMyConfigAsync`, `SetMyConfigAsync`, `ActivateConfigAsync`. Blazor admin at `/agent-config-management`. Permission: `Abp.AgentConfigs.Manage`.
- **Security Sync**: `SecuritySyncService` syncs securities from external data source on server. `SecurityClientSyncService` (`[AllowAnonymous]`) provides incremental updates to MAUI client via `api/app/security-client-sync/updates?after={timestamp}`.

## Conventions

- **Language**: Code in English; user-facing strings and comments in Chinese (中文)
- **Entity prefix**: `App` on custom entities and database tables (e.g., `AppSecurity`, `AppSecurities`) to avoid ABP conflicts
- **Error codes**: `BusinessException` with `"AlphaAgent:<Description>"` format (e.g., `"AlphaAgent:UserNotFound"`)
- **Nullable reference types**: Enabled across all projects
- **Build config**: `src/common.props` sets `LangVersion=latest`, `AbpProjectType=app`, suppresses CS1591 (missing XML doc) and 0436 (Razor import conflict). No `.editorconfig` or analyzers.
- **ViewModels**: All implement `IPageLifecycleAware` for event subscription/unsubscription during navigation
- **DTO/interface organization**: Grouped by domain subdirectory (e.g., `Dtos/Agent/`, `Interfaces/Chat/`)

### Critical Runtime conventions

- **DateTime/timezone**: Server DateTime arrives as `Unspecified` kind after JSON deserialization. Use `EnsureLocalTime()` pattern: treat `Unspecified` as UTC, then convert to local via `DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime()`
- **JSON with Chinese text**: Use `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` in `JsonSerializerOptions` to avoid `\uXXXX` escaping. Prefer static readonly `JsonSerializerOptions` fields, not new instances per call
- **ToolCall SQLite persistence**: `Dictionary<string, object>` on `ToolCall` cannot persist directly to SQLite. Use `SerializeJson()`/`DeserializeJson()` with `InputJson`/`OutputJson` string backing fields
- **MAUI cross-thread**: MAUI auto-marshals `PropertyChanged` to main thread. Do NOT wrap `ObservableCollection.Add` or property setters in `MainThread.BeginInvokeOnMainThread` — causes UI thread starvation. Only use it for imperative UI operations like `CollectionView.ScrollTo`
- **MAUI Agent streaming**: `IsStreaming=true` → use Label (lightweight append). `IsStreaming=false` → switch to MarkdownView by setting `MarkdownContent = Content`. Never update `MarkdownText` during streaming — triggers full re-parse on every change
- **Streaming versioning**: `AgentChatDetailViewModel._streamVersion` incremented when switching sessions; streaming operations capture version at start. Stale chunks are discarded when version mismatched
- **MAUI self-signed certificate**: Android 7+ and iOS require custom handling for self-signed certificates. `CustomCertificateHandler` loads the embedded `rootCA.crt` from `Resources/raw/` and configures `SocketsHttpHandler.SslOptions` with `X509ChainTrustMode.CustomRootTrust`. Both `HttpClient` (via `ConfigurePrimaryHttpMessageHandler`) and `SignalR HubConnection` (via `HttpMessageHandlerFactory`) use this handler. Android also requires `network_security_config.xml` (referenced from AndroidManifest) to trust user CA certificates at the native layer. Server address is centralized in `AppSettings.ServerBaseAddress`

## CI/CD

### Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `deploy-iis.yml` | Push to master (Web changes) or manual | Build + deploy HttpApi.Host to IIS via msdeploy |
| `build-apk.yml` | Manual (`workflow_dispatch`) only | Build signed APK → GitHub Release → deploy to IIS → register version |

### deploy-iis.yml Flow

1. `dotnet publish` (self-contained, win-x64)
2. Replace `${PLACEHOLDER}` tokens in `appsettings.json` with GitHub Secrets (each value `.Trim()` to remove trailing newlines)
3. msdeploy with `-enableRule:AppOffline` (puts `app_offline.htm` before deploy, removes after)

**Secrets**: `CONNECTION_STRING_DEFAULT`, `STRING_ENCRYPTION_PASSPHRASE`, `OPENID_DICT_CLIENT_SECRET`, `VERSION_PUBLISH_TOKEN`, `IIS_SITE_NAME`, `IIS_SERVER_HOST`, `IIS_USERNAME`, `IIS_PASSWORD`

### build-apk.yml Flow

1. Decode keystore from `KEYSTORE_BASE64` secret
2. Set version: manual input or `1.0.{run_number}` (VersionCode = integer run_number)
3. `dotnet publish` for net10.0-android Release with signing (`AndroidKeyStore=true`, keystore from secrets)
4. Upload APK to GitHub Release (`softprops/action-gh-release@v2`)
5. Deploy APK to IIS `/apk` directory via msdeploy (with `web.config` for `.apk` MIME type)
6. Call `POST /api/app/version-config/publish` with `X-Publish-Token` header to register version

**Signing secrets**: `KEYSTORE_BASE64`, `KEY_ALIAS`, `KEY_PASSWORD`, `KEYSTORE_PASSWORD`

**APK public download**: `https://{IIS_SERVER_HOST}/apk/{apk-filename}`

### Appsettings Placeholder Pattern

Production `appsettings.json` uses `${PLACEHOLDER}` tokens replaced at deploy time:
- `${CONNECTION_STRING}` → `CONNECTION_STRING_DEFAULT` secret
- `${PASSPHRASE}` → `STRING_ENCRYPTION_PASSPHRASE` secret
- `${CLIENT_SECRET}` → `OPENID_DICT_CLIENT_SECRET` secret
- `${VERSION_PUBLISH_TOKEN}` → `VERSION_PUBLISH_TOKEN` secret

Development `appsettings.Development.json` contains real values (gitignored).

### Auto-Update System

- **Server**: `VersionConfigAppService.PublishAsync` (`[AllowAnonymous]` + `X-Publish-Token` header auth) creates version records. `CheckUpdateAsync` returns latest version for platform.
- **Client**: `SplashViewModel.CheckUpdateAsync` → `UpdateService.CheckUpdateAsync` → `POST api/app/version-config/check-update` with platform + current version code. If `HasUpdate=true`, prompts download via `Launcher.OpenAsync(UpdateUrl)`.
- **Force update**: `IsForce=true` blocks app until user updates. Non-force shows "稍后再说" option.