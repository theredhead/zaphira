# Zaphira — Architecture Overview & MVP Specification

> **Status:** Draft / MVP planning baseline  
> **Working name:** Zaphira  
> **License:** MIT  
> **Primary platform:** macOS  
> **Secondary platform:** Windows may be supported  
> **Language/runtime:** C# / .NET  
> **UI:** Avalonia UI  
> **Architecture style:** Modular, provider-strategy based, client/server separated

---

## 1. Vision

Zaphira is a local-first AI assistant with a comfortable ChatGPT-like desktop interface, designed around user control over local models and runtimes.

The initial product should make this experience simple:

> **Launch Zaphira → choose an initial model → chat.**

The system must not depend on cloud connectivity for its fundamental local-chat experience.

The architecture should, however, permit:

- remote backends over the local network or Internet;
- multiple LLM runtimes/providers;
- multiple speech-to-text and text-to-speech implementations;
- multiple Markdown implementations;
- multimodal messages;
- agentic workflows and future subagents;
- future animated-avatar presentation;
- eventual independent/headless server deployment.

The MVP should implement only what is needed to provide a polished local chat experience while preserving these extension points.

---

# 2. Product Principles

1. **Local first**
   - Local operation is the primary experience.
   - Network access is not required for ordinary local chat.
   - External services should be optional enhancements.

2. **Backend/Frontend separation**
   - The UI is never the backend.
   - The backend exposes an API and owns canonical server-side state.
   - The client can launch a local backend when necessary.

3. **Strategy over assumptions**
   - Capabilities likely to acquire multiple implementations should be represented by strategies/providers.
   - The first implementation may be the only implementation for the MVP.
   - Do not prematurely generalize based on Ollama-specific behavior.

4. **Semantic models, not presentation models**
   - Domain messages contain semantic content.
   - Message parts have no knowledge of Avalonia or presentation.
   - The UI maps message parts to presentation consistently.

5. **Portable deployment**
   - The normal installation is one application icon.
   - The backend is bundled inside the client distribution.
   - A separately deployed server is created only when server mode is explicitly enabled.

6. **Security by default**
   - HTTPS is required even for local communication.
   - Certificates may be generated automatically.
   - Pairings remain valid until explicitly removed.

7. **Don't build the future too early**
   - Interfaces and boundaries should permit future capabilities.
   - Future features should not become MVP implementation requirements without a concrete need.

8. **Keep the MVP simple**
   - Simplicity means avoiding invented complexity, not ignoring real constraints.
   - The MVP should use the smallest clear contracts that satisfy the product goals.
   - Implementation choices should remain reversible where future providers,
     transports, or deployment modes are already expected.
   - Do not code Zaphira into a corner merely to save a small amount of initial work.

---

# 3. Engineering Rules

These rules guide implementation work across the codebase.

1. **Use TDD properly**
   - Start with tests for meaningful behavior before implementation where practical.
   - Keep tests focused on observable behavior, contracts, and edge cases.
   - Do not write brittle tests that merely mirror implementation details.
   - Regression fixes should include regression tests.

2. **Follow ecosystem conventions**
   - Prefer normal C#, .NET, ASP.NET Core, Avalonia, MVVM, and CommunityToolkit
     conventions over project-specific novelty.
   - Use established framework patterns before inventing custom infrastructure.
   - Keep formatting, naming, dependency injection, async patterns, and error
     handling consistent with the libraries being used.

3. **Use modern .NET standards**
   - Prefer async/await for I/O-bound work, process interaction, network calls,
     database access, streaming, and other long-running operations.
   - Propagate `CancellationToken` through cancellable operations.
   - Prefer immutable or value-oriented domain and contract types where practical.
   - Prefer clear result/error models at application boundaries over exception
     control flow for expected failures.

4. **Take null safety seriously**
   - No property should be null after type initialization.
   - Null does not mean "not assigned yet"; it means an invariant has been
     violated.
   - Domain, application, and contract models should not model null as an allowed
     state.
   - Required data should be represented with required constructor parameters,
     `required` init properties, or non-null defaults.
   - Optional or absent data should be modeled explicitly with option/result
     types, empty collections, empty value objects, or domain-specific absence
     values.
   - A dedicated non-null value may exist solely to represent intentional absence
     or "not yet available" state.
   - Collections should default to empty collections rather than null.
   - Use nullable reference types correctly and treat nullability warnings as
     design feedback, not noise.
   - Avoid null-forgiving operators except at tightly contained framework
     boundaries where the invariant is immediately established.

5. **Name things clearly**
   - Prefer descriptive names for types, methods, properties, variables, commands,
     and tests.
   - Avoid abbreviations unless they are obvious and conventional in context.
   - Long names are acceptable when they make intent clearer.
   - Method names should describe the operation or behavior they represent.

6. **Keep visibility narrow**
   - Only make types and members public when another assembly or external caller
     actually needs them.
   - Prefer private, internal, or protected visibility as appropriate.
   - Keep interfaces focused on real substitution boundaries, not speculative ones.

