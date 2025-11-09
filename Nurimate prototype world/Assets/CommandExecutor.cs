 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Executes structured command plans from LLM with state tracking and interrupt handling
/// </summary>
public class CommandExecutor : MonoBehaviour
{
    // State machine
    public enum ExecutionState { Idle, Executing, Success, Failed, Timeout, Cancelled }
    
    [Header("State")]
    public ExecutionState currentState = ExecutionState.Idle;
    public string currentCommandId = "";
    public float commandStartTime = 0f;
    
    [Header("Configuration")]
    public float defaultTimeout = 30f;
    public float positionTolerance = 0.5f;
    public float followDistance = 4f;
    public float aiMaxClimbHeight = 2.5f; // AI's maximum climb/jump capability
    
    [Header("References")]
    public NavMeshAgent navAgent;
    public Transform aiTransform;
    public Transform playerTransform;
    
    // Current execution context
    private CommandPlan currentPlan;
    private int currentStepIndex = 0;
    private Coroutine executionCoroutine;
    
    // Events for communication bridge
    public event Action<string, string> OnCommandAck; // commandId, status
    public event Action<string, float> OnCommandProgress; // commandId, progress
    public event Action<string, string, string> OnCommandComplete; // commandId, result, details
    
    private void Awake()
    {
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (aiTransform == null) aiTransform = transform;
    }
    
