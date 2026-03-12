# Testing Guide - TTRPG Worldbuilding Assistant

## Prerequisites

- Docker and Docker Compose (for PostgreSQL)
- .NET 10 SDK
- Node.js 18+ and npm
- Ollama running locally (for LLM and embeddings)

## Setup

### 1. Start PostgreSQL with pgvector

```bash
docker compose up -d
```

Verify it's running:
```bash
docker ps  # Should see ttrpghelper-db
```

### 2. Download models in Ollama

Ollama needs to be running with the required models. In another terminal:

```bash
# Start Ollama (macOS: brew install ollama, then `ollama serve`)
ollama serve

# In a new terminal, pull models
ollama pull llama3
ollama pull nomic-embed-text
```

Verify models are available:
```bash
ollama list
# Should show:
# llama3:latest
# nomic-embed-text:latest
```

### 3. Start the Backend

```bash
cd src/backend
dotnet run
```

You should see:
- EF Core migration applied automatically (tables created)
- Server listening on `http://localhost:5128`
- SignalR hub available at `/hubs/chat`

### 4. Start the Frontend

In a new terminal:

```bash
cd src/frontend
npm run dev
```

Opens at `http://localhost:5173`

---

## Feature Testing Workflow

### Test 1: Import Knowledge Base Content

1. **Navigate to Import page** → click "Import" in sidebar
2. **Paste test content** (create a sample markdown file with worldbuilding notes):

```markdown
# Aldermist Forest

## Geography
The ancient forest lies north of Thornhaven. Dense canopy, many hidden groves.

## Flora
Strange bioluminescent plants that glow only at midnight. Root network connects to underground caverns.

## Inhabitants
Fey creatures, some territorial. The wood elves have a settlement at Moonspring.

---

# The Thornhaven Kingdom

## Rulers
King Aldwin III rules from the stone throne. His council of five advisors meet quarterly.

## Cities
- Thornhaven (capital, 50,000 people)
- Millhaven (trade hub, 15,000)
- Southport (naval, 8,000)

## History
Founded 300 years ago by settlers fleeing the Cataclysm.
```

3. **Set Entity Type** to "Lore"
4. **Click Import** → should see success message
5. **Check Knowledge Base** → page should now list "Aldermist Forest" and "The Thornhaven Kingdom"

**What's tested:** Document chunking, embedding generation, entity creation, vector storage.

---

### Test 2: Conversation History & Persistence

1. **Navigate to Chat** → click "New Chat"
2. **Ask a question:**
   ```
   Tell me about Aldermist Forest
   ```
3. **Observe:**
   - Message streams in from LLM
   - Source badges show which entities were retrieved
   - Conversation appears in left sidebar with auto-generated title
   - Messages persist in database

4. **Ask a follow-up:**
   ```
   What about the fey creatures living there?
   ```
5. **Verify:**
   - LLM sees previous message context
   - Response references prior context

6. **Switch conversations** (if you have another) or **create a new one** → old messages persist
7. **Reload the page** → conversation history still there

**What's tested:**
- RAG retrieval and LLM streaming
- Message persistence
- Conversation history context window
- SignalR for real-time updates

---

### Test 3: Tool Calling & Note Proposals

1. **In Chat, ask the assistant to update notes:**
   ```
   Update Aldermist Forest to add that bioluminescent plants only bloom during the new moon
   ```

2. **Assistant response** should include a structured block:
   ```
   <<<UPDATE_NOTE>>>
   Entity: Aldermist Forest
   Description: Adding lunar cycle dependency for bioluminescent plants
   Content:
   # Aldermist Forest

   ## Geography
   The ancient forest lies north of Thornhaven. Dense canopy, many hidden groves.

   ## Flora
   Strange bioluminescent plants that glow only at midnight AND during the new moon phases. Root network connects to underground caverns.

   ## Inhabitants
   ...
   <<<END_NOTE>>>
   ```

3. **Proposal card appears** under the message with:
   - "Update" badge
   - "Aldermist Forest" name
   - Proposed content diff preview
   - **Approve**, **Edit**, **Reject** buttons

4. **Test Approve:**
   - Click "Approve"
   - Entity gets updated in database
   - Chunks re-embedded
   - Card shows "approved" status

5. **Test Edit + Approve:**
   - Ask for another update
   - Click "Edit" on proposal
   - Modify the content
   - Click "Approve"
   - Verify edited content is saved

6. **Test Reject:**
   - Ask for another update
   - Click "Reject"
   - Proposal marked as rejected
   - Entity unchanged

**What's tested:**
- LLM tool calling with structured output
- Proposal parsing and storage
- Note update workflow with GM approval
- Entity re-embedding on content change

---

### Test 4: Document Management (Relationships & Delete)

1. **Navigate to Knowledge Base**
2. **Select "Aldermist Forest"** entity
3. **In the editor, scroll to Relationships section:**
   - Should be empty initially
4. **Click "+ Add" button:**
   - Modal opens with entity dropdown
   - Search for "Thornhaven Kingdom"
   - Select it
   - Choose relationship type: "located_in"
   - Click "Add"