7. **Keep code cohesive**
   - Put behavior near the data and responsibilities it belongs to.
   - Avoid dumping unrelated logic into broad service classes.
   - Extract helpers only when they clarify the code or remove meaningful
     duplication.

8. **Handle errors deliberately**
   - Surface user-actionable failures clearly.
   - Preserve useful diagnostic information in logs without recording sensitive
     content by default.
   - Do not swallow provider, process, database, network, or filesystem errors
     without making an intentional recovery decision.

---

# 4. High-Level Architecture

```text
┌───────────────────────────────────────────────────────────┐
│                       Zaphira Client                      │
│                                                           │
│  Avalonia UI                                              │
│  MVVM / CommunityToolkit                                  │
│                                                           │
│  ┌─────────────┐   ┌──────────────┐   ┌───────────────┐  │
│  │ Chat UI     │   │ Settings UI  │   │ Model Browser │  │
│  └─────────────┘   └──────────────┘   └───────────────┘  │
│          │                  │                  │           │
│          └──────────────────┼──────────────────┘           │
│                             ▼                               │
│                    Client Application                      │
│                             │                               │
│                       HTTPS API                             │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────┐
│                       Zaphira Server                      │
│                                                           │
│  ASP.NET Core                                             │
│                                                           │
│  ┌───────────────┐  ┌────────────────┐  ┌──────────────┐ │
│  │ Chat Service  │  │ Model Catalog  │  │ Pairing/TLS  │ │
│  └───────────────┘  └────────────────┘  └──────────────┘ │
│           │                    │                          │
│           ▼                    ▼                          │
│  ┌────────────────┐   ┌──────────────────────┐           │
│  │ Provider Layer │   │ Compatibility Engine │           │
│  └────────────────┘   └──────────────────────┘           │
│           │                                               │
│           ▼                                               │
│      Ollama Provider                                      │
│           │                                               │
│           ▼                                               │
│     Ollama process/runtime                                │
│                                                           │
│  SQLite                                                   │
└───────────────────────────────────────────────────────────┘
```

---

# 5. Proposed Solution Structure

The exact project count may be adjusted during implementation, but the separation should resemble:

```text
Zaphira.sln
│
├── src/
│   ├── Zaphira.Client/
│   │   └── Avalonia application
│   │
│   ├── Zaphira.Server/
│   │   └── ASP.NET Core + optional Avalonia host
│   │
│   ├── Zaphira.Domain/
│   │   └── Core semantic/domain models
│   │
│   ├── Zaphira.Application/
│   │   └── Use cases, application services, contracts
│   │
│   ├── Zaphira.Infrastructure/
│   │   └── SQLite, filesystem, process integration, etc.
│   │
│   └── Zaphira.Contracts/
│       └── Client/server transport contracts
│
└── tests/
    ├── Zaphira.Domain.Tests/
    ├── Zaphira.Application.Tests/
    ├── Zaphira.Infrastructure.Tests/
    └── Zaphira.Server.Tests/
```

A concrete implementation should avoid unnecessary project fragmentation. The project boundaries exist to preserve important dependency directions, not to maximize the number of assemblies.

---

# 6. Dependency Direction

Preferred dependency flow:

```text
Client
  ↓
Contracts

Server
  ↓
Application
  ↓
Domain

Infrastructure
  ↓
Application / Domain
```

The domain must not depend on:

- Avalonia
- ASP.NET Core
- SQLite/EF Core
- Ollama
- HTTP
- operating-system APIs

The UI must not be required by the server.

---

# 7. Core Message Model

Messages are semantic structures containing an ordered collection of heterogeneous parts.

```csharp
public sealed class Message
{
    public required MessageRole Role { get; init; }

    public required IReadOnlyList<IMessagePart> Parts { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public interface IMessagePart
{
}
```

Potential implementations include:

```text
TextMessagePart
ImageMessagePart
AudioMessagePart
VideoMessagePart
FileMessagePart
ToolCallMessagePart
ToolResultMessagePart
ReasoningMessagePart
```

The list is intentionally open-ended.

### Important architectural rule

`IMessagePart` contains **no visualization concerns**.

It must not contain:

- Avalonia controls
- view models
- rendering methods
- UI templates
- presentation metadata specific to one frontend

The client determines how semantic parts are presented.

```text
IMessagePart
     │
     ▼
Client mapping
     │
     ├── Text → Markdown presentation
     ├── Image → Inline image + Save As
     ├── Audio → Audio player + Save As
     ├── Video → Video presentation + Save As
     └── Unknown → Consistent fallback
```

This permits different frontends to present the same semantic message differently.

---

# 8. Streaming

Streaming is an MVP requirement.

The architecture must not assume that an LLM provider produces only plain text.

The provider layer should eventually translate runner-specific streaming behavior into Zaphira-level semantic generation events.

Conceptually:

```text
Runner-specific protocol
        ↓
Provider adapter
        ↓
Zaphira generation events
        ↓
Server transport
        ↓
Client
```

The exact provider contract and event taxonomy should be designed after investigating the actual streaming capabilities of likely runtimes.

Do **not** design the abstraction solely around Ollama.

