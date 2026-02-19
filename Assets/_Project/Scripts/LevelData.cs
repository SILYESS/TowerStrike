using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "TowerWar/LevelData")]
public class LevelData : ScriptableObject
{
    public int level;
    public TowerData[] towers;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
    public int coinsReward = 50;
    public TreesData[] trees;
}
