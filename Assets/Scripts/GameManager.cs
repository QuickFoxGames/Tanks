using System.Collections;
using UnityEngine;
using MGUtilities;
using TMPro;
using System.Collections.Generic;
public class GameManager : Singleton_template<GameManager>
{
    [Header("Tank")]
    [SerializeField] private float m_moveSpeed;
    [SerializeField] private float m_moveAcceleration;
    [SerializeField] private float m_bodyRotateSpeed;
    [SerializeField] private float m_turretRotateSpeed;
    [SerializeField] private float m_fireRate;
    [SerializeField] private float m_bulletSpeed;
    [SerializeField] private float m_bulletDamage;
    [SerializeField] private int m_bulletPenetration;
    [SerializeField] private Transform m_body;
    [SerializeField] private Transform m_turret;
    [SerializeField] private Transform m_barrel;
    [SerializeField] private Rigidbody2D m_bullet;
    [SerializeField] private ParticleSystem m_shootParticleSystem;
    [SerializeField] private ParticleSystem m_deathParticleSystem;
    [SerializeField] private LayerMask m_bulletMask;
    [SerializeField] private TextMeshProUGUI m_ammoText;
    [SerializeField] private TextMeshProUGUI m_killsText;
    [Header("Camera")]
    [SerializeField] private float m_followSpeed;
    [SerializeField] private Transform m_cam;
    public Vector3 Velocity { get; private set; }
    #region GameManager
    [Header("GameManager")]
    [SerializeField] private int m_mapSize;
    [SerializeField] private Tank m_tankPrefab;
    [SerializeField] private Tower m_towerPrefab;
    [SerializeField] private Transform m_towerHolder;
    [SerializeField] private int m_numTowers;
    [SerializeField] private Transform m_mouseTarget;
    [SerializeField] private Transform m_enemyIndictors;
    [SerializeField] private Transform m_indicatorPrefab;
    [SerializeField] private GameObject m_endScreen;
    [SerializeField] private TextMeshProUGUI m_endTopText;
    [SerializeField] private TextMeshProUGUI m_endBottomText;
    private int m_totalNumTowers = 0;
    private readonly List<Transform> m_towers = new();
    private readonly List<Transform> m_indicators = new();
    public float BulletDamage { get; private set; }
    public float DeltaTime { get; private set; }
    public int NumDeadTanks { get; set; }
    public int NumDeadTowers { get; set; }
    public int Score { get; set; }
    public void SpawnTank(Vector2 pos, int level, Transform parent)
    {
        Tank t = Instantiate(m_tankPrefab, pos, Quaternion.identity, m_towerHolder);
        t.InitTank(Random.Range(level > 1 ? level - 1 : 1, level + 1), parent);
    }
    private void SpawnTowers()
    {
        for (int i = 0; i < m_numTowers; ++i)
        {
            Tower t = Instantiate(m_towerPrefab, GetPos(), Quaternion.identity, m_towerHolder);
            t.InitTower(Random.Range(1, 10));
            m_towers.Add(t.transform);
            m_indicators.Add(Instantiate(m_indicatorPrefab, m_enemyIndictors));
            m_totalNumTowers++;
        }
    }
    private Vector2 GetPos()
    {
        Vector2 pos = new(Random.Range(0, m_mapSize), Random.Range(0, m_mapSize));
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 20f, m_bulletMask);
        if (hits.Length > 0) GetPos();
        return pos;
    }
    private void HandleEndScreen(string topText, string bottomText, Color color)
    {
        m_endScreen.SetActive(true);
        m_endTopText.text = topText;
        m_endTopText.color = color;
        m_endBottomText.text = bottomText;
        m_endBottomText.color = color;
        StartCoroutine(Coroutines.LerpFloatOverTime(1, 0, 0.5f, value => Time.timeScale = value));
    }
    private void UpdateIndicators()
    {
        for (int i = 0; i < m_indicators.Count; i++)
        {
            Vector2 dir = m_towers[i].position - m_transform.position;
            if (m_towers[i].gameObject.activeInHierarchy && dir.magnitude > 8f)
            {
                m_indicators[i].gameObject.SetActive(true);
                m_indicators[i].up = (dir).normalized;
            }
            else m_indicators[i].gameObject.SetActive(false);
        }
    }
    #endregion
    private bool m_shootInput, m_canShoot = true;
    private Vector3 m_inputDirection;
    private Vector3 m_mousePos;
    private Transform m_transform;
    private Rigidbody2D m_rb;
    private BulletPool m_bulletPool;
    private HealthSystem m_healthSystem;
    void Start()
    {
        m_bulletPool = BulletPool.Instance();
        m_rb = GetComponent<Rigidbody2D>();
        m_healthSystem = GetComponent<HealthSystem>();
        m_fireRate = 1f / (m_fireRate / 60f);
        m_transform = transform;
        BulletDamage = m_bulletDamage;
        #region GameManager
        SpawnTowers();
        #endregion
    }
    void Update()
    {
        m_inputDirection = Input.GetAxisRaw("Vertical") * Vector3.up + Input.GetAxisRaw("Horizontal") * Vector3.right;
        m_shootInput = Input.GetKey(KeyCode.Mouse0);
        m_mousePos = Input.mousePosition;
        m_mousePos.z = Camera.main.nearClipPlane;
        m_mousePos = Camera.main.ScreenToWorldPoint(m_mousePos);
        DeltaTime = Time.deltaTime;

        UpdateIndicators();
        if (m_killsText) m_killsText.text = "Tanks: " + NumDeadTanks + "\nTowers: " + NumDeadTowers + "\nScore: " + Score;
        if (NumDeadTowers == m_totalNumTowers && !m_endScreen.activeInHierarchy)
            HandleEndScreen("VICTORY", "You Destroyed All The Towers", Color.green);
        if (m_healthSystem.Health <= 0f && !m_endScreen.activeInHierarchy)
        {
            HandleEndScreen("GAMEOVER", "You Were Defeated", Color.red);
            m_transform.GetChild(0).gameObject.SetActive(false);
            m_deathParticleSystem.Play();
            return;
        }
        m_mouseTarget.position = m_mousePos;
        RotateTank();
        UpdateCam();

        if (m_shootInput && m_canShoot) Shoot();
    }
    private void UpdateCam()
    {
        Vector2 pos = Vector2.LerpUnclamped(m_cam.position, m_transform.position, m_followSpeed * DeltaTime);
        m_cam.position = new Vector3(pos.x, pos.y, m_cam.position.z);
    }
    private void RotateTank()
    {
        if (m_inputDirection != Vector3.zero)
        {
            Quaternion targetBodyRotation = Quaternion.LookRotation(Vector3.forward, m_inputDirection);
            m_body.rotation = Quaternion.Slerp(m_body.rotation, targetBodyRotation, m_bodyRotateSpeed * DeltaTime);
        }
        Vector2 directionToMouse = (m_mousePos - m_transform.position).normalized;
        Quaternion targetTurretRotation = Quaternion.LookRotation(Vector3.forward, directionToMouse);
        m_turret.rotation = Quaternion.Slerp(m_turret.rotation, targetTurretRotation, m_turretRotateSpeed * DeltaTime);
    }
    private void Shoot()
    {
        var b = m_bulletPool.GetBullet(m_turret.position, m_turret.rotation);
        b.m_targetLayer = m_bulletMask.value;
        b.m_penetration = m_bulletPenetration;
        b.m_damage = m_bulletDamage;
        b.m_rb.linearVelocity = m_bulletSpeed * m_turret.up;
        m_shootParticleSystem.Play();
        m_rb.AddForce(m_turret.up * -10f, ForceMode2D.Impulse);
        StartCoroutine(Coroutines.PingPongVector2OverTime(new(0f, 0.5f), new(0f, 0.1f), 0.05f, 0.2f, value => m_barrel.localPosition = value));
        StartCoroutine(DelayShot());
    }
    private IEnumerator DelayShot()
    {
        m_canShoot = false;
        m_ammoText.text = "0 |";
        yield return new WaitForSeconds(m_fireRate);
        m_canShoot = true;
        m_ammoText.text = "1 |";
    }
    private void FixedUpdate()
    {
        if (m_healthSystem.Health <= 0) return;
        Move();
        Velocity = m_rb.linearVelocity;
    }
    private void Move()
    {
        Vector2 moveDirection = m_moveSpeed * m_inputDirection;
        Vector3 moveForce = m_rb.mass * m_moveAcceleration * (moveDirection - m_rb.linearVelocity);
        m_rb.AddForce(moveForce);
    }
}