### Required behavior

- Assistant output appears incrementally.
- The UI remains responsive.
- Cancellation is available during generation.
- Cancellation propagates through client → server → provider → runtime.
- Providers may expose reasoning/thinking information.
- Reasoning/thinking is displayed only when the selected model/provider actually exposes suitable information.
- Zaphira must not invent or infer hidden reasoning.

---

# 9. LLM Provider Strategy

The initial implementation is Ollama.

Conceptually:

```text
ILLMProvider
    │
    ├── OllamaProvider        ← MVP
    ├── FutureProvider
    └── FutureProvider
```

The application should not directly depend on Ollama command-line behavior outside the provider implementation.

The initial provider should use the Ollama command-line tools already installed on the user's system.

Embedding a runtime later is explicitly possible, but not an MVP requirement.

---

# 10. Other Provider Strategies

Capabilities where multiple implementations are reasonably foreseeable should use provider strategies.

Initial architectural targets:

```text
ILLMProvider
ISpeechToTextProvider
ITextToSpeechProvider
IMarkdownProvider
```

Potential future strategies:

```text
IModelCatalogProvider
IModelInstallationProvider
IAvatarProvider
IToolProvider
```

Not every interface needs an MVP implementation.

Simple dependency injection is sufficient initially.

User-configurable provider selection should be introduced when multiple implementations actually exist.

---

# 11. Markdown

Markdown rendering is required from day one.

Requirements:

- Common Markdown
- code blocks
- syntax highlighting
- tables where supported
- links
- Mermaid support if practical
- safe handling of HTML
- consistent rendering throughout chat

Markdown interpretation is itself a strategy.

The initial implementation should use a popular, mature, MIT-licensed C#/.NET-compatible Markdown library.

The exact library should be selected during implementation based on current maintenance, Avalonia integration requirements, Mermaid support, and licensing.

Arbitrary active HTML should not be trusted merely because it appears in model-generated Markdown.

---

# 12. Media

The message model must support arbitrary ordered parts so that media can appear inline with text.

Example:

```text
Message
├── TextMessagePart
├── ImageMessagePart
├── TextMessagePart
└── FileMessagePart
```

MVP UI requirements:

- Images display inline.
- Other supported media displays inline where practical.
- Files have a clear representation.
- Media/files expose **Save As...**
- Large binary content should not unnecessarily be stored inside SQLite.

The backend should own canonical generated/received files while the client may cache them for UI responsiveness.

---

# 13. Model Finder and Discovery

Zaphira should provide an in-app model finder that helps users discover models
for the purposes they have in mind without needing to leave the application.

The user-facing experience is **purpose-oriented model discovery**, not merely a
raw catalog browser.

The initial catalog/source strategy is Hugging Face.

Hugging Face is therefore the first metadata source, not the product concept
itself. The architecture should permit additional catalog sources later, such as
provider-native catalogs, curated recommendation data, local manifests, or other
model registries.

Conceptually:

```text
Catalog sources
    ├── Hugging Face        ← MVP source strategy
    ├── Provider-native catalog
    ├── Curated rules/data
    └── Future source
          ↓
Model understanding layer
          ↓
Purpose-oriented model finder UX
```

Users should be able to search and filter by intent, capability, and practical
fit. Examples include:

```text
Coding
Writing
Fast chat
Reasoning
Vision
Long context
Low-memory machines
Tool/function calling
Roleplay
Local/private use
```

The catalog should expose useful information such as:

- model name
- author/organization
- model size
- architecture, where available
- capabilities
- quantization information
- files/variants
- license
- detail-page URL
- provider/runtime compatibility
- compatibility with the current machine

The catalog is cached locally.

Model search results should distinguish:

- models that are directly usable with the current provider/runtime;
- models that may be usable with additional setup or conversion;
- models that are useful for discovery but not currently installable;
- models whose compatibility or capability status is unknown.

Where practical, search results should explain why a model appears for a given
purpose, using available metadata and compatibility signals rather than hiding
uncertainty behind false precision.

### Cache policy

- Normal cache lifetime: at least 24 hours.
- No continuous polling.
- User-visible **Sync Now** operation.
- Cached catalog remains usable offline.

---

# 14. Capability Inference

Model capabilities should not be assumed solely from a single Hugging Face tag.

The catalog/inference system should consider available metadata such as:

- repository tags
- model architecture
- configuration
- files
- known runtime support
- declared modalities
- tool/function-calling support
- other reliable metadata

Capability inference should expose uncertainty rather than pretending it is perfect.

Possible compatibility states:

```text
Compatible
Probably compatible
May not fit
Incompatible
Unknown
```

---

# 15. Hardware-Aware Compatibility

Compatibility is determined using both:

1. detected machine hardware;
2. relevant Zaphira settings.

The machine profile should capture, where applicable:

```text
Operating system
CPU
Physical/system memory
GPU(s)
GPU memory
Unified memory
```

Unified-memory systems require special handling.

A machine with 32 GB unified memory must not be treated as though it has 32 GB of independently available system RAM plus 32 GB of GPU VRAM.

