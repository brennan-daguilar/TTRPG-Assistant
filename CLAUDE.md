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

## Development Commands
```bash
# Backend
cd src/backend && dotnet run

# Frontend
cd src/frontend && npm run dev

# Docker (PostgreSQL + pgvector)
docker compose up -d
```

## Design Principles
- Privacy-first: All data local by default, cloud LLM opt-in
- Provider-agnostic: LLM backend swappable via configuration
- GM-controlled: Automated note updates proposed, never auto-applied
- Incremental delivery: Each phase is standalone useful

## Implementation Phases
- **Phase 1:** RAG-Powered Q&A (current focus)
- **Phase 2:** Session Transcription & Note Generation
- **Phase 3:** Live Contextual GM Screen
