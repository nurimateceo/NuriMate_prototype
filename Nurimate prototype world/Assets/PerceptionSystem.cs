using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Screen-space perception system that exports what the AI "sees" from camera perspective
/// Optimized for cost efficiency with delta updates and event-driven emission
/// </summary>
public class PerceptionSystem : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform aiTransform;
    public Transform playerTransform;
    
    [Header("Detection Settings")]
    public float detectionRadius = 50f;
    public float updateInterval = 0.2f; // 5Hz base rate
    public LayerMask detectableLayers;
    public int maxObjectsOnScreen = 20;
    
    [Header("Cost Optimization")]
    public float significantMovementThreshold = 3f; // AI moved 3m
    public float significantPlayerMovementThreshold = 5f; // Player moved 5m
    public float periodicUpdateInterval = 2f; // Force update every 2s
    public float significantEventCooldown = 0.5f;
    
    // State tracking for delta updates
    private PerceptionSnapshot lastSnapshot;
    private Vector3 lastAIPosition;
    private Vector3 lastPlayerPosition;
    private List<string> lastVisibleObjectIDs = new List<string>();
    private Dictionary<string, Vector3> lastKnownPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, float> lastEventTime = new Dictionary<string, float>();
    
    // Events - TEXT-BASED PERCEPTION
    public event Action<string> OnTextPerception; // New: sends text instead of JSON
    
    private float lastUpdateTime = 0f;
    private string currentSnapshotId = "";
    
    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        lastSnapshot = new PerceptionSnapshot();
        lastAIPosition = aiTransform.position;
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
        InvokeRepeating(nameof(UpdatePerception), 0.5f, updateInterval);
    }
    
    /// <summary>
    /// Main perception update - generates TEXT-BASED perception with change detection
    /// </summary>
    private void UpdatePerception()
    {
        // Check for significant changes FIRST (before generating perception)
        if (!HasTextPerceptionChanges())
        {
            // Nothing changed, skip this update
            return;
        }
        
        // Generate text-based perception
        string textPerception = GenerateTextPerception();
        
        // Send to backend
        Debug.Log($"[Perception] Sending update:\n{textPerception}");
        OnTextPerception?.Invoke(textPerception);
        
        // Update tracking variables
        lastAIPosition = aiTransform.position;
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
        lastUpdateTime = Time.time;
        
        // Also generate legacy JSON snapshot for backward compatibility
        PerceptionSnapshot snapshot = GenerateSnapshot();
        lastSnapshot = snapshot;
    }
    
    /// <summary>
    /// Generate perception snapshot
    /// </summary>
    private PerceptionSnapshot GenerateSnapshot()
    {
        PerceptionSnapshot snapshot = new PerceptionSnapshot
        {
            type = "state_snapshot",
            id = currentSnapshotId,
            timestamp = Time.time
        };
        
        // Self state
        snapshot.self = new SelfState
        {
            position = aiTransform.position,
            rotation = aiTransform.eulerAngles.y,
            health = 1.0f, // Placeholder - will be connected to actual health system
            stance = "stand" // Placeholder
        };
        
        // Player state
        if (playerTransform != null)
        {
            snapshot.player = new EntityState
            {
                id = "player",
                position = playerTransform.position,
                distance = Vector3.Distance(aiTransform.position, playerTransform.position),
                onScreen = IsVisibleOnScreen(playerTransform.position)
            };
        }
        
        // Detect objects in camera frustum (screen-space perception)
        snapshot.onScreen = DetectObjectsOnScreen();
        
        return snapshot;
    }
    
    /// <summary>
    /// Detect objects visible on screen from camera perspective
    /// </summary>
    private List<ScreenObject> DetectObjectsOnScreen()
    {
        List<ScreenObject> objects = new List<ScreenObject>();
        
        // Find all detectable objects nearby
        Collider[] nearbyColliders = Physics.OverlapSphere(mainCamera.transform.position, detectionRadius, detectableLayers);
        
        foreach (var col in nearbyColliders)
        {
            if (col.transform == aiTransform) continue; // Skip self
            
            Vector3 worldPos = col.bounds.center;
            
            // Check if in camera frustum
            if (!IsInCameraFrustum(worldPos)) continue;
            
            // Project to screen space
            Vector3 screenPos = mainCamera.WorldToViewportPoint(worldPos);
            
            // Create screen object
            ScreenObject obj = new ScreenObject
            {
                id = col.gameObject.GetInstanceID().ToString(),
                type = ClassifyObject(col.gameObject),
                screenPos = new float[] { screenPos.x, screenPos.y },
                worldPos = worldPos,
                distance = Vector3.Distance(aiTransform.position, worldPos),
                size = CalculateScreenSize(col.bounds, mainCamera)
            };
            
            // Add affordance data if available and object is close enough
            ObjectAffordances affordances = col.GetComponent<ObjectAffordances>();
            if (affordances != null && obj.distance < 15f)
            {
                obj.height = affordances.height;
                obj.affordances = affordances.GetAffordances();
                obj.visualDescription = affordances.visualDescription;
            }
            else
            {
                // Default height from collider
                obj.height = col.bounds.size.y;
                obj.affordances = new Dictionary<string, object>();
            }
            
            objects.Add(obj);
            
            // Track position for change detection
            lastKnownPositions[obj.id] = worldPos;
            
            if (objects.Count >= maxObjectsOnScreen) break;
        }
        
        // Sort by distance (prioritize closer objects)
        objects.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        return objects;
    }
    
    /// <summary>
    /// Check if position is in camera frustum
    /// </summary>
    private bool IsInCameraFrustum(Vector3 worldPos)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(worldPos);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }
    
    /// <summary>
    /// Check if position is visible on screen
    /// </summary>
    private bool IsVisibleOnScreen(Vector3 worldPos)
    {
        return IsInCameraFrustum(worldPos);
    }
    
    /// <summary>
    /// Calculate screen size of object
    /// </summary>
    private string CalculateScreenSize(Bounds bounds, Camera cam)
    {
        Vector3 screenMin = cam.WorldToViewportPoint(bounds.min);
        Vector3 screenMax = cam.WorldToViewportPoint(bounds.max);
        
        float screenArea = Mathf.Abs(screenMax.x - screenMin.x) * Mathf.Abs(screenMax.y - screenMin.y);
        
        if (screenArea > 0.1f) return "large";
        if (screenArea > 0.02f) return "medium";
        return "small";
    }
    
    /// <summary>
    /// Classify object type based on tags/layers
    /// </summary>
    private string ClassifyObject(GameObject obj)
    {
        // Classification logic based on tags/names
        if (obj.CompareTag("Enemy")) return "enemy";
        if (obj.CompareTag("Player")) return "player";
        if (obj.name.Contains("Cover") || obj.name.Contains("Wall")) return "cover";
        if (obj.name.Contains("Item") || obj.name.Contains("Pickup")) return "item";
        if (obj.name.Contains("Rock")) return "cover";
        if (obj.name.Contains("Tree")) return "cover";
        if (obj.name.Contains("Building")) return "cover";
        
        return "object";
    }
    
    // ============================================================================
    // TEXT-BASED PERCEPTION SYSTEM (New Implementation)
    // ============================================================================
    
    /// <summary>
    /// Get nearby objects with AIReadable component (within 50m radius)
    /// </summary>
    private List<GameObject> GetNearbyObjects()
    {
        List<GameObject> nearby = new List<GameObject>();
        
        // Find all objects with AIReadable component
        // Use FindObjectsByType instead of deprecated FindObjectsOfType
        AIReadable[] allReadables = UnityEngine.Object.FindObjectsByType<AIReadable>(FindObjectsSortMode.None);
        
        foreach (AIReadable readable in allReadables)
        {
            float distance = Vector3.Distance(aiTransform.position, readable.transform.position);
            
            // Only include if within detection radius and not self
            if (distance <= detectionRadius && distance > 0.1f)
            {
                nearby.Add(readable.gameObject);
            }
        }
        
        // Sort by distance (closest first)
        nearby.Sort((a, b) => {
            float distA = Vector3.Distance(aiTransform.position, a.transform.position);
            float distB = Vector3.Distance(aiTransform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        return nearby;
    }
    
    /// <summary>
    /// Calculate direction from AI to target in natural language
    /// </summary>
    private string GetDirection(Vector3 targetPos)
    {
        Vector3 directionVector = targetPos - aiTransform.position;
        directionVector.y = 0; // Ignore height difference
        
        // Calculate angle relative to AI's forward direction
        float angle = Vector3.SignedAngle(aiTransform.forward, directionVector, Vector3.up);
        
        // Convert angle to natural language direction
        if (angle >= -22.5f && angle < 22.5f) return "ahead";
        if (angle >= 22.5f && angle < 67.5f) return "ahead-right";
        if (angle >= 67.5f && angle < 112.5f) return "right";
        if (angle >= 112.5f && angle < 157.5f) return "behind-right";
        if (angle >= 157.5f || angle < -157.5f) return "behind";
        if (angle >= -157.5f && angle < -112.5f) return "behind-left";
        if (angle >= -112.5f && angle < -67.5f) return "left";
        return "ahead-left";
    }
    
    /// <summary>
    /// Generate natural language perception summary
    /// </summary>
    private string GenerateTextPerception()
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        
        // 1. Player information (position only, Fortnite-style)
        if (playerTransform != null)
        {
            float playerDist = Vector3.Distance(aiTransform.position, playerTransform.position);
            string playerDir = GetDirection(playerTransform.position);
            text.AppendLine($"Player: {playerDist:F0}m {playerDir}.");
        }
        else
        {
            text.AppendLine("Player: Not detected.");
        }
        
        // 2. Nearby objects (within detectionRadius)
        List<GameObject> nearbyObjects = GetNearbyObjects();
        if (nearbyObjects.Count > 0)
        {
            text.Append("Nearby: ");
            
            // Limit to max 15 objects to prevent token overflow
            int objectCount = Mathf.Min(nearbyObjects.Count, 15);
            for (int i = 0; i < objectCount; i++)
            {
                GameObject obj = nearbyObjects[i];
                AIReadable readable = obj.GetComponent<AIReadable>();
                float dist = Vector3.Distance(aiTransform.position, obj.transform.position);
                string dir = GetDirection(obj.transform.position);
                
                // Format: "object-name distance direction (properties)"
                text.Append($"{obj.name} {dist:F0}m {dir}");
                
                // Add relevant properties
                if (readable != null)
                {
                    if (readable.providesCover) text.Append(" (cover)");
                    if (readable.isThreat) text.Append(" (THREAT)");
                    if (readable.canInteract) text.Append(" (interactive)");
                }
                
                if (i < objectCount - 1) text.Append(", ");
            }
            text.AppendLine(".");
            
            // Track visible objects for change detection
            lastVisibleObjectIDs = nearbyObjects.Select(o => o.name).ToList();
        }
        else
        {
            text.AppendLine("Nearby: No objects detected.");
            lastVisibleObjectIDs.Clear();
        }
        
        // 3. Threats check
        GameObject threat = nearbyObjects.FirstOrDefault(o => {
            AIReadable r = o.GetComponent<AIReadable>();
            return r != null && r.isThreat;
        });
        
        if (threat != null)
        {
            float threatDist = Vector3.Distance(aiTransform.position, threat.transform.position);
            string threatDir = GetDirection(threat.transform.position);
            text.AppendLine($"ALERT: {threat.name} {threatDist:F0}m {threatDir}!");
        }
        else
        {
            text.AppendLine("Threats: None visible.");
        }
        
        return text.ToString();
    }
    
    /// <summary>
    /// Check if there are significant changes (OPTIMIZED for text perception)
    /// </summary>
    private bool HasTextPerceptionChanges()
    {
        // 1. AI moved significantly?
        float aiMovement = Vector3.Distance(aiTransform.position, lastAIPosition);
        if (aiMovement > significantMovementThreshold)
        {
            Debug.Log($"[Perception] AI moved {aiMovement:F1}m, sending update");
            return true;
        }
        
        // 2. Player moved significantly?
        if (playerTransform != null)
        {
            float playerMovement = Vector3.Distance(playerTransform.position, lastPlayerPosition);
            if (playerMovement > significantPlayerMovementThreshold)
            {
                Debug.Log($"[Perception] Player moved {playerMovement:F1}m, sending update");
                return true;
            }
        }
        
        // 3. Objects in view changed?
        List<GameObject> currentObjects = GetNearbyObjects();
        List<string> currentIDs = currentObjects.Select(o => o.name).ToList();
        
        if (currentIDs.Count != lastVisibleObjectIDs.Count || 
            !currentIDs.SequenceEqual(lastVisibleObjectIDs))
        {
            Debug.Log("[Perception] Visible objects changed, sending update");
            return true;
        }
        
        // 4. Minimum time elapsed (periodic update)
        if (Time.time - lastUpdateTime > periodicUpdateInterval)
        {
            Debug.Log("[Perception] Periodic update (2s interval)");
            return true;
        }
        
        // No significant changes
        return false;
    }
    
    // ============================================================================
    // LEGACY JSON PERCEPTION SYSTEM (For Backward Compatibility)
    // ============================================================================
    
    /// <summary>
    /// Check if snapshot has significant changes from last one (delta compression)
    /// </summary>
    private bool HasSignificantChanges(PerceptionSnapshot snapshot)
    {
        // Always send first snapshot
        if (lastSnapshot.id == null) return true;
        
        // Check self position change
        if (Vector3.Distance(snapshot.self.position, lastSnapshot.self.position) > significantMovementThreshold)
        {
            return true;
        }
        
        // Check player position change
        if (snapshot.player != null && lastSnapshot.player != null)
        {
            if (Vector3.Distance(snapshot.player.position, lastSnapshot.player.position) > significantMovementThreshold)
            {
                return true;
            }
        }
        
        // Check object count change
        if (snapshot.onScreen.Count != lastSnapshot.onScreen.Count)
        {
            return true;
        }
        
        // If no significant changes, skip update (cost optimization)
        return false;
    }
    
    /// <summary>
    /// Emit perception event (for immediate important occurrences)
    /// </summary>
    public void EmitEvent(string eventType, Dictionary<string, object> data)
    {
        // Check cooldown to prevent spam
        if (lastEventTime.ContainsKey(eventType))
        {
            if (Time.time - lastEventTime[eventType] < significantEventCooldown)
            {
                return;
            }
        }
        
        lastEventTime[eventType] = Time.time;
        
        // Legacy event system - now using text-based perception only
        // PerceptionEvent is kept for reference but not invoked
    }
    
    /// <summary>
    /// Get current snapshot on demand
    /// </summary>
    public PerceptionSnapshot GetCurrentSnapshot()
    {
        return GenerateSnapshot();
    }
}

/// <summary>
/// Perception snapshot structure
/// </summary>
[Serializable]
public class PerceptionSnapshot
{
    public string type;
    public string id;
    public float timestamp;
    public SelfState self;
    public EntityState player;
    public List<ScreenObject> onScreen = new List<ScreenObject>();
}

/// <summary>
/// Self state
/// </summary>
[Serializable]
public class SelfState
{
    public Vector3 position;
    public float rotation;
    public float health;
    public string stance;
}

/// <summary>
/// Entity state (player, NPCs)
/// </summary>
[Serializable]
public class EntityState
{
    public string id;
    public Vector3 position;
    public float distance;
    public bool onScreen;
}

/// <summary>
/// Object visible on screen
/// </summary>
[Serializable]
public class ScreenObject
{
    public string id;
    public string type;
    public float[] screenPos; // Normalized [0-1]
    public Vector3 worldPos;
    public float distance;
    public string size; // "small", "medium", "large"
    
    // Affordance data (compressed)
    public float height;
    public Dictionary<string, object> affordances;
    public string visualDescription;
}

/// <summary>
/// Perception event for immediate occurrences
/// </summary>
[Serializable]
public class PerceptionEvent
{
    public string type;
    public string id;
    public float timestamp;
    public string eventType;
    public Dictionary<string, object> data;
}

