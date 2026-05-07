using UnityEngine;

public class DeathHandler : MonoBehaviour
{
    [SerializeField] Canvas gameOverCanvas;
    [SerializeField] Canvas mainCanvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverCanvas.enabled = false;
        mainCanvas.enabled = true;
    }

    public void HandleDeath()
    {
        gameOverCanvas.enabled = true;
        mainCanvas.enabled = false;
        Time.timeScale = 0; //freezes time in the game exept from canvas
        Cursor.lockState = CursorLockMode.None; //this allows the player to move the cursor 
        Cursor.visible = true; //allows the player to see the cursor need to set it visible to be able to see
    }
}
