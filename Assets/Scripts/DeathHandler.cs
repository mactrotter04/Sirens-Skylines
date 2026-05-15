using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeathHandler : MonoBehaviour
{
    [SerializeField] Canvas gameOverCanvas;
    [SerializeField] Canvas mainCanvas;
    [SerializeField] Canvas pauseCanvas;
    [SerializeField] TextMeshProUGUI timeCheck;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverCanvas.enabled = false;
        mainCanvas.enabled = true;
        pauseCanvas.enabled = false;
    }

    void Update()
    {
        PauseMenu();
    }

    public void HandleDeath()
    {
        gameOverCanvas.enabled = true;
        mainCanvas.enabled = false;
        pauseCanvas.enabled = false;
        Time.timeScale = 0; //freezes time in the game exept from canvas
        Cursor.lockState = CursorLockMode.None; //this allows the player to move the cursor 
        Cursor.visible = true; //allows the player to see the cursor need to set it visible to be able to see
    }

    void PauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            mainCanvas.enabled = false;
            pauseCanvas.enabled = true;
            Time.timeScale = 0; //freezes time in the game exept from canvas
            Cursor.lockState = CursorLockMode.None; //this allows the player to move the cursor 
            Cursor.visible = true; //allows the player to see the cursor need to set it visible to be able to see
        }
    }
    public void ResumeGame()
    {
        Time.timeScale = 1f; //starts time 
        Cursor.lockState = CursorLockMode.Locked; //the cursor is invisible in the lock state
        Cursor.visible = false; //makes the cursor invisible
        mainCanvas.enabled = true;
        pauseCanvas.enabled = false;
    }

  

}
