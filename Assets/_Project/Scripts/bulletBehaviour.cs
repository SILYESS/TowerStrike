using Soldier;
using Fusion;
using Towers;
using UnityEngine;
using Utils;


[RequireComponent(typeof(Collider))]
public class bulletBehaviour : MonoBehaviour
{
    [Header("Bullet settings")]
    public float speed = 10f;
    public float hitRadius = 0.2f;
    public float lifeTime = 6f;

    [Header("Trail")]
    public TrailRenderer trailRenderer;
    [Header("Renderer")]
    [SerializeField] private Renderer bulletRenderer;
    // runtime
    public Transform target;
    public TowerType towerType;                // the tower type that spawned this bullet
    private string excludedSoldierLayerName;    // soldier layer to exclude (same-color soldiers)
    public int damage = 1;
    private float baseLifeTime;
    private Collider _collider;
    private Towers.TowerBehavior _sourceTower;
    private Collider _sourceCollider;
    private MaterialPropertyBlock _mpb;
    private NetworkObject _netObj;

    private void Awake()
    {
        baseLifeTime = lifeTime;
        _collider = GetComponent<Collider>();
        _netObj = GetComponent<NetworkObject>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (bulletRenderer == null)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r is TrailRenderer) continue;
                bulletRenderer = r;
                break;
            }
        }
    }

    private void OnEnable()
    {
        lifeTime = baseLifeTime;
        target = null;
        if (LevelManager.Instance != null) LevelManager.Instance.allBullets.Add(this);
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allBullets.Remove(this);
        if (_collider != null && _sourceCollider != null)
        {
            Physics.IgnoreCollision(_collider, _sourceCollider, false);
        }
        _sourceTower = null;
        _sourceCollider = null;
    }

    /// <summary>
    /// Call this immediately after instantiating the bullet prefab.
    /// </summary>
    public void Initialize(TowerType type, Transform preCalculatedTarget, Towers.TowerBehavior sourceTower)
    {
        towerType = type;
        _sourceTower = sourceTower;
        _sourceCollider = sourceTower != null ? sourceTower.GetComponent<Collider>() : null;
        if (_collider != null && _sourceCollider != null)
        {
            Physics.IgnoreCollision(_collider, _sourceCollider, true);
        }

        // Determine excluded soldier layer
        switch (towerType)
        {
            case TowerType.GreyTower:
                excludedSoldierLayerName = "GreySoldier";
                break;
            case TowerType.PlayerTower:
                excludedSoldierLayerName = "PlayerSoldier";
                break;
            case TowerType.RedTower:
                excludedSoldierLayerName = "RedSoldier";
                break;
            case TowerType.GreenTower:
                excludedSoldierLayerName = "GreenSoldier";
                break;
            case TowerType.YellowTower:
                excludedSoldierLayerName = "YellowSoldier";
                break;
            default:
                excludedSoldierLayerName = string.Empty;
                break;
        }
        ApplyBulletColor(towerType);
        
        this.target = preCalculatedTarget;
    }


    private Color GetColorForType(TowerType type)
    {
        switch (type)
        {
            case TowerType.PlayerTower: return Color.blue;
            case TowerType.RedTower: return Color.red;
            case TowerType.YellowTower: return Color.yellow;
            case TowerType.GreenTower: return Color.green;
            case TowerType.GreyTower: return Color.grey;
            default: return Color.white;
        }
    }

    private void ApplyBulletColor(TowerType type)
    {
        Color color = GetColorForType(type);
        // Use property blocks to avoid material instantiation
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        // Bullet mesh color
        if (bulletRenderer != null)
        {
            bulletRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", color);
            _mpb.SetColor("_BaseColor", color);
            bulletRenderer.SetPropertyBlock(_mpb);
        }
    }
    private void Update()
    {
        // lifetime tick
        if (lifeTime > 0f)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                DespawnOrReturn();
                return;
            }
        }

        // Move toward target
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;

            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            if (Vector3.Distance(transform.position, target.position) <= hitRadius)
            {
                OnHitTarget(target.gameObject);
            }
        }
        else
        {
            DespawnOrReturn();
        }
    }

    /// <summary>
    /// Finds nearest valid Soldier first; if none, picks nearest valid Tower (skips towers with same TowerType).
    /// </summary>
    private void ChooseTarget(Collider[] targets)
    {
        // Deprecated or Unused because we pass target directly in Initialize
        // Keeping logic just in case logic reverts
    }

    private void OnHitTarget(GameObject hitObj)
    {
        // Soldier hit
        var soldier = hitObj.GetComponent<SoldierBehavior>();
        if (soldier != null)
        {
            string soldierLayerName = LayerMask.LayerToName(soldier.gameObject.layer);
            if (string.IsNullOrEmpty(excludedSoldierLayerName) || soldierLayerName != excludedSoldierLayerName)
            {
                soldier.Soldierhitbehaviour();


            }
            SimplePool.Return(gameObject);
            return;
        }

        // Tower hit
        var tower = hitObj.GetComponent<TowerBehavior>();
        if (tower != null)
        {
            // Ensure TowerBehavior exposes its type field/property named 'towerType' or change accordingly
            if (tower.localType != towerType) // only damage towers of different type
            {
                tower.TakeDamage(1, towerType);
            }
            SimplePool.Return(gameObject);
            return;
        }

        // Hit something else: destroy bullet
        SimplePool.Return(gameObject);
    }

    // Trigger collision handling (soldier and tower)
    private void OnTriggerEnter(Collider other)
    {
        if (_sourceCollider != null && other == _sourceCollider) return;

        var soldier = other.GetComponent<SoldierBehavior>();
        if (soldier != null)
        {
            if (soldier.isCollidedWithSoldier) return;
            string soldierLayerName = LayerMask.LayerToName(soldier.gameObject.layer);
            if (!string.IsNullOrEmpty(excludedSoldierLayerName) && soldierLayerName == excludedSoldierLayerName)
                return;

            OnHitTarget(other.gameObject);
            return;
        }

        var tower = other.GetComponent<TowerBehavior>();
        if (tower != null)
        {
            if (tower.localType == towerType) // skip same type
                return;

            OnHitTarget(other.gameObject);
            return;
        }

        SimplePool.Return(gameObject);
    }

    private void DespawnOrReturn()
    {
        var runner = Network.MultiplayerManager.Instance != null ? Network.MultiplayerManager.Instance.runner : null;
        if (_netObj != null && runner != null)
        {
            if (_netObj != null && _netObj.IsValid) runner.Despawn(_netObj);
            return;
        }
        SimplePool.Return(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_sourceCollider != null && collision.collider == _sourceCollider) return;

        var soldier = collision.gameObject.GetComponent<SoldierBehavior>();
        if (soldier != null)
        {
            if (soldier.isCollidedWithSoldier) return;
            string soldierLayerName = LayerMask.LayerToName(soldier.gameObject.layer);
            if (!string.IsNullOrEmpty(excludedSoldierLayerName) && soldierLayerName == excludedSoldierLayerName)
                return;

            OnHitTarget(collision.gameObject);
            return;
        }

        var tower = collision.gameObject.GetComponent<TowerBehavior>();
        if (tower != null)
        {
            if (tower.localType == towerType)
                return;

            OnHitTarget(collision.gameObject);
            return;
        }

        SimplePool.Return(gameObject);
    }
}
