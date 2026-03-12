# TTRPG Worldbuilding Assistant

## Project Overview
RAG-powered application for managing TTRPG worldbuilding content with live session transcription, contextual note surfacing, and AI-assisted lore management.

## Tech Stack
- **Backend:** ASP.NET Core 10 (modular monolith, vertical slices)
- **Frontend:** React + TypeScript (Vite, Mantine UI)
- **Database:** PostgreSQL + pgvector
- **LLM Abstraction:** Microsoft.Extensions.AI
- **Real-time:** SignalR
- **Local LLM:** Ollama
- **Transcription:** Whisper / faster-whisper
- **Speaker Diarization:** pyannote.audio

## Project Structure
```
/src/backend          - ASP.NET Core Web API
  /Modules
    /KnowledgeBase    - Entity CRUD, ingestion, document management
    /Conversations    - RAG Q&A, chat history, tool calling
    /Sessions         - Audio capture, transcription, post-session review
    /GMScreen         - Live entity detection, relevance scoring, SignalR hub
  /Infrastructure
    /Database         - EF Core, pgvector, migrations
    /AI
      /LLM            - Microsoft.Extensions.AI, provider abstraction
      /Embeddings     - Embedding generation, model config
      /Transcription  - Whisper, diarization integration
  /Common
    /RAG              - Shared retrieval pipeline logic
/src/frontend         - React + Vite app
/Docs                 - Design docs and references
```

## Quick Start (Run in Separate Terminals)

Terminal 1:
```bash
docker compose up -d
```

Terminal 2:
```bash
ollama serve
```

Terminal 3:
```bash
cd src/backend && dotnet run
# http://localhost:5128
```

Terminal 4:
```bash
cd src/frontend && npm run dev
# http://localhost:5173
```

See **QUICKSTART.md** and **TESTING_GUIDE.md** for detailed instructions.

## Development Commands
```bash
# EF Core migrations
cd src/backend
dotnet ef migrations add <Name> --output-dir Infrastructure/Database/Migrations

# Type-check frontend
cd src/frontend && npx tsc --noEmit

# Build backend
cd src/backend && dotnet build
```

## Design Principles
- Privacy-first: All data local by default, cloud LLM opt-in
- Provider-agnostic: LLM backend swappable via configuration
- GM-controlled: Automated note updates proposed, never auto-applied
- Incremental delivery: Each phase is standalone useful

## Implemented Features (Phase 1)

### ✅ RAG Pipeline
- Document ingestion: Markdown/text import, chunking by sections
- Vector embeddings: 768-dim via nomic-embed-text (Ollama)
- Hybrid retrieval: Vector search + keyword matching
- Source citations: Retrieved entities shown as badges

### ✅ Conversational Q&A
- Streaming LLM responses via SignalR
- Conversation persistence: Messages saved to DB
- Multi-turn context: Last 20 messages loaded for context
- Auto-conversation creation: First message generates conversation

### ✅ Tool Calling for Note Updates
- LLM-guided UPDATE_NOTE / CREATE_NOTE blocks
- Proposal system: All changes require GM approval
- Proposal review: Inline cards with approve/edit/reject
- Bulk operations: Approve/reject multiple at once

### ✅ Document Management
- Full CRUD: Create, read, update, delete entities
- Relationships: Connect entities with typed relationships
- Delete confirmation: Prevents accidental removal
- Cascade deletion: Chunks and embeddings cleaned up

### ✅ User Interface
- Navigation: Chat, Knowledge Base, Import, Proposals
- Dark theme with Mantine UI
- Responsive design (mobile-friendly)
- Real-time updates via SignalR

## Implementation Phases
- **Phase 1:** RAG-Powered Q&A ✅ (complete)
- **Phase 2:** Session Transcription & Note Generation
- **Phase 3:** Live Contextual GM Screen
