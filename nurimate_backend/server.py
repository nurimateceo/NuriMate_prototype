import json
import os
import uuid
import time
from flask import Flask, request
from flask_sock import Sock
from openai import OpenAI
from dotenv import load_dotenv

from memory_system import MemorySystem
from personality import Personality

# Load environment variables
load_dotenv()

# Global perception state for chat context
last_perception_data = {"text": "", "timestamp": 0}

# Initialize OpenAI client
client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

# Initialize Flask and WebSocket
app = Flask(__name__)
sock = Sock(app)

# Configuration
PLAYER_ID = os.getenv("PLAYER_ID", "demo_player")
PORT = int(os.getenv("PORT", 8080))
DEBUG = os.getenv("DEBUG", "True").lower() == "true"

# Initialize systems
memory = MemorySystem(PLAYER_ID)
personality = Personality(preset_name="nova", custom_name="Nova")

# Load system prompts
with open("prompts/compressed_prompt.txt", 'r') as f:
    BASE_SYSTEM_PROMPT = f.read()

with open("prompts/chat_prompt.txt", 'r') as f:
    CHAT_SYSTEM_PROMPT = f.read()

with open("prompts/perception_prompt.txt", 'r') as f:
    PERCEPTION_PROMPT = f.read()

print(f"[Server] Initializing NuriMate AI Backend")
print(f"[Server] Player ID: {PLAYER_ID}")
print(f"[Server] Personality: {personality.name}")
print(f"[Server] Base prompt loaded: {len(BASE_SYSTEM_PROMPT)} chars")
print(f"[Server] Chat prompt loaded: {len(CHAT_SYSTEM_PROMPT)} chars")
print(f"[Server] Perception prompt loaded: {len(PERCEPTION_PROMPT)} chars (optimized)")


@sock.route('/ai')
def ai_websocket(ws):
    """Main WebSocket endpoint for Unity connection"""
    print(f"\n[Server] ✓ {personality.name} connected for player: {PLAYER_ID}")
    
    # Session start - load long-term memory
    memory_context = memory.get_context_for_llm()
    print(f"[Memory] Loaded {len(memory_context['long_term']['last_3_memories'])} memories")
    print(f"[Memory] Relationship level: {memory_context['long_term']['relationship_level']}/10")
    
    message_count = 0
    
    try:
        while True:
            # Receive message from Unity
            message = ws.receive()
            if not message:
                break
            
            message_count += 1
            
            try:
                data = json.loads(message)
                
                # Handle different message types
                if data.get('type') == 'state_snapshot':
                    print(f"\n[{message_count}] Received perception snapshot")
                    
                    # Process with memory and personality
                    response = process_with_memory(data, memory_context)
                    
                    # Send command back to Unity
                    ws.send(json.dumps(response))
                    print(f"[{message_count}] ✓ Sent command to Unity")
                    
                else:
                    print(f"[Warning] Unknown message type: {data.get('type')}")
                    
            except json.JSONDecodeError as e:
                print(f"[Error] JSON decode error: {e}")
            except Exception as e:
                print(f"[Error] Processing error: {e}")
                
    except Exception as e:
        print(f"[Error] WebSocket error: {e}")
    finally:
        # Session end - save memories
        print(f"\n[Server] Session ending...")
        memory.update_long_term()
        print(f"[Server] ✓ Memories saved")
        print(f"[Server] ✗ {personality.name} disconnected\n")


def process_with_memory(perception, memory_context):
    """Process perception with personality and memory"""
    
    # Build full prompt
    full_prompt = build_full_prompt(perception, memory_context)
    
    try:
        # Call OpenAI
        print(f"[OpenAI] Calling gpt-4o-mini...")
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=full_prompt,
            max_completion_tokens=200,
            response_format={"type": "json_object"}
        )
        
        # Extract command
        command_text = response.choices[0].message.content
        
        # DEBUG: Log raw response
        print(f"[DEBUG] Raw game control response (first 500 chars): {repr(command_text[:500] if command_text else 'None')}")
        print(f"[DEBUG] Response length: {len(command_text) if command_text else 0} chars")
        
        # Try to parse JSON
        try:
            command = json.loads(command_text)
        except json.JSONDecodeError as e:
            print(f"[DEBUG] JSON parse failed at position {e.pos}")
            print(f"[DEBUG] Full response: {repr(command_text)}")
            raise
        
        # Ensure command has required fields
        if "type" not in command:
            command["type"] = "command"
        if "id" not in command:
            command["id"] = str(uuid.uuid4())
        
        # Update short-term memory
        memory.add_conversation("user", json.dumps(perception))
        memory.add_conversation("assistant", command_text)
        
        # Check for significant events in perception
        check_for_events(perception)
        
        # Log cost
        usage = response.usage
        cost = calculate_cost(usage)
        print(f"[Cost] ${cost:.6f} | Tokens: {usage.total_tokens} (in:{usage.prompt_tokens} out:{usage.completion_tokens})")
        
        # Log action summary
        if "plan" in command and "sequence" in command["plan"]:
            actions = [step.get("action", "unknown") for step in command["plan"]["sequence"]]
            print(f"[Action] {personality.name} → {' → '.join(actions)}")
        
        return command
        
    except Exception as e:
        print(f"[Error] OpenAI API error: {e}")
        # Return fallback command
        return create_fallback_command()


