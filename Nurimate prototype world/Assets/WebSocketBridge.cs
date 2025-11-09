using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTP communication bridge for LLM integration
/// Uses HTTP polling for reliable Unity-Python communication
/// </summary>
public class WebSocketBridge : MonoBehaviour
{
    [Header("Configuration")]
    public bool enableTestMode = false;
    [Tooltip("HTTP server URL (use http://, not ws://)")]
    public string serverUrl = "http://localhost:8080";
    public bool autoConnect = true;
    public float pollInterval = 0.3f; // Poll every 300ms
    
    [Header("Test Commands")]
    [TextArea(5, 10)]
    public string testCommandJson = "";
    
    [Header("References")]
    public CommandExecutor commandExecutor;
    public BehaviorManager behaviorManager;
    public PerceptionSystem perceptionSystem;
    
    [Header("Status")]
    public bool isConnected = false;
    public int messagesSent = 0;
    public int messagesReceived = 0;
    
    // Message queue for test mode
    private Queue<string> testMessageQueue = new Queue<string>();
    
    private void Start()
    {
        // Auto-assign references if missing
        if (behaviorManager == null)
        {
            behaviorManager = GetComponent<BehaviorManager>();
            if (behaviorManager != null)
            {
                Debug.Log("[WebSocketBridge] Auto-assigned BehaviorManager reference");
            }
            else
            {
                Debug.LogWarning("[WebSocketBridge] BehaviorManager component not found!");
            }
        }
        
        if (commandExecutor == null)
        {
            commandExecutor = GetComponent<CommandExecutor>();
        }
        
        if (perceptionSystem == null)
        {
            perceptionSystem = GetComponent<PerceptionSystem>();
        }
        
        if (enableTestMode)
        {
            Debug.Log("[WebSocketBridge] Running in TEST MODE - use Inspector to send commands");
            LoadTestCommands();
        }
        else if (autoConnect)
        {
            Debug.Log("[WebSocketBridge] Starting HTTP polling mode...");
            isConnected = true;
            Debug.Log("[WebSocketBridge] ✓ Connected to AI server (HTTP mode)!");
        }
    }
    
    /// <summary>
    /// Initialize and subscribe to perception events - called by AIFriendController
    /// </summary>
    public void InitializePerceptionLink()
    {
        if (perceptionSystem != null)
        {
            // Subscribe to TEXT-BASED perception (new system)
            perceptionSystem.OnTextPerception += HandleTextPerception;
            
            Debug.Log("[WebSocketBridge] ✓ Subscribed to TEXT-BASED perception events!");
        }
        else
        {
            Debug.LogError("[WebSocketBridge] Cannot subscribe - PerceptionSystem is null!");
        }
    }
    
    /// <summary>
    /// Handle TEXT-BASED perception from perception system (NEW)
    /// </summary>
    private void HandleTextPerception(string textPerception)
    {
        if (enableTestMode)
        {
            Debug.Log($"[WebSocketBridge] TEST MODE - Ignoring text perception:\n{textPerception}");
            return;
        }
        
        // Wrap text in simple JSON envelope for backend
        string json = "{\"type\":\"text_perception\",\"perception\":\"" + 
                      textPerception.Replace("\n", "\\n").Replace("\"", "\\\"") + 
                      "\"}";
        
        SendPerceptionMessage(json);
    }
    
    /// <summary>
    /// Send message to LLM via HTTP POST
    /// </summary>
    public void SendPerceptionMessage(string jsonMessage)
    {
        messagesSent++;
        
        if (enableTestMode)
        {
            Debug.Log($"[WebSocketBridge] SEND (test): {jsonMessage}");
        }
        else if (isConnected)
        {
            Debug.Log($"[WebSocketBridge] Sending to Python server via HTTP... (msg #{messagesSent})");
            StartCoroutine(SendHttpRequest(jsonMessage));
        }
        else
        {
            Debug.LogWarning($"[WebSocketBridge] Cannot send - not connected");
        }
    }
    
    private IEnumerator SendHttpRequest(string jsonMessage)
    {
        string url = serverUrl + "/perceive";
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonMessage);
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30; // 30 second timeout for OpenAI call
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log($"[WebSocketBridge] ✓ Received response from server");
            