The compatibility engine should reserve configurable headroom for the operating system and other applications.

Zaphira should not label a model as compatible merely because its advertised model size is numerically below the machine's total memory.

---

# 16. Model Management

Model management is runtime/provider-specific.

The application should not hard-code Ollama's installation mechanics into generic model-management code.

Conceptually:

```text
Model catalog
      ↓
Compatible model
      ↓
Provider/runtime
      ↓
Install / remove / inspect
```

For MVP, the Ollama provider handles Ollama model operations.

Required UX:

- list installed models
- identify active/default model
- browse/search available models
- show compatibility
- download/install
- show progress
- cancel where supported
- remove downloaded models
- handle failures clearly

---

# 17. Agentic Architecture — Future Constraint

Sophisticated agent orchestration is **not an MVP feature**.

However, the architecture must not make future concurrent subagents impossible.

Future requirements may include:

- primary agents
- delegated subtasks
- multiple concurrent subagents
- different models per subtask
- model selection based on task requirements
- tool execution
- task monitoring
- cancellation
- scheduling based on available hardware
- result aggregation

The eventual architecture should distinguish:

```text
Logical parallelism
    ≠
Physical model/runtime parallelism
```

For example, ten document-search tasks may exist simultaneously while only three can sensibly execute given available hardware.

Do not implement the orchestration system merely to satisfy this future requirement.

---

# 18. Speech

Speech is not an MVP implementation requirement.

The architecture should eventually support:

```text
ISpeechToTextProvider
ITextToSpeechProvider
```

The client/server boundary should permit either local or remote speech processing later.

The future speech layer should not leak provider-specific APIs into the chat domain.

---

# 19. Animated Avatar

An animated avatar is explicitly a future feature.

No avatar implementation is required for MVP.

The message/content model and UI architecture should not prevent a future presentation layer from reacting to:

- speech
- assistant state
- tool activity
- reasoning/activity state

No avatar-specific domain concepts should be introduced merely for future speculation.

---

# 20. Client/Server Deployment

The normal distribution contains both client and server.

Conceptually:

```text
Zaphira.app
├── Client
└── Bundled Server payload
```

When launched normally:

- client starts;
- client checks whether the configured backend is reachable;
- if the local backend is not running, client may launch the bundled backend;
- client records whether it started that process;
- client does not terminate a backend it did not start.

When the client starts the backend:

- backend runs headlessly;
- Avalonia UI is not hydrated;
- macOS should not create a Dock icon for the headless server;
- backend exposes its ASP.NET Core API.

When the user enables server mode:

- the bundled backend can be copied/deployed as a standalone server application;
- server may run with its own desktop/admin UI;
- server can also operate headlessly.

---

# 21. Server Ownership

The client tracks whether a backend process is **owned by that client invocation**.

```text
Backend already running
    → client connects
    → client does not own process
    → client does not stop it on exit

Client launches backend
    → client owns process
    → client may perform graceful shutdown on exit
```

No forceful process termination should be part of normal lifecycle management.

---

# 22. HTTPS and Certificates

HTTPS is mandatory for all client/server communication, including localhost.

There is no HTTP exception for local connections.

Initial certificates may be generated automatically at runtime.

Certificate information may become part of the pairing/configuration information.

The design should eventually support configurable certificates, while preserving a zero-intervention first-run experience.

Certificate generation, storage, rotation, and trust behavior must be implemented in a platform-appropriate way.

---

# 23. Pairing

Remote pairing is intentionally simple and human-mediated.

The user decides whether they trust the backend they are pairing with. Zaphira
does not need to solve that human trust decision with complex policy,
reputation, or identity infrastructure in the MVP.

The software is responsible for keeping the communication secure once the user
chooses to pair:

- use HTTPS for transport;
- bind persisted pairing information to the backend identity/certificate;
- store pairing credentials appropriately;
- allow pairings to be removed or revoked;
- avoid silently continuing to use a pairing after revocation.

Initial flow:

```text
User enters DNS name / IP address
        ↓
Frontend checks for Zaphira backend
        ↓
Backend presents a 4-digit pairing code
        ↓
Frontend asks user for code
        ↓
Pairing established
        ↓
Connection persists
```

Local client-launched backend may use a trusted local bootstrap mechanism and may skip interactive pairing.

Pairings remain valid indefinitely until manually deleted at either end.

The system must support:

- viewing known pairings
- removing a pairing
- revoking a pairing
- associating the pairing with certificate/server identity information

---

# 24. Backend Admin UI

The server remains a desktop application even when its API is ASP.NET Core.

When started by the frontend in headless mode:

- no Avalonia UI needs to be initialized;
- no unnecessary desktop UI resources should be allocated;
- on macOS, no Dock icon should appear.

The server may later provide a lightweight back-office UI for:

- pairing management
- connected clients
- server configuration
- certificate configuration
- model/provider administration
- diagnostics

This is not required to be feature-complete for MVP.

---

# 25. Persistence

SQLite is the initial persistence technology.

The server owns canonical conversation persistence.

The client has separate persistent state for its own concerns.

