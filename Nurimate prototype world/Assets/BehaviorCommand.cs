using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for behavior commands from LLM
/// Serializable for JSON parsing
/// </summary>
[Serializable]
public class BehaviorCommand
{
    public string type = "behavior";
    public string behavior;
    public string target;
    public float duration = 10f;
    public Dictionary<string, object> context;
    
    // For JSON deserialization
    public BehaviorCommand() 
    {
        context = new Dictionary<string, object>();
    }
    
    public BehaviorCommand(string behavior, string target = null, Dictionary<string, object> context = null)
    {
        this.behavior = behavior;
        this.target = target;
        this.context = context ?? new Dictionary<string, object>();
    }
    
    // Helper to get context values safely
    public T GetContext<T>(string key, T defaultValue = default(T))
    {
        if (context != null && context.ContainsKey(key))
        {
            try
            {
                return (T)Convert.ChangeType(context[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}



