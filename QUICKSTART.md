# Quick Start Guide

## TL;DR - Run This in Separate Terminals

### Terminal 1: PostgreSQL
```bash
docker compose up -d
```

### Terminal 2: Ollama (if not already running)
```bash
ollama serve
```

### Terminal 3: Backend
```bash
cd src/backend
dotnet run
# Listens on http://localhost:5128
```

### Terminal 4: Frontend
```bash
cd src/frontend
npm run dev
# Opens http://localhost:5173
```

## ✅ Verify Everything Works

1. **Open http://localhost:5173** in your browser
2. **Go to Import page**
3. **Paste this test content:**

```markdown
# Thornhaven

A bustling trade city on the river.

## Districts
- Market Quarter: The heart of commerce
- Harbor Ward: Docks and shipping
- Noble Hill: Mansions of the wealthy

## Ruler
Baron Aldwin commands the city militia.
```

4. **Click Import**
5. **See success message** ✓
6. **Go to Chat page**
7. **Ask:** "What districts are in Thornhaven?"
8. **See answer with source badges** ✓

Done! You have a working RAG system with GPT-like responses grounded in your notes.

## Next: Explore Features

- **Chat:** Ask questions, get context-aware answers
- **Knowledge Base:** Create/edit entities, manage relationships
- **Import:** Bulk import markdown files
- **Proposals:** Review and approve AI-suggested note updates

See `TESTING_GUIDE.md` for comprehensive testing scenarios.

## Troubleshooting

**"Connection refused" errors?**
- Make sure all 4 services are running in separate terminals
- Check ports: 5128 (backend), 5173 (frontend), 5432 (DB), 11434 (Ollama)

**"No such model" from Ollama?**
```bash
ollama pull llama3
ollama pull nomic-embed-text
```

**"Database migration failed"?**
```bash
docker compose down -v  # Remove volume
docker compose up -d    # Recreate clean DB
```

**First message takes 30+ seconds?**
- Ollama is downloading the model on first use
- Subsequent messages are much faster (~2-3s)