Recommended layout:

```text
~/.zaphira/
│
├── client/
│   ├── settings.json
│   ├── connections.json
│   └── cache/
│
└── server/
    ├── server.db
    ├── settings.json
    ├── certificates/
    ├── pairings/
    ├── cache/
    ├── files/
    │   ├── attachments/
    │   └── audio/
    └── logs/
```

The exact persistence formats may evolve.

### Ownership

```text
Client
→ owns client settings, UI state, known connections, client cache

Server
→ owns conversations, messages, server configuration,
  model catalog/cache, certificates, pairings, server files
```

The client should not become a second authoritative owner of server conversation history.

---

# 26. Database

SQLite is appropriate because:

- Zaphira is initially a local application;
- operational complexity is low;
- it is portable;
- it supports transactional persistence;
- it is sufficient for the expected MVP workload.

EF Core may be used for database access and migrations.

Large binary assets should normally be stored as files with metadata/reference information in SQLite.

Database migrations should be versioned and tested.

---

# 27. Offline-First Behavior

The following must work without Internet access:

- launching Zaphira;
- connecting to a local backend;
- using installed models;
- viewing persisted conversations;
- continuing local conversations;
- Markdown rendering;
- browsing installed models;
- removing installed models;
- ordinary settings management.

Internet access is required only for features that inherently require external connectivity, such as:

- Hugging Face synchronization;
- downloading models from external services;
- remote backends that are not locally reachable.

Cached model metadata remains available offline.

---

# 28. First-Run and Availability States

First run should be simple when the required pieces are available:

```text
Launch Zaphira
    ↓
Start/connect to backend
    ↓
Detect available providers/runtimes
    ↓
Find installed or downloadable compatible models
    ↓
Choose model
    ↓
Chat
```

When required pieces are missing, Zaphira should not pretend it can continue.
It should show a clear blocking state, explain what is missing, suggest the most
likely solution, and provide a way to retry.

Examples:

```text
No provider/runtime available
    → explain that no local model runtime/provider is currently usable
    → suggest installing/configuring a supported provider or connecting to a backend
    → offer Retry and Settings

No installed model and offline
    → explain that no local model is available and new models cannot be fetched offline
    → suggest going online, connecting to a backend, or installing a model manually
    → offer Retry and Settings

Catalog unavailable and no cache exists
    → explain that model discovery needs Internet access for the first sync
    → suggest going online or configuring another source/backend
    → offer Retry and Settings

Backend unavailable
    → explain that Zaphira cannot reach or start the configured backend
    → suggest retrying, checking settings, or selecting another backend
    → offer Retry and Settings
```

These states are acceptable MVP outcomes. The important behavior is that failure
is understandable and recoverable, not silent or ambiguous.

---

# 29. User Stories

## First Run

### US-001 — Choose an initial model

**As a new user,**  
I want to choose an initial local model during onboarding,  
so that I can immediately start chatting.

**Acceptance criteria**

- On first launch, Zaphira presents model selection.
- Only models/providers that can reasonably run on the current machine are presented as compatible.
- The user can choose a model.
- After selection, the user enters the chat.
- If no model is available, Zaphira clearly explains what is missing.
- If no provider/runtime, model, cached catalog, or network path is available,
  Zaphira shows a clear blocking state with a suggested fix and Retry/Settings
  actions.

### US-002 — Enter settings before chatting

**As a new user,**  
I want to open settings from onboarding,  
so that I can configure a remote backend or other options before starting my first chat.

**Acceptance criteria**

- Settings is accessible directly from the initial model selection screen.
- Returning from settings preserves the onboarding state.

---

# Chat

### US-003 — Chat with a local model

**As a user,**  
I want to send written messages to a local model,  
so that I can use an AI assistant without a cloud service.

**Acceptance criteria**

- User can enter text.
- User can submit it.
- The request reaches the configured backend.
- The selected model produces a response.
- The response is persisted.

### US-004 — See streaming responses

**As a user,**  
I want responses to appear while they are being generated,  
so that Zaphira feels responsive.

**Acceptance criteria**

- Assistant content appears incrementally.
- UI remains responsive during generation.
- Completed content becomes part of the persisted message.

### US-005 — Cancel generation

**As a user,**  
I want to stop a response while it is generating,  
so that I remain in control.

**Acceptance criteria**

- A cancel/stop control is available during generation.
- Cancellation propagates to the backend.
- The provider is asked to cancel.
- The UI returns to an interactive state.
- Partial results are handled consistently.

### US-006 — View available reasoning

**As a user,**  
I want to see exposed reasoning/thinking information when the model provides it,  
so that I can understand agent activity where the runtime supports it.

**Acceptance criteria**

- Reasoning is displayed only when explicitly supplied by the model/provider.
- It is visually distinct from normal assistant content.
- It can be collapsed where appropriate.
- Models/providers that do not expose it do not fabricate it.

---

# Conversations

### US-007 — Persist conversations

**As a user,**  
I want my conversations to remain available after restarting Zaphira,  
so that I don't lose my work.

**Acceptance criteria**

