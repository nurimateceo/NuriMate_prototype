using UnityEngine;

public class AIReadable : MonoBehaviour
{
    [Header("AI Information")]
    [TextArea(2, 4)]
    [Tooltip("Describe this object for the AI in plain English")]
    public string aiDescription = "Describe what this object is";
    
    [Tooltip("What type of object is this?")]
    public ObjectType objectType = ObjectType.Environment;
    
    [Header("AI Interaction")]
    [Tooltip("Can AI interact with this? (open, use, pick up, etc.)")]
    public bool canInteract = false;
    
    [Tooltip("Is this dangerous to AI or player?")]
    public bool isThreat = false;
    
    [Tooltip("Can AI hide behind this for cover?")]
    public bool providesCover = false;
    
    [Tooltip("Can AI climb on this?")]
    public bool canClimb = false;
}

public enum ObjectType
{
    Environment,    // Trees, rocks, decorations
    Cover,          // Things to hide behind
    Enemy,          // Threats
    Item,           // Pickups, collectibles
    Objective,      // Mission goals
    Interactive,    // Doors, switches, etc.
    Player,         // The player character
    Teammate        // AI-controlled teammate
}