5. **Verify:**
   - Relationship appears as "→ located_in Thornhaven Kingdom"
   - Can click the "×" to remove it

6. **Test Delete:**
   - Click the red "Delete" button at the top
   - Confirmation modal appears
   - Click "Delete"
   - Entity removed from database
   - Knowledge Base list updates
   - Chunks/embeddings cascaded deleted

**What's tested:**
- Relationship CRUD
- Delete with confirmation
- Cascade deletion of chunks

---

### Test 5: Proposal Review Dashboard

1. **Create multiple proposals** (ask assistant to update several entities)
2. **Navigate to Proposals page**
3. **See all pending proposals** with filters:
   - **Pending** tab: shows unresolved
   - **Approved** tab: shows accepted proposals
   - **Rejected** tab: shows rejected proposals
4. **Test bulk operations:**
   - Select multiple pending proposals (checkbox on each card)
   - Click "Approve All" button
   - All marked as approved and applied

**What's tested:**
- Proposal listing and filtering
- Bulk approval/rejection workflow

---

## Testing Checklist

### Core RAG Pipeline
- [ ] Import documents via markdown
- [ ] Verify chunks created (check DB or logs)
- [ ] Vector embeddings generated (768-dim vectors)
- [ ] Ask questions that retrieve relevant context
- [ ] Source badges show correct entities

### Conversation Features
- [ ] Create new conversation
- [ ] Send messages (persist to DB)
- [ ] Load conversation history (last 20 msgs)
- [ ] Switch between conversations
- [ ] Reload page - history still there
- [ ] Conversation auto-titled from first message
- [ ] Rename conversation title from sidebar (future)

### Tool Calling & Proposals
- [ ] LLM generates UPDATE_NOTE blocks
- [ ] Proposals appear as cards
- [ ] Approve proposal → entity updated
- [ ] Edit proposal → modified content saved
- [ ] Reject proposal → entity unchanged
- [ ] Proposals persist in DB

### Document Management
- [ ] Create entity via Knowledge Base
- [ ] Edit entity content
- [ ] Add relationships between entities
- [ ] View relationships on entity card
- [ ] Remove relationship
- [ ] Delete entity with confirmation
- [ ] Verify chunks deleted when entity deleted

### UI/UX
- [ ] Navigation works between pages
- [ ] Dark theme active
- [ ] Forms validate inputs
- [ ] Loading states show during async operations
- [ ] Error messages appear on failures
- [ ] Sidebar collapses on mobile

---

## Debugging Tips

### Backend Logs
```bash
cd src/backend
dotnet run  # Full output in console
```

Look for:
- EF Core migration output
- AI client initialization
- HTTP request logs

### Database Inspection
```bash
# Connect to PostgreSQL
docker exec -it ttrpghelper-db psql -U ttrpg -d ttrpghelper

# List tables
\dt

# Check entities
SELECT id, name, entity_type FROM "WorldEntities" LIMIT 5;

# Check messages in a conversation
SELECT role, content, created_at FROM "ConversationMessages"
  WHERE conversation_id = '<conv-id>'
  ORDER BY created_at;

# Check pending proposals
SELECT id, proposal_type, target_entity_name, status FROM "NoteProposals"
  WHERE status = 'pending';
```

### Ollama Issues
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# If models missing, pull them
ollama pull llama3
ollama pull nomic-embed-text

# Test embedding model
curl http://localhost:11434/api/embeddings \
  -d '{"model":"nomic-embed-text","prompt":"test"}'
```

### Frontend Issues
```bash
# Check browser console for API errors
# Network tab shows requests to `/api` and `/hubs`

# Clear Vite cache if needed
rm -rf src/frontend/.vite

# Rebuild
cd src/frontend && npm run dev
```

---

## Example Test Scenario (End-to-End)

**Time: ~10 minutes**

1. **Import a campaign setting** (5 min)
   - Paste fantasy world notes into Import page
   - Verify entities appear in Knowledge Base

2. **Ask Q&A questions** (2 min)
   - "What cities are in the kingdom?"
   - "Tell me about the forest"
   - Verify responses use retrieved context

3. **Request note updates** (2 min)
   - "Update the forest with new creatures"
   - Approve the proposal
   - Verify entity changed in Knowledge Base

4. **Create relationships** (1 min)
   - Link forest to kingdom
   - Verify relationship shows in both entities

---

## What to Watch For

### Performance
- First vector search may be slow (Ollama warming up)
- Subsequent searches should be <1s
- Streaming LLM responses should start within 2-3s

### Data Integrity
- Messages saved even if connection drops
- Proposals survive page reload
- Deleted entities don't appear in search

### Edge Cases
- Empty knowledge base → LLM says "no context"
- Very long documents → chunking handles it
- Rapid messages → queue doesn't break
- Network disconnect → auto-reconnect (SignalR)

---

## Next Steps (Phase 2)

Once Phase 1 is stable, Phase 2 adds:
- Audio recording and transcription
- Automatic session note generation
- Speaker identification

But the RAG and document management foundation you just tested is solid!
