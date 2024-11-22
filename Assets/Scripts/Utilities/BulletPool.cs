using System.Collections.Generic;
using UnityEngine;

public class BulletPool : Singleton_template<BulletPool>
{
    [SerializeField] private int m_initialBulletCount;
    [SerializeField] private float m_flightTime;
    [SerializeField] private Rigidbody2D m_bulletPrefab;
    [SerializeField] private Transform m_bulletHolder;
    [SerializeField] private AudioClip[] m_hitSFXs;

    private readonly List<Bullet> m_bullets = new();
    private readonly List<Bullet> m_activeBullets = new();

    private PoolManager m_poolManager;
    private void Start()
    {
        m_poolManager = PoolManager.Instance();
    }
    void Update()
    {
        if (m_bullets.Count <= m_initialBulletCount)
        {
            AddBulletToPool();
        }
        foreach (Bullet bullet in new List<Bullet>(m_activeBullets))
        {
            Vector2 dir = (Vector2)bullet.m_rb.position - bullet.m_lastPosition;
            if (dir != Vector2.zero)
            {
                bullet.m_rb.transform.up = dir.normalized;
                RaycastHit2D[] hits = Physics2D.RaycastAll(bullet.m_lastPosition, dir.normalized, dir.magnitude, bullet.m_targetLayer);
                if (hits.Length > 0)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider == bullet.m_lastHit) continue;
                        else bullet.m_lastHit = hit.collider;
                        hit.collider.GetComponentInParent<HealthSystem>().TakeDamage(bullet.m_damage);
                        var source = hit.collider.GetComponentInParent<AudioSource>();
                        source.clip = m_hitSFXs[Random.Range(0, m_hitSFXs.Length-1)];
                        source.Play();
                        GameObject g = m_poolManager.SpawnFromPool("BulletHits", hit.point, Quaternion.LookRotation(Vector3.forward, hit.normal));
                        m_poolManager.ReturnToPoolDelayed("BulletHits", g, 0.5f);
                        bullet.m_penetration--;
                        bullet.m_damage *= 0.5f;
                        bullet.m_rb.transform.localScale *= 0.25f;
                        if (bullet.m_penetration < 0)
                        {
                            ReturnBullet(bullet);
                            break;
                        }
                    }
                }
            }
            bullet.m_elapsedTime += Time.deltaTime;
            if (bullet.m_elapsedTime >= m_flightTime) ReturnBullet(bullet);
            bullet.m_lastPosition = bullet.m_rb.position;
        }
    }
    private Bullet AddBulletToPool()
    {
        var b = new Bullet(Instantiate(m_bulletPrefab, m_bulletHolder));
        m_bullets.Add(b);
        b.m_rb.gameObject.SetActive(false);
        return b;
    }
    public Bullet GetBullet()
    {
        Bullet b;
        if (m_bullets.Count == 0) b = AddBulletToPool();
        else b = m_bullets[0];
        m_bullets.Remove(b);
        m_activeBullets.Add(b);
        b.m_rb.gameObject.SetActive(true);
        return b;
    }
    public Bullet GetBullet(Vector2 pos, Quaternion rot)
    {
        Bullet b;
        if (m_bullets.Count == 0) b = AddBulletToPool();
        else b = m_bullets[0];
        m_bullets.Remove(b);
        b.m_rb.transform.SetPositionAndRotation(pos, rot);
        b.m_lastPosition = pos;
        m_activeBullets.Add(b);
        b.m_rb.gameObject.SetActive(true);
        return b;
    }
    public void ReturnBullet(Bullet b)
    {
        b.ResetBullet();
        m_activeBullets.Remove(b);
        m_bullets.Add(b);
    }
}
public class Bullet
{
    public int m_targetLayer;
    public int m_penetration;
    public float m_elapsedTime;
    public float m_damage;
    public Vector2 m_lastPosition;
    public Rigidbody2D m_rb;
    public Collider2D m_lastHit;
    private Vector3 m_initScale;
    public Bullet(Rigidbody2D rb)
    {
        m_elapsedTime = 0f;
        m_damage = 0f;
        m_penetration = 0;
        m_lastPosition = Vector2.zero;
        m_rb = rb;
        m_initScale = m_rb.transform.localScale;
    }
    public void ResetBullet()
    {
        m_rb.gameObject.SetActive(false);
        m_rb.transform.localScale = m_initScale;
        m_elapsedTime = 0f;
        m_damage = 0f;
        m_penetration = 0;
        m_lastPosition = Vector2.zero;
        m_targetLayer = 100;
        m_lastHit = null;
    }
}