using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using EasyTransition;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Canvas MainCanvas;
    [SerializeField]
    private Canvas OptionsCanvas;
    [SerializeField]
    private TransitionSettings transitionSettings;
    TransitionManager transitionManager;
    [SerializeField]
    private float delay;
    [SerializeField]
    private GameObject Display;
    [SerializeField]
    public List<GameObject> OptionElements;
    [SerializeField]
    private List<Button> buttons;
    private bool isTransitioning = false;
    private void Start()
    {
      
    }
    public void switchToOptionsCanvas()
    {
        MainCanvas.gameObject.SetActive(false);
        OptionsCanvas.gameObject.SetActive(true);
    }
    public void switchToMenuCanvas()
    {
        MainCanvas.gameObject.SetActive(true);
        OptionsCanvas.gameObject.SetActive(false);
    }
    
    public void switchToMainMenu()
    {
        TransitionManager.Instance().Transition(0, transitionSettings, delay);
    }

    public void goToGameScene()
    {
        
        TransitionManager.Instance().Transition(1, transitionSettings, delay);

    }
    public void closeGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

   
    public void ToggleButtonInteractable(int index)
    {
        foreach (Button button in buttons)
        {
            button.interactable = (button != buttons[index])? true : false;
        }
    }
    public void ShowDisplay(GameObject toShow)
    {
        foreach (GameObject element in OptionElements)
        {
            element.SetActive(false);
        }
        toShow.SetActive(true);
    }
}