- Conversations are persisted by the backend.
- Restarting the client does not erase them.
- Restarting the backend does not erase them.

### US-008 — Manage conversations

**As a user,**  
I want to rename and delete conversations,  
so that my conversation list remains useful.

**Acceptance criteria**

- Conversations appear in a source/list panel.
- A conversation can be renamed.
- A conversation can be deleted.
- Deletion has an appropriate confirmation or undo mechanism.
- The current conversation is handled safely when deleted.

---

# Markdown and Media

### US-009 — Render Markdown

**As a user,**  
I want assistant responses rendered as Markdown,  
so that code, lists, tables, links, and structured responses are readable.

**Acceptance criteria**

- Markdown is rendered consistently.
- Code blocks are readable.
- Syntax highlighting is available.
- Links behave safely.
- Mermaid is supported if the selected implementation permits it within the MVP constraints.

### US-010 — View media inline

**As a user,**  
I want images and supported media to appear inline in messages,  
so that I don't have to open separate windows for ordinary results.

**Acceptance criteria**

- Supported image parts render inline.
- Supported media renders inline where practical.
- Each downloadable media/file part offers Save As.
- Unsupported parts have a predictable fallback presentation.

---

# Models

### US-011 — Find useful models

**As a user,**  
I want to find models by purpose, capability, and practical fit,  
so that I can choose the right model for what I want to do without leaving Zaphira.

**Acceptance criteria**

- Models can be searched by name and filtered by purpose/capability.
- Model name and size are visible.
- Detail-page link is available.
- Relevant capabilities are shown.
- Compatibility is shown.
- Results distinguish directly usable, possibly usable, unsupported, and unknown models.
- Where practical, Zaphira explains why a model matched the user's intent.

### US-012 — Use cached model catalog

**As a user,**  
I want the model catalog to work without constantly contacting Hugging Face,  
so that Zaphira remains fast and network-efficient.

**Acceptance criteria**

- Catalog is cached locally.
- Normal refresh interval is at least 24 hours.
- Browsing does not continuously call Hugging Face.
- Sync Now explicitly refreshes the catalog.

### US-013 — Manage installed models

**As a user,**  
I want to see, install, and remove locally available models,  
so that I can manage disk space and model selection.

**Acceptance criteria**

- Installed models are visible.
- Model installation exposes progress where available.
- Failed installations report useful errors.
- Models can be removed.
- Provider-specific mechanics remain behind the provider abstraction.

---

# Remote Backends

### US-014 — Connect to a remote backend

**As a user,**  
I want to enter a DNS name or IP address and connect to a Zaphira backend,  
so that I can use another machine as my AI server.

**Acceptance criteria**

- User can enter the server address.
- Client verifies that a Zaphira backend is reachable.
- Communication uses HTTPS.
- Pairing is required where appropriate.
- Connection information persists.

### US-015 — Pair with a backend

**As a user,**  
I want to pair my client with a backend using a short code,  
so that remote access does not require manually exchanging long credentials.

**Acceptance criteria**

- Backend presents a 4-digit code.
- Client provides a pairing-code input.
- Successful pairing persists.
- Pairing remains valid until explicitly deleted.
- Pairing can be revoked from either endpoint.

---

# 30. Non-Functional Acceptance Criteria

## Performance

- UI remains responsive during model generation.
- Streaming begins as soon as useful provider data is available.
- Cancellation does not require restarting the client.
- Catalog browsing uses local cache where possible.

## Reliability

- A backend crash does not corrupt the client.
- A client disconnect does not corrupt server conversations.
- Provider failures are surfaced as useful user-facing errors.
- Partial streamed messages are persisted consistently.

## Security

- Client/server communication always uses HTTPS.
- Pairings cannot be silently reused after revocation.
- Certificates are tied to backend identity appropriately.
- Model-generated Markdown cannot execute arbitrary unsafe active content by default.
- Sensitive data is not written to logs by default.

## Offline operation

- Local chat with installed models works without Internet.
- Cached catalog remains browseable offline.
- Persisted conversations remain available offline.
- External-only functionality fails clearly rather than making the application appear broken.

## Portability

- macOS is a first-class supported platform.
- Windows compatibility is desirable but not a blocker for MVP.
- Normal installation should remain a single application package.
- User data must survive application replacement/upgrades.

---

# 31. Environment Requirements

## Development

Required:

- macOS development environment
- .NET SDK version selected by the implementation baseline
- C# compiler/tooling
- Avalonia-compatible IDE/editor
- Git
- Ollama installed for provider integration testing

Recommended:

- JetBrains Rider or Visual Studio Code
- .NET CLI
- SQLite browser/CLI for diagnostics
- small local test model suitable for development

## Runtime — macOS

Required:

- supported macOS version determined by the selected .NET/Avalonia baseline
- .NET runtime or self-contained deployment, depending on packaging choice
- Ollama installed for the initial LLM provider

The application should clearly detect when the expected Ollama executable is missing or unusable.

## Runtime — Windows

Potentially supported:

- supported Windows version determined by the .NET/Avalonia baseline
- Ollama installed for the initial LLM provider

