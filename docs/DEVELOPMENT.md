# Development Guide

## Setup Essentials

### Prerequisites

- **.NET 10 SDK** — required for all projects
- **SQL Server** — for the Web backend database

### Installation Steps

1. **Clone and build**
   ```bash
   git clone <repository-url>
   cd AlphaAgent
   dotnet build AlphaAgent.sln
   ```

2. **Configure database connection**

   Edit `src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "Default": "Server=<your-server>,1433;Database=AlphaAgent;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
   }
   ```
   For development, place real values in `appsettings.Development.json` (gitignored) rather than modifying `appsettings.json` directly. The committed `appsettings.json` uses `${PLACEHOLDER}` tokens that are replaced by GitHub Actions at deploy time.

3. **Apply database migrations**
   ```bash
   dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator
   ```
   This creates the database schema and seeds initial data including the OpenIddict clients. The HttpApi.Host also auto-migrates on startup, so running the migrator separately is optional during development.

## Environment Configuration

The backend configuration is in `src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host/appsettings.json`:

| Setting | Purpose | Production Value |
|---|---|---|
| `App:SelfUrl` | Backend base URL | `https://localhost:44319` |
| `ConnectionStrings:Default` | SQL Server connection | `${CONNECTION_STRING}` |
| `AuthServer:Authority` | OpenIddict authority URL | Same as SelfUrl |
| `StringEncryption:DefaultPassPhrase` | ABP string encryption key | `${PASSPHRASE}` |
| `OpenIddict:Applications:AlphaAgent_Chat:ClientId` | OAuth2 client ID for mobile/desktop | `alphaagent_chat` |
| `OpenIddict:Applications:AlphaAgent_Chat:ClientSecret` | OAuth2 client secret | `${CLIENT_SECRET}` |
| `VersionConfig:PublishToken` | Token for version publish API | `${VERSION_PUBLISH_TOKEN}` |

**Placeholder token pattern**: The committed `appsettings.json` uses `${PLACEHOLDER}` tokens (e.g., `${CONNECTION_STRING}`, `${PASSPHRASE}`, `${CLIENT_SECRET}`, `${VERSION_PUBLISH_TOKEN}`) that contain no real secrets. GitHub Actions replaces these tokens with actual values from GitHub Secrets at deploy time (see `deploy-iis.yml` "Replace secrets in appsettings.json" step).

**Development setup**: Place real values in `appsettings.Development.json` (gitignored via `appsettings.*.json` pattern in `.gitignore`). ASP.NET Core automatically loads this file in Development environment, overriding the placeholder tokens.

The MAUI client hardcodes its SQLite connection (`Data Source=alphaagent.db` in `FileSystem.AppDataDirectory`) and backend base address in `AppSettings.ServerBaseAddress` (referenced from `MauiProgram.cs` via `AddInfrastructureServices(sqliteConnectionString, AppSettings.ServerBaseAddress, agentOptions, CustomCertificateHandler.CreateHandler)`). Update `AppSettings.ServerBaseAddress` to point to your backend instance. `AgentOptions` is created with an empty `ApiKey`; the real API key is loaded from the server's `AppAgentConfigs` table during `SplashViewModel.LoadAgentConfigAsync()` and applied to the `AgentOptions` singleton before any agent session is created.

For self-signed certificate servers, place the root CA certificate as `Resources/raw/rootCA.crt`. `CustomCertificateHandler` loads it at startup and configures both `HttpClient` and SignalR to trust it. Android also requires `Platforms/Android/Resources/xml/network_security_config.xml` (referenced from AndroidManifest) for native-layer certificate trust.

## CI/CD Workflows

Two GitHub Actions workflows handle automated deployment from the `master` branch:

### deploy-iis.yml — Web Backend Auto-Deployment

Triggers on push to `master` when files under `src/AlphaAgent.Web/` change, or via manual `workflow_dispatch`.

**Steps:**
1. `dotnet publish` the HttpApi.Host project (self-contained, win-x64 runtime)
2. Replace `${PLACEHOLDER}` tokens in `appsettings.json` with values from GitHub Secrets (`.Trim()` removes trailing newlines, `\r` stripped from JSON)
3. Deploy to IIS via `msdeploy.exe` with `-enableRule:AppOffline` (takes app offline during deployment to handle locked DLLs)

**Required GitHub Secrets:**

| Secret | Purpose |
|---|---|
| `CONNECTION_STRING_DEFAULT` | SQL Server connection string (replaces `${CONNECTION_STRING}`) |
| `STRING_ENCRYPTION_PASSPHRASE` | ABP string encryption passphrase (replaces `${PASSPHRASE}`) |
| `OPENID_DICT_CLIENT_SECRET` | OAuth2 client secret (replaces `${CLIENT_SECRET}`) |
| `VERSION_PUBLISH_TOKEN` | Token for version publish API (replaces `${VERSION_PUBLISH_TOKEN}`) |
| `IIS_SITE_NAME` | IIS site name for msdeploy target |
| `IIS_SERVER_HOST` | IIS server hostname (connects via `https://{host}:8172/msdeploy.axd`) |
| `IIS_USERNAME` | msdeploy Basic auth username |
| `IIS_PASSWORD` | msdeploy Basic auth password |

### build-maui.yml — APK Build + Deploy + Version Registration

Triggers via manual `workflow_dispatch` only (with optional `version` input, e.g., `1.2.0`). Does not auto-trigger on push — use when ready to release a new APK.

