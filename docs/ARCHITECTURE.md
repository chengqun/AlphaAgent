# Architecture Overview

## System Design

AlphaAgent follows Clean Architecture principles with a three-tier system: a standalone core library (AlphaAgent.Core) containing business logic, market data, and an AI Agent system, an ABP Framework backend (AlphaAgent.Web) for social features via auto-generated REST APIs, and a cross-platform client (AlphaAgent.Maui) that connects the two. The MAUI client only references the Application layer, adhering to Clean Architecture/DDD principles.

```
┌──────────────────────────────────────────────────────────────────┐
│                     AlphaAgent.Maui                              │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │ SplashVM     │  │ LoginVM      │  │ ContactsVM            │ │
│  │ InitVM       │  │ RegisterVM   │  │ (Friends/Devices/      │ │
│  │              │  │ OAuth2 Login │  │  Groups/Stocks)        │ │
│  └──────────────┘  └──────┬───────┘  └───────────┬───────────┘ │
│                           │                       │             │
│  ┌──────────────┐  ┌──────┴───────────────────────┴──────┐     │
│  │ ChatDetailVM │  │    Application Layer Services        │     │
│  │ AgentChatVM  │  │  (IAuthService, ISecurityService,   │     │
│  │ MomentsVM    │  │   IRelationshipService, IGroupService,│     │
│  │ VideoVM      │  │   IMomentService, IChatService,      │     │
│         │          │   IAgentService, IMessageCacheService,│     │
│         │          │   IConversationSyncService,          │     │
│         │          │   IContactSyncService, IVideoFeedService,│  │
│         │          │   IAgentConfigService, IDeviceService, │   │
│         │          │   IUpdateService, IPostLoginInitializer,│  │
│         │          │   ISecurityClientSyncService)          │  │
│         │          └──────────────┬──────────────────────┘      │
│         │                         │                             │
│         │                         ▼                             │
│         │              ┌─────────────────────────────────┐      │
│         │              │      Domain Layer               │      │
│         │              │  (Entities: Security, Quote,    │      │
│         │              │   Token, MessageCacheItem,      │      │
│         │              │   AgentSession, AgentMessage,   │      │
│         │              │   AgentTask, SyncMetadata;      │      │
│         │              │   Services: SecurityManager,     │      │
│         │              │   TokenManager, AnalysisManager) │      │
│         │              └─────────────────────────────────┘      │
│         │                         │                             │
│         │                         ▼                             │
│  ┌──────┴──────────┐  ┌─────────────────────────────────┐      │
│  │ISignalRChatSvc  │  │    Infrastructure Layer          │      │
│  │   (SignalR)     │  │  (SQLite, HttpClient, Quotes,   │      │
│  └────────┬────────┘  │   LlmAgent, AgentFactory,       │      │
│           │            │   MessageCacheRepository)        │      │
│           │            └─────────────────────────────────┘      │
│           │                                                     │
│           │               ┌─────────────────────────────────┐  │
│           │               │  SQLite (alphaagent.db)         │  │
│           │               │  Securities | Quotes | Tokens   │  │
│           │               │  | MessageCache | ConvCache     │  │
│           │               │  | ContactCache | MomentCaches  │  │
│           │               │  | VideoFeeds | AgentSessions   │  │
│           │               │  | AgentMessages | AgentTasks   │  │
│           │               │  | AgentConfigCache | SyncMetadata│  │
│           │               └─────────────────────────────────┘  │
└───────────┼───────────────────────────────────────────────────┘
            │ WebSocket     │ HTTP (api/app/*, connect/token)
┌───────────┼───────────────┴───────────────────────────────────┐
│           │              AlphaAgent.Web                        │
│  ┌────────┴───────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │  ChatHub       │  │ ABP Auto-API     │  │ OpenIddict   │  │
│  │  (/hubs/chat)  │  │ (api/app/*)      │  │ (OAuth2)     │  │
│  └────────────────┘  └────────┬─────────┘  └──────────────┘  │
│                               │                               │
│  ┌────────────────────────────┴──────────────────────────┐    │
│  │              Application Layer                         │    │
│  │  ChatAppService │ RelationshipService │ GroupService   │    │
│  │  MomentService │ DeviceService │ SecurityService       │    │
│  └────────────────────────────┬──────────────────────────┘    │
│                               │                               │
│  ┌────────────────────────────┴──────────────────────────┐    │
│  │               Domain Layer                            │    │
│  │  ConversationManager │ FriendshipManager              │    │
│  │  DeviceRelationshipManager │ GroupRelationshipManager │    │
│  │  StockRelationshipManager │ DeviceManager             │    │
│  │  GroupManager │ MomentManager │ SecurityManager        │    │
│  └────────────────────────────┬──────────────────────────┘    │
│                               │                               │
│  ┌────────────────────────────┴──────────────────────────┐    │
│  │           EF Core + SQL Server                        │    │
│  │  AppConversations | AppConversationParticipants        │    │
│  │  AppChatMessages | AppSecurities | AppRelationships    │    │
│  │  AppGroups | AppDevices | AppMoments | AppMessages     │    │
│  └───────────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────────┘
```

## Technology Stack

**Core:**
- .NET 10, ABP Framework 10.3.0 (commercial), Autofac DI
- .NET MAUI with CommunityToolkit.Mvvm 8.4 + Syncfusion.Maui.Toolkit 1.0.9 + CommunityToolkit.Maui.MediaElement 9.0.0 (client UI)

**AI/Agent:**
- Microsoft.Extensions.AI 10.6.0 / Microsoft.Extensions.AI.OpenAI 10.6.0 — AI abstraction layer
- Microsoft.Agents.AI 1.5.0 / Microsoft.Agents.AI.OpenAI 1.5.0 — Agent orchestration (ChatClientAgent)
- OpenAI 2.10.0 — OpenAI-compatible chat client (used with DeepSeek endpoint)

**Data:**
- SQL Server via EF Core 10.x (backend persistence)
- SQLite via EF Core (client-side caching including message cache and agent data)
- Skender.Stock.Indicators 2.7.1 (technical analysis)

**Auth:**
- OpenIddict (OAuth2/OpenID Connect)
- Resource Owner Password Credentials + Refresh Token flows

**Infrastructure:**
- Serilog (structured logging), ABP Mapperly (hand-written object mapping), SignalR (real-time messaging)

## AlphaAgent.Core Architecture

