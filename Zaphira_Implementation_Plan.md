# Zaphira — Broad Implementation Plan

> **Source spec:** `Zaphira_Architecture_MVP.md`  
> **Purpose:** Track the broad implementation path from MVP architecture to a usable local-first desktop chat product.

---

## 0. Project Baseline

- [x] Confirm .NET SDK baseline.
- [x] Confirm Avalonia version baseline.
- [x] Confirm test framework and assertion library.
- [x] Confirm code formatting/analyzer setup.
- [x] Add repository README.
- [x] Add `.gitignore`.
- [x] Add basic developer setup notes.

**Done when:**

- [x] A new developer can clone the repository, restore dependencies, build, and run tests.

---

## 1. Solution Skeleton

- [x] Create `Zaphira.sln`.
- [x] Create `src/Zaphira.Domain`.
- [x] Create `src/Zaphira.Application`.
- [x] Create `src/Zaphira.Contracts`.
- [x] Create `src/Zaphira.Infrastructure`.
- [x] Create `src/Zaphira.Server`.
- [x] Create `src/Zaphira.Client`.
- [x] Create test projects for core layers.
- [x] Wire project references according to the dependency rules.
- [x] Enable nullable reference types.
- [x] Enable warnings/analyzers appropriate for strict null safety.

**Done when:**

- [x] The solution builds.
- [x] Empty test projects run successfully.
- [x] Dependency direction prevents UI/backend/runtime details from leaking into domain models.

---

## 2. Engineering Guardrails

- [x] Add shared build settings.
- [x] Add formatting rules.
- [x] Add analyzer rules.
- [x] Add test naming conventions.
- [x] Add visibility/null-safety guidance to code review checklist.
- [x] Decide how expected failures are represented at application boundaries.
- [x] Establish async/cancellation conventions.

**Done when:**

- [x] The repository enforces or documents the main engineering rules from the architecture spec.
- [x] New model types cannot accidentally rely on uninitialized null properties.

---

## 3. Core Domain and Contracts

- [x] Define conversation identifiers.
- [x] Define message identifiers.
- [x] Define message roles.
- [x] Define semantic message model.
- [x] Define ordered message parts.
- [x] Define text message part.
- [x] Define file/media reference message parts.
- [x] Define reasoning message part placeholder.
- [x] Define unknown/unsupported part handling.
- [x] Define message status values.
- [x] Define conversation summary/list model.
- [x] Define provider/model identifiers.
- [x] Define provider capability model.
- [x] Add domain tests for invariants.

**Done when:**

- [x] Conversations and messages can be represented without UI, HTTP, SQLite, or Ollama dependencies.
- [x] No domain/contract model uses null to represent missing state.

---

## 4. Server Foundation

- [x] Create ASP.NET Core server host.
- [x] Add health endpoint.
- [x] Add structured logging.
- [x] Add server configuration loading.
- [x] Create server data directory under `~/.zaphira/server/`.
- [x] Add graceful startup and shutdown behavior.
- [x] Add basic error response conventions.
- [x] Add server tests for health/config behavior.

**Done when:**

- [x] The server can run headlessly.
- [x] The server exposes a reachable health endpoint.
- [x] Server logs are useful without recording conversation content.

---

## 5. Persistence

- [x] Add SQLite database.
- [x] Add migration mechanism.
- [x] Create conversation schema.
- [x] Create message schema.
- [x] Create message-part persistence.
- [x] Create generated/received file metadata schema.
- [x] Store large binary content outside SQLite.
- [x] Implement conversation repository.
- [x] Implement message repository.
- [x] Add tests for persistence and migrations.
- [x] Add partial-message persistence behavior.

**Done when:**

- [x] Conversations survive server restarts.
- [x] Partial, cancelled, failed, and completed assistant messages are represented consistently.
- [x] Database migrations are versioned and tested.

---

## 6. Client Foundation

- [x] Create Avalonia application shell.
- [x] Add MVVM infrastructure using CommunityToolkit.
- [x] Create client configuration loading.
- [x] Create client data directory under `~/.zaphira/client/`.
- [x] Add client logging.
- [x] Add settings shell.
- [x] Add backend connection state.
- [x] Add first-run shell.
- [x] Add basic navigation.

**Done when:**

- [x] The client starts cleanly.
- [x] The client can show connected, connecting, unavailable, and setup-required states.
- [x] UI state is separated from server-owned conversation state.

