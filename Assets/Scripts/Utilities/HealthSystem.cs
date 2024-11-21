using UnityEngine;
using UnityEngine.UI;
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float m_maxHp = 100f;
    [SerializeField] private Slider m_hpSlider;
    private float m_currentHp;
    private void Awake()
    {
        m_currentHp = m_maxHp;
    }
    private void Start()
    {
        if (m_hpSlider)
        {
            m_hpSlider.maxValue = m_maxHp;
            m_hpSlider.value = m_currentHp;
        }
    }
    public void TakeDamage(float d)
    {
        m_currentHp -= d;
        if (m_hpSlider) m_hpSlider.value = m_currentHp;
    }
    public float Health { get { return m_currentHp; } set { m_maxHp = value; m_currentHp = m_maxHp; } }
}