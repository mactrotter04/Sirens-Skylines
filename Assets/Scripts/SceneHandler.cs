using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void ReloadLevel()
    {

        Time.timeScale = 1f; //starts time 
        Cursor.lockState = CursorLockMode.Locked; //the cursor is invisible in the lock state 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR //exists out of the unity game window
        UnityEditor.EditorApplication.isPlaying = false;
#endif // exits game

        Application.Quit();
    }

    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex == SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("no more Scenes to load");
            SceneManager.LoadScene("MainMenu");
        }

        SceneManager.LoadScene(nextSceneIndex);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }


}

