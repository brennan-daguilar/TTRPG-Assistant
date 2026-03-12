# TTRPG Worldbuilding Assistant

A RAG-powered application for managing TTRPG (tabletop RPG) worldbuilding content with conversational Q&A, AI-assisted note updates, and real-time contextual information retrieval.

## Features

### 🎲 Conversational Q&A
Ask natural language questions about your worldbuilding notes and get context-aware answers backed by your actual source material. Responses include citations showing which entities were used.

### 📝 Document Management
- Import worldbuilding notes from Markdown or plain text
- Organize entities (Characters, Locations, Factions, Items, Events, etc.)
- Create relationships between entities
- Full-text search and filtering

### 🤖 AI-Assisted Note Updates
The LLM can suggest updates to your notes. All changes require your approval:
- Review proposals inline in chat
- Edit suggested content before approving
- Approve multiple proposals at once
- Keep a full history of changes

### 💾 Conversation History
- Conversations are saved and persistent
- Multi-turn context: the assistant remembers previous messages
- Switch between conversations anytime
- Auto-titled from your first question

## Stack

- **Backend:** ASP.NET Core 10 (modular monolith)
- **Frontend:** React 18 + TypeScript + Mantine UI
- **Database:** PostgreSQL 17 + pgvector (vector search)
- **LLM:** Ollama (local) or cloud providers (OpenAI, Anthropic) via Microsoft.Extensions.AI
- **Real-time:** SignalR for streaming responses

## Getting Started

### Prerequisites
- Docker & Docker Compose
- .NET 10 SDK
- Node.js 18+
- Ollama (for local LLM)

### Quick Start

```bash
# Terminal 1: Start database
docker compose up -d

# Terminal 2: Start Ollama
ollama serve

# Terminal 3: Start backend
cd src/backend && dotnet run
# Listens on http://localhost:5128

# Terminal 4: Start frontend
cd src/frontend && npm run dev
# Opens http://localhost:5173
```

See **QUICKSTART.md** for detailed instructions.

## Testing

Complete testing guide with feature walkthroughs: **TESTING_GUIDE.md**

Quick test:
1. Open http://localhost:5173
2. Go to Import page
3. Paste some worldbuilding notes
4. Go to Chat and ask questions
5. Watch the LLM answer from your own notes

## Architecture

```
/src/backend              # ASP.NET Core API
  /Modules
    /KnowledgeBase        # Entity CRUD, document import
    /Conversations        # Chat, RAG, tool calling
    /GMScreen            # (Phase 2) Live entity detection
  /Infrastructure
    /Database            # EF Core, PostgreSQL, migrations
    /AI                  # LLM abstraction, embeddings, transcription
  /Common/RAG            # Chunking, retrieval, re-ranking

/src/frontend            # React SPA
  /components
    /chat                # ChatInterface, conversation history
    /entities            # Entity editor, list, relationships
  /pages                 # Chat, Entities, Import, Proposals
  /api                   # Typed API client
```

## How It Works

### 1. Import Documents
Documents are parsed into chunks at section boundaries (headers, paragraph breaks). Each chunk is embedded using nomic-embed-text (768-dim vectors stored in pgvector).

### 2. Answer Questions
When you ask a question:
1. Your question is embedded
2. Vector search finds similar chunks (HNSW index)
3. Keyword search catches exact entity name matches
4. Retrieved chunks are formatted into a system prompt
5. LLM generates answer grounded in your context

### 3. Suggest Changes
The LLM can propose note updates using structured blocks:
```
<<<UPDATE_NOTE>>>
Entity: [name]
Description: [what changed]
Content: [new content]
<<<END_NOTE>>>
```

The tool call parser extracts these. You review and approve before the entity is updated.

### 4. Persistent Conversations
Messages are saved to the database. When you switch conversations, the last 20 messages are loaded for context, so the LLM maintains continuity.

## Configuration

### LLM Provider
Edit `src/backend/appsettings.json`:

```json
{
  "LlmProvider": {
    "Provider": "ollama",
    "Endpoint": "http://localhost:11434",
    "Model": "llama3"
  },
  "EmbeddingProvider": {
    "Provider": "ollama",
    "Endpoint": "http://localhost:11434",
    "Model": "nomic-embed-text",
    "Dimensions": 768
  }
}
```

Supports:
- **Ollama:** Local open-source models (Llama 3, Mistral, etc.)
- **OpenAI:** GPT-3.5 / GPT-4
- **Anthropic:** Claude 3 models
- Others via OpenAI-compatible endpoints

### Database
PostgreSQL connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ttrpghelper;Username=ttrpg;Password=ttrpg_dev"
  }
}
```

## Development

### Backend
```bash
cd src/backend

# Build
dotnet build

# Run
dotnet run

# Create migration
dotnet ef migrations add <MigrationName> --output-dir Infrastructure/Database/Migrations

# Type check
cd src/frontend && npx tsc --noEmit
```

### Database
```bash
# Connect to PostgreSQL
docker exec -it ttrpghelper-db psql -U ttrpg -d ttrpghelper

# See tables
\dt

# See entities
SELECT id, name, entity_type FROM "WorldEntities";
```

## Project Structure

- **CLAUDE.md** - Project context, preferences, and tech decisions
- **TESTING_GUIDE.md** - Comprehensive testing walkthrough
- **QUICKSTART.md** - Fast setup instructions
- **Docs/ttrpg-assistant-design-doc.md** - Full system design (Phases 1-3)

## Next Steps (Phase 2 & 3)

### Phase 2: Session Transcription
- Record game sessions
- Auto-transcribe with Whisper
- Speaker diarization with pyannote
- Auto-extract events and entities
- Generate session summaries

### Phase 3: Live Contextual GM Screen
- Real-time transcription during play
- Dynamic entity detection
- Live entity cards on GM screen
- Pre-fetching relevant notes
- Quick-note capture during play

## Contributing

See **CLAUDE.md** for project philosophy and conventions.

## License

MIT (or your preference)

---

**Status:** Phase 1 (RAG Q&A with proposal workflow) ✅ complete

**Questions?** See TESTING_GUIDE.md or QUICKSTART.md for common issues.
