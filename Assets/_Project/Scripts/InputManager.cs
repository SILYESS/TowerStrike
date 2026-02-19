using Solo.MOST_IN_ONE;
using Towers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Towers.TowerBehavior;
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    [Header("Prefabs")]
    public GameObject dragLinePrefab;
    public GameObject connectionPrefab;
    public GameObject cutLinePrefab;
    [Header("Settings")]
    public float groundY = 0f;
    public LayerMask towerMask;
    private Camera cam;
    private DragLineRenderer dragLine;
    private TowerBehavior selectedTower;
    // CUT SYSTEM
    private CuttingLineRenderer cutLR;
    private Vector3 lastCutPos;
    private bool isCutting = false;

    [Header("Obstacles")]
    public LayerMask obstacleMask;
    [SerializeField] private string normalhexColor = "008BFF";
    [SerializeField] private string blockedhexColor = "2D3033";
    private Color normalDragColor;
    private Color blockedDragColor;

    public bool dragBlocked = false;

    private Vector3 debugStart;
    private Vector3 debugEnd;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        cam = Camera.main;
        UnityEngine.ColorUtility.TryParseHtmlString("#" + normalhexColor, out normalDragColor);
        UnityEngine.ColorUtility.TryParseHtmlString("#" + blockedhexColor, out blockedDragColor);
    }
    private void Update()
    {
        if (!GameManager.instance.gameplayEnabled) return;

        // CRITICAL FIX: Disable legacy InputManager logic in Multiplayer
        // ConnectionManager handles input in MP.
        if (GameManager.instance.gameMode == Mode.Multiplayer) return;

        HandleInput();
    }
    #region INPUT LOGIC
    private void HandleInput()
    {
        bool pressed = InputPressed();
        bool held = InputHeld();
        bool released = InputReleased();
        Vector2 inputPos = GetPointerPosition();
        if (pressed) PointerDown(inputPos);
        if (held) PointerDrag(inputPos);
        if (released) PointerUp(inputPos);
    }
    private bool InputPressed()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }
    private bool InputHeld()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
#else
        return Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary);
#endif
    }
    private bool InputReleased()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonUp(0);
#else
        return Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled);
#endif
    }
    private Vector2 GetPointerPosition()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.mousePosition;
#else
        return Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