Windows support should not drive architectural decisions unless it conflicts with macOS requirements.

---

# 32. Observability

Separate client and server logs.

Recommended locations:

```text
~/.zaphira/client/logs/
~/.zaphira/server/logs/
```

Requirements:

- rolling logs
- configurable retention
- sensible log levels
- provider/process failures recorded
- no conversation content logged by default
- useful diagnostics for connection, startup, shutdown, TLS, pairing, and model failures

---

# 33. ADRs

## ADR-001 — Separate Client and Server

**Status:** Accepted

**Decision:** Zaphira will use a separate frontend/client and backend/server architecture.

**Rationale:**

- permits remote backends;
- permits headless operation;
- keeps UI concerns out of backend logic;
- enables future alternative clients;
- makes server deployment possible.

---

## ADR-002 — Avalonia for the Desktop UI

**Status:** Accepted

**Decision:** Use Avalonia UI with C#/.NET.

**Rationale:**

- macOS is the primary platform;
- C# is a hard technology requirement;
- cross-platform capability is desirable;
- MVVM works naturally with Avalonia.

---

## ADR-003 — MVVM + CommunityToolkit

**Status:** Accepted

**Decision:** Use MVVM patterns and the CommunityToolkit for observable state, commands, and related application concerns.

**Rationale:** Provides a mature, conventional approach without building framework infrastructure unnecessarily.

---

## ADR-004 — Provider Strategy Architecture

**Status:** Accepted

**Decision:** Capabilities likely to receive multiple implementations will be represented by provider/strategy abstractions.

**Initial providers:**

- Ollama LLM provider
- one Markdown provider

**Future strategy targets:**

- STT
- TTS
- other LLM runtimes
- model management/catalog sources where appropriate

**Rationale:** Prevents provider-specific behavior from becoming domain/application architecture.

---

## ADR-005 — Ollama as Initial LLM Provider

**Status:** Accepted

**Decision:** Initial LLM integration uses the Ollama command-line tools already installed on the host.

**Rationale:** It provides a practical local runtime without embedding a model runtime into the application.

**Future:** Embedded or alternative runtimes may be added later.

---

## ADR-006 — SQLite for Local Persistence

**Status:** Accepted

**Decision:** Use SQLite for server persistence.

**Rationale:**

- local-first workload;
- minimal operational overhead;
- portable;
- sufficient for MVP;
- supports transactional persistence.

---

## ADR-007 — Shared User Data Root

**Status:** Accepted

**Decision:** Store Zaphira user data under one hidden directory in the user's home directory:

```text
~/.zaphira/
```

with separate client and server subdirectories.

**Rationale:**

- easy to locate;
- easy to back up;
- easy to reset;
- clear ownership boundaries;
- avoids multiple product-specific dot folders.

---

## ADR-008 — Semantic Message Parts

**Status:** Accepted

**Decision:** A message contains an ordered collection of `IMessagePart`.

**Rationale:**

- supports arbitrary multimodal messages;
- permits interleaving text and media;
- supports future tools and reasoning;
- prevents a fixed property model from becoming restrictive.

**Important:** Message parts have no presentation responsibilities.

---

## ADR-009 — HTTPS Everywhere

**Status:** Accepted

**Decision:** All frontend/backend communication uses HTTPS, including localhost.

**Rationale:**

- consistent security model;
- avoids special local transport rules;
- makes remote deployment natural;
- certificate overhead is manageable.

---

## ADR-010 — Runtime-Generated Certificates

**Status:** Accepted for initial implementation

**Decision:** Zaphira can generate its own certificates at runtime for zero-intervention initial setup.

**Future:** User-configured certificates are supported as a configuration option.

---

## ADR-011 — Persistent Pairings

**Status:** Accepted

**Decision:** Pairings remain valid indefinitely until manually revoked/deleted.

**Rationale:** Avoids unnecessary re-pairing for trusted personal devices.

---

## ADR-012 — Bundled Backend

**Status:** Accepted

**Decision:** The normal client distribution contains the backend payload.

**Rationale:**

- single drag-and-drop installation;
- local backend requires no separate installation;
- server can later be promoted to standalone deployment.

---

## ADR-013 — Headless Backend Mode

**Status:** Accepted

**Decision:** The backend can run without hydrating its Avalonia UI.

**Rationale:**

- lower overhead;
- appropriate for client-launched local backend;
- avoids unnecessary desktop presence;
- supports future server-only deployments.

---

## ADR-014 — Purpose-Oriented Model Finder with Hugging Face Source

**Status:** Accepted

**Decision:** Zaphira provides an in-app model finder organized around user intent, capabilities, and practical compatibility. Hugging Face is the initial catalog/source strategy, and its metadata is cached locally for at least 24 hours with an explicit Sync Now operation.

**Rationale:**

- users should be able to discover useful models without leaving the app;
- Hugging Face provides broad initial model metadata;
- the product should not become merely a raw Hugging Face browser;
- future catalog sources can be added behind the same discovery pipeline;
- offline-first behavior;
- avoids unnecessary API traffic;
- faster model browsing.

