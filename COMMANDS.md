# Command Reference

## Start Services (4 Terminals)

### Terminal 1: Database
```bash
docker compose up -d
```
Output: PostgreSQL 17 with pgvector running on `localhost:5432`

### Terminal 2: Ollama
```bash
ollama serve
```
Output: Ollama serving on `localhost:11434`

First time: models download automatically (llama3, nomic-embed-text)

### Terminal 3: Backend API
```bash
cd src/backend
dotnet run
```
Output:
```
Application started. Press Ctrl+C to shut down.
Listening on https://localhost:7087 and http://localhost:5128
```

Visit: http://localhost:5128/swagger (if you add Swagger middleware)

### Terminal 4: Frontend
```bash
cd src/frontend
npm run dev
```
Output:
```
 VITE v5.x.x  ready in xxx ms

 ➜  Local:   http://localhost:5173/
```

Visit: http://localhost:5173

---

## Verify Setup Works

1. **Browser:** http://localhost:5173
2. **Page:** Import
3. **Content:** Paste markdown:
   ```markdown
   # MyPlace
   A cool location.
   ```
4. **Button:** Click "Import"
5. **Result:** Success message → **✓ System working**

---

## Common Commands

### Database

```bash
# Connect to PostgreSQL CLI
docker exec -it ttrpghelper-db psql -U ttrpg -d ttrpghelper

# View tables
\dt

# View entities
SELECT id, name, entity_type FROM "WorldEntities" LIMIT 5;

# View conversations
SELECT id, title, created_at FROM "Conversations" ORDER BY created_at DESC;

# View proposals
SELECT id, proposal_type, target_entity_name, status FROM "NoteProposals";

# Exit
\q
```

### Backend

```bash
# Rebuild
cd src/backend
dotnet build

# Run in Release mode
dotnet run --configuration Release

# Create database migration
dotnet ef migrations add <MigrationName> --output-dir Infrastructure/Database/Migrations

# Remove latest migration (before pushing)
dotnet ef migrations remove

# View EF Core logs
# (Already configured in Program.cs for Development)

# Clean build artifacts
dotnet clean
```

### Frontend

```bash
# Install dependencies
cd src/frontend
npm install

# Type check without building
npx tsc --noEmit

# Build for production
npm run build

# Preview production build
npm run preview

# Clear Vite cache
rm -rf .vite
```

### Git

```bash
# See commits
git log --oneline

# See current status
git status

# Stage changes
git add .

# Commit with message
git commit -m "Description"

# Create new branch
git checkout -b feature/your-feature

# Switch branch
git checkout main

# Push to remote (when ready)
git push origin main
```

### Ollama

```bash
# List installed models
ollama list

# Pull a model
ollama pull llama3
ollama pull nomic-embed-text

# Run a model interactively
ollama run llama3

# Remove a model
ollama rm llama3

# Show model info
ollama show llama3
```

---

## Troubleshooting Commands

```bash
# Check if port is in use
lsof -i :5128      # Backend port
lsof -i :5173      # Frontend port
lsof -i :5432      # Database port
lsof -i :11434     # Ollama port

# Kill process on a port
kill -9 <PID>      # After lsof

# Check Docker status
docker ps
docker ps -a       # Including stopped

# View container logs
docker logs ttrpghelper-db

# Stop database
docker compose down

# Stop and remove volume (DELETES DATA)
docker compose down -v

# Restart database
docker compose restart

# Check network connectivity
curl http://localhost:11434/api/tags     # Test Ollama
curl http://localhost:5128/health        # Test backend (if endpoint exists)

# Monitor processes
top
```

---

## Development Workflow

### Adding a New Feature

1. **Create a branch:**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes:**
   - Edit files
   - Type check: `npx tsc --noEmit` (frontend)
   - Build: `dotnet build` (backend)

3. **Test locally:**
   - Run services (4 terminals)
   - Test manually
   - Check browser console for errors

4. **Commit often:**
   ```bash
   git add .
   git commit -m "Add my feature"
   ```

5. **Push when ready:**
   ```bash
   git push origin feature/my-feature
   ```

### Database Changes

1. **Modify entities** in `src/backend/Modules/*/Entities/`
2. **Update DbContext** in `src/backend/Infrastructure/Database/AppDbContext.cs`
3. **Create migration:**
   ```bash
   dotnet ef migrations add FeatureName
   ```
4. **Commit migration files**
5. **Restart backend** (auto-applies on Development startup)

---

## File Locations

| Feature | File |
|---------|------|
| Chat streaming | `src/backend/Modules/Conversations/ChatHub.cs` |
| Tool calling | `src/backend/Modules/Conversations/ToolCallParser.cs` |
| Entity CRUD | `src/backend/Modules/KnowledgeBase/Endpoints/WorldEntityEndpoints.cs` |
| Chunking | `src/backend/Common/RAG/ChunkingService.cs` |
| Retrieval | `src/backend/Common/RAG/RetrievalService.cs` |
| Chat UI | `src/frontend/src/pages/ChatPage.tsx` |
| Proposals UI | `src/frontend/src/pages/ProposalsPage.tsx` |
| API client | `src/frontend/src/api/client.ts` |
| Styling | `src/frontend/src/components/layout/AppShell.tsx` |

---

## Performance Monitoring

### First-Time Delays (Normal)
- **First Ollama call:** 30-60 sec (model loading)
- **First embedding:** 5-10 sec (warmup)
- **Subsequent calls:** 2-3 sec (LLM), <1 sec (vector search)

### If It's Slow
1. Check Ollama is running: `curl http://localhost:11434/api/tags`
2. Check database: `docker logs ttrpghelper-db`
3. Check backend: `dotnet run` terminal for errors
4. Check browser console: F12 → Console tab

---

## Quick Testing Checklist

- [ ] Import markdown
- [ ] Ask a question
- [ ] Get answer with citations
- [ ] Request a note update
- [ ] Approve proposal
- [ ] Create relationship
- [ ] Delete entity
- [ ] Switch conversations
- [ ] Page reload - data persists

All ✓ = Everything works!
