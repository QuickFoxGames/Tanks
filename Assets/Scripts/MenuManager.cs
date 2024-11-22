using UnityEngine;
using UnityEngine.SceneManagement;
using MGUtilities;
using System.Collections;
public class MenuManager : MonoBehaviour
{
    [SerializeField] private float m_rotationSpeed;
    [SerializeField] private Transform m_loadingRing;
    private bool m_ran = false;
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
        SceneManager.LoadScene(2);
    }
    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex != 2) return;
        if (m_loadingRing && !m_ran)
        {
            m_ran = true;
            StartCoroutine(LoadGameWithLoadingScreen());
        }
    }
    private IEnumerator LoadGameWithLoadingScreen()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        while (!asyncLoad.isDone)
        {
            m_loadingRing.Rotate(0f, 0f, m_rotationSpeed * Time.deltaTime);
            yield return null;
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(1));
    }
}