AlphaAgent.Core follows Clean Architecture with four layers:

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Interfaces: IAuthService, ISecurityService,       │    │
│  │              IRelationshipService, IGroupService,   │    │
│  │              IMomentService, IChatService,          │    │
│  │              IAgentService, ICoreInitializer,       │    │
│  │              IMessageCacheService,                  │    │
│  │              IConversationSyncService,              │    │
│  │              IContactSyncService, IMomentCacheService│    │
│  │              IVideoFeedService, IAgentConfigService │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Services: AuthService, SecurityService,           │    │
│  │             RelationshipService, GroupService,     │    │
│  │             MomentService, ChatService,            │    │
│  │             AgentService, CoreInitializer,         │    │
│  │             MessageCacheService,                   │    │
│  │             ConversationSyncService,               │    │
│  │             ContactSyncService, MomentCacheService │    │
│  │             VideoFeedService, AgentConfigService   │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  DTOs: AccountInfoDto, ChatModels,                │    │
│  │        LoginModels, MomentDtos, RelationshipDtos, │    │
│  │        SecurityDto, AgentDtos, ApiResponse,       │    │
│  │        ContentPart, AgentStreamEvent hierarchy,    │    │
│  │        ContactBookDto, VideoItemDto                │    │
└───────────────────────────────┬─────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                          │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Entities: Security, Quote, Token, MessageCacheItem,│    │
│  │            ConversationCacheItem, ContactCacheItem, │    │
│  │            MomentCacheItem, VideoFeed,              │    │
│  │            AgentSession, AgentMessage, AgentTask,   │    │
│  │            AgentConfigCacheItem, SyncMetadata       │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Services: SecurityManager, TokenManager,           │    │
│  │            AnalysisManager                          │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Interfaces: IAgentRepository, IMessageCacheRepository,│    │
│  │              IConversationCacheRepository,          │    │
│  │              IContactCacheRepository,               │    │
│  │              IMomentCacheRepository, IVideoFeedRepository,│    │
│  │              IAgentConfigCacheRepository,           │    │
│  │              ISecurityRepository, IQuoteRepository, │    │
│  │              ITokenRepository, ISecurityManager,    │    │
│  │              IAnalysisManager, IFailoverQuoteProvider,│    │
│  │              IIndicatorCalculator, ITokenManager    │    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────────────┬─────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│              Domain.Abstractions Layer                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Agents: IAgent, IAgentFactory,                    │    │
│  │          ToolInfo, AgentInfo,                      │    │
│  │          AgentContext, ChatMessage, AgentResponse,  │    │
│  │          ToolCall, ToolResult, AgentResponseChunk, │    │
│  │          AgentSessionStatus                        │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Interfaces: IDatabaseInitializer,                │    │
│  │              ISignalRChatService, IHttpClientService│    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  DTOs: ChatMessage (chat DTO)                     │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Config: AgentOptions (ModelName, ApiKey,          │    │
│  │          Endpoint, SystemPrompt, Temperature)      │    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────────────┬─────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Data: SharesDbContext, Repositories                │    │
│  │       (AgentRepository, MessageCacheRepository,     │    │
│  │        ConversationCacheRepository,                 │    │
│  │        ContactCacheRepository, MomentCacheRepository,│    │
│  │        VideoFeedRepository, AgentConfigCacheRepository,│    │
│  │        SecurityRepository, QuoteRepository,         │    │
│  │        TokenRepository, SyncMetadataStore)          │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  AI: LlmAgent, AgentFactory, StockAnalystAgent,    │    │
│  │      StockAnalystNoMemoryAgent,                    │    │
│  │      TechnicalAnalysisTool, SecurityQueryTool,     │    │
│  │      IChatClient (OpenAI)                          │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │  Services: HttpClientService, IndicatorCalculator,  │    │
│  │            BearerTokenDelegatingHandler,             │    │
│  │            FailoverQuoteProvider, SignalRChatService,│    │
│  │            Quote Providers (Sina, Baidu, EastMoney),│    │
│  │            DatabaseInitializer, VideoFeedData       │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rules:**
- Domain.Abstractions → nothing (root of the dependency graph)
- Domain → Domain.Abstractions only
- Application → Domain + Domain.Abstractions only
- Infrastructure → Domain + Domain.Abstractions only
- UI (MAUI) → Application only

## Custom Business Logic

### Agent System

**Purpose**: Provide an AI Agent framework with streaming tool-use capabilities for stock analysis and other financial tasks, with session-based conversation management and SQLite persistence.

**Implementation**: The Agent system follows Clean Architecture across all four layers, using `Microsoft.Extensions.AI` abstractions for LLM interaction:

- **Domain.Abstractions**: `IAgent` defines the agent contract with `RunAsync()` and `RunStreamingAsync()` methods. `AgentMemoryMode` enum (Stateful, Stateless, SlidingWindow) controls history behavior. `IAgentFactory` provides named agent resolution and discovery. Agent models (`AgentContext`, `ChatMessage`, `AgentResponse`, `ToolCall`, `AgentResponseChunk`, `AgentSessionStatus`, `AgentInfo`, `ToolInfo`) live here as pure abstractions with zero dependencies. `AgentOptions` configures LLM connection (ModelName, ApiKey, Endpoint, DefaultSystemPrompt, Temperature). Infrastructure interfaces (`ISignalRChatService`, `IHttpClientService`) and chat DTOs (`ChatMessage`) also live here to satisfy the Dependency Inversion Principle.
- **Domain**: `AgentSession` (aggregate with `UserId`, `AgentName`, `Context`, `Status`, `Messages` collection), `AgentMessage` (with `AgentMessageRole` enum: User, Assistant, System, Tool), `AgentTask` (with lifecycle: Pending → Running → Completed/Failed). `IAgentRepository` provides session/message/task CRUD. `GetActiveSessionAsync` queries by both `UserId` and `AgentName` for session isolation (excluding sessions with non-empty Context). `GetActiveSessionByContextAsync(userId, agentName, context)` enables per-stock session isolation where Context = `"stock:{stockId}:{stockName}"`.
- **Application**: `IAgentService`/`AgentService` orchestrates sessions — `StartSessionAsync` (with optional `initialContext`), `SendMessageAsync`, `SendMessageStreamingAsync`, `GetSessionHistoryAsync`, `GetAvailableAgentsAsync`, `CloseSessionAsync`. All memory modes persist messages for display; `BuildChatHistory` controls what's sent to the LLM (Stateless returns empty, SlidingWindow returns last N, Stateful returns all). DTOs: `AgentSessionDto`, `AgentResponseDto`, `ToolCallDto`, `AgentChatMessageDto`, `AgentInfoDto`, `ToolInfoDto`. `ContentPart` tracks interleaved content order for correct history replay. `AgentStreamEvent` hierarchy (`AgentTextEvent`, `AgentToolCallEvent`, `AgentToolResultEvent`) provides typed stream discrimination.
- **Infrastructure**: `LlmAgent` wraps `ChatClientAgent` from `Microsoft.Agents.AI`, delegating both sync and streaming execution. `AgentFactory` holds named `AgentRegistration` entries for agent resolution. `StockAnalystAgent` (SlidingWindow memory, default 20 messages) and `StockAnalystNoMemoryAgent` (Stateless, same tools but no LLM history) are static factory classes that create `LlmAgent` instances with `AIFunctionFactory.Create()` tools. `TechnicalAnalysisTool` exposes `CalculateIndicators()` and `SecurityQueryTool` exposes `QuerySecurity()` as AI functions. Tool names are centralized in `ToolNames` constants. `AgentRepository` persists sessions/messages/tasks to SQLite via `SharesDbContext`.

**Agent Execution Flow**:
```
User sends message
  → IAgentService.SendMessageAsync(sessionId, message)
  or IAgentService.SendMessageStreamingAsync(sessionId, message)
    → AgentService persists user message via IAgentRepository
    → AgentService resolves IAgent via IAgentFactory.GetAgent(agentName)
    → agent.RunAsync(context) or agent.RunStreamingAsync(context)
      → LlmAgent delegates to ChatClientAgent
        → ChatClientAgent invokes LLM via IChatClient
        → LLM may call tools via AIFunctionFactory.Create() functions
          → TechnicalAnalysisTool.CalculateIndicators()
          → SecurityQueryTool.QuerySecurity()
        → Tool results fed back to LLM for final response
      → AgentResponse or IAsyncEnumerable<AgentResponseChunk> returned
    → AgentService persists assistant message (with ContentPartsJson for interleaved order)
    → Response returned to caller
```

**Streaming Architecture**:
- `IAgent.RunStreamingAsync()` yields `AgentResponseChunk` objects — text chunks, tool call events, and tool result events
- `AgentService.SendMessageStreamingAsync()` builds `ContentPart` list incrementally, tracking interleaved order (text → tool_call → tool_result → text → ...)
- `ContentPart` stored as JSON in `AgentMessage.ContentPartsJson` for correct history replay when loading sessions
- MAUI `AgentChatDetailViewModel` renders streaming via `ChatMessageItem` with `ItemType` field ("text", "tool_call", "tool_result", "thinking") for UI template selection
- MAUI streaming: `IsStreaming=true` uses Label for lightweight text display; `IsStreaming=false` switches to MarkdownView for rich rendering. `ChatMessageItem.MarkdownContent` is set from `Content` when streaming ends, avoiding MarkdownView re-parse lag

