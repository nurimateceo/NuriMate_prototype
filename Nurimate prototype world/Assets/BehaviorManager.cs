using System.Collections;
using UnityEngine;

/// <summary>
/// Routes high-level behavior commands to appropriate behavior implementations
/// Acts as the "Tactical Layer" between LLM strategy and CommandExecutor motor control
/// </summary>
public class BehaviorManager : MonoBehaviour
{
    [Header("References")]
    public CommandExecutor commandExecutor;
    
    [Header("State")]
    public string currentBehaviorName = "none";
    public bool isExecuting = false;
    
    private BaseBehavior currentBehavior;
    private Coroutine behaviorCoroutine;
    
    private void Awake()
    {
        if (commandExecutor == null)
        {
            commandExecutor = GetComponent<CommandExecutor>();
        }
    }
    
    /// <summary>
    /// Execute a behavior command from JSON
    /// </summary>
    public void ExecuteBehaviorCommand(string jsonCommand)
    {
        try
        {
            BehaviorCommand cmd = JsonUtility.FromJson<BehaviorCommand>(jsonCommand);
            ExecuteBehavior(cmd);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BehaviorManager] Failed to parse command: {e.Message}");
        }
    }
    
    /// <summary>
    /// Execute a behavior command
    /// </summary>
    public void ExecuteBehavior(BehaviorCommand cmd)
    {
        // Ensure CommandExecutor has player reference before executing behavior
        if (commandExecutor != null && commandExecutor.playerTransform == null)
        {
            // Try to find and assign player
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                commandExecutor.playerTransform = playerObj.transform;
                Debug.Log("[BehaviorManager] ✓ Pre-initialized Player Transform for behaviors");
            }
            else
            {
                GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null)
                {
                    commandExecutor.playerTransform = playerByTag.transform;
                    Debug.Log("[BehaviorManager] ✓ Pre-initialized Player Transform (by tag)");
                }
            }
        }
        
        // Cancel current behavior if executing
        if (isExecuting && currentBehavior != null)
        {
            Debug.Log($"[BehaviorManager] Superseding '{currentBehaviorName}' with '{cmd.behavior}'");
            currentBehavior.Cancel();
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
            }
        }
        
        // Create appropriate behavior
        currentBehavior = CreateBehavior(cmd);
        if (currentBehavior == null)
        {
            Debug.LogWarning($"[BehaviorManager] Unknown behavior: {cmd.behavior}");
            return;
        }
        
        currentBehaviorName = cmd.behavior;
        isExecuting = true;
        
        Debug.Log($"[BehaviorManager] Executing: {cmd.behavior}" + 
                  (string.IsNullOrEmpty(cmd.target) ? "" : $" → {cmd.target}"));
        
        // Start execution
        behaviorCoroutine = StartCoroutine(ExecuteBehaviorCoroutine(currentBehavior));
    }
    
    /// <summary>
    /// Create behavior instance based on command
    /// </summary>
    private BaseBehavior CreateBehavior(BehaviorCommand cmd)
    {
        switch (cmd.behavior)
        {
            case "follow_player":
                return new FollowBehavior(cmd);
                
            case "take_cover":
                return new TakeCoverBehavior(cmd);
                
            case "move_to":
                return new MoveToBehavior(cmd);
                
            case "hold_position":
                return new HoldPositionBehavior(cmd);
                
            default:
                // Fallback to hold position for unknown behaviors
                Debug.LogWarning($"[BehaviorManager] Unknown behavior '{cmd.behavior}', defaulting to hold_position");
                return new HoldPositionBehavior(new BehaviorCommand("hold_position"));
        }
    }
    
    /// <summary>
    /// Execute behavior in coroutine
    /// </summary>
    private IEnumerator ExecuteBehaviorCoroutine(BaseBehavior behavior)
    {
        yield return StartCoroutine(behavior.Execute(commandExecutor));
        
        // Behavior completed
        isExecuting = false;
        currentBehaviorName = "none";
        Debug.Log($"[BehaviorManager] Behavior completed");
    }
    
    /// <summary>
    /// Cancel current behavior
    /// </summary>
    public void CancelCurrentBehavior()
    {
        if (currentBehavior != null)
        {
            currentBehavior.Cancel();
        }
        
        if (behaviorCoroutine != null)
        {
            StopCoroutine(behaviorCoroutine);
            behaviorCoroutine = null;
        }
        
        isExecuting = false;
        currentBehaviorName = "none";
    }
}



