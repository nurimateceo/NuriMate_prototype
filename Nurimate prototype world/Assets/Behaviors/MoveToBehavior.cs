using System.Collections;
using UnityEngine;

/// <summary>
/// Move to object behavior - navigates to specified object/landmark
/// Simple navigation with adjustable approach style
/// </summary>
public class MoveToBehavior : BaseBehavior
{
    public MoveToBehavior(BehaviorCommand command) : base(command) { }
    
    public override IEnumerator Execute(CommandExecutor executor)
    {
        this.executor = executor;
        
        // Find target object
        GameObject targetObject = FindObjectByName(command.target);
        if (targetObject == null)
        {
            Debug.LogWarning($"[MoveToBehavior] Target object not found: {command.target}");
            yield break;
        }
        
        // Get approach style
        string approach = command.GetContext("approach", "normal");
        float tolerance = GetToleranceForApproach(approach);
        
        Debug.Log($"[MoveToBehavior] Moving to {command.target} ({approach} approach, tolerance={tolerance}m)");
        
        // Get target position
        Vector3 targetPosition = targetObject.transform.position;
        
        // Set destination
        executor.navAgent.SetDestination(targetPosition);
        
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
                Debug.LogWarning($"[MoveToBehavior] Cannot reach {command.target}");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Reached destination
        executor.navAgent.ResetPath();
        
        Debug.Log($"[MoveToBehavior] Reached {command.target}");
        
        // Optional: Look at the object
        Vector3 lookDirection = (targetPosition - executor.aiTransform.position).normalized;
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            float turnTime = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            while (turnTime < 0.5f && !isCancelled)
            {
                executor.aiTransform.rotation = Quaternion.Slerp(
                    executor.aiTransform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f
                );
                turnTime += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// Get distance tolerance based on approach style
    /// </summary>
    private float GetToleranceForApproach(string approach)
    {
        switch (approach.ToLower())
        {
            case "cautious":
                return 3.0f; // Stop further away
                
            case "urgent":
            case "fast":
                return 2.0f; // Can be less precise
                
            case "precise":
                return 0.5f; // Get very close
                
            default: // "normal"
                return 1.5f;
        }
    }
}