**Agent Registration Pattern**:
- `AgentFactory.Register(name, description, defaultSystemPrompt, factory)` stores metadata alongside the factory delegate
- `GetAvailableAgents()` tries to instantiate agents for full metadata (including Tools); falls back to registration metadata (Name, Description, DefaultSystemPrompt) if instantiation fails (e.g., ApiKey not yet loaded)
- Currently registered: `StockAnalystAgent` (SlidingWindow, with tools), `StockAnalystNoMemoryAgent` (Stateless, same tools)
- New agents are added by: (1) creating a static factory class, (2) registering it in `RegisterAgentServices()` — the system auto-creates a skeleton config on the server via `EnsureDefaultConfigsAsync`
- `IChatClient` is created per-agent invocation using the current `AgentOptions` singleton (allows runtime config updates after login)

**Agent Tool Selection**: Each agent supports configurable tool enablement. `AgentOptions.EnabledTools` (Dictionary<string, List<string>>) maps agent names to lists of enabled tool names — null or key not present means all tools (default), empty list means no tools, non-empty list means only those tools. `StockAnalystAgent.Create()` and `StockAnalystNoMemoryAgent.Create()` accept an `enabledTools` parameter that filters the available `AITool` set. Tool selections are persisted per-agent in `AgentConfigCacheItem.EnabledTools` (SQLite JSON column) and synchronized with server config. The MAUI `AgentContactDetailViewModel` provides Switch toggles for tool enable/disable. `PostLoginInitializer.ApplyAgentConfigs()` applies saved tool selections to `AgentOptions` at login. Tool names are centralized in `ToolNames` constants (`CalculateIndicators`, `QuerySecurity`).

**Agent Memory Modes (`AgentMemoryMode`)**:
- `Stateful` (default): Full memory — all history sent to LLM, messages persisted
- `SlidingWindow`: Only last N messages sent to LLM (`MaxHistoryMessages`, default 20), messages persisted
- `Stateless`: No history sent to LLM (each call is independent), messages still persisted for chat history display
- Key distinction: all modes persist messages to SQLite for display; `BuildChatHistory` controls what's sent to the LLM

**Agent Session Isolation**:
- `GetActiveSessionAsync(userId, agentName)` queries by both `UserId` and `AgentName`, excluding sessions with non-empty `Context`
- `GetActiveSessionByContextAsync(userId, agentName, context)` queries by `UserId`, `AgentName`, and `Context` for per-stock sessions where Context = `"stock:{stockId}:{stockName}"`
- `StartSessionAsync` accepts optional `initialContext` to set Context on session creation
- Messages are stored per `SessionId`, and each `AgentSession` is created for a specific `AgentName` and optional `Context`
- MAUI `AgentChatDetailViewModel.ApplyQueryAttributes` detects `AgentName` changes and resets `_isLoaded`/`_currentSessionId`/`Messages` to force re-initialization. `_streamVersion` invalidates stale streaming operations when switching sessions.

**SQLite Tables**: `AgentSessions`, `AgentMessages`, `AgentTasks` in `alphaagent.db`.

**Key files**:
- [IAgent.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/AiAgent/IAgent.cs)
- [IAgentFactory.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/AiAgent/IAgentFactory.cs)
- [Models.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/AiAgent/Models.cs)
- [AgentSession.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/AgentSession.cs)
- [AgentMessage.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/AgentMessage.cs)
- [AgentTask.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/AgentTask.cs)
- [IAgentRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Interfaces/IAgentRepository.cs)
- [IAgentService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Agent/IAgentService.cs)
- [AgentService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Agent/AgentService.cs)
- [AgentDtos.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Dtos/Agent/AgentDtos.cs)
- [AgentOptions.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/AiAgent/AgentOptions.cs)
- [LlmAgent.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/LlmAgent.cs)
- [AgentFactory.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/AgentFactory.cs)
- [StockAnalystAgent.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/Agents/StockAnalystAgent.cs)
- [StockAnalystNoMemoryAgent.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/Agents/StockAnalystNoMemoryAgent.cs)
- [TechnicalAnalysisTool.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/Tools/TechnicalAnalysisTool.cs)
- [SecurityQueryTool.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/AiAgent/Tools/SecurityQueryTool.cs)
- [ToolNames.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/AiAgent/ToolNames.cs)
- [AgentRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Data/Repositories/AgentRepository.cs)

### Failover Quote Provider

**Purpose**: Chinese financial data APIs are unreliable individually. This system provides resilient quote fetching by distributing requests across multiple providers.

**Implementation**: `FailoverQuoteProvider` receives all registered `IQuoteProvider` implementations, filters by `IsSupported()` for the requested parameters, then randomly shuffles the supported sources using Fisher-Yates shuffle. It iterates through the shuffled list, returning the first successful result. On `HttpRequestException` or any other exception, it logs the error and tries the next source.

**Three data sources**:

| Provider | Supported Markets | API |
|---|---|---|
| EastQuoteProvider | Stocks, Futures (101 freq), Index | `push2his.eastmoney.com` |
| BaiduQuoteProvider | Exchange rates, Index, Futures, Stocks, US Index, Crypto (day only) | `finance.pae.baidu.com` |
| SinaQuoteProvider | Futures only | `stock2.finance.sina.com.cn` |

**Key files**:
- [FailoverQuoteProvider.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/Quotes/FailoverQuoteProvider.cs)
- [IQuoteProvider.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Interfaces/IQuoteProvider.cs)
- [ServiceCollectionExtensions.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Extensions/ServiceCollectionExtensions.cs)

### Unified Relationship System

**Purpose**: A single entity and table handles four conceptually different relationship types (friendships, device bindings, group memberships, stock watchlists) with type-specific authorization logic.

**Implementation**: `AppRelationship` (aggregate root with soft delete) has `UserId`, `TargetType`, `TargetId`, and `Status` fields. The `RelationshipService` application service injects all four `IRelationshipManager<,,>` implementations and dispatches by `RelationshipType` using switch expressions.

**Authorization differences**:

| Type | Create | Accept | Reject | Remove |
|---|---|---|---|---|
| **Friendship** | Always Pending | Target user accepts; creates reverse relationship | Sets Rejected | Deletes both directions |
| **Device** | Auto-Accept if own device; else Pending | Device owner accepts | Sets Rejected | Owner or creator only |
| **Group** | Auto-Accept if group owner; else Pending | Group owner accepts | Sets Rejected | Owner or creator only |
| **Stock** | Always Accepted (watchlist) | Creator auto-accepts | Deletes entirely | Simple delete |

**Key files**:
- [AppRelationship.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppRelationship.cs)
- [IRelationshipManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Relationships/IRelationshipManager.cs)
- [FriendshipManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Relationships/FriendshipManager.cs)
- [RelationshipService.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Application/Services/Relationships/RelationshipService.cs)

### Moments Social Feed

**Purpose**: A multi-type social feed supporting user posts, stock-related moments, device moments, and group moments — inspired by WeChat's Moments (朋友圈).

**Implementation**: `AppMoment` stores all moment types in a single table with a `Type` discriminator (`"User"`, `"Stock"`, `"Device"`, `"Group"`) and a `Visibility` field (`"Friends"` or `"Public"`). The core challenge is that `UserId` is a `Guid` column, but stocks use integer IDs and devices/groups use string IDs. The system solves this with deterministic GUID conversion:

