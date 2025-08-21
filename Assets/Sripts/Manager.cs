using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ashsvp;

[System.Serializable]
public class GhostData
{
    public Vector3 position;
    public Quaternion rotation;
    public float timeStamp;
    
    public GhostData(Vector3 pos, Quaternion rot, float time)
    {
        position = pos;
        rotation = rot;
        timeStamp = time;
    }
}

public enum GameState
{
    MainMenu,
    RaceStart,
    Racing,
    Paused,
    RaceFinished
}

public class Manager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameState currentState = GameState.MainMenu;
    
    [Header("Race Components")]
    public SimcadeVehicleController playerVehicle;
    public Transform startPosition;
    public GameObject ghostVehiclePrefab;
    public Transform playerVehicleBody; // Reference to player vehicle body for offset calculation
    
    [Header("Ghost Visuals")]
    public Material ghostMaterial; // Материал для призрака
    
    [Header("Recording Settings")]
    public float recordingInterval = 0.05f;
    public string saveFileName = "ghost_data";
    
    // Приватные переменные
    private List<GhostData> recordedPath = new List<GhostData>(); // Текущая запись
    private List<GhostData> savedGhostData = new List<GhostData>(); // Сохраненные данные призрака
    private GameObject currentGhostInstance;
    private GhostPlayer ghostPlayer;
    private float recordingTimer = 0f;
    private float raceStartTime = 0f;

    private bool hasGhostData = false;
    
    // References to other managers
    private FinishLineTrigger finishTrigger;
    
    // For return to track feature
    private Vector3 lastTrackPosition;
    private bool isOnTrack = true;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (playerVehicle == null)
            playerVehicle = FindFirstObjectByType<SimcadeVehicleController>();
            
        finishTrigger = FindFirstObjectByType<FinishLineTrigger>();
        
        // Load saved ghost data from file
        LoadGhostData();
        
        // Set initial game state
        SetGameState(GameState.MainMenu);
    }
    
    void Update()
    {
        switch (currentState)
        {
            case GameState.RaceStart:
                HandleRaceStart();
                break;
            case GameState.Racing:
                HandleRaceUpdate();
                HandlePauseInput();
                break;
            case GameState.Paused:
                HandlePauseInput();
                break;
        }
    }
    
    void HandleRaceUpdate()
    {
        // Record player movement at specified intervals
        recordingTimer += Time.deltaTime;
        if (recordingTimer >= recordingInterval)
        {
            RecordPlayerMovement();
            recordingTimer = 0f;
        }
        
        // Continuously check if player is on track for return-to-track feature
        CheckTrackPosition();
        
        // Return to track when T is pressed and player is off track
        if (Input.GetKeyDown(KeyCode.T) && currentState == GameState.Racing && !isOnTrack)
        {
            ReturnToTrack();
        }
    }
    

    
    public void SetGameState(GameState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.MainMenu:
                HandleMainMenu();
                break;
            case GameState.RaceStart:
                HandleRaceStart();
                break;
            case GameState.Racing:
                HandleRacing();
                break;
            case GameState.Paused:
                HandlePause();
                break;
            case GameState.RaceFinished:
                HandleRaceFinished();
                break;
        }
    }
    
    void HandleMainMenu()
    {
        // Restore normal time scale
        Time.timeScale = 1f;
        
        // Disable vehicle control
        if (playerVehicle != null)
        {
            playerVehicle.CanDrive = false;
            playerVehicle.CanAccelerate = false;
        }
        
        // Clean up ghost instance
        DestroyGhost();
    }
    
    void HandleRaceStart()
    {
        // Restore normal time scale (in case returning from pause)
        Time.timeScale = 1f;
        
        // Reset finish trigger for new race (allows multiple races)
        if (finishTrigger != null)
        {
            finishTrigger.ResetTrigger();
        }
        
        // Initialize track position tracking for return-to-track feature
        if (startPosition != null)
        {
            lastTrackPosition = startPosition.position;
            isOnTrack = true;
        }
        
        // Teleport player to start position with proper physics handling
        TeleportPlayerToStart();
        
        // Start recording new ghost data
        StartNewRecording();
        
        // Clean up old ghost before creating new one
        DestroyGhost();
        
        // Spawn and start ghost playback if we have saved data
        if (hasGhostData)
        {
            SpawnAndStartGhost();
        }
        
        // Transition to racing state
        SetGameState(GameState.Racing);
    }
    
    void HandleRacing()
    {
        // Restore normal time scale (in case returning from pause)
        Time.timeScale = 1f;
        
        // Enable vehicle control
        if (playerVehicle != null)
        {
            playerVehicle.CanDrive = true;
            playerVehicle.CanAccelerate = true;
        }
    }
    
    void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Racing)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Racing);
            }
        }
    }
    
    void HandlePause()
    {
        // Stop time (pause the game)
        Time.timeScale = 0f;
        
        // Disable vehicle control
        if (playerVehicle != null)
        {
            playerVehicle.CanDrive = false;
            playerVehicle.CanAccelerate = false;
        }
    }
    
    void HandleRaceFinished()
    {
        // Disable vehicle control
        if (playerVehicle != null)
        {
            playerVehicle.CanDrive = false;
            playerVehicle.CanAccelerate = false;
        }

        // Auto-save ghost if this is the first run (no existing ghost data)
        if (!hasGhostData) {
            SaveCurrentGhost();
        }
        
        // Stop ghost playback
        if (ghostPlayer != null)
        {
            ghostPlayer.StopPlayback();
        }
    }
    
    void CheckTrackPosition()
    {
        if (playerVehicle == null) return;
        
        // Check if player is currently on track using raycast
        isOnTrack = IsOnTrack(playerVehicle.transform.position);
        
        // If on track, update the last known track position for return-to-track feature
        if (isOnTrack)
        {
            lastTrackPosition = playerVehicle.transform.position;
        }
    }
    
    bool IsOnTrack(Vector3 position)
    {
        // Cast a ray downward from the position to detect road surface
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 1f, Vector3.down, out hit, 3f, LayerMask.GetMask("Road")))
        {
            return true; // Hit road surface
        }
        return false; // No road surface detected
    }
    
    void ReturnToTrack()
    {
        if (playerVehicle == null) return;
        TeleportPlayerToPosition(lastTrackPosition, playerVehicle.transform.rotation);
    }
    
    void TeleportPlayerToStart()
    {
        if (playerVehicle == null || startPosition == null) {
            Debug.LogError("Player or start position is missing!");
            return;
        }
        TeleportPlayerToPosition(startPosition.position, startPosition.rotation);
    }
    
    void TeleportPlayerToPosition(Vector3 position, Quaternion rotation)
    {
        if (playerVehicle == null) return;
        
        // Отключаем управление автомобилем
        playerVehicle.enabled = false;
        
        // Запускаем телепортацию в следующем кадре
        StartCoroutine(TeleportAndEnableNextFrame(position, rotation));
    }
    
    System.Collections.IEnumerator TeleportAndEnableNextFrame(Vector3 position, Quaternion rotation)
    {
        // Wait for next physics frame to ensure vehicle control is fully disabled
        yield return new WaitForFixedUpdate();
        
        if (playerVehicle != null)
        {
            // Reset physics to prevent conflicts with teleportation
            Rigidbody rb = playerVehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Stop all movement
                rb.angularVelocity = Vector3.zero; // Stop all rotation
            }
            
            // Perform the actual teleportation
            playerVehicle.transform.position = position;
            playerVehicle.transform.rotation = rotation;
            
            // Re-enable vehicle control after teleportation is complete
            playerVehicle.enabled = true;
            playerVehicle.CanDrive = true;
            playerVehicle.CanAccelerate = true;
        }
    }
    
    void StartNewRecording()
    {
        recordedPath.Clear();
        raceStartTime = Time.time;
        recordingTimer = 0f;
    }
    
    void RecordPlayerMovement()
    {
        if (playerVehicle != null)
        {
            // Calculate time since race start for timestamp
            float currentTime = Time.time - raceStartTime;
            
            // Create ghost data point with current position, rotation, and timestamp
            GhostData dataPoint = new GhostData(
                playerVehicle.transform.position,
                playerVehicle.transform.rotation,
                currentTime
            );
            
            // Add to recording path
            recordedPath.Add(dataPoint);
        }
    }
    
    void SpawnAndStartGhost()
    {
        // Validate ghost prefab exists
        if (ghostVehiclePrefab == null)
        {
            Debug.LogError("Ghost prefab is missing!");
            return;
        }
        
        // Get saved ghost data for playback
        List<GhostData> ghostData = GetGhostDataForPlayback();
        if (ghostData.Count == 0)
        {
            Debug.LogWarning("No ghost data available for playback!");
            return;
        }
        
        // Create ghost instance from prefab
        currentGhostInstance = Instantiate(ghostVehiclePrefab);
        
        // Get or add GhostPlayer component
        ghostPlayer = currentGhostInstance.GetComponent<GhostPlayer>();
        if (ghostPlayer == null)
        {
            ghostPlayer = currentGhostInstance.AddComponent<GhostPlayer>();
        }
        
        // Calculate and apply position offset to match player vehicle's visual position
        SetupGhostOffset();
        
        // Apply ghost material (transparent/ghostly appearance)
        ghostPlayer.ApplyMaterial(ghostMaterial);
        
        // Start ghost playback with saved data
        ghostPlayer.StartPlayback(ghostData);
    }
    
    // Calculate ghost offset based on player vehicle body position
    void SetupGhostOffset()
    {
        if (ghostPlayer == null || playerVehicle == null || playerVehicleBody == null) return;
        
        // Use the Y coordinate of the player vehicle body as the offset
        // This compensates for any vertical adjustments made to the vehicle model
        Vector3 offset = new Vector3(0, playerVehicleBody.localPosition.y, 0);
        ghostPlayer.positionOffset = offset;
    }
    
    List<GhostData> GetGhostDataForPlayback()
    {
        // Return saved ghost data for playback
        if (savedGhostData.Count > 0)
        {
            return savedGhostData;
        }
        
        // Return empty list if no saved data available
        return new List<GhostData>();
    }
    
    void DestroyGhost()
    {
        if (currentGhostInstance != null)
        {
            // Clean up ghost instance and references
            Destroy(currentGhostInstance);
            currentGhostInstance = null;
            ghostPlayer = null;
        }
    }
    
    // Public methods for UI
    public void StartRace()
    {
        SetGameState(GameState.RaceStart);
    }
    
    public void FinishRace()
    {
        SetGameState(GameState.RaceFinished);
    }
    
    public void BackToMenu()
    {
        SetGameState(GameState.MainMenu);
    }
    
    public void SaveCurrentGhost()
    {
        if (recordedPath.Count > 0)
        {
            // Copy current recording to saved data
            savedGhostData = new List<GhostData>(recordedPath);
            SaveGhostToFile();
            hasGhostData = true;
        }
        else
        {
            // No recorded path to save
        }
    }
    
    public bool HasGhostData()
    {
        return hasGhostData;
    }
    
    public int GetRecordedPointsCount()
    {
        return recordedPath.Count;
    }
    
    // Save and load methods
    void SaveGhostToFile()
    {
        try
        {
            SerializableGhostDataList dataList = new SerializableGhostDataList(savedGhostData);
            string json = JsonUtility.ToJson(dataList);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/" + saveFileName + ".json", json);

        }
        catch (System.Exception e)
        {

        }
    }
    
    void LoadGhostData()
    {
        // Load ghost data from file and update availability flag
        savedGhostData = LoadGhostDataFromFile();
        hasGhostData = savedGhostData.Count > 0;
        if (hasGhostData)
        {
            // Ghost data successfully loaded
        }
    }
    
    List<GhostData> LoadGhostDataFromFile()
    {
        try
        {
            string filePath = Application.persistentDataPath + "/" + saveFileName + ".json";
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                SerializableGhostDataList dataList = JsonUtility.FromJson<SerializableGhostDataList>(json);
                return dataList.data ?? new List<GhostData>();
            }
        }
        catch (System.Exception e)
        {

        }
        
        return new List<GhostData>();
    }
}

[System.Serializable]
public class SerializableGhostDataList
{
    public List<GhostData> data;
    
    public SerializableGhostDataList(List<GhostData> ghostData)
    {
        data = ghostData;
    }
}