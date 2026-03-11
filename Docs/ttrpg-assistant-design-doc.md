# TTRPG Worldbuilding Assistant

**Design Document & Implementation Plan**

A RAG-powered application for managing TTRPG worldbuilding content with live session transcription, contextual note surfacing, and AI-assisted lore management.

**Stack:** ASP.NET Core 10 · React · TypeScript · SignalR · PostgreSQL + pgvector · Ollama

*March 2026*

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [System Architecture](#2-system-architecture)
3. [Phase 1: RAG-Powered Q&A](#3-phase-1-rag-powered-qa)
4. [Phase 2: Session Transcription & Note Generation](#4-phase-2-session-transcription--note-generation)
5. [Phase 3: Live Contextual GM Screen](#5-phase-3-live-contextual-gm-screen)
6. [Infrastructure & Deployment](#6-infrastructure--deployment)
7. [Risks & Mitigations](#7-risks--mitigations)
8. [Future Considerations](#8-future-considerations)

---

## 1. Project Overview

### 1.1 Vision

The TTRPG Worldbuilding Assistant is a personal tool for game masters who maintain extensive worldbuilding notes and want an intelligent interface for managing that knowledge. The application provides three core capabilities:

- **Conversational Q&A** against a worldbuilding knowledge base using Retrieval-Augmented Generation (RAG)
- **Live session transcription** with automatic entity recognition and note updates
- **A real-time contextual GM screen** that dynamically surfaces relevant lore during play

### 1.2 Core Design Principles

- **Privacy-first:** All data stays on the user's machines by default. Cloud LLM usage is opt-in and clearly indicated.
- **Provider-agnostic:** The LLM backend is swappable between local (Ollama), remote self-hosted (OpenAI-compatible), and cloud providers (OpenAI, Anthropic) through configuration alone.
- **GM-controlled:** Automated note updates are always proposed, never applied directly. The GM reviews and approves all changes to canonical lore.
- **Incremental delivery:** Each phase delivers a standalone, usable feature. Later phases build on earlier ones but earlier phases are valuable on their own.

### 1.3 Technology Stack

| Component | Technology | Notes |
|---|---|---|
| Backend API | ASP.NET Core 10 | REST + SignalR hubs for real-time |
| Frontend | React + TypeScript | Vite build, Mantine UI |
| Vector Database | PostgreSQL + pgvector | Unified DB for relational and vector storage |
| Embedding Model | nomic-embed-text (via Ollama) | 768-dim vectors, runs locally |
| LLM (Local) | Ollama (Llama 3 / Mistral) | OpenAI-compatible REST API |
| LLM (Cloud) | OpenAI / Anthropic APIs | Configured via provider abstraction |
| LLM Abstraction | Microsoft.Extensions.AI | Unified interface for all providers |
| Transcription | Whisper / faster-whisper | Local, supports custom vocabulary |
| Speaker Diarization | pyannote.audio | Maps speaker segments to identities |
| Data Storage | PostgreSQL | Entity metadata, session logs, user config, vector embeddings (via pgvector) |
| Real-time Comms | SignalR | Streaming LLM responses, live GM screen updates |

---

## 2. System Architecture

### 2.1 High-Level Architecture

The system is composed of five major subsystems that communicate through well-defined interfaces. Each subsystem can run independently and be developed and tested in isolation.

- **Document Ingestion Pipeline:** Parses worldbuilding notes into structured chunks, generates embeddings, and stores them in the vector database. Handles initial bulk import and incremental updates.
- **RAG Query Engine:** Receives natural language questions, retrieves relevant chunks from the vector database, constructs augmented prompts, and sends them to the configured LLM provider. Returns streamed responses via SignalR.
- **Audio Processing Pipeline:** Captures session audio, runs speech-to-text via Whisper, performs speaker diarization, and produces a timestamped, speaker-attributed transcript.
- **Session Intelligence Engine:** Analyzes transcript segments in near-real-time to extract mentioned entities, narrative events, and proposed note updates. Powers both the live GM screen and post-session note generation.
- **React Frontend:** Provides the conversational interface, document management UI, live GM screen, and session review/approval workflow.

### 2.2 LLM Provider Abstraction

The application uses `Microsoft.Extensions.AI` to abstract LLM communication behind a unified interface. The core abstraction exposes two capabilities: chat completion (for generation) and embedding generation (for vector search). Each is independently configurable — you could run embeddings locally via Ollama while routing generation to a cloud provider.

Provider configuration is stored per-user and switchable at runtime. A settings screen allows the GM to configure endpoints, API keys, and model names for each provider. The application validates connectivity on configuration change and falls back gracefully if a provider becomes unavailable.

> **Provider Configuration Example**
> - Local: Ollama at `http://localhost:11434` using `llama3`
> - Remote: Ollama at `http://192.168.1.50:11434` using `mixtral`
> - Cloud: Anthropic API using `claude-sonnet-4-20250514`
> - The embedding model can be configured independently from the generation model.

### 2.3 Backend Architecture

The backend follows a **modular monolith with vertical slices**. All modules run in a single process (no microservice overhead) but are organized into cohesive, independently-developable slices. Each module owns its own EF Core DbContext, API endpoints, and domain logic. Shared infrastructure (database connection, AI clients, RAG pipeline) lives in well-defined cross-cutting layers.

```
/src/backend
  /Modules
    /KnowledgeBase      # Entity CRUD, ingestion, document management
    /Conversations      # RAG Q&A, chat history, tool calling
    /Sessions           # Audio capture, transcription, post-session review
    /GMScreen           # Live entity detection, relevance scoring, SignalR hub
  /Infrastructure
    /Database           # EF Core, pgvector, migrations
    /AI
      /LLM              # Microsoft.Extensions.AI, provider abstraction
      /Embeddings       # Embedding generation, model config
      /Transcription    # Whisper, diarization integration
  /Common
    /RAG                # Shared retrieval pipeline logic (chunking, search, re-ranking)
```

Each module exposes its functionality through a clean internal API (interfaces, not direct class references) so modules can be extracted to separate services later if needed.

### 2.4 Data Model

The application uses **PostgreSQL as the single data store** for both relational data and vector embeddings, via the `pgvector` extension. This simplifies the deployment footprint significantly — one database handles entity metadata, relationships, session logs, user configuration, and embedded chunks. The `Chunks` table carries a `vector(768)` column indexed with HNSW for fast approximate nearest-neighbor search.

> **Why pgvector over a dedicated vector DB?**
> For a personal tool with a knowledge base that fits comfortably in a single PostgreSQL instance, pgvector eliminates the operational overhead of running and syncing a separate vector database. Qdrant remains a viable migration path if scale demands it later.



#### 2.4.1 Entity Types

The system supports a flexible entity type system. Built-in types include Characters, Locations, Factions, Items, Events, and Lore Articles. Each entity has structured metadata fields (name, type, tags, relationships) plus freeform note content that gets chunked and embedded. Users can define custom entity types with custom metadata fields.

#### 2.4.2 Chunking Strategy

Worldbuilding content is chunked by logical section rather than arbitrary token count. Each entity's notes are split at section boundaries (headings, paragraph breaks in longer entries) with a target chunk size of 300–500 tokens. Each chunk carries metadata including:

- Source entity ID and name
- Entity type
- Tags
- Section heading (if applicable)
- List of other entities referenced in the chunk

This metadata enables filtered retrieval (e.g., "search only location entries") and relationship-aware retrieval.

---

## 3. Phase 1: RAG-Powered Q&A

### 3.1 Overview

Phase 1 delivers the foundational RAG system: the ability to import worldbuilding notes, ask questions in natural language, receive answers grounded in your lore, and update notes through conversation. This phase establishes the core data pipeline and LLM integration that all subsequent phases build on.

### 3.2 Document Ingestion

The ingestion pipeline accepts content through multiple paths:
- Direct entry in the application UI
- Import from Markdown files
- Import from plain text files
- Bulk import from a folder structure

On import, each document is parsed into entities (one entity per file, or multiple entities per file with delimiter-based splitting), chunked according to the strategy described in Section 2.4.2, embedded via the configured embedding model, and stored in both the relational database (metadata) and Qdrant (vectors).

Re-ingestion is incremental. When a note is updated, only the affected chunks are re-embedded. A hash-based change detection system avoids unnecessary re-processing.

### 3.3 Retrieval Pipeline

When the user asks a question, the retrieval pipeline runs through several stages:

1. **Query embedding:** The user's question is embedded using the same model that embedded the chunks.
2. **Vector search:** pgvector performs an HNSW approximate nearest-neighbor search, returning the top-K most similar chunks (default K=10). Metadata filters can narrow the search to specific entity types or tags.
3. **Keyword augmentation:** A parallel keyword search runs against entity names and tags to catch exact-match references that vector search might rank lower. Results are merged with vector results.
4. **Re-ranking (optional):** If a re-ranker model is configured, the candidate chunks are re-scored for relevance to the specific question. The top-N chunks (default N=5) proceed to generation.
5. **Prompt construction:** The selected chunks are formatted into a system prompt that includes the worldbuilding context, instructions for how the LLM should use the context, and the user's question.
6. **Generation:** The prompt is sent to the configured LLM, and the response is streamed back to the frontend via SignalR.

### 3.4 Note Updates via Conversation

The LLM is given tool-calling capability with two core tools: `update_note` (modifies an existing entity's content) and `create_note` (creates a new entity). When the user instructs the assistant to make changes (e.g., "update the blacksmith's notes to show he's now hostile"), the LLM invokes the appropriate tool with structured parameters.

All tool invocations are presented to the user for approval before execution. The UI shows a diff-style preview of the proposed change. On approval, the change is committed to the relational database and the affected chunks are re-embedded. On rejection, the change is discarded and the assistant is informed.

### 3.5 Conversation History

The application maintains conversation history within a session to enable multi-turn interactions. The context window budget is managed carefully: system prompt and RAG context get priority allocation, followed by the most recent conversation turns, with older turns summarized or dropped as needed. A conversation summary is generated periodically to preserve important context when older messages are evicted.

### 3.6 Implementation Tasks

| Task | Description | Duration |
|---|---|---|
| Project scaffolding | ASP.NET Core 10 Web API + React/Vite frontend, modular monolith solution structure, Docker Compose for PostgreSQL | 2–3 days |
| LLM provider abstraction | Microsoft.Extensions.AI integration, Ollama provider, configuration UI | 2–3 days |
| Entity data model | Database schema, EF Core migrations, pgvector setup, CRUD API endpoints | 2–3 days |
| Ingestion pipeline | Markdown/text parsing, chunking logic, embedding generation, pgvector storage | 3–4 days |
| Retrieval pipeline | Vector search, keyword augmentation, prompt construction | 3–4 days |
| Conversational UI | React chat interface, SignalR streaming, message history | 3–4 days |
| Tool calling / note updates | Update and create tools, approval UI, diff preview, re-embedding | 3–4 days |
| Document management UI | Entity browser, editor, import/export, tag management | 3–4 days |
| Cloud provider support | OpenAI and Anthropic provider configs, API key management | 1–2 days |
| Testing and polish | Integration tests, error handling, loading states, edge cases | 3–4 days |

> **Phase 1 Milestone:** At the end of Phase 1, you have a fully functional worldbuilding knowledge base with natural language Q&A and AI-assisted note management. This is a useful standalone tool even if you never build the later phases.

---

## 4. Phase 2: Session Transcription & Note Generation

### 4.1 Overview

Phase 2 adds the ability to record a TTRPG session, transcribe it with speaker attribution, and process the transcript to automatically generate proposed note updates. This phase runs primarily as a post-session workflow, with audio recording happening during the session and processing happening afterward.

### 4.2 Audio Capture

The application supports two audio input modes:
- **Single microphone mode:** Uses one omnidirectional microphone and relies entirely on diarization to separate speakers.
- **Multi-channel mode:** Uses separate audio channels per speaker (via a multi-input audio interface), making speaker identification trivial since each channel maps to a known person.

Audio is captured in the browser using the Web Audio API and streamed to the backend in chunks (configurable interval, default 30 seconds). Chunks are written to disk as WAV files and queued for processing. A session recording can also be uploaded after the fact as a single audio file.

### 4.3 Transcription Pipeline

Each audio chunk is processed through a two-stage pipeline:

- **Speaker diarization (single-mic mode):** pyannote.audio segments the audio into speaker turns. A calibration step at session start creates voice profiles for each player. The system assigns speaker labels to each segment, which the GM can correct in the UI if needed.
- **Speech-to-text:** Whisper (or faster-whisper) transcribes each segment. The initial prompt is seeded with entity names from the worldbuilding database to improve recognition of proper nouns and fantasy terms. The output is a timestamped, speaker-attributed transcript.

### 4.4 Transcript Processing

After the session (or during, in chunks), the transcript is analyzed by the LLM in a multi-pass extraction pipeline:

1. **Event extraction:** The LLM reads the transcript in overlapping windows and identifies in-game narrative events, filtering out out-of-character discussion, rules debates, and social chatter.
2. **Entity resolution:** Identified events are cross-referenced against the existing knowledge base. Fuzzy references ("that dwarf city," "her sword") are resolved to specific entities using conversational context and the entity database.
3. **Update generation:** For each affected entity, the LLM generates a proposed update that integrates the new information with existing notes. New entities mentioned for the first time generate proposed create operations.
4. **Session summary:** A high-level session summary is generated, capturing the major narrative beats, decisions made, and plot threads advanced or introduced.

### 4.5 Review Workflow

All proposed updates are queued in a review interface. The GM sees each proposed change as a card showing:
- The affected entity
- A diff of the proposed changes
- The transcript excerpt that triggered the change
- A confidence indicator

The GM can approve, edit, or reject each proposal individually or in bulk. Approved changes are committed and re-embedded. The session summary is saved as a Session Log entity in the knowledge base.

### 4.6 Implementation Tasks

| Task | Description | Duration |
|---|---|---|
| Audio capture service | Web Audio API recording, chunk streaming to backend, file storage | 2–3 days |
| Whisper integration | faster-whisper setup, custom vocabulary seeding, segment processing | 3–4 days |
| Speaker diarization | pyannote.audio setup, calibration workflow, speaker profile management | 3–4 days |
| Transcript assembly | Merge diarization + transcription, timestamped speaker-attributed output | 2–3 days |
| Event extraction pipeline | LLM prompts for narrative extraction, windowed processing, OOC filtering | 3–4 days |
| Entity resolution | Fuzzy matching against knowledge base, contextual disambiguation | 3–4 days |
| Update generation | Diff generation, new entity detection, session summary creation | 2–3 days |
| Review UI | Proposal cards, diff viewer, bulk approve/reject, transcript excerpt linking | 3–4 days |
| Audio upload support | Post-session file upload, batch processing pipeline | 1–2 days |
| Testing and polish | End-to-end session recording tests, accuracy tuning, edge cases | 3–4 days |

> **Phase 2 Milestone:** At the end of Phase 2, you can record your game sessions and have the system automatically propose updates to your worldbuilding notes based on what happened in play. The GM reviews and approves all changes.

---

## 5. Phase 3: Live Contextual GM Screen

### 5.1 Overview

Phase 3 transforms the transcription pipeline into a real-time tool that dynamically surfaces relevant worldbuilding notes during play. As the conversation flows at the table, the GM's screen automatically displays notes about the characters, locations, items, and factions currently being discussed.

### 5.2 Real-Time Transcription

The transcription pipeline from Phase 2 is adapted to run in streaming mode. Audio chunks are shortened to 10–15 second intervals for lower latency. Whisper processes chunks as they arrive, producing transcript segments that feed into a rolling context buffer. The buffer maintains the last 3–5 minutes of conversation, providing enough context for entity resolution without excessive processing overhead.

### 5.3 Entity Detection & Resolution

The Session Intelligence Engine monitors the rolling transcript buffer and performs lightweight entity detection on each new segment using a two-tier approach:

- **Fast path (keyword matching):** A pre-built index of entity names, aliases, and common shorthand references is checked against the transcript. This catches direct name mentions with near-zero latency. The alias list is populated from entity metadata and can be extended by the GM.
- **Smart path (LLM-assisted):** Periodically (every 30–60 seconds), the rolling buffer is sent to the LLM with a prompt to identify any characters, locations, items, or factions being discussed. This catches indirect references, pronouns, and contextual mentions that keyword matching would miss.

Detected entities are merged and deduplicated. A recency-weighted scoring system prioritizes entities that are actively being discussed over those merely mentioned in passing.

### 5.4 GM Screen Display

The GM screen is a React-based dashboard that connects to the backend via SignalR for real-time updates. It is organized into three display tiers:

- **Active context (top):** Full note cards for the 2–3 entities most relevant to the current conversation. These cards show the entity's name, type, key metadata, and the full notes content. They update as the conversation shifts focus.
- **Related context (middle):** Compact cards for entities that are related to the active entities (e.g., the faction a character belongs to, items in a location's inventory). These provide quick-reference information without taking up much screen space.
- **Recently mentioned (bottom):** A scrollable list of entities that were discussed recently but aren't the current focus. These fade in relevance over time and eventually drop off.

### 5.5 GM Interaction

The GM can interact with the live screen in several ways:
- **Pinning:** Locks an entity card to the active tier regardless of automated relevance scoring.
- **Dismissing:** Removes an incorrectly surfaced entity and deprioritizes it for the near future.
- **Expanding:** Opens a full entity view with edit capability.
- **Quick-noting:** Lets the GM dictate a quick note that gets attached to the currently active entity for post-session review.

The GM can also trigger the Phase 1 Q&A chat from the live screen, with the current context automatically included.

### 5.6 Latency Budget

For the live screen to feel responsive, the end-to-end pipeline from speech to displayed notes needs to complete within 5–10 seconds. Target breakdown:

| Stage | Target Latency |
|---|---|
| Audio chunk capture | 10–15 seconds of buffering |
| Whisper transcription | 1–3 seconds per chunk |
| Keyword entity detection | < 100 milliseconds |
| LLM entity detection (batched) | 2–4 seconds |
| Note retrieval from pgvector | < 200 milliseconds |

Aggressive pre-fetching helps hide latency. When the party says they're heading to a new location, the system begins pulling notes for that location and its associated entities before the players arrive and start interacting.

### 5.7 Debouncing & Stability

To prevent the GM screen from flickering as conversation shifts rapidly, several stabilization mechanisms are applied:

- **Minimum display duration:** Entity cards stay visible for at least 30 seconds once surfaced.
- **Fade transitions:** Smooth animations rather than abrupt appearance/disappearance.
- **Hysteresis:** An entity's relevance score must drop below a lower threshold to be removed (not just below the display threshold), preventing cards from toggling on and off rapidly.
- **Batch updates:** Collected and applied on a 2–3 second cadence rather than individually.

### 5.8 Implementation Tasks

| Task | Description | Duration |
|---|---|---|
| Streaming transcription | Adapt Phase 2 pipeline for 10–15s chunks, rolling buffer management | 2–3 days |
| Keyword entity index | Build name/alias index from entity database, fast substring matching | 1–2 days |
| LLM entity detection | Prompt design for contextual entity extraction, batched processing | 2–3 days |
| Relevance scoring | Recency-weighted scoring, tier assignment logic, hysteresis | 2–3 days |
| SignalR hub for GM screen | Real-time entity update stream, connection management | 1–2 days |
| GM screen UI | Three-tier card layout, pin/dismiss/expand interactions, responsive design | 4–5 days |
| Pre-fetching engine | Anticipatory note retrieval based on mentioned-but-not-yet-active entities | 2–3 days |
| Quick note capture | Voice/text quick-note attached to active entity, queued for review | 1–2 days |
| Q&A integration | Launch chat from GM screen with current context, context handoff | 1–2 days |
| Performance tuning | Latency profiling, buffer tuning, debounce calibration, load testing | 3–4 days |

> **Phase 3 Milestone:** At the end of Phase 3, the GM has a live dashboard that acts as an intelligent, context-aware reference screen during play. Combined with the earlier phases, the application handles the full lifecycle: pre-session worldbuilding, in-session reference, and post-session documentation.

---

## 6. Infrastructure & Deployment

### 6.1 Development Environment

The recommended development setup uses Docker Compose to orchestrate PostgreSQL (with the pgvector extension) alongside the ASP.NET Core backend and React dev server. Ollama runs natively on the host machine to access the GPU directly. A single `docker-compose.yml` file brings up the full development stack.

### 6.2 Production Deployment

Since this is a personal tool, "production" means running reliably on the GM's own hardware. The deployment model is a Docker Compose stack that includes:
- The ASP.NET Core backend serving the built React frontend as static files
- PostgreSQL with the pgvector extension (handles both relational data and vector storage)

Ollama runs on the host or a separate GPU machine. The entire stack should be launchable with a single `docker compose up` command.

### 6.3 Multi-Machine Setup

For the scenario where the LLM runs on a different machine (e.g., a GPU workstation in another room), the application needs only a network-accessible Ollama instance. The remote machine runs Ollama configured to listen on `0.0.0.0`, and the application is configured with that machine's IP address as the LLM endpoint. No other changes are required since all LLM communication goes through the same HTTP API regardless of where it runs.

### 6.4 Hardware Recommendations

- **Minimum (Q&A only):** Any modern machine with 16GB RAM. A 7B parameter model runs adequately on CPU, though slowly. A GPU with 8GB VRAM enables much faster inference.
- **Recommended (all features):** GPU with 12–24GB VRAM for the LLM (RTX 3060 12GB or better), plus a second machine or sufficient CPU headroom for running Whisper and diarization simultaneously during live sessions.
- **Ideal:** Dedicated GPU machine for LLM inference, separate machine at the table running the application server, transcription, and serving the GM screen. Connected via local network.

---

## 7. Risks & Mitigations

- **Transcription accuracy with fantasy names:** Mitigated by seeding Whisper with entity names from the knowledge base. The GM can also add phonetic aliases to improve recognition of particularly unusual names.
- **LLM hallucination in Q&A responses:** Mitigated by explicit instructions in the system prompt to only use provided context, and by showing source citations in the UI so the GM can verify which notes were used.
- **Entity resolution errors during live sessions:** Mitigated by the tiered display system (incorrect entities appear in lower tiers) and by the GM's ability to dismiss irrelevant suggestions. All automated note updates still require approval.
- **Latency in live GM screen:** Mitigated by the two-tier detection approach (fast keyword matching plus periodic LLM analysis), pre-fetching, and configurable chunk intervals. The keyword path provides sub-second response for known entity names.
- **Hardware resource contention during live sessions:** Mitigated by supporting multi-machine deployment and by making the live GM screen features degrade gracefully if resources are constrained (e.g., falling back to keyword-only detection if the LLM is overloaded).
- **Embedding model migration:** If the embedding model is changed, all chunks must be re-embedded. Mitigated by tracking the embedding model version in chunk metadata and providing a batch re-embedding command. With pgvector, this is a single-database operation with no need to sync separate stores.

---

## 8. Future Considerations

The following features are out of scope for the initial three phases but represent natural extensions of the architecture:

- **Player-facing portal:** A read-only (or limited-write) view for players to access their character notes, session summaries, and lore articles the GM has marked as player-visible. Role-based access control would ensure players only see what the GM has released.
- **Map integration:** Linking location entities to positions on uploaded maps, with the GM screen highlighting the party's current location and nearby points of interest.
- **Timeline visualization:** A chronological view of events extracted from session logs, showing the narrative arc of the campaign over time.
- **Multi-campaign support:** Isolated knowledge bases per campaign with the option to share common lore (e.g., a shared setting used across multiple campaigns).
- **VTT integration:** Integration with virtual tabletop platforms (Foundry VTT, Roll20) to sync entity data and potentially capture game state alongside audio.
