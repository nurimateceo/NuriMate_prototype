using System.Collections;
using UnityEngine;

/// <summary>
/// Follow player behavior - maintains distance from player
/// Directly implements follow logic without using CommandExecutor wrapper
/// </summary>
public class FollowBehavior : BaseBehavior
{
    public FollowBehavior(BehaviorCommand command) : base(command) { }
    
    public override IEnumerator Execute(CommandExecutor executor)
    {
        this.executor = executor;
        
        // Extract parameters
        float duration = command.duration;
        float followDistance = command.GetContext("followDistance", 5f);
        
        Debug.Log($"[FollowBehavior] Following player for {duration}s at {followDistance}m distance");
        
        // Execute follow logic directly
        yield return ExecuteFollowDirect(duration, followDistance);
        
        Debug.Log($"[FollowBehavior] Completed");
    }
    
    /// <summary>
    /// Direct implementation of follow logic
    /// </summary>
    private IEnumerator ExecuteFollowDirect(float duration, float followDistance)
    {
        if (executor.playerTransform == null)
        {
            Debug.LogWarning("[FollowBehavior] No player to follow - trying to auto-find...");
            
            // Try to find Player if not assigned
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                executor.playerTransform = playerObj.transform;
                Debug.Log("[FollowBehavior] ✓ Auto-found Player, continuing follow");
            }
            else
            {
                // Try by tag
                GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null)
                {
                    executor.playerTransform = playerByTag.transform;
                    Debug.Log("[FollowBehavior] ✓ Auto-found Player by tag, continuing follow");
                }
                else
                {
                    Debug.LogError("[FollowBehavior] ✗ FAILED: Cannot find Player! Is it named 'Player' or tagged 'Player'?");
                    yield break;
                }
            }
        }
        
        float startTime = Time.time;
        
        while (Time.time - startTime < duration && !isCancelled)
        {
            float distToPlayer = Vector3.Distance(executor.aiTransform.position, executor.playerTransform.position);
            
            // Move closer if too far
            if (distToPlayer > followDistance + 1f)
            {
                executor.navAgent.SetDestination(executor.playerTransform.position);
            }
            // Stop if close enough
            else if (distToPlayer <= followDistance)
            {
                if (executor.navAgent.hasPath)
                {
                    executor.navAgent.ResetPath();
                }
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        // Stop movement
        if (executor.navAgent.hasPath)
        {
            executor.navAgent.ResetPath();
        }
    }
}

