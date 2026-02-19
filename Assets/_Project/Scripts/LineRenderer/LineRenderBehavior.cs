using UnityEngine;

public class LineConnection : MonoBehaviour
{
    [HideInInspector] public GameObject startTower;
    [HideInInspector] public GameObject endTower;
    [HideInInspector] public Coroutine soldierRoutine;
    [HideInInspector] public LineRenderer lineRenderer;
    [HideInInspector] public CapsuleCollider capsuleCollider;

}