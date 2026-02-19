using DG.Tweening;
using Fusion;
using System.Collections.Generic;
//using System.Drawing;
using Towers;
using UnityEngine;

namespace Soldier
{
    [RequireComponent(typeof(NetworkObject))]
    public class SoldierBehavior : NetworkBehaviour
    {
        [Header("Settings")]
        public float attackRange = 0.5f;
        public int attackDamage = 1;
        public float moveSpeed = 3f;
        private Animator animator;

        [Header("References")]
        public TowerBehavior sourceTower;
        public TowerBehavior targetTower;
        public SkinnedMeshRenderer skinnedRenderer;

        // Networked Variables
        [Networked] public Vector3 targetPosition { get; set; }
        [Networked] public TowerType solderColor { get; set; }
        public TowerType soldierColor_NormalMode;
        public TowerType soldierCurrentColor;
        public bool isInitialized;
        [Networked] public NetworkBool initialized { get; set; }
        [Networked] public Vector3 NetworkPos { get; set; }
        [Networked] public NetworkBool IsDead { get; set; }

        private bool IsNetworkMode => Runner != null;
        private ChangeDetector _changes;

        // Visuals
        private List<Material> soldierColorMaterials = new List<Material>();
        public bool isCollidedWithSoldier;

        // Safety & Local Cache
        public bool IsSpawned { get; private set; } = false;
        private TowerType _cachedLocalSolderColor; // Local cache to avoid touching Network memory in physics
        public bool startScale;
        // Tween
        private Tween moveTween;
        private bool _cancelAttack;

        public override void Spawned()
        {
            IsSpawned = true;
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (GameManager.instance.gameMode == Mode.Multiplayer)
            {
                // Sync local cache
                _cachedLocalSolderColor = solderColor;
            }
            else
            {
                // Sync local cache
                _cachedLocalSolderColor = soldierColor_NormalMode;
            }


            if (Object.HasStateAuthority)
            {
                NetworkPos = transform.position;
                IsDead = false;
            }

            UpdateVisual();
        }



        private void Start()
        {
            animator = GetComponent<Animator>();
            if (animator) animator.speed = moveSpeed / 5f;
        }

        public void Initialize(TowerType color, TowerBehavior source, TowerBehavior target)
        {
            sourceTower = source;
            targetTower = target;

            if (GameManager.instance.gameMode == Mode.Multiplayer)
            {
                solderColor = color;
                _cachedLocalSolderColor = color; // Verify local cache set immediately
                initialized = true;
            }
            else
            {
                soldierColor_NormalMode = color;
                _cachedLocalSolderColor = color; // Verify local cache set immediately
                isInitialized = true;
            }



            string typeString = color.ToString();
            string layerName = typeString.Replace("Tower", "Soldier");
            gameObject.layer = LayerMask.NameToLayer(layerName);
            var spawnCol = GetComponent<Collider>();
            if (spawnCol)
            {
                spawnCol.enabled = true;
                spawnCol.isTrigger = true;
            }
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one * 1.5f, 0.3f).OnComplete(() =>
            {
                var c = GetComponent<Collider>();
                if (c) c.isTrigger = false;
            });

            Vector3 spawnPos = sourceTower != null && sourceTower.connectionPoint != null
                ? sourceTower.connectionPoint.position
                : sourceTower != null ? sourceTower.transform.position : transform.position;
            transform.position = spawnPos;

            if (IsNetworkMode)
            {
                if (!Object.HasStateAuthority) return;

                targetPosition = target.connectionPoint.position;
                if (Object.IsValid) NetworkPos = spawnPos;

                StartTweenMovement(targetPosition);
            }
            else
            {
                if (targetTower == null) { Destroy(gameObject); return; }
                UpdateVisual();

                StartTweenMovement(targetTower.connectionPoint.position);
            }
        }

