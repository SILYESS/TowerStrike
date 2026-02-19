using UnityEngine;

public class DragLineRenderer : MonoBehaviour
{
    private LineRenderer lr;

    // Public read-only access (comme dans LrBehavior)
    public Vector3 StartPos { get; private set; }
    public Vector3 EndPos { get; private set; }

    [SerializeField] private float yOffset = 0.05f;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    // Setup initial start point (tour)
    public void Setup(Vector3 worldStartPos)
    {
        Vector3 startPosAux = new Vector3(worldStartPos.x,18.02f, worldStartPos.z);
        Vector3 endPosAux = new Vector3(worldStartPos.x, 18.02f, worldStartPos.z);
        StartPos = startPosAux;
        EndPos = endPosAux;

        UpdatePositions();
    }

    // Update during drag
    public void SetEnd(Vector3 worldEndPos)
    {
        Vector3 endPosAux = new Vector3(worldEndPos.x, 18.02f, worldEndPos.z);
        EndPos = endPosAux;
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        if (lr == null) return;

        Vector3 a = StartPos;
        Vector3 b = EndPos;

        //a.y += yOffset;
        //b.y += yOffset;

        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
    }
    public void SetColor(Color c)
    {
        lr.material.color = c;
    }
}