**Steps:**
1. Install .NET 10 + MAUI Android workloads
2. Set version: uses `workflow_dispatch` input or auto-generates `1.0.{run_number}`
3. `dotnet publish` the MAUI project for Android Release
4. Upload APK to GitHub Release (tagged `v{versionName}`)
5. Deploy APK to IIS `/apk` directory via msdeploy (includes `web.config` for `.apk` MIME type)
6. Register version on server via `POST /api/app/version-config/publish` with `X-Publish-Token` header

**APK public download URL**: `https://{IIS_SERVER_HOST}/apk/{filename}.apk`

### Manual Deployments

Both workflows support `workflow_dispatch` for manual triggers:
- **deploy-iis.yml**: Go to Actions tab → "Deploy to IIS" → "Run workflow"
- **build-maui.yml**: Go to Actions tab → "Build MAUI APK" → "Run workflow" → optionally enter a version tag (e.g., `2.0.0`)

## Auto-Update System

The auto-update system enables the MAUI client to check for new versions on startup and prompt the user to update.

### Version Registration During CI/CD

When `build-maui.yml` completes an APK build, the "Publish version to server" step calls `POST /api/app/version-config/publish` with:
- `platform`: 1 (Android)
- `versionCode`: the GitHub run number (integer, monotonically increasing)
- `versionName`: the version tag (e.g., `1.2.0`)
- `updateUrl`: `https://{IIS_SERVER_HOST}/apk/{filename}.apk`
- `updateNote`: `AlphaAgent v{versionName}`
- `isForce`: `false` (non-force by default; set to `true` in Blazor admin for critical updates)

The endpoint is authenticated via `X-Publish-Token` header matched against `VersionConfig:PublishToken` from `appsettings.json`.

### MAUI Client Update Check

On startup, `SplashViewModel.InitializeAsync()` calls `CheckUpdateAsync()` which:
1. Determines the current platform (`AppPlatform` enum) and version code (from `AppInfo.Current.BuildString`, which maps to `ApplicationVersion` in the `.csproj`)
2. Calls `IUpdateService.CheckUpdateAsync(platform, currentVersionCode)` → `POST /api/app/version-config/check-update`
3. Server compares `currentVersionCode` against the latest `VersionCode` for the platform; returns `HasUpdate=true` if a newer version exists

### Force Update vs Non-Force Update

After initialization, `SplashPage` handles the update prompt in `HandleUpdateAsync()`:

- **Force update** (`IsForce=true`): Displays a single "立即更新" (Update Now) button. Opens the download URL and does NOT navigate to the main app — the user must install the new version before continuing.
- **Non-force update** (`IsForce=false`): Displays "立即更新" (Update Now) and "稍后再说" (Later) buttons. If the user chooses "Later", navigation proceeds normally to the main app.

### Manually Adding Versions via Blazor Admin Panel

Navigate to `/version-config-management` in the Blazor admin UI. The page provides:
- **Version list**: Filterable by platform (iOS/Android/Windows/MacCatalyst), with pagination
- **Create**: Set platform, version code (integer), version name, download URL, update note, and force update flag
- **Edit**: Modify any existing version record
- **Delete**: Remove a version record

This is useful for manually registering iOS/Windows versions that are not deployed through CI/CD, or for marking a version as force-update after release.

The ConsoleDevice client configuration is in `src/AlphaAgent.ConsoleDevice/appsettings.json`:

| Setting | Purpose | Default |
|---|---|---|
| `Device:ServerUrl` | SignalR server URL | `https://localhost:44319` |
| `Device:AuthorizationCode` | Device auth code for SignalR | — |
| `Agent:ModelName` | LLM model for agent | `deepseek-chat` |
| `Agent:ApiKey` | LLM API key | — |
| `Agent:Endpoint` | LLM API endpoint | `https://api.deepseek.com/v1` |
| `Agent:DefaultSystemPrompt` | System prompt for agent | Chinese stock analyst prompt |
| `Agent:Temperature` | LLM temperature | `0.5` |

## Clean Architecture Guidelines

AlphaAgent.Core follows Clean Architecture with four layers and strict dependency rules:

| Layer | Can Reference | Cannot Reference |
|---|---|---|
| **UI (MAUI)** | Application | Domain, Domain.Abstractions, Infrastructure |
| **Application** | Domain, Domain.Abstractions | Infrastructure, UI |
| **Domain** | Domain.Abstractions | Application, Infrastructure, UI |
| **Domain.Abstractions** | Nothing (root) | All other layers |
| **Infrastructure** | Domain, Domain.Abstractions | Application, UI |

**Key Rules:**
- UI layer should only use Application services and DTOs
- Application layer should only use Domain entities/interfaces and Domain.Abstractions
- Domain.Abstractions is the root of the dependency graph — it has zero dependencies
- Infrastructure only references Domain and Domain.Abstractions (no Application dependency)
- Infrastructure interfaces (`ISignalRChatService`, `IHttpClientService`) live in Domain.Abstractions so Infrastructure can implement them without referencing Application
- No circular dependencies allowed

## Custom Development Workflows

### Adding a New Agent

Follow these steps to add a new agent to the system:

