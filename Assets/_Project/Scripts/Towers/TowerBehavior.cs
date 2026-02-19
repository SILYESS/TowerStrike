using Fusion;
using Soldier;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Towers
{
    public class TowerBehavior : NetworkBehaviour
    {
        #region Variables
        [Header("FX")]
        public ParticleSystem explosionFx;
        public ParticleSystem floorExplosionFx;
        public ParticleSystem AddFloorFx;
        public ParticleSystem MagicBlastFx;

        [Header("Connections")]
        public List<TowerBehavior> linkedTowers = new List<TowerBehavior>();
        public int currentConnections = 0;
        public int currentFloors = 0;
        public List<GameObject> dotsSprites = new List<GameObject>();
        public List<GameObject> floors = new List<GameObject>();
        public bool canCreateConnection = true;

        [Header("Soldier Spawn")]
        public Transform soldierSpawnPoint;
        public GameObject soldierPrefab;
        public GameObject bulletPrefab;

        [Header("Tower Settings")]
        public Transform connectionPoint;
        public Renderer[] towerMaterials;
        [SerializeField] private int baseHealth = 10;
        public int maxHealth = 16;
        public List<Transform> floorTopPoints;
        public Transform dotsCanvas;
        public float extraHeight = 0.3f;
        public GameObject rangeSprite;
        public int attackerRangeMultiplyer;
        public Transform attackerTowerTransform;
        private Vector3 attackerBaseScale;
        private float attackerBaseCooldown;
        public float attackerMinCooldown = 0.9f;
        [Header("Attacker Range Settings")]
        [SerializeField] private float[] attackerRadiusByFloor = new float[] { 5.5f, 8f, 10f, 12f };
        [SerializeField] private float[] rangeSpriteScaleByFloor = new float[] { 40f, 50f, 60f, 70f };
        [Header("Attacker Cooldown Settings")]
        [SerializeField] private float[] attackerCooldownByFloor = new float[] { 1.2f, 1.0f, 0.85f, 0.7f };
        [Header("Attacker Targeting")]
        [SerializeField] private float attackerSafeDistance = 1.2f;
        [Header("Normal Cooldown Settings")]
        [SerializeField] private float[] normalCooldownByFloor = new float[] { 1.8f, 1.5f, 1.2f, 1.0f };
        [Header("SphereCast Settings")]
        public float radius = 0.5f;
        public LayerMask targetLayers;
        public bool startAttack;
        public Collider[] hits;
        public InputManager inputManager;
        public int CurrentHealth => IsNetworkMode ? towerHealth : localHealth;
        public int MaxHealth => maxHealth;

        public int localHealth;
        public float spawnCooldown = 2f;
        private float normalBaseCooldown;
        public float lastSpawnTime;
        public bool canAttack = true;
        private InputManager _inputManager;
        public Vector3 healTextScaleStart;
        public TextMeshPro towerHealthText;
        public bool isStarterTower;
        public Transform starterRoundedSprite;
        public Vector3 startScale;
        public Vector3 targetScale;
        float totalDuration = 1f; // total time (scale out + back)
        public TowerType localType;
        public enum TowerAttackType
        {
            NormalTower,
            AttackerTower
        }
        public TowerAttackType attackType;
        [Networked] private TowerAttackType networkedAttackType { get; set; }
        [Networked] private int towerHealth { get; set; }
        [Fusion.Networked] public TowerType networkedType { get; set; }

        private TowerType _lastVisualType;
        private int _lastHealth;

        private bool IsNetworkMode => Runner != null;

        #endregion

        #region Init

        public void OnEnable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allTowers.Add(this);
    }

    public void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allTowers.Remove(this);
    }

    private void Awake()
        {
            if (attackType == TowerAttackType.AttackerTower)
            {
                attackerBaseScale = attackerTowerTransform != null ? attackerTowerTransform.localScale : Vector3.one;
                // attackerBaseCooldown isn't set here because spawnCooldown might be overwritten by inspector or InitTower
            }
        }

        private void Start()
        {
            healTextScaleStart = towerHealthText.transform.localScale;
            _inputManager = FindObjectOfType<InputManager>();
            localHealth = baseHealth;
            if (IsNetworkMode && Object.HasStateAuthority)
                towerHealth = baseHealth;
            lastSpawnTime = spawnCooldown;
            normalBaseCooldown = spawnCooldown;
            UpdateVisual();
            if (starterRoundedSprite != null)
                startScale = starterRoundedSprite.localScale;
            if (inputManager == null)
                inputManager = FindObjectOfType<InputManager>();
            if (attackType == TowerAttackType.AttackerTower)
            {
                // attackerBaseScale moved to Awake
                attackerBaseCooldown = spawnCooldown;
            }

            if (GameManager.instance.gameMode == Mode.Multiplayer)
            {
                InitTower(spawnCooldown,baseHealth);
                towerHealthText.text = CurrentHealth.ToString();
            }
        }

        public void InitTower(float solderSpawnRate, int startHealth)
        {
            localHealth = startHealth;
            baseHealth = startHealth;
            spawnCooldown = solderSpawnRate;
            if (attackType == TowerAttackType.NormalTower) normalBaseCooldown = spawnCooldown;
            else attackerBaseCooldown = spawnCooldown;
            Debug.Log("InitTower");
            if (IsNetworkMode && Object.HasStateAuthority) towerHealth = localHealth;
            UpdateVisual();
            if (attackType == TowerAttackType.NormalTower)
            {
                setFloors(CurrentHealth);
            }
            else if (attackType == TowerAttackType.AttackerTower)
            {
                //setAttackerFloors(CurrentHealth);
            }
        }

        public void Initialize(TowerType color)
        {
            type = color;
            UpdateVisual();
        }

        public void SetAttackEnabled(bool enabled) => canAttack = enabled;

        public override void Spawned()
        {
            spawnCooldown = 2f; // Force on spawn too
            if (Object.HasStateAuthority)
            {
                networkedType = localType;
                towerHealth = localHealth;
            }

            // Auto Parenting for Clients (and Host safety)
            if (IsNetworkMode && Network.MultiplayerManager.Instance && Network.MultiplayerManager.Instance.levelParent)
            {
                // If my root is not the level parent, fix it.
                // Note: transform.root returns the topmost parent. If the map is unparented, it returns the Map.
                // If I am already parented to levelParent, transform.parent check or root check works.
                if (transform.root != Network.MultiplayerManager.Instance.levelParent && transform.root != Network.MultiplayerManager.Instance.transform)
                {
                    // Only move the ROOT (The Map Prefab)
                    transform.root.SetParent(Network.MultiplayerManager.Instance.levelParent);
                }
            }

            UpdateVisual();
        }

        public override void Render()
        {
            // Detect changes in networked vars for visual updates
            if (networkedType != _lastVisualType)
            {
                _lastVisualType = networkedType;
                UpdateVisual();
            }
            if (towerHealth != _lastHealth)
            {
                _lastHealth = towerHealth;
                UpdateVisual();
            }
        }

        public TowerType type
        {
            get => IsNetworkMode ? networkedType : localType;
            set
            {
                if (IsNetworkMode) networkedType = value;
                else localType = value;
            }
        }

        #endregion

        #region Soldier Spawn

        public override void FixedUpdateNetwork()
        {
            // Only server spawns units
            if (!Object.HasStateAuthority) return;
            if (!canAttack || !GameManager.instance.gameplayEnabled) return;

            // Only player/enemy towers spawn (Neutral/Grey do not)
            if (networkedType == TowerType.GreyTower) return;

            lastSpawnTime += Runner.DeltaTime;
            if (lastSpawnTime >= spawnCooldown)
            {
                lastSpawnTime = 0;
                // Debug.Log($"[Tower {name}] Spawning Soldiers! Cooldown: {spawnCooldown}, Connections: {linkedTowers.Count}");

                // Spawn one soldier for EACH connection
                foreach (var t in linkedTowers)
                {
                    if (t != null) SpawnSoldier(t);
                }
            }
        }

        private void Update()
        {
            if (IsNetworkMode) return;

            if (attackType == TowerAttackType.NormalTower)
            {
                if (!canAttack || linkedTowers.Count == 0 || LevelManager.Instance.levelCompleted || !GameManager.instance.gameplayEnabled)
                {
                    return;
                }
            }
            else if (attackType == TowerAttackType.AttackerTower)
            {
                if (!canAttack || LevelManager.Instance.levelCompleted || !GameManager.instance.gameplayEnabled)
                {
                    return;
                }
            }

            lastSpawnTime += Time.deltaTime;
            // Solo mode
            if (!IsNetworkMode && lastSpawnTime >= spawnCooldown)
            {
                if (attackType == TowerAttackType.NormalTower)
                {
                    lastSpawnTime = 0;
                    foreach (var t in linkedTowers) SpawnSoldier(t);
                }
                else if (attackType == TowerAttackType.AttackerTower)
                {
                    // Optimization: Check for targets ONLY when ready to fire
                    Transform target = FindAttackerTarget();
                    if (target != null)
                    {
                        lastSpawnTime = 0; // Only reset if we actually fired
                        SpawnBullet(this, target);
                    }
                }

            }

            // Removed per-frame OverlapSphere
        }
        
        private Transform FindAttackerTarget()
        {
            if (LevelManager.Instance == null) return null;
            
            float bestDistSq = radius * radius;
            float safeSq = attackerSafeDistance * attackerSafeDistance;
            Transform bestTarget = null;
            Vector3 myPos = transform.position;

            // 1. Check Soldiers (Priority)
            foreach (var s in LevelManager.Instance.allSoldiers)
            {
                if (s == null || !s.gameObject.activeInHierarchy) continue;
                bool isDeadOrDying = s.isCollidedWithSoldier;
                if (s.Object != null && s.Object.IsValid && s.IsSpawned)
                {
                    if (s.IsDead) isDeadOrDying = true;
                }
                if (isDeadOrDying) continue;
                
                // Check color
                if (s.soldierColor_NormalMode == localType) continue; // Don't shoot friends
                if (localType == TowerType.GreyTower && s.soldierColor_NormalMode == TowerType.GreyTower) continue;

                float dSq = (s.transform.position - myPos).sqrMagnitude;
                if (dSq < safeSq) continue; // ignore too close targets
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    bestTarget = s.transform;
                }
            }
            
            if (bestTarget != null) return bestTarget;

            // 2. Check Towers (Secondary)
            foreach (var t in LevelManager.Instance.allTowers)
            {
                if (t == null || !t.gameObject.activeInHierarchy || t == this) continue;
                
                // Check color
                if (t.localType == localType && localType != TowerType.GreyTower) continue; // Don't shoot friends (unless everyone is grey?)

                float dSq = (t.transform.position - myPos).sqrMagnitude;
                if (dSq < safeSq) continue; // ignore too close targets
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    bestTarget = t.transform;
                }
            }

            return bestTarget;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        private void SpawnSoldier(TowerBehavior target)
        {
            if (soldierPrefab == null || target == null) return;

            Vector3 pos = connectionPoint != null ? connectionPoint.position : transform.position;

            if (IsNetworkMode && Object.HasStateAuthority)
            {
                var obj = Runner.Spawn(soldierPrefab, pos, Quaternion.identity);
                var soldier = obj.GetComponent<Soldier.SoldierBehavior>();
                soldier?.Initialize(type, this, target);
                //soldier.transform.DOScale(Vector3.one * 2, 0.7f);
            }
            else if (!IsNetworkMode)
            {
                var soloObj = Utils.SimplePool.Get(soldierPrefab, pos, Quaternion.identity);
                var soloSoldier = soloObj.GetComponent<Soldier.SoldierBehavior>();
                soloSoldier?.Initialize(type, this, target);
            }
        }

        private void SpawnBullet(TowerBehavior tower, Transform target)
        {
            if (bulletPrefab == null) return;
            Vector3 pos = soldierSpawnPoint != null ? soldierSpawnPoint.position : transform.position;
            if (target != null)
            {
                Vector3 dir = (target.position - pos).normalized;
                pos += dir * 0.6f; // push bullet spawn slightly away from tower to avoid self-collisions
            }
            // Multiplayer spawn
            if (IsNetworkMode && Object.HasStateAuthority)
            {
                var obj = Runner.Spawn(bulletPrefab, pos, Quaternion.identity);
                var bullet = obj.GetComponent<bulletBehaviour>();
                bullet.Initialize(tower.localType, target, this);
                return;
            }

            // Solo spawn
            var soloObj = Utils.SimplePool.Get(bulletPrefab, pos, Quaternion.identity);
            var solobullet = soloObj.GetComponent<bulletBehaviour>();
            solobullet.Initialize(tower.localType, target, this);

        }
        #endregion

        #region Combat

        public void TakeDamage(int amount, TowerType attackerColor, TowerBehavior source = null)
        {
            if (!gameObject.activeInHierarchy) return;

            if (IsNetworkMode && Object.HasStateAuthority)
            {
                HandleDamageNetwork(amount, attackerColor, source);
            }
            else if (!IsNetworkMode)
            {
                HandleDamageLocal(amount, attackerColor);
            }

            UpdateVisual();
        }

        private void HandleDamageNetwork(int amount, TowerType attackerColor, TowerBehavior source)
        {
            if (attackerColor == networkedType)
            {
                // HEAL
                if (towerHealth >= maxHealth && linkedTowers.Count > 0)
                {
                    // Overflow logic: forward to another tower
                    TowerBehavior forwardTarget = linkedTowers.Find(t => t != source);
                    if (forwardTarget != null) SpawnSoldier(forwardTarget);
                }
                towerHealth = Mathf.Clamp(towerHealth + amount, 0, maxHealth);
            }
            else
            {
                // DAMAGE
                towerHealth -= amount;
                if (towerHealth <= 0)
                {
                    CaptureTower(attackerColor);
                    towerHealth = Mathf.Clamp(Mathf.Abs(towerHealth), 1, maxHealth);
                }
            }
        }

        private void HandleDamageLocal(int amount, TowerType attackerColor)
        {
            if (attackerColor == localType)
            {
                localHealth = Mathf.Clamp(localHealth + amount, 0, maxHealth);
            }
            else
            {
                localHealth -= amount;
                if (localHealth <= 0)
                {
                    Debug.Log("START Captured Tower");

                    localHealth = Mathf.Clamp(Mathf.Abs(localHealth), 1, maxHealth);
                    LevelManager.Instance.UpdatePercentageUI();
                    OnCaptured_Single(localType, attackerColor);

                    if (LevelManager.Instance.currentLevel == 0) 
                    { 
                        if (attackType == TowerAttackType.AttackerTower)
                        {
                            if (!LevelManager.Instance.towerPool[2].activeInHierarchy && !LevelManager.Instance.towerPool[3].activeInHierarchy)
                            {
                                LevelManager.Instance.towerPool[2].SetActive(true);
                                LevelManager.Instance.towerPool[3].SetActive(true);
                            }
                            GameManager.instance.secondText.SetActive(true);
                            GameManager.instance.pointerCut.SetActive(true);
                        }
                    }
                }
            }
            if (localHealth < 30) AnimateHealthText(healTextScaleStart * 0.5f);
        }

        private void CaptureTower(TowerType newOwner)
        {
            networkedType = newOwner;

            // Assign Authority
            if (newOwner == TowerType.PlayerTower)
            {
                if (Network.MultiplayerManager.Instance.HostPlayer != PlayerRef.None)
                    Object.AssignInputAuthority(Network.MultiplayerManager.Instance.HostPlayer);
            }
            else if (newOwner == TowerType.RedTower)
            {
                if (Network.MultiplayerManager.Instance.ClientPlayer != PlayerRef.None)
                    Object.AssignInputAuthority(Network.MultiplayerManager.Instance.ClientPlayer);
            }
            else
            {
                // Neutral or other
                Object.RemoveInputAuthority();
            }

            // Cleanup - Re-enabled for Visual consistency on capture
            RPC_ClearConnections();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ClearConnections()
        {
            //linkedTowers.Clear();
            //currentConnections = 0;

            // Visual Cleanup via ConnectionManager usually, but we can brute force clean lines
            // Actually ConnectionManager maintains the list, so we tell IT to clear.
            if (ConnectionManager.Instance) ConnectionManager.Instance.RemoveOutgoingConnections(this);
        }

        // Local Callback
        private void OnCaptured_Single(TowerType previousType, TowerType newType)
        {
            // Local logic...
            if (GameManager.instance.gameMode == Mode.Single)
            {

                if (
                newType == TowerType.PlayerTower &&
                (previousType == TowerType.RedTower ||
                 previousType == TowerType.YellowTower ||
                 previousType == TowerType.GreenTower))
                {
                    Debug.Log("Captured Tower");
                    var ai = FindObjectOfType<AITowerController>();
                    if (ai != null)
                        ai.OnTowerConverted(this);

                    localType = newType;
                }
                else if (newType != TowerType.PlayerTower)
                {
                    localType = newType;
                }
                else
                {
                    localType = newType;
                }


                /*
                // Remove all soldiers targeting this tower
                var allSoldiers = FindObjectsOfType<SoldierBehavior>();
                foreach (var s in allSoldiers)
                {
                    if (s != null && s.targetTower == this)
                    {
                        if (Runner != null)
                            Runner.Despawn(s.Object);
                        else
                            Destroy(s.gameObject);
                    }
                }
                */
                // Remove all connections (logic + loops)
                var cm = FindObjectOfType<ConnectionManager>();
                if (cm != null)
                {
                    //cm.StopAllCoroutinesForTower(this);
                    cm.RemoveAllConnectionsForTower(this);
                }

                // Clear this tower's linked towers
                linkedTowers.Clear();
                currentConnections = 0;
                AITowerController.Instance.FindAITowers();


                if (ConnectionManager.Instance)
                {
                    ConnectionManager.Instance.CleanUpConnectionsLocal(this);
                }
                else
                {
                    // Fallback try find
                    var cm_found = FindObjectOfType<ConnectionManager>();
                    if (cm_found) cm_found.CleanUpConnectionsLocal(this);
                }

                resetTowerConnections();

                LevelManager.Instance.UpdatePercentageUI();

            }

        }


        #endregion

        #region Visuals


        public void UpdateVisual()
        {
            ApplyColor();
            Debug.Log("Update Visual");
            int displayHealth = IsNetworkMode ? towerHealth : localHealth;

            if (towerHealthText != null && GameManager.instance.gameplayEnabled)
            {
                towerHealthText.text = displayHealth >= 30 ? "MAX" : displayHealth.ToString();
            }

            if (attackType == TowerAttackType.NormalTower && GameManager.instance.gameplayEnabled)
            {
                SetDots(displayHealth);
            }
            else if (attackType == TowerAttackType.AttackerTower && GameManager.instance.gameplayEnabled)
            {
                setAttackerFloors(displayHealth);
            }
            else
            {
                //setAttackerFloors(CurrentHealth);
            }

        }

        private MaterialPropertyBlock _mpb;

        private void ApplyColor()
        {
            if (towerMaterials == null) return;
            Color selected = Color.white;

            // Decide Color based on "Who am I" (InputAuthority)
            if (IsNetworkMode)
            {
                if (networkedType == TowerType.GreyTower)
                    selected = LevelManager.Instance.greyColor;
                else
                {
                    if (Object != null && Object.HasInputAuthority)
                        selected = LevelManager.Instance.blueColor;
                    else
                        selected = LevelManager.Instance.redColor;
                }
            }
            else
            {
                selected = localType switch
                {
                    TowerType.GreyTower => LevelManager.Instance.greyColor,
                    TowerType.PlayerTower => LevelManager.Instance.blueColor,
                    TowerType.RedTower => LevelManager.Instance.redColor,
                    TowerType.YellowTower => LevelManager.Instance.yellowColor,
                    TowerType.GreenTower => LevelManager.Instance.greenColor,
                    _ => LevelManager.Instance.greyColor,
                };
            }

            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            foreach (var item in towerMaterials)
            {
                if (item != null)
                {
                    // Only modify the FIRST material (index 0)
                    // GetPropertyBlock overwrites existing, so we fetch, set, and apply.
                    // To target specific material index, use SetPropertyBlock(mpb, index)
                    
                    item.GetPropertyBlock(_mpb, 0); 
                    _mpb.SetColor("_BaseColor", selected);
                    _mpb.SetColor("_Color", selected);
                    item.SetPropertyBlock(_mpb, 0);
                }
            }

        }

        public void SetDots(int currentHealth)
        {

            int activeCount = (currentHealth / 10) + 1;
            if (activeCount == 0)
                activeCount = 1;
            CheckConnectionPossibility();
            // Activate correct number of dots
            for (int i = 0; i < dotsSprites.Count; i++)
            {
                dotsSprites[i].SetActive(i < activeCount);
            }




            setFloors(currentHealth);
        }

        public void setFloors(int currentHealth)
        {

            Debug.Log("SET FLOORS");
            int activeCount = (currentHealth / 10);
            if (activeCount == 0) 
            activeCount = 1;
            if (currentFloors > activeCount && GameManager.instance.gameplayEnabled)
            {
                floorExplosionFx.Play(true);
                soundManager.Instance.playSoundEffect(soundManager.Instance.explosionSound);
            }
            else if (currentFloors < activeCount && GameManager.instance.gameplayEnabled)
            {
                AddFloorFx.Play();
                MagicBlastFx.Play(true);
                soundManager.Instance.playSoundEffect(soundManager.Instance.newFloorSound);
            }
            currentFloors = activeCount;
            activeCount = Mathf.Clamp(activeCount, 0, 3);
            for (int i = 0; i < floors.Count; i++)
            {
                floors[i].SetActive(i < activeCount);
            }

            UpdateDotsCanvasPosition(activeCount);

            if (attackType == TowerAttackType.NormalTower && normalBaseCooldown > 0f)
            {
                int idx = Mathf.Clamp(currentHealth / 10, 0, 3);
                if (normalCooldownByFloor != null && normalCooldownByFloor.Length > idx)
                {
                    spawnCooldown = normalCooldownByFloor[idx];
                }
                else
                {
                    float factor = Mathf.Clamp01(1f - 0.15f * activeCount);
                    spawnCooldown = normalBaseCooldown * Mathf.Max(0.4f, factor);
                }
            }
        }

        public void setAttackerFloors(int currentHealth)
        {

            attackerRangeMultiplyer = (currentHealth / 10);
            int floorsCount = Mathf.Clamp(attackerRangeMultiplyer, 0, 3);
            setupTowerRange(floorsCount);

            if (currentFloors > floorsCount && GameManager.instance.gameplayEnabled)
            {
                floorExplosionFx.Play(true);
            }
            else if (currentFloors < floorsCount && GameManager.instance.gameplayEnabled)
            {
                AddFloorFx.Play();
                MagicBlastFx.Play(true);
            }
            currentFloors = floorsCount;
            attackerRangeMultiplyer = floorsCount;

            for (int i = 0; i < floors.Count; i++)
            {
                floors[i].SetActive(i < floorsCount);
            }

            UpdateDotsCanvasPosition(floorsCount);

            int idx = Mathf.Clamp(floorsCount, 0, 3);
            if (attackerCooldownByFloor != null && attackerCooldownByFloor.Length > idx)
            {
                spawnCooldown = Mathf.Max(attackerCooldownByFloor[idx], attackerMinCooldown);
            }
            else
            {
                float factor = Mathf.Clamp01(1f - 0.15f * floorsCount);
                spawnCooldown = attackerBaseCooldown * Mathf.Max(0.4f, factor);
                spawnCooldown = Mathf.Max(spawnCooldown, attackerMinCooldown);
            }
            if (attackerTowerTransform != null)
                attackerTowerTransform.localScale = attackerBaseScale * (1f + 0.1f * floorsCount);

        }
        public void setupTowerRange(int attackerRangeMultiplyer)
        {
            int idx = Mathf.Clamp(attackerRangeMultiplyer, 0, 3);
            if (attackerRadiusByFloor != null && attackerRadiusByFloor.Length > idx)
                radius = attackerRadiusByFloor[idx];
            if (rangeSprite != null && rangeSpriteScaleByFloor != null && rangeSpriteScaleByFloor.Length > idx)
                rangeSprite.transform.localScale = Vector3.one * rangeSpriteScaleByFloor[idx];
        }

        private void UpdateDotsCanvasPosition(int activeCount)
        {
            if (floorTopPoints == null || floorTopPoints.Count == 0) return;
            int index = Mathf.Clamp(activeCount - 1, 0, floorTopPoints.Count - 1);
            Transform top = floorTopPoints[index];
            if (dotsCanvas != null) dotsCanvas.position = top.position + Vector3.up * extraHeight;
            if (AddFloorFx != null) AddFloorFx.transform.position = top.position;
        }

        public void AnimateHealthText(Vector3 targetScale)
        {
            towerHealthText.transform.DOKill();
            towerHealthText.transform.localScale = healTextScaleStart;
            towerHealthText.transform.DOPunchScale(targetScale, 1, 1);
        }

        #endregion

        public bool CheckConnectionPossibility()
        {
            currentConnections = linkedTowers.Count;
            int health = IsNetworkMode ? towerHealth : localHealth;
            int limit = 2;
            if (health < 10) limit = 1;
            else if (health > 20) limit = 3;
            //else if (health >= 10) limit = 2;
            // Check limits
            if (currentConnections >= limit)
            {
                canCreateConnection = false;
                return false;
            }
            else
            {
                canCreateConnection = true;
                return true;
            }

        }

        public void resetTowerConnections()
        {
            currentConnections = 0;
            canCreateConnection = true;

            if (dotsSprites != null)
            {
                foreach (var dot in dotsSprites)
                {
                    if (dot != null) dot.GetComponent<Image>().color = Color.white;
                }
            }
        }

        public void PunchRoundedSprite(TowerBehavior tower)
        {
            Debug.Log("Punch Starter Tower");
            Sequence seq = DOTween.Sequence();
            seq.Append(starterRoundedSprite.DOScale(targetScale, totalDuration));
            seq.Append(starterRoundedSprite.DOScale(startScale, totalDuration));

        }
    }
}
