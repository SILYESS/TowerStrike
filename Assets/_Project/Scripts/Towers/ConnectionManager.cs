using Fusion;
using Soldier;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Towers
{
    [RequireComponent(typeof(NetworkObject))]
    public class ConnectionManager : NetworkBehaviour
    {
        public static ConnectionManager Instance;

        [Header("Prefabs")]
        public GameObject linePrefab;
        public GameObject cutLinePrefab; // Added for Cut Feature
        public DragLineRenderer currentLineDragged;
        public NetworkObject soldierPrefab;

        private Camera cam;

        private List<Connection> connections = new List<Connection>();

        private TowerBehavior startTower = null;
        private LineRenderer previewLine = null;
        private bool isDragging = false;
        public bool haveObstacle = false;
        private Vector3 debugStart = Vector3.one;
        private Vector3 debugEnd = Vector3.one;
        public static bool isConnectingMode = true;
        public float groundY = 20f;
        private TowerBehavior selectedTower;

        // CUT VARIABLES
        private CuttingLineRenderer cutLR;
        private Vector3 lastCutPos;
        private bool isCutting = false;

        public LayerMask obstacleMask;
        [SerializeField] private string normalhexColor = "008BFF";
        [SerializeField] private string blockedhexColor = "2D3033";
        private Color normalDragColor;
        private Color blockedDragColor;

        public override void Spawned()
        {
            Instance = this;
            // Fallback: Get Prefabs from InputManager if not assigned
            if (linePrefab == null && InputManager.Instance != null) linePrefab = InputManager.Instance.connectionPrefab;
            if (cutLinePrefab == null && InputManager.Instance != null) cutLinePrefab = InputManager.Instance.cutLinePrefab;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            cam = Camera.main;
            UnityEngine.ColorUtility.TryParseHtmlString("#" + normalhexColor, out normalDragColor);
            UnityEngine.ColorUtility.TryParseHtmlString("#" + blockedhexColor, out blockedDragColor);
        }

        void Update()
        {
            if (Object == null || !Object.IsValid) return;

            if (!isConnectingMode) return;
            HandleLocalInput();
        }

        private Vector2 GetPointerPosition()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.mousePosition;
#else
        return Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
#endif
        }

        #region Input & Preview
        private Vector3 GetInputWorldPoint()
        {
            Ray ray = cam.ScreenPointToRay(GetPointerPosition());
            Plane plane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
            if (plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);
            return Vector3.zero;
        }

        void HandleLocalInput()
        {
            bool inputDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool inputUp = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
            bool inputHeld = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved);
            // Debug.Log("Start Dragging");
            Vector2 screenPos = Input.mousePosition;
            if (Input.touchCount > 0) screenPos = Input.GetTouch(0).position;

            //Check Obstacle
            Vector3 dragPos = GetInputWorldPoint();
            // Debug points for Scene View
            if (selectedTower != null)
                debugStart = selectedTower.transform.position;
            debugEnd = dragPos;

            // Check obstacle between start → drag point
            Vector3 dir = (dragPos - debugStart).normalized;
            float dist = Vector3.Distance(debugStart, dragPos);

            if (Physics.Raycast(debugStart, dir, out RaycastHit hit1, dist, obstacleMask))
            {
                // BLOCKED
                haveObstacle = true;
            }
            else
            {
                // CLEAR
                haveObstacle = false;
            }

            if (inputDown)
            {
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TowerBehavior tb = hit.collider.GetComponent<TowerBehavior>();
                    selectedTower = tb;
                    if (tb != null)
                    {
                        if (tb.Object != null && tb.Object.HasInputAuthority)
                        {
                            startTower = tb;
                            StartPreviewLine(startTower);
                        }
                    }
                    else
                    {
                        // Start CUT mode if not hitting a tower
                        StartCutMode(dragPos);
                    }
                }
                else
                {
                    // Start CUT mode if hitting nothing
                    StartCutMode(dragPos);
                }
            }
            else if (inputHeld)
            {
                if (isDragging)
                {
                    UpdatePreviewLine(screenPos);
                }
                else if (isCutting && cutLR != null)
                {
                    Vector3 w = GetInputWorldPoint();
                    cutLR.UpdateEnd(w);
                    CheckLocalCut(lastCutPos, w);
                    lastCutPos = w;
                }
            }
            else if (inputUp)
            {
                if (isDragging)
                {
                    if (!haveObstacle)
                    {
                        Ray ray = cam.ScreenPointToRay(screenPos);
                        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<TowerBehavior>())
                        {
                            TowerBehavior endTower = hit.collider.GetComponent<TowerBehavior>();
                            if (endTower != null && endTower != startTower)
                            {
                                RpcRequestCreateConnection(startTower.name, endTower.name);
                            }
                        }
                    }
                    DestroyPreview();
                }
                else if (isCutting)
                {
                    isCutting = false;
                    if (cutLR != null) Destroy(cutLR.gameObject);
                }
            }
        }

        void StartPreviewLine(TowerBehavior tower)
        {
            if (linePrefab == null || tower == null) return;

            GameObject lineObj = Instantiate(linePrefab);
            previewLine = lineObj.GetComponent<LineRenderer>();

            if (previewLine != null)
            {
                previewLine.positionCount = 2;
                previewLine.SetPosition(0, tower.transform.position);
                previewLine.SetPosition(1, tower.transform.position);
            }

            var lr = lineObj.GetComponent<LrBehavior>();
            Color c = Color.white;
            if (LevelManager.Instance != null && tower.Object != null)
            {
                c = (tower.Object.HasInputAuthority) ? LevelManager.Instance.blueLineColor : LevelManager.Instance.redLineColor;
                normalDragColor = c;
            }

            if (lr) lr.SetColor(c);

            isDragging = true;
        }

        void UpdatePreviewLine(Vector2 screenPos)
        {
            if (previewLine == null || startTower == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            float dist;
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, startTower.transform.position.y, 0)); // Use plane at tower height

            if (groundPlane.Raycast(ray, out dist))
            {
                Vector3 point = ray.GetPoint(dist);
                // Force Y to ensure it's flat
                point.y = 18f;
                previewLine.SetPosition(1, point);
            }
            if (haveObstacle)
            {
                previewLine.GetComponent<LrBehavior>().SetColor(Color.black);
            }
            else
            {
                previewLine.GetComponent<LrBehavior>().SetColor(normalDragColor);
            }
        }

        void DestroyPreview()
        {
            if (previewLine != null) Destroy(previewLine.gameObject);
            previewLine = null;
            startTower = null;
            isDragging = false;
        }
        #endregion

        #region CUT LOGIC
        private void StartCutMode(Vector3 worldPos)
        {
            if (cutLinePrefab != null)
            {
                GameObject obj = Instantiate(cutLinePrefab);
                cutLR = obj.GetComponent<CuttingLineRenderer>();
                cutLR.Init(worldPos);
            }
            lastCutPos = worldPos;
            isCutting = true;
        }

        private void CheckLocalCut(Vector3 p1, Vector3 p2)
        {
            Vector2 s1 = cam.WorldToScreenPoint(p1);
            Vector2 s2 = cam.WorldToScreenPoint(p2);

            // Loop through existing local connection visuals
            foreach (var c in connections)
            {
                if (c.startTower == null || c.endTower == null) continue;

                Vector2 a = cam.WorldToScreenPoint(c.startTower.transform.position);
                Vector2 b = cam.WorldToScreenPoint(c.endTower.transform.position);

                if (LinesIntersect(s1, s2, a, b))
                {
                    // Directional Cut Check: Only cut if I own the SOURCE of the link
                    if (c.startTower.Object.HasInputAuthority)
                    {
                        // I own Start -> End. requesting to cut that.
                        RpcRequestCutConnection(c.startTower.name, c.endTower.name);
                        break;
                    }
                    else if (c.endTower.Object.HasInputAuthority)
                    {
                        // I own End. If this is a valid link (End -> Start), I can cut it.
                        // Even if visually it looks like Start->End, if I am End and I have a link to Start, I request to cut End->Start.
                        if (c.endTower.linkedTowers.Contains(c.startTower))
                        {
                            RpcRequestCutConnection(c.endTower.name, c.startTower.name);
                        }
                        break;
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
        #endregion

        #region RPC Server Logic

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestCreateConnection(string startName, string endName, RpcInfo info = default)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);

            if (start == null || end == null) return;

            if (start.linkedTowers.Contains(end))
            {
                // Ignore if already connected
            }
            else
            {
                // CHECK INTERSECT (Reverse connection exists)
                if (end.linkedTowers.Contains(start))
                {
                    // Update to INTERSECT
                    start.linkedTowers.Add(end);
                    start.currentConnections++;
                    RPC_BroadcastIntersectConnection(startName, endName);
                }
                else if (start.CheckConnectionPossibility())
                {
                    start.linkedTowers.Add(end);
                    start.currentConnections++;

                    RPC_BroadcastCreateConnection(startName, endName);

                    // Auto-Retaliation (Counter-Attack)
                    bool isEnemy = false;
                    if (start.type == TowerType.PlayerTower && end.type == TowerType.RedTower) isEnemy = true;
                    if (start.type == TowerType.RedTower && end.type == TowerType.PlayerTower) isEnemy = true;

                    if (isEnemy)
                    {
                        // Removed Auto-Retaliation to match single player behavior
                    }
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestCutConnection(string startName, string endName)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);

            if (start == null || end == null) return;

            // 1. Remove the specific directional link
            if (start.linkedTowers.Contains(end))
            {
                start.linkedTowers.Remove(end);
                start.currentConnections--;
            }

            // 2. Check if the reverse link still exists
            if (end.linkedTowers.Contains(start))
            {
                // Downgrade to Single (The reverse link becomes the ONLY link)
                // The surviving link is End -> Start
                RPC_BroadcastUpdateToSingle(endName, startName);
            }
            else
            {
                // No links left, break fully
                RPC_BreakConnectionHost(startName, endName);
            }
        }

        private void RPC_BreakConnectionHost(string startName, string endName)
        {
            RPC_BroadcastBreakConnection(startName, endName);
        }

        #endregion

        #region Broadcast Visuals

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastCreateConnection(string startName, string endName)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);
            if (start == null || end == null) return;

            if (linePrefab == null) return;

            GameObject obj = Instantiate(linePrefab);
            LrBehavior lr = obj.GetComponent<LrBehavior>();

            if (lr == null)
            {
                Destroy(obj);
                return;
            }

            if (start.connectionPoint == null || end.connectionPoint == null)
            {
                lr.SetTargets(start.transform, end.transform);
            }
            else
            {
                lr.SetTargets(start.connectionPoint, end.connectionPoint);
            }

            Color c = Color.white;
            if (LevelManager.Instance != null && start.Object != null)
            {
                c = (start.Object.HasInputAuthority) ? LevelManager.Instance.blueLineColor : LevelManager.Instance.redLineColor;
            }

            lr.SetColor(c);

            var lineRenderer = obj.GetComponent<LineRenderer>();
            Connection conn = new Connection() { startTower = start, endTower = end, lineRenderer = lineRenderer };
            connections.Add(conn);

            if (!start.linkedTowers.Contains(end))
            {
                start.linkedTowers.Add(end);
            }

            SwitchDotsColor(start);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastIntersectConnection(string startName, string endName)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);
            if (start == null || end == null) return;

            // Find the EXISTING connection (Reverse)
            // It should be end -> start
            Connection existing = connections.Find(c => c.startTower == end && c.endTower == start);

            if (existing != null && existing.lineRenderer != null)
            {
                LrBehavior lr = existing.lineRenderer.GetComponent<LrBehavior>();
                if (lr != null)
                {
                    Color c1 = (end.Object.HasInputAuthority) ? LevelManager.Instance.blueLineColor : LevelManager.Instance.redLineColor;
                    Color c2 = (start.Object.HasInputAuthority) ? LevelManager.Instance.blueLineColor : LevelManager.Instance.redLineColor;
                    Debug.Log($"[Intersect] Setting Colors: End(OrigSource)={c1}, Start(NewSource)={c2}");
                    lr.SetTwoColors(c1, c2);
                }
            }

            if (!start.linkedTowers.Contains(end))
            {
                start.linkedTowers.Add(end);
            }
            SwitchDotsColor(start);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastBreakConnection(string startName, string endName)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);
            if (start == null || end == null) return;

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                var c = connections[i];
                if (c.startTower == start && c.endTower == end)
                {
                    if (c.lineRenderer != null) Destroy(c.lineRenderer.gameObject);
                    connections.RemoveAt(i);
                }
            }

            if (start.linkedTowers.Contains(end)) start.linkedTowers.Remove(end);
            start.currentConnections--;

            // Also remove reverse connection if exists (Bidirectional Cut)
            if (end.linkedTowers.Contains(start))
            {
                end.linkedTowers.Remove(start);
                end.currentConnections--;
            }

            // Visual Update for dots
            SwitchDotsColor(start);
            SwitchDotsColor(end);

            if (InputManager.Instance != null)
                InputManager.Instance.DestroySoldiersOnConnection(start, end);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastUpdateToSingle(string newStartName, string newEndName)
        {
            TowerBehavior newStart = FindTowerByName(newStartName);
            TowerBehavior newEnd = FindTowerByName(newEndName);
            if (newStart == null || newEnd == null) return;

            // Find existing connection (order doesn't matter for finding)
            Connection existing = connections.Find(c =>
                (c.startTower == newStart && c.endTower == newEnd) ||
                (c.startTower == newEnd && c.endTower == newStart));

            if (existing != null)
            {
                // Update structure to reflect the Surviving Single Link
                existing.startTower = newStart;
                existing.endTower = newEnd;

                if (existing.lineRenderer != null)
                {
                    LrBehavior lr = existing.lineRenderer.GetComponent<LrBehavior>();
                    if (lr != null)
                    {
                        // Reset to Single Color of the New Start
                        Color c = Color.white;
                        if (newStart.Object != null)
                        {
                            c = (newStart.Object.HasInputAuthority) ?
                                LevelManager.Instance.blueLineColor : LevelManager.Instance.redLineColor;
                        }

                        lr.isIntersect = false; // Important: Tell LrBehavior it's single
                        lr.SetColor(c);

                        // Ensure targets are correct (swapping if needed)
                        if (newStart.connectionPoint != null && newEnd.connectionPoint != null)
                            lr.SetTargets(newStart.connectionPoint, newEnd.connectionPoint);
                        else
                            lr.SetTargets(newStart.transform, newEnd.transform);
                    }
                }
            }

            // Sync dots
            SwitchDotsColor(newStart);
            SwitchDotsColor(newEnd);

            // Destroy soldiers going the WRONG way (from the old Start which is now newEnd)
            // The cut was "oldStart -> oldEnd". New Single is "newStart (oldEnd) -> newEnd (oldStart)"
            // So we destroy soldiers moving newEnd -> newStart
            if (InputManager.Instance != null)
                InputManager.Instance.DestroySoldiersDirectional(newEnd, newStart);
        }

        public void BreakConnectionSingleMode(string startName, string endName)
        {
            TowerBehavior start = FindTowerByName(startName);
            TowerBehavior end = FindTowerByName(endName);
            if (start == null || end == null) return;

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                var c = connections[i];
                if (c.startTower == start && c.endTower == end)
                {
                    if (c.lineRenderer != null) Destroy(c.lineRenderer.gameObject);
                    connections.RemoveAt(i);
                }
            }

            if (start.linkedTowers.Contains(end)) start.linkedTowers.Remove(end);
            start.currentConnections--;

            // Also remove reverse connection if exists (Bidirectional Cut)
            if (end.linkedTowers.Contains(start))
            {
                end.linkedTowers.Remove(start);
                end.currentConnections--;
            }

            // Visual Update for dots
            SwitchDotsColor(start);
            SwitchDotsColor(end);

            if (InputManager.Instance != null)
                InputManager.Instance.DestroySoldiersOnConnection(start, end);
        }

        public void RemoveAllConnectionsForTower(TowerBehavior tower)
        {
            if (Object == null)
                return;
            if (Object.HasStateAuthority)
            {
                List<Connection> toKill = new List<Connection>();
                foreach (var c in connections)
                {
                    if (c.startTower == tower || c.endTower == tower) toKill.Add(c);
                }
                foreach (var c in toKill)
                {
                    RPC_BreakConnectionHost(c.startTower.name, c.endTower.name);
                }
            }
        }

        public void RemoveOutgoingConnections(TowerBehavior tower)
        {
            if (Object.HasStateAuthority)
            {
                List<Connection> toKill = new List<Connection>();
                foreach (var c in connections)
                {
                    // Only kill if tower is the START (Outgoing)
                    if (c.startTower == tower) toKill.Add(c);
                }
                foreach (var c in toKill)
                {
                    RPC_BreakConnectionHost(c.startTower.name, c.endTower.name);
                }
            }
        }

        public void CleanUpConnectionsLocal(TowerBehavior tower)
        {
            var lines = FindObjectsOfType<LrBehavior>();
            if (InputManager.Instance == null)
            {
                Debug.LogError("InputManager.Instance is null in CleanUpConnectionsLocal");
                return;
            }

            Debug.Log($"Cleaning up connections for tower: {tower.name} (Total lines: {lines.Length})");

            foreach (var lr in lines)
            {
                if (lr == null) continue;

                var start = lr.StartPoint?.GetComponentInParent<TowerBehavior>();
                var end = lr.EndPoint?.GetComponentInParent<TowerBehavior>();

                //Debug.Log($"Checking Line {lr.name}: Start={(start ? start.name : "null")} End={(end ? end.name : "null")}");

                if (start == tower) // Outgoing (or Intersected Outgoing)
                {
                    if (end != null)
                    {
                        Debug.Log($"[Cleanup] FULLY Destroying Line from {tower.name} to {end.name}");

                        // If intersected, the Other tower (end) thinks it's connected to me. Break that link too.
                        if (end.linkedTowers.Contains(tower))
                        {
                            end.linkedTowers.Remove(tower);
                            end.currentConnections--;
                            SwitchDotsColor(end); // Update dots on the neighbor
                        }

                        // Destroy soldiers in BOTH directions
                        InputManager.Instance.DestroySoldiersOnConnection(tower, end);
                    }

                    start.CheckConnectionPossibility();
                    end.CheckConnectionPossibility();
                    // Destroy the visual line
                    Destroy(lr.gameObject);

                }
                else if (end == tower) // Incoming
                {
                    if (lr.isIntersect && start != null)
                    {
                        // Incoming Intersect is technically an Outgoing link viewed from the other side?
                        // No, if end == tower, then 'start' is the other guy.
                        // If it is intersected, it means *I* (tower) also have a link to 'start'.
                        // My link to 'start' is cleared by linkedTowers.Clear() in OnCaptured.
                        // But the visual line 'lr' is owned by 'start' (since start != tower).

                        Debug.Log($"[Cleanup] Downgrading Incoming Intersect (Line owner: {start.name})");

                        // The line belongs to 'start'. We just remove the "Intersect" flag.
                        // 'start' stays connected to 'tower'.
                        lr.isIntersect = false;
                        lr.SetColor(LineColors.Get(start.type));

                        // Destroy soldiers from ME to START (since I am no longer connected)
                        // Soldiers from START to ME should persist (Start is attacking Me)
                        InputManager.Instance.DestroySoldiersDirectional(tower, start);
                    }
                    if (start)
                    start.CheckConnectionPossibility();
                    if (end)
                    end.CheckConnectionPossibility();
                    // Pure Incoming (Single): Persist. (Enemy attacking me).
                }
            }
        }



        #endregion

        #region Utilities
        private TowerBehavior FindTowerByName(string name)
        {
            GameObject go = GameObject.Find(name);
            return go ? go.GetComponent<TowerBehavior>() : null;
        }

        private void SwitchDotsColor(TowerBehavior tower)
        {
            if (tower == null) return;
            foreach (var item in tower.dotsSprites)
            {
                if (item != null)
                    item.GetComponent<Image>().color = Color.white;
            }
            for (int i = 0; i < tower.currentConnections; i++)
            {
                if (i < tower.dotsSprites.Count && tower.dotsSprites[i] != null)
                    tower.dotsSprites[i].GetComponent<Image>().color = Color.grey;
            }
        }

        private class Connection
        {
            public TowerBehavior startTower;
            public TowerBehavior endTower;
            public LineRenderer lineRenderer;
        }
        #endregion
    }
}