- **Stock ID → Guid**: Integer is formatted as 8-char uppercase hex, left-padded to 32 chars, formatted as a Guid. Reversible — the first 8 hex characters decode back to the integer stock ID. Example: stock ID 1 → `00000001-0000-0000-0000-000000000000`.
- **String ID → Guid**: UTF-8 bytes are SHA-256 hashed, first 16 bytes form the Guid. Not reversible — display names are hardcoded ("设备" for devices, "群组" for groups).

**Feed logic** (`GetFriendsMomentsAsync`): Merges two feeds — (1) moments by accepted friends with `"Friends"` visibility, and (2) stock moments for stocks the user follows. Results are ordered by `CreatedAt` descending with pagination.

**Targeted feed** (`GetMomentsAsync`): Smart routing by `type` parameter — parses `targetId` as Guid for users, as int or stock code for stocks, and applies `CreateTargetGuid` for devices/groups.

**API surface**: `CreateMomentAsync`, `CreateStockMomentAsync`, `CreateDeviceMomentAsync`, `CreateGroupMomentAsync`, `GetFriendsMomentsAsync`, `GetMomentsAsync`, `GetMomentAsync`, `DeleteMomentAsync` (owner-only).

**Key files**:
- [AppMoment.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppMoment.cs)
- [IMomentManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Moment/IMomentManager.cs)
- [MomentManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Moment/MomentManager.cs)
- [MomentAppService.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Application/Services/Moment/MomentAppService.cs)

### Device Management

**Purpose**: Device registration and authorization system with per-user device limits.

**Implementation**: `AppDevice` stores `DeviceId`, `DeviceName`, `DeviceType`, `AuthorizationCode`, `IsSearchable`, and `UserId`. The `DeviceManager` auto-generates a GUID-based authorization code on creation and auto-creates an Accepted Device relationship. The `DeviceAppService` supports device lookup by authorization code for external device authentication flows. `IsSearchable` controls whether other users can discover and initiate conversations with the device.

**Key files**:
- [AppDevice.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppDevice.cs)
- [IDeviceManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Devices/IDeviceManager.cs)
- [DeviceManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Devices/DeviceManager.cs)
- [DeviceAppService.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Application/Services/Devices/DeviceAppService.cs)

### Real-Time Chat System

**Purpose**: Multi-party real-time messaging supporting user-to-user, user-to-device, and group conversations via SignalR.

**Implementation**: The chat system uses a dual-protocol approach — REST for history/CRUD operations (`api/app/chat/*`) and SignalR (`/hubs/chat`) for real-time message delivery. Three entity types form the persistence layer: `AppConversation` (aggregate root with `ConversationType` enum: Direct, Group), `AppConversationParticipant` (tracks per-user unread counts and roles), and `AppChatMessage` (individual messages). The `ConversationManager` domain service provides idempotent get-or-create semantics using deterministic conversation keys — sorted GUID pairs for direct chats, `"device_{deviceId}"` for device conversations, and groupId for group conversations.

**Dual authentication in ChatHub**: JWT `sub` claim for human users, `authorizationCode` query parameter for devices (validated via `IDeviceManager`). On connection, the hub resolves identity, loads all conversations, and subscribes to per-conversation SignalR groups (`conv_{conversationId}`). `SignalRQueryTokenMiddleware` extracts tokens from query parameters and injects them into request headers for WebSocket authentication.

**Client-side architecture**: MAUI uses `ISignalRChatService` (Singleton, wraps SignalR client) for real-time connectivity and Application layer services for REST calls. `ChatViewModel` uses cache-first loading — loads from local SQLite cache first (instant display), then syncs from server in background via `IConversationSyncService`. Agent conversations (Type 3/4) loaded separately via `IAgentRepository`. `ChatDetailViewModel` uses `IMessageCacheService` for local message caching, loads cached messages first, then syncs from server only if no cache exists. Incremental insertion prevents UI flicker. ViewModels implement `IPageLifecycleAware` for proper event subscription/unsubscription.

**Self-signed certificate handling**: Android 7+ and iOS require custom handling for self-signed server certificates. `CustomCertificateHandler` loads the embedded `rootCA.crt` from `Resources/raw/` and configures `SocketsHttpHandler.SslOptions` with `X509ChainTrustMode.CustomRootTrust`. Both `HttpClient` (via `ConfigurePrimaryHttpMessageHandler`) and SignalR `HubConnection` (via `HttpMessageHandlerFactory`) use this handler. On Android, `network_security_config.xml` (referenced from AndroidManifest) additionally trusts user CA certificates for the server IP at the native layer. Server address is centralized in `AppSettings.ServerBaseAddress`.

**API surface (REST)**: `GetOrCreateDirectConversation`, `GetOrCreateGroupConversation`, `GetOrCreateDeviceConversation`, `GetMyConversations`, `GetMessages`, `GetUnreadMessagesWithMark` (fetches + marks read in one call), `SendMessage`, `MarkAsRead`, `DeleteConversation`, `GetUnreadCount`.

**Key files**:
- [ChatHub.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi/Hubs/ChatHub.cs)
- [SignalRQueryTokenMiddleware.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.HttpApi/Services/SignalRQueryTokenMiddleware.cs)
- [AppConversation.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppConversation.cs)
- [AppConversationParticipant.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppConversationParticipant.cs)
- [AppChatMessage.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppChatMessage.cs)
- [ConversationManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Chat/ConversationManager.cs)
- [ChatAppService.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Application/Services/Chat/ChatAppService.cs)
- [ISignalRChatService.cs](../src/AlphaAgent.Core/AlphaAgent.Domain.Abstractions/Interfaces/ISignalRChatService.cs)
- [SignalRChatService.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/Chat/SignalRChatService.cs)
- [ChatDetailViewModel.cs](../src/AlphaAgent.Maui/ViewModels/ChatDetailViewModel.cs)
- [CustomCertificateHandler.cs](../src/AlphaAgent.Maui/Services/CustomCertificateHandler.cs)
- [AppSettings.cs](../src/AlphaAgent.Maui/Services/AppSettings.cs)

### Group Management

**Purpose**: Groups as a first-class entity beyond the relationship system, with admin-only creation and owner-based member management.

**Implementation**: `AppGroup` is a `FullAuditedAggregateRoot` with `Name`, `Description`, and `OwnerId`. The `GroupManager` auto-creates an owner relationship on group creation. `GroupAppService` enforces ownership checks (`EnsureOwnerAsync`, `EnsureAdminOrOwnerAsync`, `EnsureMemberAsync`) and provides: `CreateGroup` (admin-only), `GetMyGroups`, `SearchGroups`, `GetGroupMembers`, `AddMember`, `RemoveMember`, `DisbandGroup`.

**Key files**:
- [AppGroup.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Entities/AppGroup.cs)
- [IGroupManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Groups/IGroupManager.cs)
- [GroupManager.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Domain/Services/Groups/GroupManager.cs)
- [GroupAppService.cs](../src/AlphaAgent.Web/AlphaAgent.Abp.Application/Services/Groups/GroupAppService.cs)

### Technical Indicator Calculator

**Purpose**: Calculate configurable technical indicators on quote data for charting and analysis.

**Implementation**: `IndicatorCalculator` converts domain `Quote` objects to `Skender.Stock.Indicators.IQuote` via an adapter, then calculates each requested indicator. Supports parameterized indicators (e.g., `SMA(20)`, `MACD(12,26,9)`) and outputs CSV with OHLCV columns.

**8 indicators with default parameters**:

