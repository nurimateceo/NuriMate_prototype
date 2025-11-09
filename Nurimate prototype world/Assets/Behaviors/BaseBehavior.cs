using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all AI behaviors
/// Behaviors interpret high-level commands and use CommandExecutor for low-level actions
/// </summary>
public abstract class BaseBehavior
{
    protected BehaviorCommand command;
    protected CommandExecutor executor;
    protected bool isCancelled = false;
    
    public BaseBehavior(BehaviorCommand command)
    {
        this.command = command;
    }
    
    /// <summary>
    /// Execute the behavior (coroutine)
    /// </summary>
    public abstract IEnumerator Execute(CommandExecutor executor);
    
    /// <summary>
    /// Cancel the behavior mid-execution
    /// </summary>
    public virtual void Cancel()
    {
        isCancelled = true;
        Debug.Log($"[Behavior] Cancelled: {command.behavior}");
    }
    
    /// <summary>
    /// Get current status of behavior
    /// </summary>
    public virtual string GetStatus()
    {
        return isCancelled ? "cancelled" : "executing";
    }
    
    /// <summary>
    /// Helper to find GameObject by name (AIReadable objects)
    /// </summary>
    protected GameObject FindObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;
            
        // First try exact match
        GameObject obj = GameObject.Find(objectName);
        if (obj != null && obj.GetComponent<AIReadable>() != null)
            return obj;
        
        // Fallback: search all AIReadable objects (using modern API)
        AIReadable[] readables = UnityEngine.Object.FindObjectsByType<AIReadable>(FindObjectsSortMode.None);
        foreach (var readable in readables)
        {
            if (readable.gameObject.name == objectName)
                return readable.gameObject;
        }
        
        return null;
    }
}



