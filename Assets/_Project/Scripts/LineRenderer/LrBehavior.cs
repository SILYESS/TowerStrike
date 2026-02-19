using UnityEngine;

public class LrBehavior : MonoBehaviour
{
    private LineRenderer lr;

    public Transform StartPoint { get; set; }
    public Transform EndPoint { get; set; }
    public bool isIntersect;
    [SerializeField] private float offsetY = 0.05f;

    private void OnEnable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allLines.Add(this);
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allLines.Remove(this);
    }

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null && lr.material != null)
        {
            lr.material = new Material(lr.material);
        }
    }

    public void SetTargets(Transform start, Transform end)
    {
        StartPoint = start;
        EndPoint = end;

        if (lr == null)
            lr = GetComponent<LineRenderer>();

        lr.positionCount = 2;
        UpdatePositions();
        if (_loop != null) StopCoroutine(_loop);
        _lastA = Vector3.zero;
        _lastB = Vector3.zero;
        _loop = StartCoroutine(UpdateLoop());
    }

    private Vector3 _lastA;
    private Vector3 _lastB;
    private Coroutine _loop;

    private void UpdatePositions()
    {
        if (StartPoint == null || EndPoint == null || lr == null)
            return;

        Vector3 a = StartPoint.position;
        Vector3 b = EndPoint.position;

        a.y += offsetY;
        b.y += offsetY;

        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        _lastA = a;
        _lastB = b;
    }

    private System.Collections.IEnumerator UpdateLoop()
    {
        var wait = new WaitForSeconds(0.05f);
        while (enabled)
        {
            if (StartPoint != null && EndPoint != null && lr != null)
            {
                Vector3 a = StartPoint.position;
                Vector3 b = EndPoint.position;
                a.y += offsetY;
                b.y += offsetY;
                if ((a - _lastA).sqrMagnitude > 0.0001f || (b - _lastB).sqrMagnitude > 0.0001f)
                {
                    lr.SetPosition(0, a);
                    lr.SetPosition(1, b);
                    _lastA = a;
                    _lastB = b;
                }
            }
            yield return wait;
        }
    }

    public void SetColor(Color c)
    {
        if (lr == null)
            lr = GetComponent<LineRenderer>();

        Material mat = lr.material;
        if (mat == null) return;

        if (mat != null)
        {
            if (mat.HasProperty("_Attacker1"))
                mat.SetColor("_Attacker1", c);

            // FORCE both colors to be the same to avoid blending issues
            if (mat.HasProperty("_Attacker2"))
                mat.SetColor("_Attacker2", c);

            if (mat.HasProperty("_Amount"))
                mat.SetFloat("_Amount", 1f);
        }



    }

    public void SetTwoColors(Color attacker1, Color attacker2)
    {
        var mat = lr.material;

        if (mat != null)
        {
            if (mat.HasProperty("_Attacker1"))
                mat.SetColor("_Attacker1", attacker1);

            if (mat.HasProperty("_Attacker2"))
                mat.SetColor("_Attacker2", attacker2);

            if (mat.HasProperty("_Amount"))
                mat.SetFloat("_Amount", 0.5f);
        }
    }

    public void SetPlayerFullControl(Color playerColor)
    {
        var mat = lr.material;
        if (mat == null) return;

        if (mat.HasProperty("_Attacker2"))
            mat.SetColor("_Attacker2", playerColor);

        if (mat.HasProperty("_Amount"))
            mat.SetFloat("_Amount", 0f); // FULL PLAYER COLOR
    }

    public void SetColorMaterial(Color c)
    {
        lr.material.color = c;
    }

}
