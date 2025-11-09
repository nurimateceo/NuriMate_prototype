using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines what actions can be performed with a game object
/// Provides context to LLM about physical properties and interaction possibilities
/// </summary>
public class ObjectAffordances : MonoBehaviour
{
    [Header("Object Classification")]
    [Tooltip("Type of object for LLM context")]
    public string objectType = "object";
    
    [Header("Interaction Affordances")]
    [Tooltip("Can AI take cover behind/near this object")]
    public bool canTakeCover = false;
    
    [Tooltip("Can AI climb this object")]
    public bool canClimb = false;
    
    [Tooltip("Maximum height AI can climb on this object (meters)")]
    [Range(0f, 10f)]
    public float climbHeight = 0f;
    
    [Tooltip("Can AI jump onto this object")]
    public bool canJumpOn = false;
    
    [Tooltip("Height required to jump onto this object (meters)")]
    [Range(0f, 5f)]
    public float jumpHeight = 0f;
    
    [Tooltip("Can AI pick up this object")]
    public bool canPickup = false;
    
    [Tooltip("Can AI open this object (door, chest, etc)")]
    public bool canOpen = false;
    
    [Tooltip("Does opening require a key or tool")]
    public bool requiresKey = false;
    
    [Tooltip("Can AI interact with this object (button, lever, etc)")]
    public bool canInteract = false;
    
    [Header("Physical Properties")]
    [Tooltip("Overall height of object (meters)")]
    [Range(0f, 20f)]
    public float height = 1f;
    
    [Tooltip("Object blocks movement")]
    public bool isObstacle = true;
    
    [Tooltip("Object is solid (vs transparent/passthrough)")]
    public bool isSolid = true;
    
    [Header("Visual Description")]
    [Tooltip("Brief description for LLM context (e.g., 'tall brick wall', 'wooden crate')")]
    [TextArea(2, 4)]
    public string visualDescription = "";
    
    [Header("Quick Presets")]
    [Tooltip("Apply common configurations")]
    public PresetType preset = PresetType.None;
    
    public enum PresetType
    {
        None,
        SmallCover,
        TallWall,
        ClimbableObject,
        PickupItem,
        Door,
        Interactive
    }
    
    /// <summary>
    /// Get affordances as dictionary for JSON serialization
    /// </summary>
    public Dictionary<string, object> GetAffordances()
    {
        var affordances = new Dictionary<string, object>();
        
        if (canTakeCover)
        {
            affordances["cv"] = true; // Compressed: cover
        }
        
        if (canClimb && climbHeight > 0)
        {
            affordances["cl"] = climbHeight; // Compressed: climb
        }
        
        if (canJumpOn && jumpHeight > 0)
        {
            affordances["jp"] = jumpHeight; // Compressed: jump
        }
        
        if (canPickup)
        {
            affordances["pk"] = true; // Compressed: pickup
        }
        
        if (canOpen)
        {
            affordances["op"] = !requiresKey; // Compressed: open (false if needs key)
        }
        
        if (canInteract)
        {
            affordances["in"] = true; // Compressed: interact
        }
        
        return affordances;
    }
    
    /// <summary>
    /// Get compressed object data for LLM
    /// </summary>
    public Dictionary<string, object> GetCompressedData()
    {
        var data = new Dictionary<string, object>
        {
            ["t"] = objectType,  // type
            ["h"] = height,      // height
            ["a"] = GetAffordances()  // affordances
        };
        
        if (!string.IsNullOrEmpty(visualDescription))
        {
            data["d"] = visualDescription;  // description
        }
        
        return data;
    }
    