| Indicator | Default Params | Output |
|---|---|---|
| SMA | Period=20 | `SMA(20)` |
| EMA | Period=20 | `EMA(20)` |
| RSI | Period=14 | `RSI(14)` |
| MACD | Fast=12, Slow=26, Signal=9 | `MACD`, `MACD_Signal`, `MACD_Histogram` |
| BB | Period=20, StdDev=2.0 | `BB_Upper`, `BB_Middle`, `BB_Lower` |
| SAR | AF=0.02, AFMax=0.2 | `SAR(0.02,0.2)` |
| KDJ | Period=9, Signal=3, Smooth=3 | `KDJ_K`, `KDJ_D`, `KDJ_J` (J = 3K - 2D) |
| ADX | Period=14 | `ADX(14)` |

**Key files**:
- [IndicatorCalculator.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Services/Indicators/IndicatorCalculator.cs)
- [IIndicatorCalculator.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Services/Security/IIndicatorCalculator.cs)

### Hybrid Message Cache System

**Purpose**: Reduce network requests and improve chat page loading performance by caching message history locally.

**Implementation**: The cache system uses a hybrid approach:
- **Memory Cache**: `ConcurrentDictionary<Guid, string>` for fast in-memory access
- **SQLite Persistence**: `MessageCacheItem` entity for offline storage and cross-session persistence

**Cache Flow**:
1. **Open Chat**: `LoadMessagesAsync()` checks memory cache first, then SQLite, and falls back to network if both are empty
2. **Receive Message**: Update memory cache synchronously, then async write to SQLite
3. **Append Message**: Update cache incrementally without rewriting the entire conversation
4. **Cache Expiration**: 5-minute TTL, configurable via `_cacheExpirationMinutes`
5. **Message Limit**: Maximum 50 messages per conversation to prevent memory bloat

**Cache Service Architecture**:
```
MAUI ViewModel
      │
      ▼
IMessageCacheService (Application Layer)
      │
      ▼
MessageCacheService (Application Layer - JSON serialization)
      │
      ▼
IMessageCacheRepository (Domain Layer - interface)
      │
      ▼
MessageCacheRepository (Infrastructure Layer - hybrid caching)
```

**Key files**:
- [MessageCacheItem.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/MessageCacheItem.cs)
- [IMessageCacheRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Interfaces/IMessageCacheRepository.cs)
- [IMessageCacheService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Chat/IMessageCacheService.cs)
- [MessageCacheService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Chat/MessageCacheService.cs)
- [MessageCacheRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Data/Repositories/MessageCacheRepository.cs)
- [SharesDbContext.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Data/SharesDbContext.cs)

### Event Bus System

**Purpose**: Enable cross-page communication for real-time updates without direct dependencies between components.

**Implementation**: `EventBusService` provides a simple publish/subscribe pattern with generic event handling.

**Events**:
- **NewMessageEvent**: Published when a new message is received via SignalR
- **NewConversationEvent**: Published when a new conversation is created (from Agent chat, ChatDetail, etc.)
- **ConversationReadEvent**: Published when a conversation is opened/marked as read
- **UnreadCountUpdatedEvent**: Published when unread counts change
- **ContactChangedEvent**: Published when relationships change (Action: "added", "accepted", "deleted"), triggers contact re-sync

**Global Message Handler**: `GlobalMessageHandler` subscribes to SignalR messages on app startup and publishes events to the bus. ViewModels can subscribe to events when they appear and unsubscribe when they disappear (using `IPageLifecycleAware`).

**Key files**:
- [EventBusService.cs](../src/AlphaAgent.Maui/Services/EventBusService.cs)
- [GlobalMessageHandler.cs](../src/AlphaAgent.Maui/Services/GlobalMessageHandler.cs)
- [NewMessageEvent.cs](../src/AlphaAgent.Maui/Events/NewMessageEvent.cs)
- [NewConversationEvent.cs](../src/AlphaAgent.Maui/Events/NewConversationEvent.cs)
- [ConversationReadEvent.cs](../src/AlphaAgent.Maui/Events/ConversationReadEvent.cs)
- [UnreadCountUpdatedEvent.cs](../src/AlphaAgent.Maui/Events/UnreadCountUpdatedEvent.cs)
- [ContactChangedEvent.cs](../src/AlphaAgent.Maui/Events/ContactChangedEvent.cs)

### Sync Services and Cache-First Loading

**Purpose**: Provide instant UI rendering by loading from local SQLite cache first, then syncing from server in the background. Prevents UI flicker through incremental updates.

**Implementation**: Two sync services manage local caches that mirror server data:

- **ConversationSyncService** (`IConversationSyncService`): Methods: `GetCachedConversationsAsync(userId)`, `SyncFromServerAsync(userId)`, `UpsertConversationAsync(conversation, userId)`, `DeleteConversationAsync(conversationId)`. Filters out Agent conversations (Type 3/4) since they have independent storage. Deletes stale local cache entries that no longer exist on the server. Falls back to cache on network error.

- **ContactSyncService** (`IContactSyncService`): Methods: `GetCachedContactsAsync(userId)`, `SyncFromServerAsync(userId)`. Uses full-replace strategy (delete all then upsert) for contacts. Aggregates Friends, Groups, Devices, and Stocks into a single `ContactBookDto`.

- **MomentCacheService** (`IMomentCacheService`): Methods: `GetCachedMomentsAsync()`, `GetLatestCachedCreatedAtAsync()`, `UpdateCacheAsync()`, `AddMomentAsync()`, `ClearCacheAsync()`. Supports incremental loading via `GetLatestCachedCreatedAtAsync()`.

- **AgentConfigService** (`IAgentConfigService`): Methods: `GetCachedConfigsAsync(userId)`, `SyncFromServerAsync(userId)`, `EnsureDefaultConfigsAsync(userId, existingConfigs)`, `SetConfigAsync(config)`. Cache-first with server sync and automatic skeleton creation for newly registered agents. `EnsureDefaultConfigsAsync` compares `IAgentFactory.GetAvailableAgents()` against existing server configs, creates skeleton entries (AgentName + DefaultSystemPrompt, empty ApiKey) for missing agents, then re-syncs. Users must fill ApiKey via Blazor management UI before agents can call LLMs.

- **SecurityClientSyncService** (`ISecurityClientSyncService`): Client-side incremental sync of security data from server to local SQLite. Uses `ISyncMetadataStore` (keyed by sync type, e.g., `"SecurityLastSyncTime"`) to track last sync timestamp. `SyncFromServerAsync()` fetches incremental updates from `api/app/security-client-sync/updates?after={lastSyncTime}`. Falls back to full sync if no local data exists. Server-side `SecurityClientSyncService` exposes `GetUpdatesAsync(after?)` API for incremental delivery.

**Cache-First Loading Pattern in ViewModels**:
1. `OnAppearingAsync` loads from local cache first (instant display)
2. `SyncInBackgroundAsync()` syncs from server with 30-second throttle (`_minSyncInterval`)
3. `SortConversations()` / `UpdateContactBookIfNeeded()` only reorders the collection when the order actually changed, to prevent UI flicker
4. Agent conversations (Type 3/4) loaded separately via `IAgentRepository` in `ChatViewModel`

**Cache Entities** (SQLite tables):
- `ConversationCacheItem` — Id, Type, Name, OtherUserName, OtherUserId, OtherDeviceId, DeviceType, UnreadCount, LastMessage, LastMessageTime, MemberCount, Context, CachedAt, UserId. Indexes: UserId, (UserId, LastMessageTime).
- `ContactCacheItem` — Id, Type, TargetId, TargetName, DeviceType, Status, CachedAt, UserId. Indexes: UserId, (UserId, Type).
- `MomentCacheItem` — Id, UserId, Username, Content, ImageUrl, CreatedAt, Type, Visibility.
- `AgentConfigCacheItem` — Id, UserId, AgentName, ModelName, ApiKey, Endpoint, DefaultSystemPrompt, Temperature, EnabledTools (JSON string: Dictionary<string, List<string>>), IsActive, CachedAt. Indexes: UserId, (UserId, AgentName, IsActive).

