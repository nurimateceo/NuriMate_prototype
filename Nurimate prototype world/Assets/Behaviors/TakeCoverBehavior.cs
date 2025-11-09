using System.Collections;
using UnityEngine;

/// <summary>
/// Take cover behavior - finds cover object and moves to safe position behind it
/// Calculates position based on threat direction
/// </summary>
public class TakeCoverBehavior : BaseBehavior
{
    public TakeCoverBehavior(BehaviorCommand command) : base(command) { }
    
    public override IEnumerator Execute(CommandExecutor executor)
    {
        this.executor = executor;
        
        // Find cover object
        GameObject coverObject = FindObjectByName(command.target);
        if (coverObject == null)
        {
            Debug.LogWarning($"[TakeCoverBehavior] Cover object not found: {command.target}");
            yield break;
        }
        
        // Verify it provides cover
        AIReadable readable = coverObject.GetComponent<AIReadable>();
        if (readable != null && !readable.providesCover)
        {
            Debug.LogWarning($"[TakeCoverBehavior] Object {command.target} doesn't provide cover");
        }
        
        // Get threat direction
        string threatDir = command.GetContext("threat_direction", "ahead");
        string urgency = command.GetContext("urgency", "normal");
        
        Debug.Log($"[TakeCoverBehavior] Taking cover behind {command.target}, threat from {threatDir}");
        
        // Calculate safe position behind cover
        Vector3 coverPosition = CalculateCoverPosition(coverObject, threatDir);
        
        // Move to cover position
        float tolerance = urgency == "high" ? 2.0f : 1.0f;
        
        executor.navAgent.SetDestination(coverPosition);
        
        // Wait until reached or cancelled
        while (!isCancelled)
        {
            if (!executor.navAgent.pathPending && executor.navAgent.remainingDistance <= tolerance)
            {
                break;
            }
            
            // Check for path failure
            if (!executor.navAgent.hasPath || executor.navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("[TakeCoverBehavior] Path to cover failed");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Stop at cover
        executor.navAgent.ResetPath();
        
        // Face threat direction
        Vector3 threatVector = DirectionToVector(threatDir, executor.aiTransform);
        Vector3 lookTarget = executor.aiTransform.position + threatVector * 10f;
        
        // Gradual turn to face threat
        float turnTime = 0f;
        while (turnTime < 1f && !isCancelled)
        {
            Vector3 direction = (lookTarget - executor.aiTransform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                executor.aiTransform.rotation = Quaternion.Slerp(
                    executor.aiTransform.rotation, 
                    targetRotation, 
                    Time.deltaTime * 3f
                );
            }
            turnTime += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log("[TakeCoverBehavior] In cover, watching for threats");
    }
    
    /// <summary>
    /// Calculate safe position behind cover object, away from threat
    /// </summary>
    private Vector3 CalculateCoverPosition(GameObject coverObj, string threatDirection)
    {
        Vector3 threatVector = DirectionToVector(threatDirection, executor.aiTransform);
        
        // Position is behind cover, away from threat
        // Place AI 2 meters behind cover from threat perspective
        Vector3 coverPos = coverObj.transform.position - (threatVector * 2f);
        
        // Keep Y coordinate at ground level
        coverPos.y = executor.aiTransform.position.y;
        
        return coverPos;
    }
    
    /// <summary>
    /// Convert direction string to Vector3
    /// </summary>
    private Vector3 DirectionToVector(string direction, Transform reference)
    {
        switch (direction.ToLower())
        {
            case "ahead":
            case "forward":
                return reference.forward;
                
            case "behind":
            case "back":
                return -reference.forward;
                
            case "left":
                return -reference.right;
                
            case "right":
                return reference.right;
                
            case "ahead-left":
                return (reference.forward - reference.right).normalized;
                
            case "ahead-right":
                return (reference.forward + reference.right).normalized;
                
            case "behind-left":
                return (-reference.forward - reference.right).normalized;
                
            case "behind-right":
                return (-reference.forward + reference.right).normalized;
                
            default:
                return reference.forward;
        }
    }
}