        private void StartTweenMovement(Vector3 dest)
        {
            if (moveTween != null) moveTween.Kill();
            _cancelAttack = false;
            transform.LookAt(dest);
            float dist = Vector3.Distance(transform.position, dest);
            float duration = dist / moveSpeed;

            moveTween = transform.DOMove(dest, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (IsNetworkMode)
                    {
                        if (Object.HasStateAuthority && !_cancelAttack && gameObject.activeInHierarchy) Attack();
                    }
                    else
                    {
                        if (!_cancelAttack && gameObject.activeInHierarchy) Attack();
                    }
                });
        }

        private void MoveTowardTarget(bool network)
        {
            if (isCollidedWithSoldier)
                return;

            if (IsNetworkMode && IsDead) return;

            if (targetTower == null)
            {
                Debug.Log("Destroy Tower Target 1");
                SafeDestroy(network);
                return;
            }

            Vector3 dest = network ? targetPosition : targetTower.connectionPoint.position;



            if (Vector3.Distance(transform.position, dest) < (attackRange * 2))
            {
                //Debug.Log("Enter Safe Area");
                gameObject.layer = 0;

            }
            if (!network)
            {
                if (Vector3.Distance(transform.position, dest) < (attackRange * 2.75f) && !startScale)
                {
                    //Debug.Log("Do Scale");
                    transform.DOScale(Vector3.zero, 0.5f);
                    startScale = true;
                }
            }

        }

        private void SafeDestroy(bool network)
        {
            _cancelAttack = true;
            if (moveTween != null) moveTween.Kill();
            if (network)
            {
                if (Runner != null && Object != null && Object.IsValid)
                    Runner.Despawn(Object);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Death Timer for Network Mode
        private float _deathTimer;

        public override void FixedUpdateNetwork()
        {
            if (!IsNetworkMode || !Object.IsValid || !Object.HasStateAuthority) return;
            if (!IsSpawned || !initialized) return;

            if (IsDead)
            {
                _deathTimer += Runner.DeltaTime;
                if (_deathTimer >= 1.2f)
                {
                    Runner.Despawn(Object);
                }
                return;
            }

            MoveTowardTarget(true);
            NetworkPos = transform.position;
        }

        public void Update()
        {
            if (GameManager.instance.gameMode == Mode.Single && isInitialized)
                MoveTowardTarget(false);
        }

        public override void Render()
        {
            if (!Object.IsValid) return;
            // On clients, verify IsSpawned. If not (rare race?), skip.
            if (IsNetworkMode && !IsSpawned) return;

            if (IsNetworkMode && !Object.HasStateAuthority)
            {
                float t = Runner.DeltaTime * 10f;
                transform.position = Vector3.Lerp(transform.position, NetworkPos, t);

                if (targetPosition != Vector3.zero)
                {
                    Vector3 dir = (targetPosition - transform.position);
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        var rot = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
                    }
                }
            }

            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(solderColor))
                {
                    if (GameManager.instance.gameMode == Mode.Multiplayer)
                    {
                        _cachedLocalSolderColor = solderColor; // Update local cache
                        UpdateVisual();
                    }
                    else
                    {
                        _cachedLocalSolderColor = soldierColor_NormalMode; // Update local cache
                        UpdateVisual();
                    }
                }
                else if (change == nameof(IsDead))
                {
                    if (IsDead) Soldierhitbehaviour();
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (moveTween != null) moveTween.Kill();
            IsSpawned = false;
            base.Despawned(runner, hasState);
        }

        private void OnDestroy()
        {
            if (moveTween != null) moveTween.Kill();
        }

        private void OnEnable()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.allSoldiers.Add(this);

            isCollidedWithSoldier = false;
            startScale = false;
            var col = GetComponent<Collider>();
            if (col) col.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Default");
            if (animator) animator.SetBool("fall", false);
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.allSoldiers.Remove(this);
            _cancelAttack = true;
            if (moveTween != null) moveTween.Kill();
        }

        private void Attack()
        {
            if (_cancelAttack || !gameObject.activeInHierarchy) return;
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;

            if (IsNetworkMode && Object.HasStateAuthority)
            {
                if (!Object.IsValid) return;

                if (_overlapBuffer == null || _overlapBuffer.Length != 16) _overlapBuffer = new Collider[16];
                int count = Physics.OverlapSphereNonAlloc(transform.position, 1.0f, _overlapBuffer);
                for (int i = 0; i < count; i++)
                {
                    var tower = _overlapBuffer[i].GetComponent<TowerBehavior>();
                    if (tower != null)
                    {
                        if (GameManager.instance.gameMode == Mode.Multiplayer)
                        {
                            tower.TakeDamage(attackDamage, solderColor, sourceTower);
                        }
                        else
                        {
                            tower.TakeDamage(attackDamage, soldierColor_NormalMode, sourceTower);
                        }

                        break;
                    }
                }
                Runner.Despawn(Object);
            }
            else if (!IsNetworkMode)
            {
                if (targetTower != null)
                {
                    if (GameManager.instance.gameMode == Mode.Multiplayer)
                    {
                        targetTower.TakeDamage(attackDamage, solderColor, sourceTower);
                    }
                    else
                    {
                        targetTower.TakeDamage(attackDamage, soldierColor_NormalMode, sourceTower);
                    }

                }

                Utils.SimplePool.Return(gameObject);
            }
        }
        private static Collider[] _overlapBuffer;

        public void Soldierhitbehaviour()
        {
            isCollidedWithSoldier = true;
            _cancelAttack = true;
            if (moveTween != null) moveTween.Kill();
            if (animator) animator.SetBool("fall", true);
            var col = GetComponent<Collider>();
            if (col) col.enabled = false; // Disable collider to prevent further interactions
            gameObject.layer = 0;

            if (!IsNetworkMode)
                Utils.SimplePool.ReturnDelayed(gameObject, 1.2f);
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Utils.SimplePool.Return(gameObject);
        }

        // Helper to safely get color from other soldier
        public bool TryGetOtherColor(SoldierBehavior other, out TowerType color)
        {
            color = TowerType.GreyTower;
            if (other == null) return false;

            // Use the local cache for BOTH Single and Multiplayer to avoid Network Property access issues
            // This is safe because _cachedLocalSolderColor is set in Spawned() and Updated in Render()
            // When OnCollisionEnter runs, the other object MUST be sufficiently initialized for the collider to be active.
            // If it's a proxy not yet spawned, we might get stale data, but OnCollision implies it exists in physics.

            // However, to be ultra safe for the bug report:
            if (IsNetworkMode)
            {
                if (!other.IsSpawned) return false;
                // We use the cached value on the remote object too.
                // Assuming _cachedLocalSolderColor is public? No, it's private.
                // We need to access it. Modifying accessibility might be needed or using a public accessor.
                // Wait, I can't see the private field of another instance strictly speaking if I respect encapsulation, but internal to class is fine.
                // But _cachedLocalSolderColor is private in the original code. I need to make sure I can access it.
                // The original code passed 'other.solderColor'.
                // I need to change _cachedLocalSolderColor to internal or public OR add a public getter.
                // For now, I will assume I can access it because I am in the same class type.
                color = other._cachedLocalSolderColor;
                return true;
            }
            else
            {
                color = other._cachedLocalSolderColor;
                return true;
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            // use local cache 'IsSpawned' for safety
            if (IsNetworkMode && (!IsSpawned || IsDead)) return;
            if (!IsNetworkMode && isCollidedWithSoldier) return; // Dead/Dying ignore

            if (collision.gameObject.CompareTag("Soldier"))
            {
                var other = collision.gameObject.GetComponent<SoldierBehavior>();
                if (other == null) return;

                if (!TryGetOtherColor(other, out TowerType otherColor))
                {
                    return;
                }

                if (otherColor == _cachedLocalSolderColor) // Allies
                {
                    Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                    return;
                }
                else // Enemies
                {
                    // Mutual Death
                    if (IsNetworkMode && Object.HasStateAuthority)
                    {
                        // Set Self Dead
                        IsDead = true;

                        // Set Enemy Dead (if I have authority over them? No, usually I don't)
                        // If I am Host and I own both (e.g. one is Player, one is AI?), I can kill both.
                        // But usually Client owns their soldier.
                        // Implication: Each client detects the collision and kills their OWN soldier?
                        // Or Authoritative Server kills both?
                        // If Host owns Player 1 and Client owns Player 2:
                        // Host detects collision: Kill P1. Can Host kill P2? Only if Host has State Authority over P2? 
                        // Usually Players own their units (Input Authority) but Host has State Authority.
                        // In Fusion, usually Server has State Authority over everything unless Client Authority is used.
                        // If Server has State Authority, it can kill both.

                        if (other.Object != null && other.Object.IsValid && other.Object.HasStateAuthority)
                        {
                            other.IsDead = true;
                        }
                    }
                    else if (!IsNetworkMode)
                    {
                        if (GameManager.instance.gameMode == Mode.Single)
                        {
                            Soldierhitbehaviour();
                        }
                        else
                        {
                            Soldierhitbehaviour();
                            other.Soldierhitbehaviour();
                        }

                    }
                }
            }
        }

        private MaterialPropertyBlock _mpb;

        private void UpdateVisual()
        {
            Color selected = Color.white;

            // Safe Visual Logic: Use _cachedLocalSolderColor if available?
            // Actually _cachedLocalSolderColor is updated in Render from Network var.
            // On Spawn, it is synced.

            TowerType displayType = _cachedLocalSolderColor;

            if (IsNetworkMode)
            {
                var mm = Network.MultiplayerManager.Instance;
                if (mm != null && mm.runner != null && mm.runner.IsRunning)
                {
                    bool amIHost = (mm.runner.LocalPlayer == mm.HostPlayer);
                    if (displayType == TowerType.PlayerTower) selected = amIHost ? Color.blue : Color.red;
                    else if (displayType == TowerType.RedTower) selected = amIHost ? Color.red : Color.blue;
                    else selected = Color.grey;
                }
                else selected = Color.grey;
            }
            else
            {
                selected = displayType switch
                {
                    TowerType.GreyTower => Color.grey,
                    TowerType.PlayerTower => Color.blue,
                    TowerType.RedTower => Color.red,
                    TowerType.YellowTower => Color.yellow,
                    TowerType.GreenTower => Color.green,
                    _ => Color.white,
                };
            }

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var rend in renderers)
            {
                _mpb.Clear();
                _mpb.SetColor("_Color", selected);
                _mpb.SetColor("_BaseColor", selected);
                rend.SetPropertyBlock(_mpb);
            }
        }
    }
}
