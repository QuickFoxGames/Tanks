using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1.0f;
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void QuitToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void PlayAgain()
    {
        SceneManager.LoadScene(1);
    }
}