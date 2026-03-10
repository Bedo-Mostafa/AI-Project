// using UnityEngine;
// using TMPro; // Required for the timer text

// public class GameManager : MonoBehaviour
// {
//     public static GameManager Instance { get; private set; }

//     [Header("Game Settings")]
//     [Tooltip("Total time in seconds to kill all zombies")]
//     public float timeLimit = 120f;

//     [Header("UI References")]
//     public TextMeshProUGUI timerText;
//     public GameObject winPanel;
//     public GameObject losePanel;

//     private float currentTime;
//     private int totalZombies;
//     private bool gameEnded = false;

//     private void Awake()
//     {
//         // Set up the Singleton so other scripts can easily access this
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     private void Start()
//     {
//         currentTime = timeLimit;

//         // Count all zombies in the scene at the start
//         // IMPORTANT: Make sure all your zombie prefabs have the tag "Zombie"
//         totalZombies = 15; //GameObject.FindGameObjectsWithTag("Zombie").Length;

//         // Ensure win/lose panels are hidden at the start
//         if (winPanel != null) winPanel.SetActive(false);
//         if (losePanel != null) losePanel.SetActive(false);
//     }

//     private void Update()
//     {
//         if (gameEnded) return;

//         // Timer countdown
//         currentTime -= Time.deltaTime;
//         UpdateTimerUI();

//         // Check for Lose Condition (Time runs out)
//         if (currentTime <= 0)
//         {
//             LoseGame();
//         }
//     }

//     private void UpdateTimerUI()
//     {
//         if (timerText != null)
//         {
//             // Format time as Minutes:Seconds (e.g., 01:30)
//             int minutes = Mathf.FloorToInt(Mathf.Max(currentTime, 0) / 60);
//             int seconds = Mathf.FloorToInt(Mathf.Max(currentTime, 0) % 60);
//             timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
//         }
//     }

//     // This will be called by the ZombieController when a zombie dies
//     public void ZombieDied()
//     {
//         if (gameEnded) return;

//         totalZombies--;

//         // Check for Win Condition (All zombies dead)
//         if (totalZombies <= 0)
//         {
//             WinGame();
//         }
//     }

//     private void WinGame()
//     {
//         gameEnded = true;
//         Time.timeScale = 0f; // Stop the game time
//         if (winPanel != null) winPanel.SetActive(true);
//         UnlockCursor();
//     }

//     private void LoseGame()
//     {
//         gameEnded = true;
//         Time.timeScale = 0f; // Stop the game time
//         if (losePanel != null) losePanel.SetActive(true);
//         UnlockCursor();
//     }

//     private void UnlockCursor()
//     {
//         Cursor.lockState = CursorLockMode.None;
//         Cursor.visible = true;
//     }
// }


using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float timeLimit = 120f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI zombiesRemainingText; // NEW: Reference for the counter
    public GameObject winPanel;
    public GameObject losePanel;

    private float currentTime;
    private int totalZombies;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentTime = timeLimit;

        // Count all zombies in the scene at the start
        totalZombies = 5;//GameObject.FindGameObjectsWithTag("Zombie").Length;

        // NEW: Update the UI text right when the game starts
        UpdateZombieCounterUI();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void Update()
    {
        if (gameEnded) return;

        currentTime -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTime <= 0)
        {
            LoseGame();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(Mathf.Max(currentTime, 0) / 60);
            int seconds = Mathf.FloorToInt(Mathf.Max(currentTime, 0) % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void ZombieDied()
    {
        if (gameEnded) return;

        totalZombies--;

        // NEW: Update the text every time a zombie dies
        UpdateZombieCounterUI();

        if (totalZombies <= 0)
        {
            WinGame();
        }
    }

    // NEW: Method to handle the text formatting
    private void UpdateZombieCounterUI()
    {
        if (zombiesRemainingText != null)
        {
            // Ensures the counter never visually drops below 0
            zombiesRemainingText.text = "Zombies Left: " + Mathf.Max(totalZombies, 0);
        }
    }

    private void WinGame()
    {
        gameEnded = true;
        Time.timeScale = 0f;
        if (winPanel != null) winPanel.SetActive(true);
        UnlockCursor();
    }

    private void LoseGame()
    {
        gameEnded = true;
        Time.timeScale = 0f;
        if (losePanel != null) losePanel.SetActive(true);
        UnlockCursor();
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}