    private void Start()
    {
        // Auto-assign player transform if missing (done in Start, not Awake, for proper initialization order)
        if (playerTransform == null)
        {
            Debug.Log("[CommandExecutor] Player Transform not assigned, searching for Player object...");
            
            // Try to find Player object by name
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.Log("[CommandExecutor] ✓ Auto-assigned Player Transform from GameObject.Find('Player')");
            }
            else
            {
                Debug.LogWarning("[CommandExecutor] Could not find GameObject named 'Player'");
                
                // Try to find by tag
                GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null)
                {
                    playerTransform = playerByTag.transform;
                    Debug.Log("[CommandExecutor] ✓ Auto-assigned Player Transform (by tag)");
                }
                else
                {
                    Debug.LogError("[CommandExecutor] ✗ FAILED: Player Transform not found! Please either:\n1. Name your player 'Player'\n2. Tag it with 'Player'\n3. Manually assign in Inspector");
                }
            }
        }
        else
        {
            Debug.Log("[CommandExecutor] Player Transform already assigned in Inspector");
        }
    }
    
    /// <summary>
    /// Execute a command plan from JSON
    /// </summary>
    public void ExecuteCommand(CommandPlan plan)
    {
        // Cancel current if executing
        if (currentState == ExecutionState.Executing)
        {
            CancelCurrent("superseded");
        }
        
        currentPlan = plan;
        currentCommandId = plan.id;
        currentStepIndex = 0;
        commandStartTime = Time.time;
        currentState = ExecutionState.Executing;
        
        // Send acknowledgment
        OnCommandAck?.Invoke(plan.id, "accepted");
        
        // Start execution
        if (executionCoroutine != null) StopCoroutine(executionCoroutine);
        executionCoroutine = StartCoroutine(ExecutePlanCoroutine());
    }
    
    /// <summary>
    /// Cancel current execution
    /// </summary>
    public void CancelCurrent(string reason = "cancelled")
    {
        if (executionCoroutine != null)
        {
            StopCoroutine(executionCoroutine);
            executionCoroutine = null;
        }
        
        if (navAgent.hasPath)
        {
            navAgent.ResetPath();
        }
        
        currentState = ExecutionState.Cancelled;
        OnCommandComplete?.Invoke(currentCommandId, "cancelled", reason);
        
        currentCommandId = "";
        currentPlan = null;
    }
    
    /// <summary>
    /// Main execution coroutine
    /// </summary>
    private IEnumerator ExecutePlanCoroutine()
    {
        if (currentPlan == null || currentPlan.sequence == null || currentPlan.sequence.Count == 0)
        {
            FinishExecution(ExecutionState.Failed, "empty_plan");
            yield break;
        }
        
        for (currentStepIndex = 0; currentStepIndex < currentPlan.sequence.Count; currentStepIndex++)
        {
            var step = currentPlan.sequence[currentStepIndex];
            
            // Report progress
            float progress = (float)currentStepIndex / currentPlan.sequence.Count;
            OnCommandProgress?.Invoke(currentCommandId, progress);
            
            // Check timeout
            if (Time.time - commandStartTime > defaultTimeout)
            {
                FinishExecution(ExecutionState.Timeout, "execution_timeout");
                yield break;
            }
            
            // Execute step
            yield return ExecuteStepCoroutine(step);
            
            // Check if step failed
            if (currentState == ExecutionState.Failed || currentState == ExecutionState.Cancelled)
            {
                yield break;
            }
        }
        
        // All steps completed
        FinishExecution(ExecutionState.Success, "completed");
    }
    
    /// <summary>
    /// Execute a single step
    /// </summary>
    private IEnumerator ExecuteStepCoroutine(PlanStep step)
    {
        switch (step.action)
        {
            case "move_to":
                yield return ExecuteMoveTo(step.parameters);
                break;
                
            case "follow_player":
                yield return ExecuteFollow(step.parameters);
                break;
                
            case "patrol":
                yield return ExecutePatrol(step.parameters);
                break;
                
            case "look_at":
                yield return ExecuteLookAt(step.parameters);
                break;
                
            case "hold_position":
                yield return ExecuteHoldPosition(step.parameters);
                break;
                
            case "wait":
                yield return ExecuteWait(step.parameters);
                break;
                
            default:
                Debug.LogWarning($"Unknown action: {step.action}");
                yield return null;
                break;
        }
    }
    
    /// <summary>
    /// Move to a specific position
    /// </summary>
    private IEnumerator ExecuteMoveTo(Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("pos"))
        {
            FinishExecution(ExecutionState.Failed, "missing_position");
            yield break;
        }
        
        Vector3 targetPos = ParseVector3(parameters["pos"]);
        float tolerance = parameters.ContainsKey("tolerance") ? 
            Convert.ToSingle(parameters["tolerance"]) : positionTolerance;
        
        // Check if reachable
        NavMeshPath path = new NavMeshPath();
        if (!navAgent.CalculatePath(targetPos, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            FinishExecution(ExecutionState.Failed, "unreachable_position");
            yield break;
        }
        
        navAgent.SetDestination(targetPos);
        
        // Wait until reached
        while (navAgent.pathPending || navAgent.remainingDistance > tolerance)
        {
            // Check for path failure
            if (!navAgent.hasPath || navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                FinishExecution(ExecutionState.Failed, "path_failed");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        navAgent.ResetPath();
    }
    
    /// <summary>
    /// Follow the player
    /// </summary>
    private IEnumerator ExecuteFollow(Dictionary<string, object> parameters)
    {
        if (playerTransform == null)
        {
            FinishExecution(ExecutionState.Failed, "no_player");
            yield break;
        }
        
        float distance = parameters.ContainsKey("followDistance") ? 
            Convert.ToSingle(parameters["followDistance"]) : followDistance;
        
        float duration = parameters.ContainsKey("duration") ? 
            Convert.ToSingle(parameters["duration"]) : 5f;
        
        float startTime = Time.time;
        
        while (Time.time - startTime < duration)
        {
            float distToPlayer = Vector3.Distance(aiTransform.position, playerTransform.position);
            
            if (distToPlayer > distance + 1f)
            {
                navAgent.SetDestination(playerTransform.position);
            }
            else if (distToPlayer < distance - 1f && navAgent.hasPath)
            {
                navAgent.ResetPath();
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        if (navAgent.hasPath)
        {
            navAgent.ResetPath();
        }
    }
    
    /// <summary>
    /// Patrol waypoints
    /// </summary>
    private IEnumerator ExecutePatrol(Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("waypoints"))
        {
            FinishExecution(ExecutionState.Failed, "missing_waypoints");
            yield break;
        }
        
        List<Vector3> waypoints = ParseVector3List(parameters["waypoints"]);
        
        foreach (var waypoint in waypoints)
        {
            navAgent.SetDestination(waypoint);
            
            while (navAgent.pathPending || navAgent.remainingDistance > positionTolerance)
            {
                if (!navAgent.hasPath)
                {
                    FinishExecution(ExecutionState.Failed, "patrol_path_failed");
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        navAgent.ResetPath();
    }
    
    /// <summary>
    /// Look at target
    /// </summary>
    private IEnumerator ExecuteLookAt(Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("target"))
        {
            yield break;
        }
        
        Vector3 target = ParseVector3(parameters["target"]);
        float duration = parameters.ContainsKey("duration") ? 
            Convert.ToSingle(parameters["duration"]) : 1f;
        
        Vector3 direction = (target - aiTransform.position).normalized;
        direction.y = 0; // Keep on horizontal plane
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            aiTransform.rotation = Quaternion.Slerp(aiTransform.rotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        aiTransform.rotation = targetRotation;
    }
    
    /// <summary>
    /// Hold position
    /// </summary>
    private IEnumerator ExecuteHoldPosition(Dictionary<string, object> parameters)
    {
        float duration = parameters.ContainsKey("duration") ? 
            Convert.ToSingle(parameters["duration"]) : 5f;
        
        if (navAgent.hasPath)
        {
            navAgent.ResetPath();
        }
        
        yield return new WaitForSeconds(duration);
    }
    
    /// <summary>
    /// Wait for duration
    /// </summary>
    private IEnumerator ExecuteWait(Dictionary<string, object> parameters)
    {
        float duration = parameters.ContainsKey("duration") ? 
            Convert.ToSingle(parameters["duration"]) : 1f;
        
        yield return new WaitForSeconds(duration);
    }
    
    /// <summary>
    /// Finish execution with result
    /// </summary>
    private void FinishExecution(ExecutionState state, string details)
    {
        currentState = state;
        
        string result = state == ExecutionState.Success ? "success" : 
                       state == ExecutionState.Failed ? "failed" :
                       state == ExecutionState.Timeout ? "timeout" : "cancelled";
        
        OnCommandComplete?.Invoke(currentCommandId, result, details);
        
        currentCommandId = "";
        currentPlan = null;
    }
    
    // Affordance validation methods
    
    /// <summary>
    /// Validate if AI can perform action on object
    /// </summary>
    private bool ValidateAffordance(GameObject obj, string action, out string failureReason)
    {
        failureReason = "";
        
        if (obj == null)
        {
            failureReason = "object_not_found";
            return false;
        }
        
        ObjectAffordances affordances = obj.GetComponent<ObjectAffordances>();
        
        // If no affordances component, use basic validation
        if (affordances == null)
        {
            return ValidateBasicAction(obj, action, out failureReason);
        }
        
        // Check affordances
        bool canPerform = affordances.CanPerformAction(action, aiMaxClimbHeight);
        
        if (!canPerform)
        {
            // Provide specific failure reason
            switch (action.ToLower())
            {
                case "climb":
                    if (!affordances.canClimb)
                        failureReason = "not_climbable";
                    else if (affordances.climbHeight > aiMaxClimbHeight)
                        failureReason = $"too_tall_max_{aiMaxClimbHeight}m";
                    else
                        failureReason = "cannot_climb";
                    break;
                    
                case "jump":
                case "jump_on":
                    if (!affordances.canJumpOn)
                        failureReason = "not_jumpable";
                    else if (affordances.jumpHeight > aiMaxClimbHeight)
                        failureReason = $"too_high_max_{aiMaxClimbHeight}m";
                    else
                        failureReason = "cannot_jump";
                    break;
                    
                case "pickup":
                    failureReason = "cannot_pickup";
                    break;
                    
                case "open":
                    if (affordances.requiresKey)
                        failureReason = "needs_key";
                    else
                        failureReason = "cannot_open";
                    break;
                    
                case "cover":
                case "take_cover":
                    failureReason = "no_cover_available";
                    break;
                    
                default:
                    failureReason = $"action_{action}_not_possible";
                    break;
            }
        }
        
        return canPerform;
    }
    
    /// <summary>
    /// Basic validation when no affordances component present
    /// </summary>
    private bool ValidateBasicAction(GameObject obj, string action, out string failureReason)
    {
        failureReason = "";
        
        // Get object height from collider
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            failureReason = "no_collider";
            return false;
        }
        
        float height = col.bounds.size.y;
        
        switch (action.ToLower())
        {
            case "climb":
            case "jump":
            case "jump_on":
                if (height > aiMaxClimbHeight)
                {
                    failureReason = $"too_tall_{height:F1}m_max_{aiMaxClimbHeight}m";
                    return false;
                }
                return true;
                
            case "cover":
            case "take_cover":
                // Most objects can provide cover if tall enough
                return height >= 1.0f;
                
            default:
                // Unknown actions allowed by default (fail gracefully during execution)
                return true;
        }
    }
    
    /// <summary>
    /// Find nearest object with specific affordance
    /// </summary>
    private GameObject FindNearestObjectWithAffordance(string affordance, float maxDistance = 20f)
    {
        GameObject nearest = null;
        float nearestDist = maxDistance;
        
        Collider[] nearby = Physics.OverlapSphere(aiTransform.position, maxDistance);
        
        foreach (var col in nearby)
        {
            ObjectAffordances aff = col.GetComponent<ObjectAffordances>();
            if (aff != null && aff.CanPerformAction(affordance, aiMaxClimbHeight))
            {
                float dist = Vector3.Distance(aiTransform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearest = col.gameObject;
                    nearestDist = dist;
                }
            }
        }
        
        return nearest;
    }
    
    // Utility parsers
    private Vector3 ParseVector3(object data)
    {
        if (data is List<object> list && list.Count >= 3)
        {
            return new Vector3(
                Convert.ToSingle(list[0]),
                Convert.ToSingle(list[1]),
                Convert.ToSingle(list[2])
            );
        }
        return Vector3.zero;
    }
    
    private List<Vector3> ParseVector3List(object data)
    {
        List<Vector3> result = new List<Vector3>();
        if (data is List<object> list)
        {
            foreach (var item in list)
            {
                result.Add(ParseVector3(item));
            }
        }
        return result;
    }
}

/// <summary>
/// Command plan structure
/// </summary>
[Serializable]
public class CommandPlan
{
    public string type = "command";
    public string id;
    public List<PlanStep> sequence;
}

/// <summary>
/// Individual plan step
/// </summary>
[Serializable]
public class PlanStep
{
    public string action;
    public Dictionary<string, object> parameters = new Dictionary<string, object>();
}

