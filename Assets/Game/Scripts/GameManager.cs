using UnityEngine;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float timeLimit = 120f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI zombiesRemainingText;
    public TextMeshProUGUI batsRemainingText;
    public GameObject winPanel;
    public GameObject losePanel;

    private float currentTime;
    private int totalZombies;
    private int totalBats;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentTime = timeLimit;

        totalZombies = 5;
        totalBats = 5;

        UpdateZombieCounterUI();
        UpdateBatsCounterUI();

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
        UpdateZombieCounterUI();
        CheckWinCondition();
    }

    public void BatDied()
    {
        if (gameEnded) return;

        totalBats--;
        UpdateBatsCounterUI();
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (totalZombies <= 0 && totalBats <= 0)
            WinGame();
    }

    private void UpdateZombieCounterUI()
    {
        if (zombiesRemainingText != null)
        {
            zombiesRemainingText.text = "Zombies Left: " + Mathf.Max(totalZombies, 0);
        }
    }

    private void UpdateBatsCounterUI()
    {
        if (batsRemainingText != null)
        {
            batsRemainingText.text = "Bats Left: " + Mathf.Max(totalBats, 0);
        }
    }

    private void WinGame()
    {
        gameEnded = true;
        Time.timeScale = 0f;
        if (winPanel != null) winPanel.SetActive(true);
        UnlockCursor();
    }

    public void LoseGame()
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

    public void ZombieSpawned()
    {
        totalZombies++;
        UpdateZombieCounterUI();
    }
}