def process_text_perception(perception_text, memory_context):
    """Process TEXT-BASED perception (NEW SYSTEM)"""
    
    # Build full prompt with text perception
    full_prompt = build_text_prompt(perception_text, memory_context)
    
    try:
        # Call OpenAI
        print(f"[OpenAI] Calling gpt-4o-mini with TEXT perception...")
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=full_prompt,
            max_completion_tokens=200,
            response_format={"type": "json_object"}
        )
        
        # Extract command
        command_text = response.choices[0].message.content
        
        # DEBUG: Log raw response
        print(f"[DEBUG] Raw response (first 300 chars): {repr(command_text[:300] if command_text else 'None')}")
        
        # Parse JSON
        try:
            command = json.loads(command_text)
        except json.JSONDecodeError as e:
            print(f"[DEBUG] JSON parse failed: {e}")
            print(f"[DEBUG] Full response: {repr(command_text)}")
            raise
        
        # Ensure command has required fields
        if "type" not in command:
            # Default to behavior type for new system
            command["type"] = "behavior"
        
        # Update short-term memory
        memory.add_conversation("user", perception_text)
        memory.add_conversation("assistant", command_text)
        
        # Log cost
        usage = response.usage
        cost = calculate_cost(usage)
        print(f"[Cost] ${cost:.6f} | Tokens: {usage.total_tokens} (in:{usage.prompt_tokens} out:{usage.completion_tokens})")
        
        # Log action summary
        if command.get("type") == "behavior":
            # New behavior system
            behavior = command.get("behavior", "unknown")
            target = command.get("target", "")
            target_str = f" → {target}" if target else ""
            print(f"[Behavior] {personality.name} → {behavior}{target_str}")
        elif "plan" in command and "sequence" in command["plan"]:
            # Legacy command system
            actions = [step.get("action", "unknown") for step in command["plan"]["sequence"]]
            print(f"[Action] {personality.name} → {' → '.join(actions)}")
        
        return command
        
    except Exception as e:
        print(f"[Error] OpenAI API error: {e}")
        import traceback
        print(traceback.format_exc())
        return create_fallback_command()


def build_text_prompt(perception_text, memory_context):
    """Build prompt for TEXT-BASED perception (NEW) - ULTRA-OPTIMIZED for minimal tokens"""
    
    # Use LIGHTWEIGHT perception prompt (not full BASE_SYSTEM_PROMPT!)
    # This cuts token usage from 1,300 → ~150
    system_message = PERCEPTION_PROMPT
    
    # Add ONLY recent player chat commands (if any)
    # This allows AI to follow player orders from chat
    recent_chat = memory_context["short_term"].get("recent_chat_messages", [])
    if recent_chat:
        chat_summary = "\n\nRECENT PLAYER COMMANDS:\n"
        for msg in recent_chat[-2:]:  # Last 2 chat messages only
            if msg.get("role") == "user":
                chat_summary += f"- {msg.get('content', '')}\n"
        system_message += chat_summary
    
    messages = [
        {"role": "system", "content": system_message}
    ]
    
    # NO conversation history for perception updates!
    # Each perception is stateless - AI evaluates current situation only
    
    # Add current TEXT perception
    messages.append({
        "role": "user",
        "content": perception_text
    })
    
    return messages


def build_full_prompt(perception, memory_context):
    """Build complete prompt with personality + memory + perception (LEGACY JSON)"""
    
    # Build system message
    system_message = BASE_SYSTEM_PROMPT + "\n\n"
    system_message += personality.get_system_prompt_addition() + "\n\n"
    system_message += format_memory_context(memory_context)
    
    messages = [
        {"role": "system", "content": system_message}
    ]
    
    # Add recent conversation history (short-term memory)
    recent_convos = memory_context["short_term"].get("recent_conversations", [])
    for convo in recent_convos[-6:]:  # Last 3 exchanges
        messages.append({
            "role": convo["role"],
            "content": convo["content"]
        })
    
    # Add current perception
    messages.append({
        "role": "user",
        "content": json.dumps(perception)
    })
    
    return messages


