using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class EnergyDisplay : MonoBehaviour
{
    [SerializeField] GameObject EnergyStonePrefab;
    Transform SpawnPoint;
    List<GameObject> Stones;

    void Awake()
    {
        SpawnPoint = transform;
        Stones = new List<GameObject>();
    }

    void AddEnergy(int amount)
    {
        if (amount < 1) return;
        Debug.Log($"Adding {amount} Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject newStone = Instantiate(EnergyStonePrefab,
                SpawnPoint.position + Stones.Count * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            Stones.Add(newStone);
        }
    }

    void RemoveEnergy(int amount)
    {
        if (amount < 1) return;
        Debug.Log($"Removing {amount} Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject removedStone = Stones[Stones.Count - 1];
            Stones.RemoveAt(Stones.Count - 1);
            Destroy(removedStone);
        }
    }

    public void SetEnergy(int target)
    {
        Assert.IsTrue(target >= 0);
        Debug.Log($"Setting Energy To {target}");
        if (Stones.Count == target) return;
        if (Stones.Count < target)
        {
            AddEnergy(target - Stones.Count);
        }
        else
        {
            RemoveEnergy(Stones.Count - target);
        }
    }
}
