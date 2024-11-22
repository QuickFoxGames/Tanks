using UnityEngine;
using TMPro;
using System.Collections;
using MGUtilities;
public class Tank : MonoBehaviour
{
    [SerializeField] private float m_moveSpeed;
    [SerializeField] private float m_moveAcceleration;
    [SerializeField] private float m_bodyRotateSpeed;
    [SerializeField] private float m_turretRotateSpeed;
    [SerializeField] private float m_fireRate;
    [SerializeField] private float m_bulletSpeed;
    [SerializeField] private float m_bulletDamage;
    [SerializeField] private float m_shootAngle;
    [SerializeField] private float m_avoidMulti = 5f;
    [SerializeField] private Transform m_body;
    [SerializeField] private Transform m_turret;
    [SerializeField] private Transform m_barrel;
    [SerializeField] private Rigidbody2D m_bullet;
    [SerializeField] private ParticleSystem m_shootParticleSystem;
    [SerializeField] private ParticleSystem m_deathParticleSystem;
    [SerializeField] private LayerMask m_bulletMask;
    [SerializeField] private TextMeshProUGUI m_levelText;

    private bool m_setNewMoveDirection = true;
    private bool m_canShoot = true;
    private bool m_seekPlayer = false;
    private int m_level = 1;
    private float m_detectDistance = 8f;
    private Vector2 m_moveDirection;
    private Transform m_transform;
    private Transform m_managerTransform;
    private Transform m_parentTower;
    private Rigidbody2D m_rb;
    private GameManager m_manager;
    private HealthSystem m_healthSystem;
    private BulletPool m_bulletPool;
    public void InitTank(int newLevel, Transform parent)
    {
        m_manager = GameManager.Instance();
        m_rb = GetComponent<Rigidbody2D>();
        m_healthSystem = GetComponent<HealthSystem>();
        m_bulletPool = BulletPool.Instance();
        m_transform = transform;
        m_managerTransform = m_manager.transform;
        m_parentTower = parent;

        m_level = newLevel;
        m_healthSystem.Health += (m_level * 0.35f * m_healthSystem.Health) - (0.35f * m_healthSystem.Health);
        m_moveSpeed += (m_level * 0.15f * m_moveSpeed) - (0.15f * m_moveSpeed);
        m_turretRotateSpeed += (m_level * 0.25f * m_turretRotateSpeed) - (0.25f * m_turretRotateSpeed);
        m_fireRate += (m_level * 0.25f * m_fireRate) - (0.25f * m_fireRate);
        m_bulletSpeed += (m_level * 0.25f * m_bulletSpeed) - (0.25f * m_bulletSpeed);
        m_bulletDamage += (m_level * 0.25f * m_bulletDamage) - (0.25f * m_bulletDamage);
        m_detectDistance += (m_level * 0.08f * m_detectDistance) - (0.08f * m_detectDistance);
        if (m_levelText) m_levelText.text = "LV: " + m_level;
        m_fireRate = 1f / (m_fireRate / 60f);
    }
    void Update()
    {
        if (m_healthSystem.Health <= 0)
        {
            if (m_transform.GetChild(0).gameObject.activeInHierarchy)
                Die();
            return;
        }
        if (m_healthSystem.Hits > 0 && !m_seekPlayer) StartCoroutine(RunSeekPlayer());
        RotateTank();
        if (Vector2.Distance(m_transform.position, m_managerTransform.position) < m_detectDistance || m_seekPlayer)
        {
            PointAtPlayer();
            m_moveDirection = (m_managerTransform.position - m_transform.position).normalized;
        }
        else if (m_setNewMoveDirection) StartCoroutine(DelaySetNewMoveDirection());
        if (Vector2.Distance(m_transform.position, m_parentTower.position) > m_detectDistance)
            m_moveDirection = (m_parentTower.position - m_transform.position).normalized;
    }
    private void Shoot()
    {
        var b = m_bulletPool.GetBullet(m_turret.position, m_turret.rotation);
        b.m_targetLayer = m_bulletMask.value;
        b.m_penetration = 0;
        b.m_damage = m_bulletDamage;
        b.m_rb.linearVelocity = m_bulletSpeed * m_turret.up;
        m_shootParticleSystem.Play();
        m_rb.AddForce(m_turret.up * -10f, ForceMode2D.Impulse);
        StartCoroutine(Coroutines.PingPongVector2OverTime(new(0f, 0.5f), new(0f, 0.1f), 0.05f, 0.2f, value => m_barrel.localPosition = value));
        StartCoroutine(DelayShot());
    }
    private IEnumerator RunSeekPlayer()
    {
        m_seekPlayer = true;
        yield return new WaitForSeconds(1f);
        m_seekPlayer = false;
    }
    private IEnumerator DelayShot()
    {
        m_canShoot = false;
        yield return new WaitForSeconds(m_fireRate);
        m_canShoot = true;
    }
    private IEnumerator DelaySetNewMoveDirection()
    {
        m_setNewMoveDirection = false;
        m_moveDirection = (Random.Range(-6f, 6f) * m_transform.right + Random.Range(-6f, 6f) * m_transform.up).normalized;
        yield return new WaitForSeconds(Random.Range(1f, 4f));
        m_setNewMoveDirection = true;
    }
    private void RotateTank()
    {
        if (m_rb.linearVelocity.normalized == Vector2.zero) return;
        Quaternion targetBodyRotation = Quaternion.LookRotation(Vector3.forward, m_rb.linearVelocity.normalized);
        m_body.rotation = Quaternion.Slerp(m_body.rotation, targetBodyRotation, m_bodyRotateSpeed * m_manager.DeltaTime);
    }
    private void PointAtPlayer()
    {
        Vector3 directionToPlayer = ((m_managerTransform.position + m_manager.DeltaTime * m_level * m_manager.Velocity) - m_transform.position).normalized;
        Quaternion targetTurretRotation = Quaternion.LookRotation(Vector3.forward, directionToPlayer);
        m_turret.rotation = Quaternion.Slerp(m_turret.rotation, targetTurretRotation, m_turretRotateSpeed * m_manager.DeltaTime);
        if (Vector2.Angle(m_turret.up, directionToPlayer) < m_shootAngle && m_canShoot) Shoot();
    }
    private void FixedUpdate()
    {
        if (m_healthSystem.Health <= 0) return;
        Move();
    }
    private void Move()
    {
        Vector2 moveVelocity = m_moveSpeed * m_moveDirection;
        Vector3 moveForce = m_rb.mass * m_moveAcceleration * (moveVelocity - m_rb.linearVelocity);
        moveForce = AvoidObstacles(moveForce);
        m_rb.AddForce(moveForce);
    }
    private Vector2 AvoidObstacles(Vector2 moveForce)
    {
        Vector2 castStart = (Vector2)m_transform.position + m_manager.DeltaTime * m_rb.linearVelocity.magnitude * m_moveDirection;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(castStart, 0.45f, m_moveDirection, m_rb.linearVelocity.magnitude);
        if (hits.Length > 0)
        {
            Vector2 avoidForce = Vector2.zero;
            foreach (RaycastHit2D hit in hits)
            {
                float distToObj = Vector2.Distance(m_transform.position, hit.transform.position);
                float distToRay = Vector2.Distance(castStart, hit.point);
                avoidForce += Vector2.Lerp(m_moveDirection, hit.normal, distToRay / distToObj) * m_avoidMulti;
            }
            return 0.5f * moveForce + avoidForce;
        }
        return moveForce;
    }
    private void Die()
    {
        m_transform.GetChild(0).gameObject.SetActive(false);
        gameObject.GetComponent<TrailRenderer>().enabled = false;
        StartCoroutine(DelaySetActive(false, 0.5f));
        m_deathParticleSystem.Play();
        m_manager.NumDeadTanks++;
        m_manager.Score += m_level * 10;
    }
    private IEnumerator DelaySetActive(bool state, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        gameObject.SetActive(state);
    }
}