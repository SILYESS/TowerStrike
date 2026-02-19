using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TowerData
{
    public GameObject towerPrefab;
    public Vector3 position;
    public Vector3 rotation;
    public TowerType type;
    public int TowerLevel=1;
    public int startHealth = 10;
    public int maxHealth = 16;
    public float solderSpawnRate = 1.2f;
    public bool isStarterTower;
}