    /// <summary>
    /// Check if specific action is possible
    /// </summary>
    public bool CanPerformAction(string action, float aiMaxHeight = 2.5f)
    {
        switch (action.ToLower())
        {
            case "cover":
            case "take_cover":
                return canTakeCover;
                
            case "climb":
                return canClimb && climbHeight > 0 && climbHeight <= aiMaxHeight;
                
            case "jump":
            case "jump_on":
                return canJumpOn && jumpHeight > 0 && jumpHeight <= aiMaxHeight;
                
            case "pickup":
                return canPickup;
                
            case "open":
                return canOpen && !requiresKey;
                
            case "interact":
            case "use":
                return canInteract;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Apply preset configuration
    /// </summary>
    [ContextMenu("Apply Preset")]
    public void ApplyPreset()
    {
        switch (preset)
        {
            case PresetType.SmallCover:
                objectType = "cover";
                canTakeCover = true;
                canClimb = false;
                canJumpOn = true;
                jumpHeight = 1.2f;
                height = 1.2f;
                isObstacle = true;
                isSolid = true;
                visualDescription = "small cover object";
                break;
                
            case PresetType.TallWall:
                objectType = "cover";
                canTakeCover = true;
                canClimb = false;
                canJumpOn = false;
                height = 3.5f;
                isObstacle = true;
                isSolid = true;
                visualDescription = "tall wall";
                break;
                
            case PresetType.ClimbableObject:
                objectType = "structure";
                canTakeCover = true;
                canClimb = true;
                climbHeight = 2.0f;
                canJumpOn = true;
                jumpHeight = 2.0f;
                height = 2.0f;
                isObstacle = true;
                isSolid = true;
                visualDescription = "climbable structure";
                break;
                
            case PresetType.PickupItem:
                objectType = "item";
                canPickup = true;
                height = 0.3f;
                isObstacle = false;
                isSolid = true;
                visualDescription = "pickup item";
                break;
                
            case PresetType.Door:
                objectType = "door";
                canOpen = true;
                requiresKey = false;
                height = 2.5f;
                isObstacle = true;
                isSolid = true;
                visualDescription = "door";
                break;
                
            case PresetType.Interactive:
                objectType = "interactive";
                canInteract = true;
                height = 1.0f;
                isObstacle = false;
                isSolid = true;
                visualDescription = "interactive object";
                break;
        }
        
        Debug.Log($"[ObjectAffordances] Applied preset: {preset} to {gameObject.name}");
    }
    
    /// <summary>
    /// Auto-detect affordances from object properties
    /// </summary>
    [ContextMenu("Auto-Detect Affordances")]
    public void AutoDetectAffordances()
    {
        // Try to get collider for height
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            height = col.bounds.size.y;
            
            // Objects under 2.5m might be jumpable
            if (height <= 2.5f && height >= 0.5f)
            {
                canJumpOn = true;
                jumpHeight = height;
            }
            
            // Objects under 3m might provide cover
            if (height >= 1.0f)
            {
                canTakeCover = true;
            }
        }
        
        // Detect from name
        string name = gameObject.name.ToLower();
        
        if (name.Contains("wall") || name.Contains("cover"))
        {
            objectType = "cover";
            canTakeCover = true;
        }
        
        if (name.Contains("rock") || name.Contains("stone"))
        {
            objectType = "cover";
            canTakeCover = true;
            visualDescription = "rock";
        }
        
        if (name.Contains("tree"))
        {
            objectType = "cover";
            canTakeCover = true;
            visualDescription = "tree";
        }
        
        if (name.Contains("crate") || name.Contains("box"))
        {
            objectType = "object";
            canTakeCover = true;
            canJumpOn = true;
            visualDescription = "crate";
        }
        
        if (name.Contains("door"))
        {
            objectType = "door";
            canOpen = true;
            visualDescription = "door";
        }
        
        if (name.Contains("building"))
        {
            objectType = "building";
            canTakeCover = true;
            visualDescription = "building";
        }
        
        Debug.Log($"[ObjectAffordances] Auto-detected affordances for {gameObject.name}");
    }
    
    private void OnValidate()
    {
        // Auto-apply preset when changed in inspector
        if (preset != PresetType.None)
        {
            ApplyPreset();
            preset = PresetType.None; // Reset so it can be selected again
        }
        
        // Validate logical constraints
        if (canClimb && climbHeight <= 0)
        {
            climbHeight = height;
        }
        
        if (canJumpOn && jumpHeight <= 0)
        {
            jumpHeight = height;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visual debugging in Scene view
        Gizmos.color = Color.yellow;
        
        if (canTakeCover)
        {
            // Draw cover indicator
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, 
                new Vector3(1f, height, 1f));
        }
        
        if (canClimb || canJumpOn)
        {
            // Draw climb/jump height indicator
            Gizmos.color = Color.green;
            float maxHeight = Mathf.Max(climbHeight, jumpHeight);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * maxHeight, 0.3f);
        }
    }
}

