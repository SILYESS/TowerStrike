using System.Collections;
using System.Collections.Generic;
using Towers;
using UnityEngine;

public class AITowerController : MonoBehaviour
{
    [Header("AI Settings")]
    public float searchRadius = 50f;
    public float attackInterval = 2f;
    public float initialDelay = 2f;

    [Header("AI Connection Delay")]
    [SerializeField] private float minConnectionDelay = 0.3f;
    [SerializeField] private float maxConnectionDelay = 1.2f;

    [Header("References")]
    public GameObject connectionPrefab;
    public InputManager inputManager;

    [Header("Obstacles")]
    public LayerMask obstacleMask;

    public List<TowerBehavior> aiTowers = new List<TowerBehavior>();
    public bool isRunning = false;

    public static AITowerController Instance;

    // Prevent multiple delayed connections per tower
    private readonly HashSet<TowerBehavior> pendingConnections = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (inputManager == null)
            inputManager = FindObjectOfType<InputManager>();

        //StartCoroutine(InitializeAI());
    }

    public void StartAI()
    {
        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(AIBehaviorLoop());
        }
    }

    public IEnumerator InitializeAI()
    {
        yield return new WaitForSeconds(initialDelay);

        FindAITowers();

        if (aiTowers.Count > 0)
        {
            foreach (var tower in aiTowers)
                tower.SetAttackEnabled(true);

            isRunning = true;
            StartCoroutine(AIBehaviorLoop());
        }
    }


    public void FindAITowers()
    {
        aiTowers.Clear();
        if (LevelManager.Instance == null) return;

        foreach (var tower in LevelManager.Instance.allTowers)
        {
            if (tower.type == TowerType.RedTower ||
                tower.type == TowerType.YellowTower ||
                tower.type == TowerType.GreenTower)
            {
                aiTowers.Add(tower);
            }
        }
    }

    private IEnumerator AIBehaviorLoop()
    {
        while (isRunning && GameManager.instance.gameplayEnabled)
        {
            Debug.Log("AIBehaviorLoop");
            // Remove invalid or converted towers
            aiTowers.RemoveAll(t =>
                t == null ||
                !t.gameObject.activeInHierarchy ||
                t.type == TowerType.PlayerTower ||
                t.type == TowerType.GreyTower
            );

            foreach (var aiTower in aiTowers)
            {
                if (aiTower == null || !aiTower.gameObject.activeInHierarchy)
                    continue;

                if (aiTower.type == TowerType.PlayerTower || aiTower.type == TowerType.GreyTower)
                {
                    aiTower.SetAttackEnabled(false);
                    continue;
                }

                TowerBehavior target = FindNearestEnemyTower(aiTower);

                if (target != null &&
                    !IsAlreadyConnected(aiTower, target) &&
                    !pendingConnections.Contains(aiTower) &&
                    aiTower.CheckConnectionPossibility())
                {
                    StartCoroutine(DelayedCreateAIConnection(aiTower, target));
                }
            }

            yield return new WaitForSeconds(attackInterval);
        }
    }

    private IEnumerator DelayedCreateAIConnection(TowerBehavior from, TowerBehavior to)
    {
        pendingConnections.Add(from);

        float baseDelay = Random.Range(minConnectionDelay, maxConnectionDelay);
        float levelFactor = Mathf.Clamp((float)from.CurrentHealth / 30f, 0f, 1f);
        float thinkMultiplier = Mathf.Lerp(1.25f, 0.85f, levelFactor);
        float delay = baseDelay * thinkMultiplier;
        yield return new WaitForSeconds(delay);

        // Revalidate after delay
        if (from == null || to == null)
            goto Cleanup;

        if (!from.gameObject.activeInHierarchy)
            goto Cleanup;

        if (from.type == TowerType.PlayerTower || from.type == TowerType.GreyTower)
            goto Cleanup;

        if (IsAlreadyConnected(from, to))
            goto Cleanup;

        if (!from.CheckConnectionPossibility())
            goto Cleanup;

        CreateAIConnection(from, to);

    Cleanup:
        pendingConnections.Remove(from);
    }

    private TowerBehavior FindNearestEnemyTower(TowerBehavior aiTower)
    {
        TowerBehavior nearest = null;
        float minDistance = searchRadius;

        if (LevelManager.Instance == null) return null;

        foreach (var tower in LevelManager.Instance.allTowers)
        {
            if (tower == null || tower == aiTower)
                continue;

            // Skip AI towers
            if (tower.type == TowerType.RedTower ||
                tower.type == TowerType.YellowTower ||
                tower.type == TowerType.GreenTower)
                continue;

            Vector3 start = aiTower.transform.position;
            Vector3 end = tower.transform.position;
            Vector3 direction = (end - start).normalized;
            float dist = Vector3.Distance(start, end);

            if (!Physics.Raycast(start, direction, dist, obstacleMask))
            {
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = tower;
                }
            }
        }

        return nearest;
    }

    private bool IsAlreadyConnected(TowerBehavior from, TowerBehavior to)
    {
        return from.linkedTowers != null && from.linkedTowers.Contains(to);
    }

    private void CreateAIConnection(TowerBehavior from, TowerBehavior to)
    {
        if (from == null || to == null || from == to || from.attackType == TowerBehavior.TowerAttackType.AttackerTower)
            return;

        if (IsAlreadyConnected(from, to))
            return;
        LrBehavior existingLr = null;
        if (LevelManager.Instance != null)
        {
            foreach (var llr in LevelManager.Instance.allLines)
            {
                if (llr == null) continue;
                var t1 = llr.StartPoint != null ? llr.StartPoint.GetComponentInParent<TowerBehavior>() : null;
                var t2 = llr.EndPoint != null ? llr.EndPoint.GetComponentInParent<TowerBehavior>() : null;
                if (t1 == null || t2 == null) continue;
                if ((t1 == from && t2 == to) || (t1 == to && t2 == from))
                {
                    Debug.Log("There Is AI Intersection");
                    existingLr = llr;
                    break;
                }
            }
        }
        if (existingLr ==  null)
        {
            GameObject obj = Instantiate(connectionPrefab);
            LrBehavior lr = obj.GetComponent<LrBehavior>();

            if (lr != null)
            {
                lr.SetTargets(from.connectionPoint, to.connectionPoint);
                lr.SetColor(LineColors.Get(from.type));

                from.lastSpawnTime = from.spawnCooldown;
                from.SetAttackEnabled(true);
            }
            else
            {
                Debug.LogError("LrBehavior missing on connectionPrefab!");
            }

            if (from.linkedTowers == null)
                from.linkedTowers = new List<TowerBehavior>();

            from.linkedTowers.Add(to);
        }
        else
        {
            Debug.Log("Create AI Intersection");
            // Update existing to two colors (merge into bidirectional)
            TowerBehavior originalStart = existingLr.StartPoint.GetComponentInParent<TowerBehavior>();
            Color color1 = LineColors.Get(originalStart.type);
            Color color2 = LineColors.Get(from.type);
            Debug.Log("Create Intersect Connection");
            // If directions don't match, reverse points to make player the start
            if (originalStart != from)
            {
                Transform temp = existingLr.StartPoint;
                existingLr.StartPoint = existingLr.EndPoint;
                existingLr.EndPoint = temp;
                existingLr.SetTwoColors(LineColors.Get(from.type), LineColors.Get(to.type));
            }
            else
            {
                existingLr.SetTwoColors(color1, color2);
            }
            // Add the link (make bidirectional if not already)
            from.linkedTowers.Add(to);
            from.currentConnections++;
            from.SetAttackEnabled(true);
        }
        
    }

    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
        pendingConnections.Clear();
    }

    public void RestartAI()
    {
        StopAI();
        StartCoroutine(InitializeAI());
    }

    private void OnDisable()
    {
        StopAI();
    }

    public void RefreshAI()
    {
        StopAllCoroutines();
        pendingConnections.Clear();
        isRunning = false;
        StartCoroutine(InitializeAI());
    }

    public void OnTowerConverted(TowerBehavior tower)
    {
        if (aiTowers.Contains(tower))
        {
            aiTowers.Remove(tower);
            if (tower.attackType != TowerBehavior.TowerAttackType.AttackerTower)
            tower.SetAttackEnabled(false);
            BreakAllConnectionsFrom(tower);
        }
    }

    private void BreakAllConnectionsFrom(TowerBehavior tower)
    {
        if (tower == null || LevelManager.Instance == null)
            return;

        // Iterate backwards or use a copy if we might destroy objects, though here we mostly call inputManager or modify color.
        // But if InputManager destroys lines, we should be careful. 
        // DestroySoldiersOnConnection calls FindObjectsOfType<Soldier> in InputManager, we will fix that next.
        // Here we just iterate lines.
        
        var lines = LevelManager.Instance.allLines;

        foreach (var lr in lines)
        {
            if (lr == null) continue;

            var start = lr.StartPoint != null ? lr.StartPoint.GetComponentInParent<TowerBehavior>() : null;
            var end = lr.EndPoint != null ? lr.EndPoint.GetComponentInParent<TowerBehavior>() : null;

            bool outgoing = (start == tower);
            bool incoming = (end == tower);

            if (!outgoing && !incoming)
                continue;

            if (outgoing)
            {
                inputManager.DestroySoldiersOnConnection(start, end);
                //Destroy(lr.gameObject);
                lr.SetColor(LineColors.Get(start.type));
                continue;
            }

            if (incoming)
            {
                if (start != null)
                {
                    lr.SetColor(LineColors.Get(start.type));
                }
                else
                {
                    inputManager.DestroySoldiersOnConnection(start, end);
                }
            }
        }

        tower.linkedTowers.Clear();
    }

    public void ClearConnectionsOnExit()
    {
        foreach (var item in aiTowers)
        {
            BreakAllConnectionsFrom(item);
        }
    }
}