#endif
    }
    #endregion
    #region POINTER HANDLING
    private void PointerDown(Vector2 pos)
    {
        if (IsPointerOverUI()) return;
        TowerBehavior tower = GetTowerUnderPointer(pos);
        if (tower != null && tower.attackType == TowerBehavior.TowerAttackType.NormalTower)
        {
            // Permission check
            if (GameManager.instance.gameMode == Mode.Multiplayer)
            {
                if (tower.Object == null || !tower.Object.HasInputAuthority)
                {
                    Debug.Log("Player Does Not Have InputAuthority");
                    return;
                }
            }
            else
            {
                if (tower.type != TowerType.PlayerTower) return; // Only blue towers can start a drag
            }
            if (!tower.canCreateConnection) return;
            selectedTower = tower;
            if (dragLinePrefab != null && GameManager.instance.gameMode != Mode.Multiplayer)
            {
                GameObject obj = Instantiate(dragLinePrefab);
                dragLine = obj.GetComponent<DragLineRenderer>();
                dragLine.Setup(tower.transform.position);
            }
        }
        else
        {
            // Start CUT mode
            Vector3 w = GetInputWorldPoint();
            if (cutLinePrefab != null)
            {
                GameObject obj = Instantiate(cutLinePrefab);
                cutLR = obj.GetComponent<CuttingLineRenderer>();
                cutLR.Init(w);
            }
            lastCutPos = w;
            isCutting = true;
        }
    }
    private void PointerDrag(Vector2 pos)
    {
        if (selectedTower != null)
        {


            if (GameManager.instance.gameMode != Mode.Multiplayer)
            {
                //Vector3 dragPos = GetInputWorldPoint();
                Vector3 dragPos = GetInputWorldPoint();
                TowerBehavior snapTower = GetTowerUnderPointer(pos);

                if (snapTower != null && snapTower != selectedTower)
                {
                    dragPos = snapTower.transform.position;
                }
                // Debug points for Scene View
                debugStart = selectedTower.transform.position;
                debugEnd = dragPos;

                // Check obstacle between start → drag point
                Vector3 dir = (dragPos - debugStart).normalized;
                float dist = Vector3.Distance(debugStart, dragPos);
                if (Physics.Raycast(debugStart, dir, out RaycastHit hit, dist, obstacleMask))
                {
                    // BLOCKED
                    dragBlocked = true;
                    dragLine?.SetColor(blockedDragColor);
                }
                else
                {
                    // CLEAR
                    dragBlocked = false;
                    dragLine?.SetColor(normalDragColor);
                }

                dragLine?.SetEnd(dragPos);
                return;
            }



        }
        if (isCutting && cutLR != null)
        {
            Vector3 w = GetInputWorldPoint();
            cutLR.UpdateEnd(w);
            CheckLocalCut(lastCutPos, w);
            lastCutPos = w;
        }
    }
    private void PointerUp(Vector2 pos)
    {
        if (selectedTower != null)
        {
            TowerBehavior target = GetTowerUnderPointer(pos);
            if (target != null && target != selectedTower)
            {
                if (!dragBlocked)
                {
                    if (GameManager.instance.gameMode == Mode.Multiplayer)
                    {
                        // NETWORKED connection
                        if (Towers.ConnectionManager.Instance != null)
                            Towers.ConnectionManager.Instance.RpcRequestCreateConnection(selectedTower.name, target.name);
                        //selectedTower.RPC_CreateConnection(selectedTower, target);
                    }
                    else
                    {
                        // SINGLE PLAYER connection
                        CreateConnection(selectedTower, target);
                        if (LevelManager.Instance.currentLevel == 0)
                        {
                            if (GameManager.instance.firstText.activeInHierarchy)
                            {
                                GameManager.instance.firstText.SetActive(false);
                                GameManager.instance.pointerMove.SetActive(false);
                            }
                        }
                    }
                }


            }
            if (dragLine != null) Destroy(dragLine.gameObject);
            selectedTower = null;
        }
        if (isCutting)
        {
            isCutting = false;
            if (cutLR != null) Destroy(cutLR.gameObject);
        }
    }
    #endregion
    #region HELPERS
    private TowerBehavior GetTowerUnderPointer(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, towerMask))
        {
            if (hit.collider.GetComponent<TowerBehavior>())
            {

                return hit.collider.GetComponent<TowerBehavior>();
            }

        }

        return null;
    }
    private Vector3 GetInputWorldPoint()
    {
        Ray ray = cam.ScreenPointToRay(GetPointerPosition());
        Plane plane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector3.zero;
    }
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return false;
#endif
    }
    #endregion
    #region CONNECTION LOGIC
    public void CreateConnection(TowerBehavior from, TowerBehavior to)
    {
        if (from == null || to == null || from == to) return;
        if (from.linkedTowers.Contains(to)) return;
        // Only allow blue towers to connect
        if (from.type != TowerType.PlayerTower) return;
        // Find if existing lr between them (ignore direction)
        LrBehavior existingLr = null;
        if (LevelManager.Instance != null)
        {
            foreach (var lr in LevelManager.Instance.allLines)
            {
                if (lr == null) continue;
                var t1 = lr.StartPoint != null ? lr.StartPoint.GetComponentInParent<TowerBehavior>() : null;
                var t2 = lr.EndPoint != null ? lr.EndPoint.GetComponentInParent<TowerBehavior>() : null;
                if (t1 == null || t2 == null) continue;
                if ((t1 == from && t2 == to) || (t1 == to && t2 == from))
                {
                    existingLr = lr;
                    break;
                }
            }
        }
        if (existingLr != null)
        {
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
            existingLr.isIntersect = true;
            // Add the link (make bidirectional if not already)
            from.linkedTowers.Add(to);
            from.currentConnections++;
            from.SetAttackEnabled(true);
            from.CheckConnectionPossibility();
        }
        else
        {
            // Create new as usual
            GameObject obj = Instantiate(connectionPrefab);
            LrBehavior lr = obj.GetComponent<LrBehavior>();
            Debug.Log("Create Connection");
            lr.SetTargets(from.connectionPoint, to.connectionPoint);
            lr.SetColor(LineColors.Get(from.type));
            from.linkedTowers.Add(to);
            from.currentConnections++;
            from.lastSpawnTime = from.spawnCooldown;
            from.SetAttackEnabled(true);
            from.CheckConnectionPossibility();

            SwitchDotsColor(from);


        }
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
    }
    private void RemoveExistingLine(TowerBehavior a, TowerBehavior b)
    {
        if (LevelManager.Instance == null) return;
        foreach (var lr in LevelManager.Instance.allLines)
        {
            if (lr == null) continue;
            var t1 = lr.StartPoint != null ? lr.StartPoint.GetComponentInParent<TowerBehavior>() : null;
            var t2 = lr.EndPoint != null ? lr.EndPoint.GetComponentInParent<TowerBehavior>() : null;
            if (t1 == null || t2 == null) continue;
            if ((t1 == a && t2 == b) || (t1 == b && t2 == a))
            {
                t1.linkedTowers.Remove(t2);
                t2.linkedTowers.Remove(t1);
                DestroySoldiersOnConnection(t1, t2);
                Destroy(lr.gameObject);
                break;
            }
        }
    }


    #endregion
    #region CUT SYSTEM
    private void CheckLocalCut(Vector3 p1, Vector3 p2)
    {
        Debug.Log("Check Cut");
        if (LevelManager.Instance == null) return;

        Vector2 s1 = cam.WorldToScreenPoint(p1);
        Vector2 s2 = cam.WorldToScreenPoint(p2);
        
        // Iterate over a copy or iterate safely if we destroy inside loop
        // However, RemoveConnection destroys the LR object.
        // So we should break after finding one or iterate backwards if multiple cuts allowed (usually single cut per frame is fine or safer).
        // The original code uses 'break' after first cut.
        
        foreach (var lr in LevelManager.Instance.allLines)
        {
            if (lr == null || lr.StartPoint == null || lr.EndPoint == null) continue;

            Vector2 a = cam.WorldToScreenPoint(lr.StartPoint.position);
            Vector2 b = cam.WorldToScreenPoint(lr.EndPoint.position);
            if (LinesIntersect(s1, s2, a, b))
            {
                TowerBehavior t1 = lr.StartPoint.GetComponentInParent<TowerBehavior>();
                TowerBehavior t2 = lr.EndPoint.GetComponentInParent<TowerBehavior>();

                if (t1 != null && t2 != null)
                {
                    bool t1IsPlayer = t1.type == TowerType.PlayerTower;
                    bool t2IsPlayer = t2.type == TowerType.PlayerTower;

                    // If either is player, check if they have a connection to remove
                    if ((t1IsPlayer && t1.linkedTowers.Contains(t2)) ||
                        (t2IsPlayer && t2.linkedTowers.Contains(t1)))
                    {
                        RemoveConnection(lr);
                        break;
                    }
                }
            }
        }
    }
    private void RemoveConnection(LrBehavior lr)
    {
        TowerBehavior startNode = lr.StartPoint.GetComponentInParent<TowerBehavior>();
        TowerBehavior endNode = lr.EndPoint.GetComponentInParent<TowerBehavior>();

        Debug.Log("Start Cut");
        if (startNode != null && endNode != null)
        {
            // Identify Player and Enemy (if applicable)
            TowerBehavior playerTower = null;
            TowerBehavior otherTower = null;

            if (startNode.type == TowerType.PlayerTower) { playerTower = startNode; otherTower = endNode; }
            else if (endNode.type == TowerType.PlayerTower) { playerTower = endNode; otherTower = startNode; }

            // If no player involved (shouldn't happen due to checks), just standard destroy
            if (playerTower == null)
            {
                // Fallback for safety
                Destroy(lr.gameObject);
                return;
            }

            // Check if Bidirectional (Intersect)
            bool isBidirectional = startNode.linkedTowers.Contains(endNode) && endNode.linkedTowers.Contains(startNode);

            if (isBidirectional)
            {
                Debug.Log("Cut Intersect Connection (One Side)");

                // 1. Remove ONLY Player -> Other connection
                playerTower.linkedTowers.Remove(otherTower);
                playerTower.currentConnections--;
                playerTower.CheckConnectionPossibility();
                SwitchDotsColor(playerTower);

                // 2. Kill only Player soldiers (Player -> Other)
                DestroySoldiersDirectional(playerTower, otherTower);

                // 3. Update Visuals to Enemy (Other) Line
                lr.isIntersect = false;

                // IMPORTANT: Ensure StartPoint is the OtherTower (Enemy)
                // This makes the remaining line "owned" by the enemy
                lr.StartPoint = otherTower.connectionPoint != null ? otherTower.connectionPoint : otherTower.transform;
                lr.EndPoint = playerTower.connectionPoint != null ? playerTower.connectionPoint : playerTower.transform;

                // Set color to Enemy (Other)
                lr.SetColor(LineColors.Get(otherTower.type));

                // Do NOT destroy the line object
                return;
            }
            else
            {
                Debug.Log("Cut Full Connection");

                if (LevelManager.Instance.currentLevel == 0)
                {
                    if (GameManager.instance.secondText.activeInHierarchy)
                    {
                        GameManager.instance.secondText.SetActive(false);
                        GameManager.instance.pointerCut.SetActive(false);
                        GameManager.instance.thirdText.SetActive(true);
                    }
                }
                // Remove forward and backward (standard safety)
                if (startNode.linkedTowers.Contains(endNode))
                {
                    startNode.linkedTowers.Remove(endNode);
                    startNode.currentConnections--;
                    SwitchDotsColor(startNode);
                }
                if (endNode.linkedTowers.Contains(startNode))
                {
                    endNode.linkedTowers.Remove(startNode);
                    endNode.currentConnections--;
                    SwitchDotsColor(endNode);
                }

                startNode.CheckConnectionPossibility();
                endNode.CheckConnectionPossibility(); // Check both

                DestroySoldiersOnConnection(startNode, endNode);
            }
        }
        Destroy(lr.gameObject);
    }

    public void DestroySoldiersOnConnection(TowerBehavior a, TowerBehavior b)
    {
        if (LevelManager.Instance == null) return;
        float arrivalThreshold = 0.5f;
        // Use global list
        var soldiers = LevelManager.Instance.allSoldiers;
        
        for (int i = soldiers.Count - 1; i >= 0; i--)
        {
            var s = soldiers[i];
            if (s == null) continue;
            if ((s.sourceTower == a && s.targetTower == b) || (s.sourceTower == b && s.targetTower == a))
            {
                if (Vector3.Distance(s.transform.position, s.targetTower.transform.position) > arrivalThreshold)
                {
                    if (GameManager.instance.gameMode == Mode.Single)
                        Utils.SimplePool.Return(s.gameObject);
                    else
                    {
                        var runner = Network.MultiplayerManager.Instance != null ? Network.MultiplayerManager.Instance.runner : null;
                        if (runner != null && s.Object != null && s.Object.IsValid)
                            runner.Despawn(s.Object);
                        else
                            Destroy(s.gameObject);
                    }
                }
            }
        }
    }

    public void DestroySoldiersDirectional(TowerBehavior from, TowerBehavior to)
    {
        if (LevelManager.Instance == null) return;
        float arrivalThreshold = 0.5f;
        // Use global list
        var soldiers = LevelManager.Instance.allSoldiers;

        for (int i = soldiers.Count - 1; i >= 0; i--)
        {
            var s = soldiers[i];
            if (s == null) continue;
            // Only destroy soldiers moving FROM -> TO
            if (s.sourceTower == from && s.targetTower == to)
            {
                if (Vector3.Distance(s.transform.position, s.targetTower.transform.position) > arrivalThreshold)
                {
                    if (GameManager.instance.gameMode == Mode.Single)
                        Utils.SimplePool.Return(s.gameObject);
                    else
                    {
                        var runner = Network.MultiplayerManager.Instance != null ? Network.MultiplayerManager.Instance.runner : null;
                        if (runner != null && s.Object != null && s.Object.IsValid)
                            runner.Despawn(s.Object);
                        else
                            Destroy(s.gameObject);
                    }
                }
            }
        }
    }
    private bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float s1x = p2.x - p1.x;
        float s1y = p2.y - p1.y;
        float s2x = q2.x - q1.x;
        float s2y = q2.y - q1.y;
        float denom = (-s2x * s1y + s1x * s2y);
        if (Mathf.Approximately(denom, 0f)) return false;
        float s = (-s1y * (p1.x - q1.x) + s1x * (p1.y - q1.y)) / denom;
        float t = (s2x * (p1.y - q1.y) - s2y * (p1.x - q1.x)) / denom;
        return (s >= 0 && s <= 1 && t >= 0 && t <= 1);
    }

    public void SwitchDotsColor(TowerBehavior tower)
    {
        foreach (var item in tower.dotsSprites)
        {
            item.GetComponent<Image>().color = Color.white;
        }
        for (int i = 0; i < tower.currentConnections; i++)
        {
            tower.dotsSprites[i].GetComponent<Image>().color = Color.grey;
        }
    }
    #endregion
}
