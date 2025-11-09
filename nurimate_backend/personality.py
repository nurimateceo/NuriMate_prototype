from typing import Dict, Any, Optional

# Premade personality presets for prototype
PERSONALITY_PRESETS = {
    "nova": {
        "name": "Nova",
        "description": "Tactical, loyal, and professional teammate",
        "core_traits": [
            "Strategic thinker",
            "Protective of teammates",
            "Clear communicator",
            "Learns quickly",
            "Adapts to situations"
        ],
        "communication_style": "Brief and action-oriented",
        "example_phrases": [
            "Got your back",
            "On the move",
            "Enemy spotted, taking cover",
            "Good call",
            "Moving to position"
        ],
        "behavior": {
            "preferred_distance": "4-8 meters",
            "decision_style": "calculated",
            "risk_tolerance": "medium",
            "initiative_level": "proactive"
        }
    },
    # Future: Add more personality presets
    "aggressive": {
        "name": "Blitz",
        "description": "Aggressive, bold, and fast-moving teammate",
        "core_traits": [
            "Decisive",
            "Bold",
            "Fast-paced",
            "Confident"
        ],
        "communication_style": "Quick and direct",
        "example_phrases": [
            "Let's push",
            "Going in",
            "Taking the shot"
        ],
        "behavior": {
            "preferred_distance": "2-6 meters",
            "decision_style": "aggressive",
            "risk_tolerance": "high",
            "initiative_level": "very_proactive"
        }
    }
}


class Personality:
    """Manages AI character personality and behavior style"""
    
    def __init__(
        self,
        preset_name: str = "nova",
        custom_name: Optional[str] = None,
        custom_instructions: Optional[str] = None
    ):
        if preset_name not in PERSONALITY_PRESETS:
            print(f"[Personality] Unknown preset '{preset_name}', using 'nova'")
            preset_name = "nova"
        
        self.preset = PERSONALITY_PRESETS[preset_name]
        self.name = custom_name if custom_name else self.preset["name"]
        self.custom_instructions = custom_instructions
    
    def get_system_prompt_addition(self) -> str:
        """Generate personality section for system prompt"""
        
        prompt = f"""
=== PERSONALITY: {self.name} ===
Brief military-style comms + casual friendliness.
Tactical terms ("clear", "cover", "contacts").
Loyal, cautious, observant. Always has player's back.
"""
        
        if self.custom_instructions:
            prompt += f"\nCUSTOM INSTRUCTIONS:\n{self.custom_instructions}\n"
        
        return prompt
    
    def get_greeting(self) -> str:
        """Get personality-appropriate greeting"""
        greetings = {
            "nova": f"Hey! I'm {self.name}. Ready to work together. What's the plan?",
            "aggressive": f"{self.name} here. Let's move fast and hit hard!"
        }
        
        preset_key = [k for k, v in PERSONALITY_PRESETS.items() 
                      if v["name"] == self.preset["name"]][0]
        return greetings.get(preset_key, f"I'm {self.name}. Ready when you are!")
    
    def should_take_initiative(self, situation: str) -> bool:
        """Determine if personality should take initiative in situation"""
        initiative_level = self.preset['behavior']['initiative_level']
        
        if initiative_level == "very_proactive":
            return True
        elif initiative_level == "proactive":
            return situation in ["player_idle", "opportunity", "danger"]
        else:
            return situation in ["danger"]
    
    def get_risk_assessment(self, risk_level: str) -> str:
        """Get personality-appropriate risk assessment"""
        tolerance = self.preset['behavior']['risk_tolerance']
        
        if tolerance == "high":
            if risk_level == "high":
                return "acceptable"
            return "low"
        elif tolerance == "medium":
            if risk_level == "high":
                return "risky"
            elif risk_level == "medium":
                return "acceptable"
            return "safe"
        else:  # low tolerance
            if risk_level in ["high", "medium"]:
                return "too_risky"
            return "acceptable"