1. **Infrastructure** — Create a static factory class in `AlphaAgent.Infrastructure/Services/AiAgent/Agents/`:
   ```csharp
   public static class YourAgent
   {
       public const string Name = "YourAgent";
       public const string Description = "描述智能体功能";

       public static IAgent Create(IServiceProvider serviceProvider, IChatClient chatClient, string systemPrompt, float temperature)
       {
           var scope = serviceProvider.CreateScope();
           var yourTool = scope.ServiceProvider.GetRequiredService<YourTool>();

           var aiTools = new[]
           {
               AIFunctionFactory.Create(yourTool.YourMethod),
           };

           var chatClientAgent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
           {
               Name = Name,
               Description = Description,
           });

           return new LlmAgent(Name, Description, systemPrompt, chatClientAgent, aiTools, temperature);
       }
   }
   ```

   **Memory mode options** — pass `memoryMode` and `maxHistoryMessages` to `LlmAgent` constructor:
   - Default: `AgentMemoryMode.SlidingWindow` with `maxHistoryMessages=20` (like `StockAnalystAgent`)
   - Stateless: `memoryMode: AgentMemoryMode.Stateless` (like `StockAnalystNoMemoryAgent`) — messages are persisted for display but no history sent to LLM
   - Stateful: `memoryMode: AgentMemoryMode.Stateful` — all history sent to LLM

   **Session isolation** — each agent has its own active session queried by `userId + agentName`. When creating a stateless variant of an existing agent, use a distinct `Name` (e.g., `"指标分析Agent(无记忆)"`).

2. **Infrastructure** — Create tool classes in `AlphaAgent.Infrastructure/Services/AiAgent/Tools/` with methods decorated by `[Description]` attributes for AI function schema generation

3. **Infrastructure** — Register the tool and agent factory in `RegisterAgentServices()` within `ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<YourTool>();

   // Inside the IAgentFactory registration:
   factory.Register(YourAgent.Name, YourAgent.Description, YourAgent.DefaultSystemPrompt, serviceProvider =>
       YourAgent.Create(serviceProvider, chatClient, options.DefaultSystemPrompt, options.Temperature));
   ```

4. **Application** — No changes needed; `IAgentService` handles all agents generically via `IAgentFactory`

5. **MAUI** — Add UI for the new agent if needed (agent list auto-discovers registered agents via `IAgentService.GetAvailableAgentsAsync()`)

6. **Server config** — On next app startup, `EnsureDefaultConfigsAsync` automatically creates a skeleton config (AgentName + DefaultSystemPrompt, empty ApiKey) on the server for the new agent. Users must fill the ApiKey via the Blazor admin panel (`/agent-config-management`) before the agent can call LLMs.

### Adding a New Tool for an Agent

1. **Infrastructure** — Create a tool class in `AlphaAgent.Infrastructure/Services/AiAgent/Tools/`:
   ```csharp
   public class YourTool
   {
       public class Input
       {
           [Description("参数描述")]
           public string Param1 { get; set; } = string.Empty;
       }

       private readonly IDependency _dependency;

       public YourTool(IDependency dependency)
       {
           _dependency = dependency;
       }

       [Description("工具功能描述")]
       public async Task<string> YourMethod(Input input, CancellationToken ct = default)
       {
           // Implement tool logic
           return "result";
       }
   }
   ```

2. **Infrastructure** — Register the tool in `RegisterAgentServices()`: `services.AddScoped<YourTool>();`

3. **Infrastructure** — Add `AIFunctionFactory.Create(yourTool.YourMethod)` to the agent's `aiTools` array in the agent factory class

### Adding a New Entity to AlphaAgent.Core

Follow Clean Architecture principles:

1. **Domain.Abstractions** — Add shared interfaces if the entity needs an abstraction that both Domain and Application reference
2. **Domain Layer** — Create the entity class in `AlphaAgent.Domain/Entities/`
3. **Domain Layer** — Add repository interface in `AlphaAgent.Domain/Interfaces/`
4. **Application Layer** — Create DTO in `AlphaAgent.Application/Dtos/<domain>/`
5. **Application Layer** — Add service interface in `AlphaAgent.Application/Interfaces/<domain>/`
6. **Application Layer** — Implement service in `AlphaAgent.Application/Services/<domain>/`
7. **Infrastructure Layer** — Add `DbSet<T>` to `SharesDbContext`
8. **Infrastructure Layer** — Implement repository in `AlphaAgent.Infrastructure/Data/Repositories/`
9. **Infrastructure Layer** — Register repository and services in `ServiceCollectionExtensions.cs`
10. **MAUI** — Update ViewModels to use the new Application service

### Adding a New Service to AlphaAgent.Core

1. **Domain.Abstractions** — Define shared interface if needed (e.g., for agent abstractions)
2. **Domain Layer** — Define repository interface in `AlphaAgent.Domain/Interfaces/` (if needed)
3. **Application Layer** — Define service interface in `AlphaAgent.Application/Interfaces/<domain>/`
4. **Application Layer** — Implement service in `AlphaAgent.Application/Services/<domain>/`
5. **Application Layer** — Register in `AddApplicationServices()` extension method
6. **MAUI** — Inject the interface into ViewModels

### Adding a New Entity to the ABP Backend

Follow this order to maintain DDD layer dependencies:

1. **Domain.Shared** — Add enum values or constants if needed
2. **Domain** — Create the entity class (extend `Entity<T>` or `FullAuditedAggregateRoot<T>`, use `App` prefix)
3. **Domain** — Add `DbSet<T>` to `AlphaAgentAbpDbContext` and configure in `OnModelCreating`
4. **Application.Contracts** — Create DTO and `IAppService` interface
5. **Application** — Implement the `AppService`, add Mapperly mapper if needed
6. **EF Core** — Add migration: `dotnet ef migrations add Add<AppEntity> --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host`
7. **MAUI** — Add page/ViewModel if UI is needed (using AlphaAgent.Core services)

### Adding a New Quote Source

