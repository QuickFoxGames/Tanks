using UnityEngine;
using UnityEngine.SceneManagement;
using MGUtilities;
using System.Collections;
public class MenuManager : MonoBehaviour
{
    [SerializeField] private float m_rotationSpeed;
    [SerializeField] private Transform m_loadingRing;
    [SerializeField] private GameObject m_pauseMenu;
    private bool m_ran = false;
    private GameManager m_gameManager;
    private void Start()
    {
        Time.timeScale = 1.0f;
        m_gameManager = GameManager.Instance();
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
        if (m_loadingRing && !m_ran)
        {
            m_ran = true;
            StartCoroutine(LoadGameWithLoadingScreen());
        }
        if (m_pauseMenu && Input.GetKeyDown(KeyCode.Escape) && !m_gameManager.GameIsDone)
        {
            m_gameManager.ToggleMouse();
            m_pauseMenu.SetActive(!m_pauseMenu.activeInHierarchy);
            Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
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