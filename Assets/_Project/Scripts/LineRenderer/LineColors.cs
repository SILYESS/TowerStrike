using UnityEngine;
using Towers;

public static class LineColors
{
    public static Color Get(TowerType type)
    {
        switch (type)
        {
            case TowerType.PlayerTower:
                return LevelManager.Instance.blueLineColor; 

            case TowerType.RedTower:
                return LevelManager.Instance.redLineColor;
                ;

            case TowerType.YellowTower:
                return LevelManager.Instance.yellowLineColor;
                ;
            case TowerType.GreenTower:
                return LevelManager.Instance.greenLineColor;
                ;


            default:
                return LevelManager.Instance.greyLineColor;
                ;
        }
    }
}