1. Create a class implementing `IQuoteProvider` in `AlphaAgent.Infrastructure/Services/Quotes/Providers/`
2. Implement `Name`, `IsSupported(code, freq, type, exchange)`, and `GetKlineAsync(code, freq, type, exchange)`
3. Register it in `AlphaAgent.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — add both `services.AddSingleton<IQuoteProvider, YourProvider>()` and `services.AddHttpClient<YourProvider>()`
4. The `FailoverQuoteProvider` will automatically pick it up via collection injection

### Adding a New Relationship Type

1. **Domain.Shared** — Add value to `RelationshipType` enum in `ChatEnums.cs`
2. **Domain** — Create a new `IRelationshipManager<AppRelationship, TTargetEntity, Guid>` implementation with type-specific authorization rules
3. **Domain** — Register the manager in `AbpDomainModule.ConfigureServices()`:
   ```csharp
   context.Services.AddTransient<IRelationshipManager<AppRelationship, YourEntity, Guid>, YourManager>();
   ```
4. **Application** — Add the new case to the switch expressions in `RelationshipService`
5. **Application.Contracts** — Update `RelationshipDto` if new fields are needed

### Working with the Chat System

The chat system uses both REST (for history/CRUD) and SignalR (for real-time). The key layers:

- **Domain (Web)**: `ConversationManager` — conversation lifecycle, participants, messages
- **Application (Web)**: `ChatAppService` — REST API at `api/app/chat/*`
- **HttpApi**: `ChatHub` at `/hubs/chat` — SignalR hub with dual auth (JWT + device auth code)
- **Application (Core)**: `IChatService`/`ChatService` — high-level chat operations
- **Domain.Abstractions (Core)**: `ISignalRChatService` — Singleton SignalR connection manager interface
- **MAUI**: `ChatViewModel` (conversation list) + `ChatDetailViewModel` (real-time messaging)

To add a new conversation type:
1. **Domain.Shared** — Add value to `ConversationType` enum if needed
2. **Domain (Web)** — Add a `GetOrCreateXxxConversationAsync` method to `ConversationManager` with a deterministic key
3. **Application (Web)** — Add the corresponding endpoint to `ChatAppService` and interface to `IChatAppService`
4. **Application (Core)** — Add models/methods to `IChatService` for MAUI consumption
5. **HttpApi.Host** — No changes needed (hub routes by conversation ID generically)

The `ChatHub` uses per-conversation SignalR groups (`conv_{conversationId}`). All participants in a conversation are added to the group on connection.

### Adding a New Moment Type

1. **Domain (Web)** — Add the type string constant in `MomentManager` (alongside `"User"`, `"Stock"`, `"Device"`, `"Group"`)
2. **Domain (Web)** — Add a GUID conversion method in `MomentManager` if the target entity uses a non-Guid ID (follow the `CreateStockGuid` or `CreateTargetGuid` pattern)
3. **Application (Web)** — Add a `CreateXxxMomentAsync` method in `MomentAppService` and a case in `GetMomentsAsync` smart routing
4. **Application.Contracts** — Create `CreateXxxMomentDto` and add the method to `IMomentAppService`
5. **MAUI** — Add UI for creating and viewing the new moment type (using Core services)

### Working with Technical Indicators

Indicators are configured by string name with optional parameters. The parsing supports three formats:
- `SMA(20)` — type with parentheses params
- `SMA20` — type with trailing number (treated as Period)
- `SMA` — type only (uses defaults)

To add a new indicator:
1. Add value to `IndicatorType` enum in `AlphaAgent.Domain/`
2. Add the calculation case in `IndicatorCalculator.CalculateAsCsvAsync()`
3. Add the name-parsing case in `ParseIndicatorNameWithParameters()`

### Working with the MAUI Client

The MAUI client uses Shell-based navigation with `Routing.RegisterRoute()` for detail pages. To add a new page:

1. Create a ViewModel in `ViewModels/` using CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
2. Register it as `AddScoped` in `MauiProgram.cs`
3. Create a XAML page in `Views/` with constructor injection
4. Register the route in `App.xaml.cs` via `Routing.RegisterRoute(nameof(YourPage), typeof(YourPage))`
5. If it's a tab page, add it to `AppShell.xaml` as a `ShellContent`

For pages that receive navigation parameters, use `[QueryProperty]` attributes on the page class.

**Service Registration Pattern:**
```csharp
// MauiProgram.cs
CustomCertificateHandler.Initialize(); // Load embedded rootCA.crt
// AgentOptions starts with empty ApiKey — populated from server config at startup
var agentOptions = new AgentOptions { ApiKey = string.Empty, ... };
builder.Services.AddInfrastructureServices(sqliteConnectionString, AppSettings.ServerBaseAddress, agentOptions, CustomCertificateHandler.CreateHandler);
builder.Services.AddApplicationServices();

// ViewModel constructor injection (cache-first pattern)
public ChatViewModel(IChatService chatService, IAuthService authService, IConversationSyncService conversationSyncService, IAgentRepository agentRepository)
{
    _chatService = chatService;
    _authService = authService;
    _conversationSyncService = conversationSyncService;
    _agentRepository = agentRepository;
}
```

### Working with the Agent System

The Agent system provides AI-powered stock analysis with session-based conversations and streaming support:

**Agent Service Interface**:
```csharp
// Starting a session
var session = await _agentService.StartSessionAsync("StockAnalyst");

// Starting a session with context (per-stock isolation)
var session = await _agentService.StartSessionAsync("StockAnalyst", initialContext: "stock:600519:浦发银行");

// Sending a message (synchronous)
var response = await _agentService.SendMessageAsync(session.Id, "分析浦发银行的技术指标");

// Sending a message (streaming)
await foreach (var streamEvent in _agentService.SendMessageStreamingAsync(session.Id, "分析浦发银行的技术指标"))
{
    switch (streamEvent)
    {
        case AgentTextEvent textEvent:
            // Handle incremental text
            break;
        case AgentToolCallEvent toolCallEvent:
            // Handle tool invocation
            break;
        case AgentToolResultEvent toolResultEvent:
            // Handle tool result
            break;
    }
}

// Getting session history
var messages = await _agentService.GetSessionHistoryAsync(session.Id);

// Listing available agents
var agents = await _agentService.GetAvailableAgentsAsync();

// Closing a session
await _agentService.CloseSessionAsync(session.Id);
```

**Key Interfaces**:
- `IAgent` (Domain.Abstractions) — Agent contract with `RunAsync()` and `RunStreamingAsync()`
- `IAgentFactory` (Domain.Abstractions) — Named agent resolution and discovery
- `IAgentService` (Application) — Session management and message orchestration
- `IAgentRepository` (Domain) — Session/message/task persistence

**Agent Configuration** (`AgentOptions`, Domain.Abstractions):
- `ModelName` — LLM model (default: `deepseek-chat`)
- `ApiKey` — LLM API key (initially empty, populated from server config at startup)
- `Endpoint` — LLM API endpoint (default: `https://api.deepseek.com/v1`)
- `AgentSystemPrompts` — Per-agent system prompts dictionary (populated from server config)
- `Temperature` — LLM temperature (default: `0.5`)

**Agent Config Loading** (`SplashViewModel.LoadAgentConfigAsync`):
1. Read local SQLite cache → apply to `AgentOptions` if configs have real ApiKey
2. Sync from server → update cache with server data; fallback to cache on network error
3. `EnsureDefaultConfigsAsync` → create skeleton configs (AgentName + DefaultSystemPrompt, empty ApiKey) for newly registered agents missing from server
4. Re-read cache → apply final configs to `AgentOptions`
- If no config has a real ApiKey, `AgentOptions.ApiKey` stays empty and agents cannot call LLMs — users must fill ApiKey via Blazor admin panel

**MAUI Agent UI**:
- `AgentContactDetailViewModel` — Displays agent info and available tools, navigates to chat
- `AgentChatDetailViewModel` — Manages agent session, sends/receives messages with streaming via `IAgentService`. Supports dual modes:
  - **Normal Agent mode** (via `agentName` query parameter): Standard agent session
  - **Stock mode** (via `stockId` + `stockName` query parameters): Uses Context-based session isolation (`"stock:{stockId}:{stockName}"`), auto-queries the stock on new session via `AutoQueryStockAsync()`. `_streamVersion` field invalidates stale streaming operations when switching sessions.

### Working with the Message Cache System

The hybrid message cache system provides memory + SQLite caching for chat messages:

**Cache Flow:**
1. **Read**: Check memory cache → Check SQLite → Fetch from network
2. **Write**: Update memory synchronously → Async write to SQLite
3. **Append**: Update cache incrementally

**Key Interfaces:**
- `IMessageCacheService` (Application) — exposed to MAUI ViewModels
- `IMessageCacheRepository` (Domain) — interface for repository
- `MessageCacheRepository` (Infrastructure) — hybrid implementation

**Usage in ViewModels:**
```csharp
// Load messages with caching
var cachedMessages = await _messageCacheService.GetCachedMessagesAsync(conversationId, 50);
if (cachedMessages.Any())
{
    // Use cached messages
}
else
{
    // Fetch from network and update cache
    var messages = await _chatService.GetMessagesAsync(conversationId);
    await _messageCacheService.CacheMessagesAsync(conversationId, messages);
}

// Append single message
await _messageCacheService.AppendMessageAsync(conversationId, message);
```

**Cache Configuration:**
- **Expiration**: 5 minutes (configurable in `MessageCacheRepository`)
- **Message Limit**: 50 messages per conversation (enforced in `MessageCacheService`)

### Working with the Sync Services and Cache-First Loading

The sync services provide cache-first loading for conversations and contacts, with background server sync:

**ConversationSyncService** (`IConversationSyncService`):
```csharp
// Load cached conversations (instant display)
var cached = await _conversationSyncService.GetCachedConversationsAsync(userId);

// Sync from server (background)
await _conversationSyncService.SyncFromServerAsync(userId);

// Save/update a conversation in cache
await _conversationSyncService.UpsertConversationAsync(conversation, userId);

// Delete a conversation from cache
await _conversationSyncService.DeleteConversationAsync(conversationId);
```

**ContactSyncService** (`IContactSyncService`):
```csharp
// Load cached contacts (instant display)
var cached = await _contactSyncService.GetCachedContactsAsync(userId);

// Sync from server (background) — full-replace strategy
await _contactSyncService.SyncFromServerAsync(userId);
```

**AgentConfigService** (`IAgentConfigService`):
```csharp
// Load cached agent configs (instant display)
var cached = await _agentConfigService.GetCachedConfigsAsync(userId);

// Sync from server (background) — full-replace strategy, fallback to cache on error
var synced = await _agentConfigService.SyncFromServerAsync(userId);

// Auto-create skeleton configs for newly registered agents (empty ApiKey)
await _agentConfigService.EnsureDefaultConfigsAsync(userId, existingConfigs);

// Update a config on the server (upsert by userId + AgentName)
await _agentConfigService.SetConfigAsync(config);
```

**Cache-First ViewModel Pattern**:
1. `OnAppearingAsync()` → Load from local cache first (instant display)
2. `SyncInBackgroundAsync()` → Sync from server with 30-second throttle (`_minSyncInterval`)
3. Use `SortConversations()` / `UpdateContactBookIfNeeded()` to compare ID sets/order before updating `ObservableCollection` — prevents UI flicker
4. Subscribe to events (`NewConversationEvent`, `ContactChangedEvent`) for real-time updates

**SQLite New Table Migration**: When adding new cache entities to `SharesDbContext`, add the table creation in `DatabaseInitializer.EnsureNewTablesAsync()` using `CREATE TABLE IF NOT EXISTS`, since `EnsureCreatedAsync` does not add new tables to an existing database.

**Key Interfaces**:
- `IConversationSyncService` (Application) — conversation cache management
- `IContactSyncService` (Application) — contact cache management
- `IAgentConfigService` (Application) — agent config cache + server sync + auto-skeleton creation
- `IMomentCacheService` (Application) — moment cache with incremental loading
- `IConversationCacheRepository` / `IContactCacheRepository` / `IMomentCacheRepository` / `IAgentConfigCacheRepository` (Domain) — SQLite persistence

### Working with the Event Bus System

The event bus enables cross-page communication:

**Events Available:**
- `NewMessageEvent` — new message received
- `NewConversationEvent` — new conversation created
- `ConversationReadEvent` — conversation marked as read
- `UnreadCountUpdatedEvent` — unread count changed
- `ContactChangedEvent` — relationship changed (Action: "added", "accepted", "deleted")

**Publishing Events:**
```csharp
await _eventBus.PublishAsync(new NewMessageEvent(conversationId, message));
```

**Subscribing to Events:**
ViewModels should implement `IPageLifecycleAware` to subscribe/unsubscribe properly:
```csharp
public void OnAppearingAsync()
{
    _eventBus.Subscribe<NewMessageEvent>(HandleNewMessage);
}

public void OnDisappearingAsync()
{
    _eventBus.Unsubscribe<NewMessageEvent>(HandleNewMessage);
}

private void HandleNewMessage(NewMessageEvent args)
{
    // Handle the event
}
```

**Global Message Handling:**
`GlobalMessageHandler` subscribes to SignalR messages on app startup and publishes events to the bus. This ensures all components receive real-time updates regardless of which page is active.

## Key Commands

### Development
```bash
# Build entire solution
dotnet build AlphaAgent.sln

# Run backend (HTTP API host with Swagger UI + SignalR)
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Run MAUI client
dotnet run --project src/AlphaAgent.Maui

# Run console device client (SignalR + Agent-enabled)
dotnet run --project src/AlphaAgent.ConsoleDevice
```

### Database
```bash
# Run migrator (creates DB + seeds data)
dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator

# Add a migration
dotnet ef migrations add <Name> --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host

# Apply migrations
dotnet ef database update --project src/AlphaAgent.Web/AlphaAgent.Abp.EntityFrameworkCore --startup-project src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi.Host
```

### Troubleshooting
```bash
# Clear NuGet cache (if ABP packages changed after pull)
dotnet nuget locals all --clear

# Install EF Core tools (if dotnet ef is not found)
dotnet tool install --global dotnet-ef
```

### ABP CLI
```bash
# Generate HTTP API proxies (if HttpApi.Client is used)
abp generate-proxy -t csharp -m app -u AlphaAgent.Abp.HttpApi.Client
```

## Project Conventions

### Naming
- **Entity prefix**: `App` on all custom entities and tables (e.g., `AppSecurity` → `AppSecurities` table)
- **Error codes**: `BusinessException` uses `"AlphaAgent:<Description>"` format (e.g., `"AlphaAgent:UserNotFound"`)
- **Language**: Code in English, user-facing strings and comments in Chinese (中文)
- **DTO organization**: DTOs grouped by domain in `Dtos/<domain>/` (e.g., `Dtos/Agent/`, `Dtos/Auth/`, `Dtos/Chat/`)
- **Interface organization**: Interfaces grouped by domain in `Interfaces/<domain>/` (e.g., `Interfaces/Agent/`, `Interfaces/Chat/`, `Interfaces/Relationship/`)

### Dependency Injection
- Domain services: `Transient` lifetime, registered explicitly in module `ConfigureServices()` (Web) or `AddApplicationServices()` (Core)
- Application services: Auto-discovered by ABP via `IAppService` interface (Web), or registered via `AddApplicationServices()` (Core)
- Generic implementations: Must be registered individually by their specific type parameters
- Core library: Uses extension methods on `IServiceCollection` (`AddApplicationServices()`, `AddInfrastructureServices()`)
- Agent tools: Registered as concrete types in `RegisterAgentServices()`, exposed via `AIFunctionFactory.Create()` in agent factory classes
- Agent factory: `IAgentFactory` registered as Singleton with named agent registrations (including description and defaultSystemPrompt metadata)
- Agent config cache: `IAgentConfigCacheRepository` registered as Scoped in `AddInfrastructureServices()`
- MAUI ViewModels: Registered as `AddScoped` in `MauiProgram.cs`
- SignalR connection: `ISignalRChatService` (Domain.Abstractions) registered as Singleton in Infrastructure for global connection management

### Object Mapping
- Uses ABP's `Volo.Abp.Mapperly.MapperBase<TSource, TDest>` with hand-written mappers
- Not Riok.Mapperly source generators — mappers are written manually, not auto-generated
- Registered via `context.Services.AddMapperlyObjectMapper<TModule>()`

### DateTime/Timezone Handling
- Server DateTime values arrive as `Unspecified` kind after JSON deserialization
- Use the `EnsureLocalTime()` pattern: treat `Unspecified` as UTC, then convert to local
  ```csharp
  return dt.Kind == DateTimeKind.Local
      ? dt
      : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
  ```

### JSON Serialization
- Use `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` in `JsonSerializerOptions` to avoid `\uXXXX` escaping of Chinese characters
- Prefer static readonly `JsonSerializerOptions` fields rather than creating new instances per call

### ToolCall Serialization
- `Dictionary<string, object>` properties on `ToolCall` cannot be persisted directly to SQLite
- Use `SerializeJson()`/`DeserializeJson()` methods with `InputJson`/`OutputJson` string backing fields

### MAUI Cross-Thread Updates
- MAUI auto-marshals `PropertyChanged` to the main thread — do NOT wrap `ObservableCollection.Add` or property setters in `MainThread.BeginInvokeOnMainThread`
- Overuse of `MainThread.BeginInvokeOnMainThread` causes UI thread starvation and freezing
- Only use `MainThread.BeginInvokeOnMainThread` for imperative UI operations like `CollectionView.ScrollTo`

### MAUI Agent Streaming
- During streaming (`IsStreaming=true`): use Label for lightweight text display (direct `Content +=` append)
- After streaming ends (`IsStreaming=false`): switch to MarkdownView for rich rendering by setting `MarkdownContent = Content`
- Never update `MarkdownText` during streaming — it triggers full MarkdownView re-parse on every change and causes severe lag
- `AgentChatDetailViewModel.ApplyQueryAttributes` detects `AgentName` changes and resets state to force re-initialization when switching agents
- `_streamVersion` field: incremented when switching sessions, each streaming operation captures the version at start. If the version has changed by the time a chunk arrives, the operation is discarded to prevent stale data in wrong session
- `AgentChatDetailViewModel` supports dual modes: normal Agent (via `agentName`) and Stock mode (via `stockId` + `stockName`). Stock mode sets `IsStockMode=true` and uses Context-based session isolation

### Cache-First Loading Pattern
- ViewModels load from local SQLite cache first for instant display, then sync from server in background
- `ChatViewModel` uses `IConversationSyncService.GetCachedConversationsAsync()` → `SyncInBackgroundAsync()` with 30-second throttle
- `ContactsViewModel` uses `IContactSyncService.GetCachedContactsAsync()` → `SyncInBackgroundAsync()`
- `SplashViewModel` uses `IAgentConfigService.GetCachedConfigsAsync()` → `SyncFromServerAsync()` → `EnsureDefaultConfigsAsync()` to load agent configs and populate `AgentOptions`
- `ChatDetailViewModel` uses `IMessageCacheService.GetCachedMessagesAsync()` → syncs only if no cache exists
- Use `SortConversations()` / `UpdateContactBookIfNeeded()` that compare ID sets/order before updating `ObservableCollection` to prevent UI flicker
- Sync is throttled (`_minSyncInterval`, default 30 seconds) to avoid redundant network calls

### SQLite New Table Migration
- When adding new cache entities to `SharesDbContext`, add `CREATE TABLE IF NOT EXISTS` statements in `DatabaseInitializer.EnsureNewTablesAsync()`
- `EnsureCreatedAsync()` only creates tables that didn't exist at initial database creation — it does NOT add new tables to an existing database
- The `EnsureNewTablesAsync()` method uses raw SQL to handle this gap

### Build Properties
- `src/common.props` sets `LangVersion=latest`, `AbpProjectType=app`, and suppresses CS1591 (missing XML doc warnings)
- No `.editorconfig`, analyzers, or formatting rules are configured

## Data Seeding

The only `IDataSeedContributor` is `OpenIddictDataSeedContributor`, which seeds:
- **Scopes**: `"Abp"` and `"alphaagent_chat"`
- **OAuth clients**:
  - **Swagger** — Public, AuthorizationCode flow, redirect to `/swagger/oauth2-redirect.html`
  - **AlphaAgent Chat** — Confidential, Password + RefreshToken grant

No entity seed data (sample securities, users, relationships, moments) is created by the DbMigrator.

## Scaffold Features (Not Yet Implemented)

- **AppMessage** — Entity exists with `IsUser`, `Content`, `SenderName`, `SessionType` (defaults to "AI"), `SessionId`. Permissions defined (Manage, Send, Receive). No `MessageManager` or `MessageAppService`. Real-time chat uses separate `AppChatMessage` + `ChatAppService` instead.
- **OpenAI / AgentPrompts** — Permissions defined (Manage). No service or entity code yet.

Note: The Agent system in AlphaAgent.Core is fully implemented with `LlmAgent`, `AgentFactory`, `StockAnalystAgent`, and SQLite persistence. The Video Feed module (`VideoFeed`, `VideoFeedService`, `VideoChannelsViewModel`) is also fully implemented. Both are separate from the scaffolded OpenAI/AgentPrompts permissions in the ABP Web project.

## Troubleshooting

### Build Errors After Pull
If ABP packages changed, clear the cache:
```bash
dotnet nuget locals all --clear
dotnet build AlphaAgent.sln
```

### Database Connection Issues
- Verify SQL Server is accessible and the connection string in `appsettings.json` is correct
- Ensure `TrustServerCertificate=True` is set for non-SSL connections
- Run the DbMigrator to verify: `dotnet run --project src/AlphaAgent.Web/AlphaAgent.Abp.DbMigrator`

### EF Migration Errors
If `dotnet ef` is not found:
```bash
dotnet tool install --global dotnet-ef
```
Migrations must be run with both `--project` (EF Core project) and `--startup-project` (HttpApi.Host entry point) specified.

### MAUI Client Cannot Connect
- Verify the backend is running at the URL configured in `AppSettings.ServerBaseAddress`
- Check that the `alphaagent_chat` OAuth2 client is seeded (run DbMigrator)
- Clear the local SQLite cache: delete `alphaagent.db` in the app's data directory
- For self-signed certificates: ensure `rootCA.crt` is in `Resources/raw/` and `CustomCertificateHandler.Initialize()` is called in `MauiProgram.cs`
- On Android: verify `network_security_config.xml` is referenced from AndroidManifest and includes the server IP

### SignalR Chat Not Working
- Ensure the `HttpApi.Host` project is running (it hosts the SignalR hub at `/hubs/chat`)
- Check that `SignalRQueryTokenMiddleware` is registered before `UseAuthentication()` in the host pipeline
- For devices: verify the `AuthorizationCode` matches a registered device in the database
- For MAUI: check that `ISignalRChatService.ConnectAsync()` receives a valid access token
- Hub logs connection issues at `Information` level — check Serilog output
- **Android SSL errors**: SignalR uses `SocketsHttpHandler` which doesn't trust user-installed CA certificates. Verify `CustomCertificateHandler.Initialize()` is called at startup and `rootCA.crt` exists in `Resources/raw/`. Check that `SignalRChatService` receives the `httpMessageHandlerFactory` parameter via `AddInfrastructureServices`

### Agent System Issues
- Verify Agent tables exist in SQLite (`AgentSessions`, `AgentMessages`, `AgentTasks`, `AgentConfigCacheItem`) — run `IDatabaseInitializer.InitializeAsync()` or start the ConsoleDevice
- Check that `TechnicalAnalysisTool` is registered in `RegisterAgentServices()`
- Verify the agent name matches when calling `IAgentService.StartSessionAsync(agentName)` — sessions are isolated by `userId + agentName`
- For per-stock sessions: use `IAgentService.StartSessionAsync(agentName, initialContext: "stock:{stockId}:{stockName}")` and `GetActiveSessionByContextAsync(userId, agentName, context)`
- Use `IAgentService.GetAvailableAgentsAsync()` to list registered agents and their tools
- Check `AgentOptions` configuration (ModelName, ApiKey, Endpoint) — ApiKey is populated from server config at startup via `SplashViewModel.LoadAgentConfigAsync`
- If `AgentOptions.ApiKey` is empty: ensure the user has an Agent config with a real ApiKey on the server (Blazor admin: `/agent-config-management`), then restart the MAUI client to sync
- If `AgentFactory.GetAvailableAgents()` throws: the try-catch fallback returns registration metadata (Name, Description) without instantiating the full agent — this is expected when ApiKey is not yet loaded
- If different agents share chat history: verify `GetActiveSessionAsync` is called with the correct `agentName` parameter
- If streaming causes UI lag: ensure `MarkdownText` is NOT being updated during streaming — only set `MarkdownContent` after streaming ends
- If switching agents shows wrong history: check that `ApplyQueryAttributes` resets `_isLoaded` and `_currentSessionId` when `AgentName` changes
- If stale streaming data appears in wrong session: verify `_streamVersion` is being checked in the streaming loop

### Clean Architecture Violations
If MAUI is referencing Domain or Infrastructure directly:
- Remove the reference from `AlphaAgent.Maui.csproj`
- Use Application layer services and DTOs instead
- If needed, add new services to the Application layer

If Infrastructure is referencing Application:
- Infrastructure should only reference Domain and Domain.Abstractions
- Infrastructure interfaces that Infrastructure implements must live in Domain.Abstractions (e.g., `ISignalRChatService`, `IHttpClientService`)
- DTOs and config types used by Infrastructure must also live in Domain.Abstractions (e.g., `ChatMessage`, `AgentOptions`)

### Message Cache Not Working
- Check that `MessageCacheRepository` is registered in `AddInfrastructureServices()`
- Verify SQLite database is being created (check for `alphaagent.db` in app data directory)
- Enable debug logging in `MessageCacheRepository` to trace cache operations
- Ensure `Database.EnsureCreatedAsync()` is being called during initialization
- For new cache tables (ConversationCache, ContactCache, MomentCaches, VideoFeeds, AgentConfigCacheItem): verify `DatabaseInitializer.EnsureNewTablesAsync()` is being called — `EnsureCreatedAsync` does not add new tables to an existing database
- If cache tables are missing: delete `alphaagent.db` and restart the app to trigger fresh database creation

### Event Bus Not Working
- Verify `EventBusService` is registered as a Singleton
- Check that ViewModels implement `IPageLifecycleAware` for proper subscription management
- Ensure events are being published from the correct location (e.g., `GlobalMessageHandler`)

### IIS Deployment Issues

**DLL locked by IIS process**: The `deploy-iis.yml` workflow uses `-enableRule:AppOffline` which places an `app_offline.htm` file in the site root during deployment, causing IIS to unload the application domain and release file locks. If you still encounter locked file errors, verify the AppOffline rule is enabled in the msdeploy command.

**appsettings.json JSON parse error after deployment**: GitHub Secrets can contain trailing newlines that corrupt the JSON after token replacement. The workflow applies `.Trim()` to each secret and strips `\r` characters, but if the JSON is still invalid, check the secret values in GitHub Settings → Secrets for hidden whitespace or newlines.

**APK returns 404 on IIS**: IIS does not serve `.apk` files by default. The `build-maui.yml` workflow deploys a `web.config` alongside the APK that adds the MIME type (`application/vnd.android.package-archive`). If the APK is still 404, verify the `web.config` exists in the `/apk` directory on the server and that IIS is not overriding the `staticContent` configuration at the server level.

**msdeploy parameter issues**: When calling msdeploy from PowerShell, the `-dest` parameter must be passed as a single string (e.g., `"-dest:$destArg"`), not as a PowerShell array. Splitting the destination arguments into separate parameters causes msdeploy to misinterpret them. The workflow builds the full destination string and passes it as one argument.

---

**Content Note**: All operational procedures are based on the project's actual configuration and code structure. Verify file paths and commands against your current checkout.
