using UnityEngine;
using Ashsvp;

public class FinishLineTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public bool triggerOnlyOnce = true;
    public string playerTag = "Player";
    
    private Manager gameManager;
    private bool hasTriggered = false;
    
    void Start()
    {
        // Find Manager
        gameManager = FindFirstObjectByType<Manager>();
        if (gameManager == null)
        {
            Debug.LogError("FinishLineTrigger: Manager not found!");
        }
        
        // Ensure this is a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;
    }
    
    private GameState lastGameState = GameState.MainMenu;
    
    void Update()
    {
        // Monitor game state changes to reset trigger when a new race starts
        if (gameManager != null)
        {
            // If transitioning from non-racing to racing state, reset the trigger
            if (lastGameState != GameState.Racing && gameManager.currentState == GameState.Racing)
            {
                ResetTrigger(); // Allow finish line to trigger again
            }
            lastGameState = gameManager.currentState;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Only trigger if game is in racing state
        if (gameManager == null || gameManager.currentState != GameState.Racing)
            return;
            
        // Don't trigger if already triggered and triggerOnlyOnce is enabled
        if (triggerOnlyOnce && hasTriggered)
            return;
        
        // Check if the colliding object is the player vehicle
        bool isPlayer = false;
        
        // Check by tag first
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            isPlayer = true;
        }
        // Check if the object itself has the vehicle controller
        else if (other.GetComponent<SimcadeVehicleController>() != null)
        {
            isPlayer = true;
        }
        // Check if the object's parent has the vehicle controller (for child colliders)
        else if (other.transform.parent != null && other.transform.parent.GetComponent<SimcadeVehicleController>() != null)
        {
            isPlayer = true;
        }
        
        // If it's the player, trigger the finish
        if (isPlayer)
        {
            TriggerFinish();
        }
    }
    
    void TriggerFinish()
    {
        hasTriggered = true;
        
        Debug.Log("Finish line crossed!");
        
        // Notify Manager about race completion
        if (gameManager != null)
        {
            gameManager.FinishRace();
        }
    }
    
    // Reset trigger for new race
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
    
    void OnDrawGizmos()
    {
        
        Gizmos.color = new Color(1, 1, 0, 0.3f); 
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
        
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, "FINISH LINE");
        #endif
    }
}