def format_memory_context(memory_context):
    """Format memory for prompt (compressed)"""
    short = memory_context["short_term"]
    long = memory_context["long_term"]
    
    # Format recent chat messages (so game control can see player instructions)
    chat_text = ""
    for msg in short.get("recent_chat_messages", []):
        role = "Player" if msg.get("role") == "user" else "You"
        content = msg.get("content", "")
        chat_text += f"- {role}: {content}\n"
    
    # Format recent events
    events_text = ""
    for event in short.get("recent_events", [])[-3:]:
        events_text += f"- {event.get('type')}: {event.get('details', {})}\n"
    if not events_text:
        events_text = "- No recent events\n"
    
    # Format memories
    memories_text = ""
    for mem in long.get("last_3_memories", []):
        memories_text += f"- {mem.get('summary', 'N/A')}\n"
    if not memories_text:
        memories_text = "- First time playing together\n"
    
    # Format preferences
    prefs = long.get("player_preferences", {})
    likes = ", ".join(prefs.get("likes", [])[:3]) or "Unknown"
    
    # Build context with chat messages if present
    chat_section = f"""
Recent Chat:
{chat_text}
""" if chat_text else ""
    
    context = f"""
MEMORY CONTEXT:
{chat_section}
Recent Events:
{events_text}

About Player ({long.get("player_name", "Player")}):
- Playstyle: {long.get("playstyle", "unknown")}
- Relationship: Level {long.get("relationship_level", 1)}/10
- Sessions played: {long.get("sessions_count", 0)}
- Likes: {likes}

Recent Shared Memories:
{memories_text}
"""
    return context


def check_for_events(perception):
    """Check perception for memorable events"""
    # Check if there are events in perception
    if "events" in perception:
        for event in perception.get("events", []):
            event_type = event.get("type")
            
            # Detect significant events
            if event_type == "took_damage":
                damage = event.get("amount", 0)
                if damage > 30:
                    memory.add_game_event("close_call", {"damage": damage})
            
            elif event_type == "reached_point":
                memory.add_game_event("milestone", {"location": event.get("position")})
            
            elif event_type == "path_blocked":
                memory.add_game_event("obstacle", {"reason": event.get("reason")})


def calculate_cost(usage):
    """Calculate cost for GPT-5-mini"""
    # GPT-5-mini pricing: $0.25 per 1M input tokens, $2.00 per 1M output tokens
    # About 2x more expensive than GPT-4o-mini, but still very affordable
    input_cost = (usage.prompt_tokens / 1_000_000) * 0.25
    output_cost = (usage.completion_tokens / 1_000_000) * 2.00
    return input_cost + output_cost


def create_fallback_command():
    """Create safe fallback command if LLM fails"""
    return {
        "type": "command",
        "id": str(uuid.uuid4()),
        "plan": {
            "sequence": [
                {
                    "action": "hold_position",
                    "params": {"duration": 5}
                }
            ]
        }
    }


def build_chat_prompt(user_text: str, memory_context, chat_history: list, current_perception: str = ""):
    """Build conversational prompt for terminal chat replies with REAL-TIME game awareness.
    Uses chat_prompt.txt which focuses on natural conversation, not game commands.
    Uses ONLY chat history (no game control JSON pollution).
    """
    # Use CHAT prompt (conversational) instead of BASE prompt (game control)
    system_message = CHAT_SYSTEM_PROMPT + "\n\n"
    system_message += personality.get_system_prompt_addition() + "\n\n"
    system_message += format_memory_context(memory_context)
    
    # Add current game situation if available
    if current_perception:
        age = time.time() - last_perception_data.get("timestamp", 0)
        system_message += f"\n\n=== CURRENT GAME SITUATION ===\n{current_perception}\n"
        if age > 5:
            system_message += f"(Data is {int(age)}s old)\n"

    messages = [
        {"role": "system", "content": system_message}
    ]

    # Include ONLY chat history (clean, no game control JSON)
    for convo in chat_history:
        messages.append({
            "role": convo["role"],
            "content": convo["content"]
        })

    # Add current user message
    messages.append({"role": "user", "content": user_text})
    return messages


