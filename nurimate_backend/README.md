# NuriMate AI Backend - Phase 2

AI teammate backend server with memory, personality, and OpenAI integration.

## Features

- ✅ OpenAI GPT-4o-mini integration
- ✅ Short-term memory (in-session)
- ✅ Long-term memory (persistent across sessions)
- ✅ Personality system ("Nova" preset)
- ✅ Cost-optimized prompts
- ✅ WebSocket communication with Unity

## Setup

### 1. Install Python Dependencies

```bash
cd nurimate_backend
pip install -r requirements.txt
```

### 2. Configure Environment

Create `.env` file:

```bash
cp .env.example .env
```

Edit `.env` and add your OpenAI API key:

```
OPENAI_API_KEY=sk-your-actual-key-here
```

### 3. Run Server

```bash
python server.py
```

Server will start on `http://localhost:8080`

## Usage

### Connect from Unity

WebSocket endpoint: `ws://localhost:8080/ai`

### Test Server

1. Open browser: `http://localhost:8080`
2. Check health: `http://localhost:8080/health`

## File Structure

```
nurimate_backend/
├── server.py              # Main server
├── memory_system.py       # Memory management
├── personality.py         # Personality presets
├── requirements.txt       # Python dependencies
├── .env                   # API keys (create this!)
├── prompts/
│   └── compressed_prompt.txt  # System prompt
└── data/
    └── memory_*.json      # Player memories (auto-created)
```

## Cost Monitoring

Server logs cost per LLM call:

```
[Cost] $0.000410 | Tokens: 2350 (in:2200 out:150)
```

Expected: **$0.04-$0.10 per hour** of gameplay

## Memory System

### Short-term (Session)
- Last 10 conversations
- Last 10 game events
- Cleared on restart

### Long-term (Persistent)
- Player preferences
- Playstyle
- Memorable moments
- Relationship level
- Saved to `data/memory_{player_id}.json`

## Personality

Current preset: **Nova**
- Tactical and strategic
- Loyal and protective
- Clear communicator

Customize in `personality.py` or via constructor.

## Troubleshooting

**Server won't start:**
- Check Python version (3.8+)
- Install dependencies: `pip install -r requirements.txt`
- Check port 8080 not in use

**OpenAI errors:**
- Verify API key in `.env`
- Check API key has credits
- Test key: `curl https://api.openai.com/v1/models -H "Authorization: Bearer YOUR_KEY"`

**Unity won't connect:**
- Server must be running first
- Check firewall allows port 8080
- Verify WebSocket URL: `ws://localhost:8080/ai`

**High costs:**
- Check logs for token usage
- Reduce update frequency in Unity
- Verify compression is working

## Next Steps

1. ✅ Server running
2. [ ] Update Unity WebSocketBridge
3. [ ] Test connection
4. [ ] Play and iterate!

## Support

Check logs for detailed error messages.
Cost and performance metrics logged in real-time.




