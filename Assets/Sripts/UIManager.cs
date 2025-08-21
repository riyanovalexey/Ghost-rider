using UnityEngine;

public class UIManager : MonoBehaviour
{
    private Manager gameManager;
    
    void Start()
    {
        gameManager = FindFirstObjectByType<Manager>();
        if (gameManager == null)
        {
            Debug.LogError("UIManager: Manager not found!");
        }
    }
    
    // Methods for UI buttons
    public void OnPlayButtonPressed()
    {
        if (gameManager != null)
            gameManager.StartRace();
    }
    
    public void OnSaveGhostPressed()
    {
        if (gameManager != null)
            gameManager.SaveCurrentGhost();
    }
    
    public void OnBackToMenuPressed()
    {
        if (gameManager != null)
            gameManager.BackToMenu();
    }
    
    void OnGUI()
    {
        if (gameManager == null) return;
        
        switch (gameManager.currentState)
        {
            case GameState.MainMenu:
                DrawMainMenuGUI();
                break;
            case GameState.RaceStart:
                DrawRaceGUI();
                break;
            case GameState.Racing:
                DrawRaceGUI();
                break;
            case GameState.Paused:
                DrawPauseGUI();
                break;
            case GameState.RaceFinished:
                DrawFinishMenuGUI();
                break;
        }
    }
    
    void DrawMainMenuGUI()
    {
        // Title
        GUI.Box(new Rect(Screen.width/2 - 200, Screen.height/2 - 150, 400, 300), "");
        
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 - 130, 400, 40), "Ghost Racing", titleStyle);
        
        // Play button
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        if (GUI.Button(new Rect(Screen.width/2 - 100, Screen.height/2 - 50, 200, 50), "Play", buttonStyle))
        {
            OnPlayButtonPressed();
        }
        
        // Ghost status
        GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.fontSize = 20;
        
        if (gameManager.HasGhostData())
        {
            GUI.contentColor = Color.green;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 + 20, 300, 25), "Ghost Available!", centeredStyle);
        }
        else
        {
            GUI.contentColor = Color.gray;
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 + 20, 300, 25), "No saved ghost", centeredStyle);
        }
        GUI.contentColor = Color.white; // Reset color
        
        // Instructions
        GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        instructionStyle.fontSize = 20;
        
        GUI.contentColor = Color.gray;
        GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 + 60, 400, 60), 
            "First run records your route\nSecond run - race against ghost", instructionStyle);
        GUI.contentColor = Color.white;
    }
    
    void DrawRaceGUI()
    {
        // Information panel
        GUI.Box(new Rect(10, 10, 300, 130), "");
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(20, 25, 200, 25), "Race", headerStyle);
        
        GUI.Label(new Rect(20, 50, 250, 20), "Reach the finish line!");
        GUI.Label(new Rect(20, 70, 200, 20), "ESC - pause");
        GUI.Label(new Rect(20, 90, 200, 20), "R - flip");
        GUI.Label(new Rect(20, 110, 200, 20), "T - return to track");
        
    }
    
    void DrawPauseGUI()
    {
        // Screen dimming
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        
        // Main pause window
        GUI.Box(new Rect(Screen.width/2 - 150, Screen.height/2 - 100, 300, 200), "");
        
        // Заголовок
        GUI.contentColor = Color.yellow;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 - 80, 300, 40), "PAUSED", titleStyle);
        GUI.contentColor = Color.white;
        
        // Instructions
        GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        instructionStyle.fontSize = 16;
        
        GUI.contentColor = Color.gray;
        GUI.Label(new Rect(Screen.width/2 - 150, Screen.height/2 - 30, 300, 60), 
            "ESC - continue game\n\nTime stopped", instructionStyle);
        GUI.contentColor = Color.white;
        
        // Menu exit button
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        if (GUI.Button(new Rect(Screen.width/2 - 100, Screen.height/2 + 40, 200, 35), "Main Menu", buttonStyle))
        {
            OnBackToMenuPressed();
        }
    }
    
    void DrawFinishMenuGUI()
    {
        // Main window
        GUI.Box(new Rect(Screen.width/2 - 200, Screen.height/2 - 120, 400, 240), "");
        
        // Заголовок
        GUI.contentColor = Color.green;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 - 100, 400, 35), "Finish!", titleStyle);
        GUI.contentColor = Color.white;
        
        // Buttons
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
        
        if (GUI.Button(new Rect(Screen.width/2 - 120, Screen.height/2 - 25, 240, 35), "Play Again", buttonStyle))
        {
            OnPlayButtonPressed();
        }
        
        if (GUI.Button(new Rect(Screen.width/2 - 120, Screen.height/2 + 20, 240, 35), "Save Ghost", buttonStyle))
        {
            OnSaveGhostPressed();
        }
        
        if (GUI.Button(new Rect(Screen.width/2 - 120, Screen.height/2 + 65, 240, 35), "Main Menu", buttonStyle))
        {
            OnBackToMenuPressed();
        }
    }
}