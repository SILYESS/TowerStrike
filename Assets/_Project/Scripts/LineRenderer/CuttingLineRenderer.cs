using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingLineRenderer : MonoBehaviour
{
    private TrailRenderer tr;
    public float yOffset = 0.1f;


    private void OnEnable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allTrails.Add(this);
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.allTrails.Remove(this);
    }

    private void Awake()
    {
        tr = GetComponent<TrailRenderer>();
        tr.emitting = true; // commence � tracer
    }

    public void Init(Vector3 startPos)
    {
        startPos.y += yOffset;
        transform.position = startPos;

        // Reset du trail si jamais le prefab a une trace pr�c�dente
        tr.Clear();
    }

    public void UpdateEnd(Vector3 endPos)
    {
        endPos.y += yOffset;
        transform.position = endPos;
    }
}