**SQLite New Table Migration**: `DatabaseInitializer.EnsureNewTablesAsync()` uses `CREATE TABLE IF NOT EXISTS` for adding new tables to existing databases, since `EnsureCreatedAsync` only creates tables that didn't exist at initial database creation.

**Key files**:
- [IConversationSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Chat/IConversationSyncService.cs)
- [ConversationSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Chat/ConversationSyncService.cs)
- [IContactSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Relationship/IContactSyncService.cs)
- [ContactSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Relationship/ContactSyncService.cs)
- [IAgentConfigService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Agent/IAgentConfigService.cs)
- [AgentConfigService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Agent/AgentConfigService.cs)
- [IMomentCacheService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Moment/IMomentCacheService.cs)
- [MomentCacheService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Moment/MomentCacheService.cs)
- [IAgentConfigCacheRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Interfaces/IAgentConfigCacheRepository.cs)
- [AgentConfigCacheRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Data/Repositories/AgentConfigCacheRepository.cs)
- [ISecurityClientSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Security/ISecurityClientSyncService.cs)
- [SecurityClientSyncService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Security/SecurityClientSyncService.cs)
- [ConversationCacheItem.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/ConversationCacheItem.cs)
- [ContactCacheItem.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/ContactCacheItem.cs)
- [MomentCacheItem.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/MomentCacheItem.cs)
- [AgentConfigCacheItem.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/AgentConfigCacheItem.cs)
- [ContactBookDto.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Dtos/Relationship/ContactBookDto.cs)
- [ChatViewModel.cs](../src/AlphaAgent.Maui/ViewModels/ChatViewModel.cs)
- [ContactsViewModel.cs](../src/AlphaAgent.Maui/ViewModels/ContactsViewModel.cs)
- [ChatDetailViewModel.cs](../src/AlphaAgent.Maui/ViewModels/ChatDetailViewModel.cs)
- [SplashViewModel.cs](../src/AlphaAgent.Maui/ViewModels/SplashViewModel.cs)

### Video Feed Module

**Purpose**: Video browsing with vertical swiping, paginated loading, and deduplication.

**Implementation**: Complete subsystem across all Core layers and MAUI client:

- **Domain**: `VideoFeed` entity (Id, Title, VideoUrl, CoverUrl, Author, Duration, CreatedAt) with private constructor and domain validation. `IVideoFeedRepository` interface.
- **Application**: `IVideoFeedService`/`VideoFeedService` with paginated query support. `VideoItemDto` DTO.
- **Infrastructure**: `VideoFeedRepository` implementation. `VideoFeedData` seed data (populated if VideoFeeds table is empty).
- **MAUI**: `VideoChannelsViewModel` with vertical swiping (NextVideo/PreviousVideo), paginated loading with offset, deduplication via `_displayedVideoIds` HashSet. `VideoChannelsPage` uses `CommunityToolkit.Maui.MediaElement` for video playback (Runtime inflation, not SourceGen, since MediaElement is not compatible with XAML SourceGen).

**Key files**:
- [VideoFeed.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Entities/VideoFeed.cs)
- [IVideoFeedRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Domain/Interfaces/IVideoFeedRepository.cs)
- [IVideoFeedService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Interfaces/Video/IVideoFeedService.cs)
- [VideoFeedService.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Services/Video/VideoFeedService.cs)
- [VideoItemDto.cs](../src/AlphaAgent.Core/AlphaAgent.Application/Dtos/Video/VideoItemDto.cs)
- [VideoFeedRepository.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/Data/Repositories/VideoFeedRepository.cs)
- [VideoFeedData.cs](../src/AlphaAgent.Core/AlphaAgent.Infrastructure/InitData/VideoFeedData.cs)
- [VideoChannelsViewModel.cs](../src/AlphaAgent.Maui/ViewModels/VideoChannelsViewModel.cs)
- [VideoChannelsPage.xaml](../src/AlphaAgent.Maui/Views/VideoChannelsPage.xaml)

## Integration Patterns

### OAuth2 Authentication Flow

The MAUI client authenticates via Resource Owner Password Credentials grant against the Web backend's OpenIddict server:

1. User submits username/password to `LoginViewModel`
2. `AuthService.LoginAsync()` sends `POST connect/token` with `grant_type=password`, `client_id=alphaagent_chat`, `scope=offline_access Abp alphaagent_chat`
3. On success, tokens are persisted to SQLite via `TokenRepository`
4. `BearerTokenDelegatingHandler` automatically attaches the Bearer token to all requests (skipping `connect/` and `api/account/register` endpoints)
5. If the token is expired, `RefreshTokenAsync()` calls `connect/token` with `grant_type=refresh_token`
6. On logout, all token records are deleted from SQLite
7. On app startup, `SplashViewModel` attempts auto-login via stored refresh token, initialized via `ICoreInitializer.InitializeAsync()`

### Post-Login Initialization Flow

After successful login, `IPostLoginInitializer`/`PostLoginInitializer` orchestrates a 3-step initialization with `IProgress<PostLoginProgress>` for visual feedback:

1. **Connect SignalR** — `ISignalRChatService.ConnectAsync(accessToken, serverBaseAddress)` establishes WebSocket connection for real-time messaging
2. **Load Agent config** — `IAgentConfigService.SyncFromServerAsync(userId)` fetches per-user LLM configs from server, then `EnsureDefaultConfigsAsync()` creates skeleton entries for new agents, then `ApplyAgentConfigs()` applies configs (including `EnabledTools`) to the in-memory `AgentOptions`
3. **Sync securities** — `ISecurityClientSyncService.SyncFromServerAsync()` performs incremental sync of security data from server to local SQLite

MAUI `InitializingViewModel` displays step-by-step progress with spinner/checkmark/X icons. On failure, the user is still navigated to the main app (non-blocking initialization).

### Real-Time Chat Flow

The MAUI client uses a dual-protocol approach for messaging:

1. **Conversation list**: `ChatViewModel` loads from local cache first via `IConversationSyncService.GetCachedConversationsAsync()`, then syncs from server in background. Agent conversations (Type 3/4) loaded separately via `IAgentRepository`.
2. **Open chat**: User selects conversation → navigates to `ChatDetailPage` with query params (conversationId, name, type)
3. **Load history**: `ChatDetailViewModel.InitializeAsync()` → Load cached messages from `IMessageCacheService` → Sync from server only if no cache exists
4. **Connect SignalR**: `ISignalRChatService.ConnectAsync(accessToken, baseUrl)` → WebSocket to `/hubs/chat`, joins conversation group `conv_{id}`
5. **Send message**: `IChatService.SendMessageAsync(conversationId, content)` → SignalR hub method `SendMessage`
6. **Receive message**: `ISignalRChatService.OnMessageReceived` event → Update UI and local cache → Publish `NewMessageEvent`
7. **Pull to refresh**: `RefreshMessagesAsync()` fetches incremental unread messages via REST, saves to local cache

For devices (`AlphaAgent.ConsoleDevice`):
1. Connects to `/hubs/chat?authorizationCode={code}` (no JWT)
2. Hub resolves device identity via `IDeviceManager`
3. Device receives messages and can process commands via the Agent system

### Agent Chat Flow

The MAUI client provides an Agent chat interface with streaming support:

