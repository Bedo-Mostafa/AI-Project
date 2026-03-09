using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EasyTransition;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The pause menu panel to show/hide")]
    public GameObject pauseMenuPanel;

    [Header("Buttons")]
    public Button mainMenuButton;
    public Button retryButton;
    public Button resumeButton;

    [Header("Input")]
    public InputActionReference pauseAction;

    [Header("Settings")]
    [Tooltip("Cursor will be shown and unlocked when paused")]
    public bool manageCursor = true;

    [Header("Transition")]
    public TransitionSettings transitionSettings;
    public float transitionDelay = 0f;

    private bool isPaused = false;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    public static PauseManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Setup button listeners
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryScene);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        // Make sure pause menu starts hidden
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        isPaused = false;

        // Subscribe to pause action
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPauseAction;
        }
    }

    private void OnPauseAction(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPauseAction;
        }

        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Store and change cursor state
        if (manageCursor)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Force UI refresh in case resolution was changed
        Canvas.ForceUpdateCanvases();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Restore cursor state
        if (manageCursor)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    public void GoToMainMenu()
    {
        // Unpause before loading to reset timeScale
        Time.timeScale = 1f;
        isPaused = false;

        TransitionManager.Instance().Transition(0, transitionSettings, transitionDelay);
    }

    public void RetryScene()
    {
        // Unpause before loading to reset timeScale
        Time.timeScale = 1f;
        isPaused = false;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        TransitionManager.Instance().Transition(currentSceneIndex, transitionSettings, transitionDelay);
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    private void OnDisable()
    {
        // Safety: reset timeScale if disabled while paused
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
    }
}
