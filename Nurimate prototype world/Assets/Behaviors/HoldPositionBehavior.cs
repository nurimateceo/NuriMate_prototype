using System.Collections;
using UnityEngine;

/// <summary>
/// Hold position behavior - stays in place and optionally scans surroundings
/// Used as fallback for unknown behaviors
/// </summary>
public class HoldPositionBehavior : BaseBehavior
{
    public HoldPositionBehavior(BehaviorCommand command) : base(command) { }
    
    public override IEnumerator Execute(CommandExecutor executor)
    {
        this.executor = executor;
        
        // Get parameters
        float duration = command.duration;
        bool scan = command.GetContext("scan", false);
        
        Debug.Log($"[HoldPositionBehavior] Holding position for {duration}s" + (scan ? " (scanning)" : ""));
        
        // Stop any current movement
        if (executor.navAgent.hasPath)
        {
            executor.navAgent.ResetPath();
        }
        
        float startTime = Time.time;
        float scanAngle = 0f;
        
        while (Time.time - startTime < duration && !isCancelled)
        {
            // Optional scanning behavior - slowly rotate to look around
            if (scan)
            {
                scanAngle += Time.deltaTime * 30f; // 30 degrees per second
                Vector3 scanDirection = Quaternion.Euler(0, scanAngle, 0) * Vector3.forward;
                Quaternion targetRotation = Quaternion.LookRotation(scanDirection);
                
                executor.aiTransform.rotation = Quaternion.Slerp(
                    executor.aiTransform.rotation,
                    targetRotation,
                    Time.deltaTime * 2f
                );
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[HoldPositionBehavior] Completed");
    }
}