            if (!string.IsNullOrEmpty(response))
            {
                ReceiveMessage(response);
            }
        }
        else
        {
            Debug.LogError($"[WebSocketBridge] HTTP Error: {request.error}");
            Debug.LogError($"[WebSocketBridge] Result type: {request.result}");
            Debug.LogError($"[WebSocketBridge] Response code: {request.responseCode}");
            Debug.LogError($"[WebSocketBridge] URL was: {url}");
            
            // Retry logic
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("[WebSocketBridge] Connection error, will retry next perception update");
            }
        }
        
        request.Dispose();
    }
    
    /// <summary>
    /// Receive message from LLM
    /// </summary>
    private void ReceiveMessage(string jsonMessage)
    {
        messagesReceived++;
        
        if (enableTestMode)
        {
            Debug.Log($"[WebSocketBridge] RECV (test): {jsonMessage}");
        }
        else
        {
            Debug.Log($"[WebSocketBridge] Processing command from AI...");
        }
        
        try
        {
            // DEBUG: Show what we received
            Debug.Log($"[WebSocketBridge] Received command: {jsonMessage.Substring(0, Mathf.Min(200, jsonMessage.Length))}");
            Debug.Log($"[WebSocketBridge] BehaviorManager is null? {behaviorManager == null}");
            
            // Check if this is a behavior command (new system) - more flexible check
            bool isBehaviorCommand = (jsonMessage.Contains("\"type\":\"behavior\"") || jsonMessage.Contains("\"type\": \"behavior\""));
            Debug.Log($"[WebSocketBridge] Is behavior command? {isBehaviorCommand}");
            
            if (isBehaviorCommand && behaviorManager != null)
            {
                Debug.Log("[WebSocketBridge] Routing to BehaviorManager (new system)");
                behaviorManager.ExecuteBehaviorCommand(jsonMessage);
            }
            // Legacy command system (backward compatibility)
            else if (jsonMessage.Contains("\"type\":\"command\"") && commandExecutor != null)
            {
                Debug.Log("[WebSocketBridge] Routing to CommandExecutor (legacy system)");
                CommandPlan command = JsonUtility.FromJson<CommandPlan>(jsonMessage);
                if (command != null)
                {
                    commandExecutor.ExecuteCommand(command);
                }
            }
            else
            {
                Debug.LogWarning("[WebSocketBridge] Unknown command format or missing manager reference");
                Debug.LogWarning($"[WebSocketBridge] Debug: behaviorManager={behaviorManager}, commandExecutor={commandExecutor}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebSocketBridge] Failed to parse command: {e.Message}");
        }
    }
    
    
    // Test mode methods
    private void LoadTestCommands()
    {
        if (!string.IsNullOrEmpty(testCommandJson))
        {
            testMessageQueue.Enqueue(testCommandJson);
        }
    }
    
    [ContextMenu("Send Test Command")]
    public void SendTestCommand()
    {
        if (!string.IsNullOrEmpty(testCommandJson))
        {
            Debug.Log("[WebSocketBridge] Sending test command...");
            ReceiveMessage(testCommandJson);
        }
        else
        {
            Debug.LogWarning("[WebSocketBridge] Test command JSON is empty!");
        }
    }
    
    [ContextMenu("Test: Move Forward")]
    public void TestMoveForward()
    {
        if (commandExecutor == null)
        {
            Debug.LogError("[WebSocketBridge] CommandExecutor not assigned!");
            return;
        }
        
        Vector3 targetPos = transform.position + transform.forward * 5f;
        string json = $"{{\"type\":\"command\",\"id\":\"test_move\",\"plan\":{{\"sequence\":[{{\"action\":\"move_to\",\"params\":{{\"pos\":[{targetPos.x},{targetPos.y},{targetPos.z}]}}}}]}}}}";
        ReceiveMessage(json);
    }
    
    [ContextMenu("Test: Get Perception")]
    public void TestGetPerception()
    {
        if (perceptionSystem != null)
        {
            Debug.Log("[WebSocketBridge] Manually triggering perception snapshot...");
        }
        else
        {
            Debug.LogError("[WebSocketBridge] PerceptionSystem not assigned!");
        }
    }
}

/// <summary>
/// Simple JSON serializer for Dictionary<string, object>
/// </summary>
public static class SimpleJson
{
    public static string SerializeObject(Dictionary<string, object> dict)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        bool first = true;
        
        foreach (var kvp in dict)
        {
            if (!first) sb.Append(",");
            first = false;
            
            sb.Append("\"").Append(kvp.Key).Append("\":");
            SerializeValue(sb, kvp.Value);
        }
        
        sb.Append("}");
        return sb.ToString();
    }
    
    private static void SerializeValue(StringBuilder sb, object value)
    {
        if (value == null)
        {
            sb.Append("null");
        }
        else if (value is string)
        {
            sb.Append("\"").Append(((string)value).Replace("\"", "\\\"")).Append("\"");
        }
        else if (value is bool)
        {
            sb.Append(((bool)value) ? "true" : "false");
        }
        else if (value is int || value is long || value is float || value is double)
        {
            sb.Append(value.ToString());
        }
        else if (value is Dictionary<string, object>)
        {
            sb.Append(SerializeObject((Dictionary<string, object>)value));
        }
        else if (value is Dictionary<string, float>)
        {
            sb.Append("{");
            bool first = true;
            foreach (var kvp in (Dictionary<string, float>)value)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append("\"").Append(kvp.Key).Append("\":").Append(kvp.Value);
            }
            sb.Append("}");
        }
        else if (value is List<Dictionary<string, object>>)
        {
            sb.Append("[");
            bool first = true;
            foreach (var item in (List<Dictionary<string, object>>)value)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append(SerializeObject(item));
            }
            sb.Append("]");
        }
        else if (value is float[])
        {
            sb.Append("[");
            float[] arr = (float[])value;
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(arr[i]);
            }
            sb.Append("]");
        }
        else
        {
            sb.Append("\"").Append(value.ToString()).Append("\"");
        }
    }
}