1. **Agent list**: `AgentContactDetailViewModel` displays available agents and tools via `IAgentService.GetAvailableAgentsAsync()`
2. **Start session**: Navigate to `AgentChatDetailPage` → `IAgentService.StartSessionAsync(agentName, initialContext)` — optional `initialContext` for stock sessions (`"stock:{stockId}:{stockName}"`)
3. **Send message**: `IAgentService.SendMessageStreamingAsync(sessionId, message)` → Agent streams results via `LlmAgent` → ContentParts built incrementally. `_streamVersion` invalidates stale operations when switching sessions.
4. **Stock mode**: Navigate with `stockId` + `stockName` → `IsStockMode=true` → auto-queries stock on new session via `AutoQueryStockAsync()`
5. **Session history**: `IAgentService.GetSessionHistoryAsync(sessionId)` loads previous messages with `ContentParts` for interleaved replay
6. **Close session**: `IAgentService.CloseSessionAsync(sessionId)` marks session as Closed

### ABP Auto-Generated API

Application services implementing `IAppService` are automatically exposed as HTTP endpoints. The `RelationshipService` generates endpoints like `api/app/relationship/create`, `api/app/relationship/accept`, etc. No manual controller code is required — ABP's convention-based routing handles this.

## Data Flow

**Quote fetching and indicator calculation**:
```
User searches "600519"
  → SecurityService.CalculateIndicatorsAsync("600519", "101", "SMA(20),RSI(14),MACD")
    → SecurityManager.SearchSecuritiesAsync("600519")
      → SecurityRepository.SearchAsync("600519")  [SQLite]
    → FailoverQuoteProvider.GetKlineAsync(...)  [live API, shuffled sources]
    → QuoteRepository.AddRangeAsync(...)  [SQLite cache]
    → IndicatorCalculator.CalculateAsCsvAsync(quotes, "SMA(20),RSI(14),MACD")
      → Skender.Stock.Indicators library calculations
      → CSV output with OHLCV + indicator columns
```

**Agent query processing (streaming)**:
```
User sends "分析浦发银行的技术指标"
  → IAgentService.SendMessageStreamingAsync(sessionId, "分析浦发银行的技术指标")
    → AgentService persists user message
    → IAgentFactory.GetAgent("StockAnalyst") → LlmAgent instance
    → LlmAgent.RunStreamingAsync(context)
      → ChatClientAgent.RunStreamingAsync()
        → IChatClient (DeepSeek) processes with tools registered via AIFunctionFactory
        → LLM calls TechnicalAnalysisTool.CalculateIndicators()
        → Tool results fed back to LLM
      → Yields AgentResponseChunk stream (text + tool_call + tool_result)
    → AgentService builds ContentPart list incrementally
    → Persists assistant message with ContentPartsJson
    → Returns IAsyncEnumerable<AgentStreamEvent> to MAUI ViewModel
```

**Relationship management**:
```
Client sends CreateRelationship(userId, targetId, type)
  → POST api/app/relationship/create
    → RelationshipService dispatches to IRelationshipManager by type
      → Domain manager enforces type-specific authorization
        → Repository persists AppRelationship entity
```

**Moments feed**:
```
Client requests friends feed
  → GET api/app/moment/friends-moments?limit=20&offset=0
    → MomentManager.GetFriendsMomentsAsync()
      → Load accepted Friendships → collect friendIds
      → Load accepted Stock relationships → collect stockIds
      → Query moments: (UserId in friendIds AND Visibility="Friends")
                     OR (Type="Stock" AND decoded stockId in stockIds)
      → Order by CreatedAt desc, paginate
```

**Message caching**:
```
Open chat conversation
  → ChatDetailViewModel.OnAppearingAsync()
    → IMessageCacheService.GetCachedMessagesAsync(conversationId, limit)
      → MessageCacheRepository.GetCachedMessagesJsonAsync(conversationId)
        → Check memory cache (ConcurrentDictionary)
        → If not found, check SQLite (MessageCacheItem)
        → If not found, return null
    → If cache exists, display instantly
    → SyncInBackgroundAsync() (30s throttle)
      → If no cache, fetch from REST API
      → Update cache with new messages

Conversation list (cache-first):
  → ChatViewModel.OnAppearingAsync()
    → IConversationSyncService.GetCachedConversationsAsync(userId)
      → Display cached conversations instantly
    → SyncInBackgroundAsync()
      → IConversationSyncService.SyncFromServerAsync(userId)
        → Upsert new conversations, delete stale ones
    → LoadAgentConversationsAsync() via IAgentRepository

Contact list (cache-first):
  → ContactsViewModel.OnAppearingAsync()
    → IContactSyncService.GetCachedContactsAsync(userId)
      → Display cached contacts instantly
    → SyncInBackgroundAsync()
      → IContactSyncService.SyncFromServerAsync(userId)
        → Full-replace strategy: delete all then upsert
      → UpdateContactBookIfNeeded() compares ID sets before updating
```

## Security Architecture

- **Backend**: OpenIddict with two OAuth2 clients — Swagger (AuthorizationCode), AlphaAgent Chat (Password + RefreshToken). Token lifetimes: access 30 days, refresh 365 days.
- **Client**: Bearer token stored in SQLite. `BearerTokenDelegatingHandler` attaches token to all API requests (skipping auth-free endpoints). On 401, automatically refreshes token and retries the request.
- **Permissions**: Permission groups include Devices, Messages, Friendships, Groups, Moments, Stocks, Securities, OpenAI, AgentPrompts, OpenIddict, and Relationships with granular children.
- **Multi-tenancy**: Configurable via `MultiTenancyConsts.IsEnabled` in Domain.Shared.
- **Exception handling**: Custom `ExceptionHandlerMiddleware` in the HttpApi.Host pipeline.

## Technology Decisions

### Agent System with LlmAgent and IAgentFactory

**Decision**: Implement `IAgent`/`IAgentFactory` abstraction using `Microsoft.Extensions.AI` and `Microsoft.Agents.AI` for LLM interaction, with `AIFunctionFactory.Create()` for tool registration.
**Rationale**: Leverages standard .NET AI abstractions (`IChatClient`, `ChatClientAgent`) for compatibility across LLM providers. `AIFunctionFactory` automatically generates tool schemas from method signatures. Factory pattern enables named agent resolution and dynamic agent creation.
**Trade-offs**: Ties the Agent system to `Microsoft.Extensions.AI` ecosystem. Adding a new agent requires understanding the factory registration pattern rather than simply implementing an interface.

### Clean Architecture for Core Library

**Decision**: Separate AlphaAgent.Core into Domain.Abstractions, Domain, Application, and Infrastructure layers.
**Rationale**: Enforces separation of concerns, makes testing easier. Domain.Abstractions provides pure abstractions that both Domain, Application, and Infrastructure can reference.
**Trade-offs**: More project files and slightly more complex dependency management. Infrastructure interfaces (`ISignalRChatService`, `IHttpClientService`) must live in Domain.Abstractions (not Application) so Infrastructure can implement them without creating a circular dependency.

### ABP Mapperly over AutoMapper

**Decision**: Hand-written mappers extending ABP's `MapperBase<TSource, TDestination>`.
**Rationale**: Compile-time safety and zero runtime reflection overhead. Catches mapping errors at build time.
**Trade-offs**: More manual code than AutoMapper profiles, but full control and no runtime cost.

### Failover with Random Shuffle

**Decision**: Fisher-Yates shuffle of quote sources before iteration.
**Rationale**: Distributes API load across providers instead of always hammering the first source.
**Trade-offs**: Non-deterministic ordering means occasionally a slower source is tried first.

### Unified Relationship Table

**Decision**: Single `AppRelationship` entity with `RelationshipType` discriminator instead of separate tables.
**Rationale**: Simplifies queries and reduces schema complexity.
**Trade-offs**: Type-specific authorization logic is more complex.

### Deterministic GUID Conversion for Moments

