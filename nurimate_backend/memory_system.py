import json
import os
import time
from datetime import datetime
from typing import Dict, List, Any

class MemorySystem:
    """Manages both short-term (session) and long-term (persistent) memory for AI character"""
    
    def __init__(self, player_id: str):
        self.player_id = player_id
        self.short_term = ShortTermMemory()
        self.long_term = LongTermMemory(player_id)
    
    def add_conversation(self, role: str, message: str):
        """Add to short-term conversation history (game control)"""
        self.short_term.add_conversation(role, message)
    
    def add_chat_message(self, role: str, message: str):
        """Add to chat-only conversation history (separate from game control)"""
        self.short_term.add_chat_message(role, message)
    
    def get_chat_history(self) -> List[Dict[str, Any]]:
        """Get recent chat-only conversation history"""
        return self.short_term.get_chat_history()
    
    def add_game_event(self, event_type: str, details: Dict[str, Any]):
        """Add significant game event"""
        self.short_term.add_event(event_type, details)
        
        # Check if event is memorable enough for long-term storage
        if self.is_memorable(event_type, details):
            self.long_term.add_memory(event_type, details)
    
    def is_memorable(self, event_type: str, details: Dict[str, Any]) -> bool:
        """Determine if event should be stored in long-term memory"""
        memorable_types = [
            "close_call",
            "victory",
            "defeat",
            "milestone",
            "funny_moment",
            "first_time",
            "achievement"
        ]
        return event_type in memorable_types
    
    def get_context_for_llm(self) -> Dict[str, Any]:
        """Get compressed memory context for LLM prompt"""
        context = {
            "short_term": self.short_term.get_summary(),
            "long_term": self.long_term.get_relevant_memories()
        }
        return context
    
    def update_long_term(self):
        """Update long-term memory at session end"""
        session_summary = self.short_term.summarize_session()
        self.long_term.integrate_session(session_summary)
        self.long_term.save()
    
    def compress_for_cost(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Compress context to minimize token usage"""
        # Already compressed in get_summary methods
        return context


class ShortTermMemory:
    """In-session memory (cleared on restart)"""
    
    def __init__(self, max_conversations: int = 10, max_events: int = 10):
        self.conversations: List[Dict[str, Any]] = []  # Game control conversations
        self.chat_conversations: List[Dict[str, Any]] = []  # Chat-only conversations
        self.game_events: List[Dict[str, Any]] = []
        self.max_conversations = max_conversations
        self.max_events = max_events
        self.session_start = time.time()
    
    def add_conversation(self, role: str, content: str):
        """Add conversation turn (game control)"""
        self.conversations.append({
            "role": role,
            "content": content,
            "time": time.time()
        })
        
        # Keep only recent conversations
        if len(self.conversations) > self.max_conversations:
            self.conversations.pop(0)
    
    def add_chat_message(self, role: str, content: str):
        """Add chat-only message (separate from game control)"""
        self.chat_conversations.append({
            "role": role,
            "content": content,
            "time": time.time()
        })
        
        # Keep only recent chat messages (last 10)
        if len(self.chat_conversations) > 10:
            self.chat_conversations.pop(0)
    
    def get_chat_history(self) -> List[Dict[str, Any]]:
        """Get recent chat-only conversations (last 6 = 3 exchanges)"""
        return self.chat_conversations[-6:]
    
    def add_event(self, event_type: str, details: Dict[str, Any]):
        """Add game event"""
        self.game_events.append({
            "type": event_type,
            "details": details,
            "time": time.time()
        })
        
        # Keep only recent events
        if len(self.game_events) > self.max_events:
            self.game_events.pop(0)
    
    def get_summary(self) -> Dict[str, Any]:
        """Get compressed summary for LLM (last 5 items only)"""
        return {
            "recent_conversations": self.conversations[-5:],
            "recent_chat_messages": self.chat_conversations[-3:],  # Include recent chat for game control awareness
            "recent_events": self.game_events[-5:],
            "session_duration": time.time() - self.session_start
        }
    
    def summarize_session(self) -> Dict[str, Any]:
        """Create session summary for long-term storage"""
        return {
            "duration": time.time() - self.session_start,
            "conversation_count": len(self.conversations),
            "event_count": len(self.game_events),
            "notable_events": [e for e in self.game_events if self._is_notable(e)]
        }
    
    def _is_notable(self, event: Dict[str, Any]) -> bool:
        """Check if event is notable"""
        notable_types = ["close_call", "victory", "milestone"]
        return event.get("type") in notable_types


class LongTermMemory:
    """Cross-session persistent memory"""
    
    def __init__(self, player_id: str):
        self.player_id = player_id
        self.file_path = f"data/memory_{player_id}.json"
        self.data = self.load()
    
    def load(self) -> Dict[str, Any]:
        """Load from JSON file"""
        if os.path.exists(self.file_path):
            try:
                with open(self.file_path, 'r') as f:
                    return json.load(f)
            except Exception as e:
                print(f"[Memory] Error loading: {e}")
                return self.create_new_profile()
        return self.create_new_profile()
    
    def save(self):
        """Save to JSON file"""
        try:
            os.makedirs("data", exist_ok=True)
            with open(self.file_path, 'w') as f:
                json.dump(self.data, f, indent=2)
            print(f"[Memory] Saved for player: {self.player_id}")
        except Exception as e:
            print(f"[Memory] Error saving: {e}")
    
    def create_new_profile(self) -> Dict[str, Any]:
        """Create new player profile"""
        return {
            "player_id": self.player_id,
            "player_name": "Player",
            "created_at": datetime.now().isoformat(),
            "playstyle": "unknown",
            "relationship_level": 1,
            "memorable_moments": [],
            "preferences": {
                "likes": [],
                "dislikes": []
            },
            "total_time_played": 0.0,
            "sessions_count": 0
        }
    
    def add_memory(self, event_type: str, details: Dict[str, Any]):
        """Add memorable moment"""
        memory = {
            "date": datetime.now().isoformat(),
            "type": event_type,
            "summary": self._summarize_event(event_type, details)
        }
        
        self.data["memorable_moments"].append(memory)
        
        # Keep only last 10 memories to control token cost
        if len(self.data["memorable_moments"]) > 10:
            self.data["memorable_moments"].pop(0)
    
    def _summarize_event(self, event_type: str, details: Dict[str, Any]) -> str:
        """Create brief summary of event"""
        summaries = {
            "close_call": "Had a close call in combat",
            "victory": "Achieved victory together",
            "defeat": "Faced defeat but learned from it",
            "milestone": "Reached an important milestone",
            "funny_moment": "Shared a funny moment",
            "first_time": "First time experience"
        }
        return summaries.get(event_type, str(details))
    
    def get_relevant_memories(self) -> Dict[str, Any]:
        """Get compressed relevant memories for LLM"""
        return {
            "player_name": self.data["player_name"],
            "playstyle": self.data["playstyle"],
            "relationship_level": self.data["relationship_level"],
            "last_3_memories": self.data["memorable_moments"][-3:],
            "player_preferences": self.data["preferences"],
            "sessions_count": self.data["sessions_count"]
        }
    
    def integrate_session(self, session_summary: Dict[str, Any]):
        """Integrate session data into long-term memory"""
        # Update play time
        self.data["total_time_played"] += session_summary.get("duration", 0) / 3600.0
        self.data["sessions_count"] += 1
        
        # Update relationship level based on session length
        if session_summary.get("duration", 0) > 600:  # 10+ minutes
            self.data["relationship_level"] = min(10, self.data["relationship_level"] + 1)
        
        # Analyze playstyle from events
        self._update_playstyle(session_summary.get("notable_events", []))
    
    def _update_playstyle(self, events: List[Dict[str, Any]]):
        """Update player playstyle based on behavior"""
        # Simple heuristic - can be made more sophisticated
        if len(events) > 5:
            self.data["playstyle"] = "aggressive"
        elif len(events) > 2:
            self.data["playstyle"] = "balanced"
        else:
            self.data["playstyle"] = "tactical"