---

## 7. Local Backend Process Management

- [x] Bundle or locate the server payload for local development.
- [x] Implement client-launched backend startup.
- [x] Track whether the client owns the backend process.
- [x] Avoid stopping a backend the client did not start.
- [x] Implement graceful shutdown for owned local backend.
- [x] Handle backend startup failures.
- [x] Add retry behavior.

**Done when:**

- [x] Launching the client can start/connect to a local backend.
- [x] Backend ownership rules are respected.
- [x] Failure states are clear and recoverable.

---

## 8. HTTPS and Local Trust

- [x] Generate runtime certificate when needed.
- [x] Store certificate material in the server data directory or platform-appropriate store.
- [x] Configure ASP.NET Core HTTPS.
- [x] Configure client trust for local client-launched backend.
- [x] Associate connection records with backend identity/certificate information.
- [x] Add diagnostics for certificate/trust failures.

**Done when:**

- [x] Client/server communication uses HTTPS.
- [x] Local first-run remains zero-intervention when possible.
- [x] Trust failures are explained clearly.

---

## 9. Provider Foundation

- [x] Define LLM provider interface around semantic capabilities.
- [x] Define model listing contract.
- [x] Define generation request contract.
- [x] Define generation event contract.
- [x] Define cancellation behavior.
- [x] Define provider error model.
- [x] Add fake/test provider.
- [x] Add provider contract tests.

**Done when:**

- [x] Chat can be exercised through a fake provider without Ollama.
- [x] The application layer does not depend on Ollama-specific behavior.

---

## 10. Ollama Provider

- [x] Detect Ollama availability.
- [x] Decide whether CLI, HTTP API, or both are used internally by the provider.
- [x] List installed Ollama models.
- [x] Inspect model metadata where available.
- [x] Generate a basic response.
- [x] Stream response output.
- [x] Propagate cancellation.
- [x] Surface provider errors clearly.
- [x] Add integration tests gated on local Ollama availability.

**Done when:**

- [x] An installed Ollama model can produce a streamed response through the provider abstraction.
- [x] Missing or unusable Ollama is represented as a clear provider availability state.

---

## 11. Chat API

- [x] Create conversation endpoints.
- [x] Create message retrieval endpoints.
- [x] Create send-message endpoint.
- [x] Create streaming generation endpoint.
- [x] Create cancellation endpoint or cancellation mechanism.
- [x] Persist user messages.
- [x] Persist assistant message state during generation.
- [x] Return clear errors for missing provider/model/backend state.
- [x] Add server/API tests.

**Done when:**

- [x] A client can create/select a conversation, send text, stream an answer, cancel generation, and reload persisted history.

---

## 12. Chat UI

- [ ] Build main chat layout.
- [ ] Build conversation list.
- [ ] Build conversation rename.
- [ ] Build conversation delete with confirmation or undo.
- [ ] Build message composer.
- [ ] Build streaming assistant message view.
- [ ] Build stop control.
- [ ] Build message status/error presentation.
- [ ] Build Markdown rendering.
- [ ] Build syntax-highlighted code rendering.
- [ ] Build fallback rendering for unknown message parts.
- [ ] Add keyboard and accessibility basics.

**Done when:**

- [ ] A user can chat with an installed local model and see persisted conversation history after restart.
- [ ] Streaming and cancellation feel responsive.

---

## 13. First-Run and Availability States

- [ ] Detect backend unavailable.
- [ ] Detect no provider/runtime available.
- [ ] Detect no installed model.
- [ ] Detect offline with no cached catalog.
- [ ] Detect catalog unavailable.
- [ ] Provide clear blocking states.
- [ ] Provide Retry action.
- [ ] Provide Settings action.
- [ ] Preserve onboarding state when returning from settings.

**Done when:**

- [ ] Missing dependencies never look like a broken app.
- [ ] The user gets a clear explanation and the next sensible action.

---

## 14. Model Finder and Catalog

- [ ] Create catalog-source abstraction.
- [ ] Implement Hugging Face catalog source.
- [ ] Cache catalog metadata locally.
- [ ] Add 24-hour normal cache policy.
- [ ] Add Sync Now operation.
- [ ] Define purpose/capability taxonomy.
- [ ] Implement search by name.
- [ ] Implement purpose/capability filters.
- [ ] Represent compatibility confidence.
- [ ] Distinguish directly usable, possibly usable, unsupported, and unknown models.
- [ ] Explain why a model matched where practical.
- [ ] Preserve cached catalog access offline.