**Decision**: Pack non-Guid IDs into the `UserId` Guid column using deterministic conversion.
**Rationale**: Reuses existing column without schema changes.
**Trade-offs**: Device/group ID conversion is not reversible.

### SignalR with Dual Authentication

**Decision**: Use SignalR for real-time messaging with dual auth — JWT for users and authorization codes for devices.
**Rationale**: Devices don't have user credentials; authorization codes serve as shared secrets.
**Trade-offs**: Custom middleware needed for WebSocket token extraction.

### Hybrid Message Cache

**Decision**: Memory + SQLite hybrid caching for chat messages.
**Rationale**: Fast memory access for active sessions, SQLite for persistence across app restarts.
**Trade-offs**: Additional complexity in cache synchronization and expiration management.

### Event Bus for Cross-Page Communication

**Decision**: Simple publish/subscribe event bus for MAUI.
**Rationale**: Decouples components and enables real-time updates across pages without tight coupling.
**Trade-offs**: Requires careful event subscription management to avoid memory leaks.

### Cache-First Loading Pattern

**Decision**: ViewModels load from local SQLite cache first, then sync from server in the background, with incremental updates to prevent UI flicker.
**Rationale**: Provides instant UI rendering on page load, improving perceived performance. Background sync ensures data freshness without blocking the UI. Incremental updates (comparing ID sets/order before updating ObservableCollection) prevent UI flicker from full collection rebuilds.
**Trade-offs**: Additional cache entities and sync services increase code complexity. Cache may be briefly stale until background sync completes. Sync throttling (30-second minimum interval) means rapid page switches may show slightly outdated data.

## CI/CD Architecture

### Deployment Pipeline

Two GitHub Actions workflows handle automated deployment:

1. **deploy-iis.yml** — Web backend deployment
   - Trigger: push to master (paths: `src/AlphaAgent.Web/**`) or manual `workflow_dispatch`
   - Runner: windows-latest
   - Flow: `dotnet publish` (self-contained, win-x64) → replace `${PLACEHOLDER}` tokens in appsettings.json with GitHub Secrets → msdeploy with `-enableRule:AppOffline` to IIS via WMSVC (port 8172)
   - AppOffline rule: automatically places `app_offline.htm` before deploy (releases DLL locks), removes after deploy

2. **build-apk.yml** — MAUI APK build + deploy + version registration
   - Trigger: push to master (paths: `src/AlphaAgent.Core/**`, `src/AlphaAgent.Maui/**`) or manual `workflow_dispatch` (with optional version input)
   - Runner: windows-latest
   - Flow: set version → `dotnet publish` net10.0-android → upload to GitHub Release → deploy APK to IIS `/apk` via msdeploy → call Publish API to register version
   - APK MIME type: `web.config` with `application/vnd.android.package-archive` deployed alongside APK

### Secret Management

Production `appsettings.json` uses `${PLACEHOLDER}` tokens:
- `${CONNECTION_STRING}` → SQL Server connection string
- `${PASSPHRASE}` → ABP string encryption passphrase
- `${CLIENT_SECRET}` → OpenIddict client secret
- `${VERSION_PUBLISH_TOKEN}` → API publish token for version registration

GitHub Secrets store the actual values. The deploy workflow replaces tokens before msdeploy using PowerShell string replacement with `.Trim()` to prevent trailing newlines from corrupting JSON.

### Auto-Update System

```
Build Pipeline                    Server                           MAUI Client
─────────────                     ───────                          ───────────
dotnet publish ──► APK
                  Release (GitHub)
                  msdeploy ──► /apk/*.apk
                  POST /publish ──► AppVersionConfigs table

                                  CheckUpdateAsync ◄── CheckUpdateAsync ◄── SplashViewModel
                                  (query latest       (POST with           (on startup,
                                   version by          platform +            shows update
                                   platform)           currentVersion)       prompt)
```

- **PublishAsync**: `[AllowAnonymous]` + `X-Publish-Token` header authentication. Creates `AppVersionConfig` record with platform, version code/name, update URL, notes, force flag.
- **CheckUpdateAsync**: `[AllowAnonymous]`. Queries `AppVersionConfigs` for latest version by platform. Returns `HasUpdate=true` if server VersionCode > client VersionCode.
- **Force update**: `IsForce=true` blocks app navigation until user updates. Non-force shows "稍后再说" option.
- **Download**: `Launcher.OpenAsync(UpdateUrl)` opens APK download URL in system browser. APK hosted at `https://{server}/apk/{filename}.apk`.

## Known Issues

### Security
- OpenIddict certificate password hardcoded in module startup code
- `Console.WriteLine` in Application layer (SecurityClientSyncService) logs sync operations — should use `ILogger<T>`
- Access token lifetime (30 days) and refresh token lifetime (365 days) are excessively long — industry standard is 15-60 min access, 7-30 days refresh
- Device `AuthorizationCode` exposed in `accepted-contacts` API response

### Architecture
- No unit tests — entire solution has no test projects
- ABP Web entities (`AppSecurity`, `AppDevice`, `AppMoment`) use anemic domain model with public setters, inconsistent with Core's DDD patterns
- `SecurityManager.FindAsync/SearchAsync` loads entire securities table into memory (N+1 pattern)
- MAUI project references Infrastructure layer (pragmatic compromise for DI registration)

### Runtime
- `EventBusService.Unsubscribe` has race condition with concurrent `TryRemove`/re-assign
- `UnreadMessageCacheService` uses `ConcurrentDictionary` but inner `List<ChatMessage>` is not thread-safe
- `StockAnalystAgent.Create()` creates `IServiceScope` that is never disposed (scope leak only occurs when AgentFactory.GetAvailableAgents() instantiates agents; metadata-only listing avoids this via try-catch fallback)
- Quote providers (Sina/East/Baidu) silently return empty lists on exceptions
- `HttpClientService` returns `default` on all exceptions — callers cannot distinguish "no data" from "request failed"
- 7 `Console.WriteLine` calls in Core Application layer (SecurityClientSyncService) instead of `ILogger<T>`; remaining calls are in console apps where appropriate

### Code Quality
- Duplicate code: `GetCurrentUserIdAsync`, `EnsureLocalTime`, `StringToGuid`, `PadBase64` repeated across ViewModels
- Empty catch blocks in quote providers and ToolCall deserialization

## Roadmap

### Short-term (Priority)
- Remove hardcoded secrets (cert password) from source code — use configuration/environment variables
- Replace `Console.WriteLine` with `ILogger<T>` in Application layer (SecurityClientSyncService has 7 remaining calls)
- Reduce token lifetimes: AccessToken → 1 hour, RefreshToken → 7 days

### Mid-term (Quality)
- Add unit tests starting from Core Domain layer (SecurityManager, TokenManager, AnalysisManager)
- Extract duplicate ViewModel code into base classes or utility methods
- Fix SecurityManager N+1 queries — server-side pagination/filtering
- Add domain logic to ABP entities — replace public setters with methods
- Add structured logging and retry logic to quote providers
- Make MAUI server address configurable (not hardcoded IP)

### Long-term (Features)
- iOS publishing (Apple Developer account + certificate pipeline)
- Push notifications via Firebase Cloud Messaging (Android) + APNs (iOS)
- Real-time quote push via SignalR (replace client-side polling)
- Multi-agent collaboration (fundamental + technical analysis agents)
- Trading simulation / backtesting with historical data
- Data visualization: K-line charts and indicator charts (SkiaSharp or Syncfusion Charts, already have Syncfusion.Maui.Toolkit dependency)

---

**Documentation Focus**: This document emphasizes custom implementations and unique architectural decisions. Standard ABP Framework patterns are documented in [CLAUDE.md](../CLAUDE.md).
