#!/bin/bash
# Quick start script for TTRPG Worldbuilding Assistant

set -e

echo "🎲 TTRPG Worldbuilding Assistant - Quick Start"
echo "=============================================="
echo ""

# Check prerequisites
echo "📋 Checking prerequisites..."

if ! command -v docker &> /dev/null; then
  echo "❌ Docker not found. Please install Docker."
  exit 1
fi

if ! command -v dotnet &> /dev/null; then
  echo "❌ .NET SDK not found. Please install .NET 10 SDK."
  exit 1
fi

if ! command -v node &> /dev/null; then
  echo "❌ Node.js not found. Please install Node.js 18+."
  exit 1
fi

if ! command -v ollama &> /dev/null; then
  echo "⚠️  Ollama not found. Please install Ollama and run 'ollama serve' in another terminal."
  echo "   Then run this script again."
  exit 1
fi

echo "✅ All prerequisites met"
echo ""

# Start PostgreSQL
echo "🐘 Starting PostgreSQL + pgvector..."
docker compose up -d
echo "✅ Database running"
echo ""

# Wait for DB to be ready
echo "⏳ Waiting for database to be ready..."
sleep 3

# Check Ollama
echo "🤖 Checking Ollama..."
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
  echo "❌ Ollama not responding at http://localhost:11434"
  echo "   Please start Ollama with: ollama serve"
  exit 1
fi

# Check required models
echo "📦 Checking Ollama models..."
if ! curl -s http://localhost:11434/api/tags | grep -q "llama3"; then
  echo "⚠️  llama3 model not found. Pulling..."
  ollama pull llama3 &
fi

if ! curl -s http://localhost:11434/api/tags | grep -q "nomic-embed-text"; then
  echo "⚠️  nomic-embed-text model not found. Pulling..."
  ollama pull nomic-embed-text &
fi

echo "✅ Ollama ready"
echo ""

# Start backend
echo "🚀 Starting backend (http://localhost:5128)..."
cd src/backend
dotnet run &
BACKEND_PID=$!
echo "✅ Backend starting (PID: $BACKEND_PID)"

# Wait for backend to be ready
sleep 5

# Start frontend
echo "🎨 Starting frontend (http://localhost:5173)..."
cd ../frontend
npm run dev &
FRONTEND_PID=$!
echo "✅ Frontend starting (PID: $FRONTEND_PID)"

echo ""
echo "=============================================="
echo "🎉 All services running!"
echo ""
echo "Frontend:  http://localhost:5173"
echo "Backend:   http://localhost:5128"
echo "Database:  postgresql://localhost:5432/ttrpghelper"
echo "Ollama:    http://localhost:11434"
echo ""
echo "📖 See TESTING_GUIDE.md for how to test features"
echo ""
echo "Press Ctrl+C to stop all services"
echo "=============================================="

# Keep script running
wait