**Done when:**

- [ ] Users can find potentially useful models in-app by purpose and practical fit.
- [ ] Hugging Face is a source strategy, not the product boundary.

---

## 15. Hardware-Aware Compatibility

- [ ] Detect operating system.
- [ ] Detect CPU.
- [ ] Detect physical/system memory.
- [ ] Detect GPU information where practical.
- [ ] Detect unified memory where practical.
- [ ] Add configurable memory headroom.
- [ ] Estimate compatibility conservatively.
- [ ] Explain uncertainty in compatibility results.
- [ ] Add tests for compatibility calculations.

**Done when:**

- [ ] Zaphira avoids presenting obviously unsuitable models as compatible.
- [ ] Compatibility results expose uncertainty instead of pretending to be perfect.

---

## 16. Model Management

- [ ] List installed models.
- [ ] Select active/default model.
- [ ] Install/download model through provider-specific mechanism.
- [ ] Show installation progress where available.
- [ ] Cancel installation where supported.
- [ ] Remove installed models.
- [ ] Handle disk/network/provider failures.
- [ ] Keep provider-specific mechanics behind provider abstractions.

**Done when:**

- [ ] Users can see and manage installed local models without understanding provider internals.

---

## 17. Remote Backend and Pairing

- [ ] Add remote backend address setting.
- [ ] Check whether a remote Zaphira backend is reachable.
- [ ] Implement human-mediated pairing flow.
- [ ] Generate/display pairing code on backend.
- [ ] Accept pairing code in client.
- [ ] Issue and store pairing credentials.
- [ ] Bind pairing to backend identity/certificate.
- [ ] Persist connection information.
- [ ] View known pairings.
- [ ] Remove/revoke pairings.
- [ ] Surface connection and trust errors clearly.

**Done when:**

- [ ] A user can intentionally pair with a trusted backend over HTTPS.
- [ ] Revoked pairings are not silently reused.

---

## 18. Media and Files

- [ ] Define backend-owned file storage layout.
- [ ] Define client cache behavior.
- [ ] Render image parts inline.
- [ ] Render other supported media where practical.
- [ ] Render files clearly.
- [ ] Implement Save As for media/file parts.
- [ ] Prevent large binary content from being stored directly in SQLite.
- [ ] Add fallback presentation for unsupported media.

**Done when:**

- [ ] Semantic media/file message parts can be displayed, cached, and saved without making the client authoritative.

---

## 19. Packaging and Upgrade Safety

- [ ] Choose macOS packaging approach.
- [ ] Produce self-contained or runtime-dependent build.
- [ ] Bundle backend payload.
- [ ] Ensure headless backend does not create an unwanted Dock icon.
- [ ] Verify user data survives app replacement.
- [ ] Add basic upgrade/migration test path.
- [ ] Document runtime requirements.

**Done when:**

- [ ] A user can install/replace the app without losing conversations or settings.

---

## 20. Observability and Diagnostics

- [ ] Add rolling client logs.
- [ ] Add rolling server logs.
- [ ] Add configurable retention.
- [ ] Add startup/shutdown diagnostics.
- [ ] Add backend connection diagnostics.
- [ ] Add TLS/pairing diagnostics.
- [ ] Add provider/model diagnostics.
- [ ] Ensure conversation content is not logged by default.

**Done when:**

- [ ] Failures can be diagnosed without exposing sensitive content by default.

---

## 21. MVP Validation

- [ ] Fresh install starts.
- [ ] Local backend starts automatically when needed.
- [ ] Missing backend/provider/model states are clear.
- [ ] Installed model can be selected.
- [ ] Text message can be sent.
- [ ] Assistant response streams.
- [ ] Generation can be cancelled.
- [ ] Markdown renders.
- [ ] Code blocks render readably.
- [ ] Conversations persist after restart.
- [ ] Conversations can be renamed.
- [ ] Conversations can be deleted safely.
- [ ] Cached model metadata works offline.
- [ ] Sync Now refreshes when online.
- [ ] Remote backend can be configured and paired.
- [ ] Logs are useful and do not record sensitive content by default.

**Done when:**

- [ ] Zaphira feels like a complete local chat product rather than a technical demo.
