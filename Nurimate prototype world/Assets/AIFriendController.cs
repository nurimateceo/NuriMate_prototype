using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Main AI Friend Controller - integrates all AI body systems
/// This is the central hub that connects Command Executor, Perception, and Communication
/// </summary>
public class AIFriendController : MonoBehaviour 
{
    [Header("References")]
    public Transform player;
    public Camera mainCamera;
    
    [Header("Components")]
    private NavMeshAgent agent;
    private CommandExecutor commandExecutor;
    private PerceptionSystem perceptionSystem;
    private WebSocketBridge webSocketBridge;
    
    [Header("Status")]
    public bool systemsInitialized = false;
    
    void Start() 
    {
        InitializeSystems();
    }
    
    /// <summary>
    /// Initialize all AI body systems
    /// </summary>
    private void InitializeSystems()
    {
        // Get or add NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Debug.LogWarning("[AIFriendController] NavMeshAgent was missing, added automatically");
        }
        
        // Get or add CommandExecutor
        commandExecutor = GetComponent<CommandExecutor>();
        if (commandExecutor == null)
        {
            commandExecutor = gameObject.AddComponent<CommandExecutor>();
        }
        
        // Configure CommandExecutor
        commandExecutor.navAgent = agent;
        commandExecutor.aiTransform = transform;
        commandExecutor.playerTransform = player;
        
        // Get or add PerceptionSystem
        perceptionSystem = GetComponent<PerceptionSystem>();
        if (perceptionSystem == null)
        {
            perceptionSystem = gameObject.AddComponent<PerceptionSystem>();
        }
        
        // Configure PerceptionSystem
        perceptionSystem.mainCamera = mainCamera != null ? mainCamera : Camera.main;
        perceptionSystem.aiTransform = transform;
        perceptionSystem.playerTransform = player;
        
        // Get or add WebSocketBridge
        webSocketBridge = GetComponent<WebSocketBridge>();
        if (webSocketBridge == null)
        {
            webSocketBridge = gameObject.AddComponent<WebSocketBridge>();
        }
        
        // Configure WebSocketBridge
        webSocketBridge.commandExecutor = commandExecutor;
        webSocketBridge.perceptionSystem = perceptionSystem;
        
        // Initialize perception link AFTER all references are set
        webSocketBridge.InitializePerceptionLink();
        
        systemsInitialized = true;
        Debug.Log("[AIFriendController] All systems initialized successfully!");
        Debug.Log("[AIFriendController] Use WebSocketBridge context menu for testing");
    }
    
    /// <summary>
    /// Manual command execution for testing
    /// </summary>
    public void ExecuteTestCommand(string action, Vector3 targetPosition)
    {
        if (!systemsInitialized || commandExecutor == null)
        {
            Debug.LogError("[AIFriendController] Systems not initialized!");
            return;
        }
        
        CommandPlan plan = new CommandPlan
        {
            id = System.Guid.NewGuid().ToString(),
            sequence = new System.Collections.Generic.List<PlanStep>
            {
                new PlanStep
                {
                    action = action,
                    parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "pos", new System.Collections.Generic.List<object> { targetPosition.x, targetPosition.y, targetPosition.z } }
                    }
                }
            }
        };
        
        commandExecutor.ExecuteCommand(plan);
    }
    
    void Update() 
    {
        // Legacy fallback behavior if no commands are executing
        // This ensures the AI does something useful even without LLM connection
        if (systemsInitialized && commandExecutor != null && player != null)
        {
            if (commandExecutor.currentState == CommandExecutor.ExecutionState.Idle)
            {
            float distance = Vector3.Distance(player.position, transform.position);
                if (distance > 6f)
                {
                    // Auto-follow if too far and idle
                agent.SetDestination(player.position);
            }
                else if (distance < 6f && agent.hasPath)
                {
                    agent.ResetPath();
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visual debugging
        if (systemsInitialized && commandExecutor != null && commandExecutor.currentState == CommandExecutor.ExecutionState.Executing)
        {
            Gizmos.color = Color.green;
            if (agent != null && agent.hasPath)
            {
                Gizmos.DrawLine(transform.position, agent.destination);
                Gizmos.DrawWireSphere(agent.destination, 0.5f);
            }
        }
    }
}
