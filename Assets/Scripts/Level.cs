using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "New Level", menuName = "Level")]
public class Level : ScriptableObject
{
    public int LevelNumber;
    public EntityBaseStats PlayerBaseStats;
    public EntityBaseStats EnemyBaseStats;
}