---

## ADR-015 — Hardware-Aware Model Compatibility

**Status:** Accepted

**Decision:** Model compatibility is inferred using machine hardware and Zaphira configuration.

**Rationale:** A model's nominal size alone is insufficient to establish whether it will run acceptably.

---

## ADR-016 — Future Agent Orchestration

**Status:** Accepted as architectural constraint, not MVP scope

**Decision:** Future agent/subagent orchestration must remain possible, including concurrent delegated tasks and different model selection per task.

**Rationale:** Avoid building a fundamentally synchronous architecture that would prevent future orchestration.

**Implementation:** Deferred.

---

# 34. MVP Scope

## Must Have

### Application

- macOS desktop application
- Avalonia UI
- MVVM
- CommunityToolkit
- separate client/server architecture
- bundled backend
- headless local backend mode

### Chat

- persistent conversations
- conversation list
- rename
- delete
- text input
- streaming responses
- cancellation
- Markdown
- syntax-highlighted code
- optional exposed reasoning/thinking
- inline supported media
- Save As

### Models

- Ollama provider
- installed model management
- purpose-oriented model finder backed initially by Hugging Face
- 24-hour catalog cache
- Sync Now
- hardware-aware compatibility information

### Networking

- HTTPS
- local backend connection
- remote backend connection architecture
- pairing
- persistent pairings
- automatic local backend startup

### Persistence

- SQLite backend database
- separate client/server data
- `~/.zaphira/`

---

# 35. Explicitly Deferred

These should not delay the first useful release:

- TTS implementation
- STT implementation
- animated avatar
- multiple LLM providers
- embedded model runtimes
- sophisticated multi-agent orchestration
- large tool ecosystem
- distilled long-term memories
- advanced back-office server UI
- polished Windows support
- automatic model recommendation beyond basic compatibility
- cloud-provider integrations

Interfaces should be introduced only where doing so meaningfully protects the architecture.

---

# 36. Suggested MVP Build Order

## Phase 1 — Skeleton

1. Create solution and project structure.
2. Establish domain models.
3. Establish client/server process model.
4. Establish configuration/data directories.
5. Establish dependency injection.
6. Establish logging.
7. Create minimal Avalonia shell.

## Phase 2 — Server

1. ASP.NET Core host.
2. HTTPS.
3. Certificate generation.
4. Health endpoint.
5. Local process startup/shutdown behavior.
6. SQLite and migrations.
7. Conversation persistence.

## Phase 3 — Ollama

1. Ollama discovery.
2. Installed model listing.
3. Model selection.
4. Basic generation.
5. Streaming investigation.
6. Cancellation.
7. Provider abstraction based on observed runtime behavior.

## Phase 4 — Client Chat

1. Chat layout.
2. Conversation list.
3. Message-part model.
4. Streaming presentation.
5. Stop control.
6. Persistence.
7. Markdown rendering.
8. Code highlighting.
9. Inline media presentation.

## Phase 5 — Model Finder and Catalog

1. Hugging Face integration.
2. Catalog caching.
3. Purpose/capability taxonomy.
4. Search and filtering.
5. Capability metadata.
6. Compatibility evaluation.
7. Directly usable / possibly usable / unsupported / unknown result states.
8. Installed/downloaded model management.
9. Sync Now.

## Phase 6 — Remote Connectivity

1. Backend discovery/check.
2. Pairing.
3. Persistent pairing credentials.
4. Certificate identity handling.
5. Remote connection settings.
6. Graceful connection failure/recovery.

## Phase 7 — Onboarding & Polish

1. First-run model selection.
2. Settings shortcut from onboarding.
3. Empty states.
4. Error handling.
5. Loading states.
6. Keyboard navigation.
7. Accessibility.
8. macOS packaging.
9. Upgrade/data-preservation testing.

---

# 37. MVP Definition of Done

Zaphira MVP is considered successful when a new user can:

1. Drag the application into Applications.
2. Launch Zaphira.
3. Have Zaphira automatically start its bundled backend if required.
4. See compatible installed/downloadable models.
5. Select a model.
6. Enter a chat.
7. Send a text message.
8. Watch the response stream in.
9. See exposed reasoning when the selected runtime actually provides it.
10. Cancel a response.
11. Read properly rendered Markdown.
12. View supported media inline.
13. Save media/files with Save As.
14. Close Zaphira.
15. Reopen it.
16. Find the conversation exactly where they left it.
17. Rename and delete conversations.
18. Browse cached model information offline.
19. Explicitly synchronize the model catalog when Internet access is available.
20. Configure a remote Zaphira backend and pair with it securely over HTTPS.

The application should feel like a **complete local chat product**, rather than a technical demo of an Ollama wrapper.

---

# 38. Guiding Architectural Rule

The most important rule for future development is:

> **Zaphira should depend on semantic capabilities, not on the implementation details of today's model runner.**

Ollama is the first provider.

Avalonia is the first client.

Hugging Face is the first model catalog/source strategy.

SQLite is the first persistence implementation.

None of those should accidentally become the definition of what Zaphira is.