@app.route('/perceive', methods=['POST'])
def perceive():
    """HTTP endpoint for perception data from Unity - accepts TEXT or JSON"""
    try:
        data = request.get_json()
        
        # NEW: Handle text-based perception
        if data.get('type') == 'text_perception':
            print(f"\n[HTTP] Received TEXT perception")
            perception_text = data.get('perception', '')
            print(f"[Perception Content]:\n{perception_text}")
            
            # Store perception globally for chat context
            global last_perception_data
            last_perception_data = {"text": perception_text, "timestamp": time.time()}
            
            # Load memory context
            memory_context = memory.get_context_for_llm()
            
            # Process text perception
            response = process_text_perception(perception_text, memory_context)
            
            # DEBUG: Log what we're sending
            print(f"[HTTP DEBUG] Response type: {type(response)}")
            print(f"[HTTP DEBUG] Response content: {response}")
            print(f"[HTTP] ✓ Sending command to Unity")
            return response
        
        # LEGACY: Handle old JSON snapshots (backward compatibility)
        elif data.get('type') == 'state_snapshot':
            print(f"\n[HTTP] Received LEGACY JSON snapshot (consider upgrading to text)")
            
            # Load memory context
            memory_context = memory.get_context_for_llm()
            
            # Process with memory and personality
            response = process_with_memory(data, memory_context)
            
            print(f"[HTTP] ✓ Sending command to Unity")
            return response
        else:
            print(f"[Warning] Unknown message type: {data.get('type')}")
            return {"error": "Unknown message type"}, 400
            
    except Exception as e:
        print(f"[Error] HTTP processing error: {e}")
        import traceback
        print(traceback.format_exc())
        return create_fallback_command()


@app.route('/chat', methods=['POST'])
def chat():
    """Terminal chat endpoint. Stores user's text in memory and returns a short assistant reply.
    This does NOT send actions to Unity directly; actions still come from /perceive using updated memory.
    """
    try:
        data = request.get_json(silent=True, force=True) or {}
        text = (data.get('text') or '').strip()
        if not text:
            return {"error": "Missing 'text' in payload"}, 400

        print(f"\n[Chat] User: {text}")

        # Append user's message to CHAT memory (separate from game control)
        memory.add_chat_message("user", text)

        # Build chat prompt using current memory context and CLEAN chat history
        memory_context = memory.get_context_for_llm()
        chat_history = memory.get_chat_history()
        print(f"[Chat] Using {len(chat_history)} chat messages (no game JSON)")
        messages = build_chat_prompt(text, memory_context, chat_history, 
                                      current_perception=last_perception_data.get("text", ""))

        # Call OpenAI to produce a conversational reply (plain text, NOT JSON)
        print(f"[Chat] Calling gpt-4o-mini for conversational reply...")
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=messages,
            max_completion_tokens=300  # Generous limit - gives LLM breathing room, still cheap
            # NOTE: No response_format for conversational replies
        )
        
        # Get raw response before stripping
        raw_reply = response.choices[0].message.content or ""
        
        # DEBUG: Log raw chat response
        print(f"[DEBUG] Chat raw response (first 200 chars): {repr(raw_reply[:200])}")
        print(f"[DEBUG] Chat response length: {len(raw_reply)} chars")
        
        reply = raw_reply.strip()

        # Log usage and cost
        usage = response.usage
        cost = calculate_cost(usage)
        print(f"[Cost] ${cost:.6f} | Tokens: {usage.total_tokens} (in:{usage.prompt_tokens} out:{usage.completion_tokens})")
        
        # Debug: Log raw response if it's problematic
        if not reply and raw_reply:
            print(f"[Warning] OpenAI returned whitespace-only response: {repr(raw_reply)}")
            reply = "I'm here, what do you need?"
        elif not reply:
            print(f"[Warning] OpenAI returned completely empty response")
            reply = "I'm here, what do you need?"
        
        print(f"[Chat] Nova: {reply}\n")

        # Save assistant reply to CHAT memory (separate from game control)
        memory.add_chat_message("assistant", reply)

        return {"reply": reply}
        
    except Exception as e:
        # Log detailed error to console
        import traceback
        print(f"\n[Error] /chat endpoint error: {e}")
        print(traceback.format_exc())
        
        # Return error to user so they can see what went wrong
        return {"error": f"Chat failed: {str(e)}", "reply": "[Error - check server logs]"}, 500


@app.route('/')
def index():
    """Simple status page"""
    return f"""
    <h1>NuriMate AI Backend</h1>
    <p>Status: Running</p>
    <p>Personality: {personality.name}</p>
    <p>Player ID: {PLAYER_ID}</p>
    <p>HTTP endpoint: http://localhost:{PORT}/perceive</p>
    <p>WebSocket endpoint: ws://localhost:{PORT}/ai</p>
    """


@app.route('/health')
def health():
    """Health check endpoint"""
    return {"status": "ok", "personality": personality.name, "player_id": PLAYER_ID}


if __name__ == '__main__':
    print(f"\n{'='*60}")
    print(f"  NuriMate AI Backend Server")
    print(f"  Personality: {personality.name}")
    print(f"  Player: {PLAYER_ID}")
    print(f"{'='*60}\n")
    print(f"Starting server on http://0.0.0.0:{PORT}")
    print(f"WebSocket endpoint: ws://localhost:{PORT}/ai")
    print(f"\nWaiting for Unity connection...\n")
    
    # use_reloader=False prevents Flask from double-loading and caching old code
    app.run(host='0.0.0.0', port=PORT, debug=DEBUG, use_reloader=False)

