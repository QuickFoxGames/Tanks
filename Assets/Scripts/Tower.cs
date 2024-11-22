using MGUtilities;
using System.Collections;
using TMPro;
using UnityEngine;
public class Tower : MonoBehaviour
{
    [SerializeField] private int m_level = 1;
    [SerializeField] private int m_minDefenderTanks = 1;
    [SerializeField] private int m_maxDefenderTanks = 2;
    [SerializeField] private TextMeshProUGUI m_levelText;
    [SerializeField] private ParticleSystem m_deathParticleSystem;
    private GameManager m_manager;
    private HealthSystem m_healthSystem;
    public void InitTower(int newLevel)
    {
        m_manager = GameManager.Instance();
        m_healthSystem = GetComponent<HealthSystem>();

        m_level = newLevel;
        m_healthSystem.Health += (m_level * 0.25f * m_healthSystem.Health) - (0.25f * m_healthSystem.Health);
        if (m_levelText) m_levelText.text = "LV: " + m_level;

        for (int i = 0; i < Random.Range(m_minDefenderTanks, m_maxDefenderTanks + 1); i++)
        {
            m_manager.SpawnTank((Vector2)transform.position + (Random.Range(0, 2) * 2 - 1) * Vector2.left, m_level, transform);
        }
    }
    private void Update()
    {
        if (m_healthSystem.Health <= 0 && transform.GetChild(0).gameObject.activeInHierarchy)
            Die();
    }
    private void Die()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        StartCoroutine(DelaySetActive(false, 0.5f));
        m_deathParticleSystem.Play();
        m_manager.NumDeadTowers++;
        m_manager.Score += m_level * 100;
    }
    private IEnumerator DelaySetActive(bool state, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        gameObject.SetActive(state);
    }
}