using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class EnergyDisplay : MonoBehaviour
{
    [SerializeField] GameObject EnergyStonePrefab;
    Transform SpawnPoint;
    List<GameObject> Stones;
    List<GameObject> TransparentStones;

    void Awake()
    {
        SpawnPoint = transform;
        Stones = new List<GameObject>();
        TransparentStones = new List<GameObject>();
    }

    IEnumerator AddEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Adding {amount} Energy");

        for (int i = 0; i < amount; i++)
        {
            foreach (GameObject stone in TransparentStones)
            {
                stone.transform.position += Constants.EnergySpawnDisplacement;
            }
            GameObject newStone = Instantiate(EnergyStonePrefab,
                SpawnPoint.position + Stones.Count * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            Stones.Add(newStone);
            yield return StartCoroutine(GetStoneComponent(newStone).FlashColor(Constants.FlashHighlight));
        }
    }

    IEnumerator RemoveEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Removing {amount} Energy");
        for (int i = 0; i < amount; i++)
        {
            foreach (GameObject stone in TransparentStones)
            {
                stone.transform.position -= Constants.EnergySpawnDisplacement;
            }
            GameObject removedStone = Stones[Stones.Count - 1];
            yield return StartCoroutine(GetStoneComponent(removedStone).FlashColor(Constants.FlashHighlight));
            Stones.RemoveAt(Stones.Count - 1);
            Destroy(removedStone);
        }
    }

    public IEnumerator SetEnergy(int target)
    {
        Assert.IsTrue(target >= 0);
        Debug.Log($"Setting Energy To {target}");
        if (Stones.Count == target) yield break;
        if (Stones.Count < target)
        {
            yield return StartCoroutine(AddEnergy(target - Stones.Count));
        }
        else
        {
            yield return StartCoroutine(RemoveEnergy(Stones.Count - target));
        }
    }

    IEnumerator AddTransparentEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Adding {amount} Transparent Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject newStone = Instantiate(EnergyStonePrefab,
                SpawnPoint.position + (Stones.Count + TransparentStones.Count) * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            GetStoneComponent(newStone).SetDimmed(true);
            TransparentStones.Add(newStone);
            yield return StartCoroutine(GetStoneComponent(newStone).FlashColor(Constants.FlashHighlight));
        }
    }

    IEnumerator RemoveTransparentEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Removing {amount} Transparent Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject removedStone = TransparentStones[TransparentStones.Count - 1];
            yield return StartCoroutine(GetStoneComponent(removedStone).FlashColor(Constants.FlashHighlight));
            TransparentStones.RemoveAt(Stones.Count - 1);
            Destroy(removedStone);
        }
    }

    public IEnumerator SetTransparentEnergy(int target)
    {
        Assert.IsTrue(target >= 0);
        Debug.Log($"Setting Transparent Energy To {target}");
        if (Stones.Count == target) yield break;
        if (Stones.Count < target)
        {
            yield return StartCoroutine(AddTransparentEnergy(target - TransparentStones.Count));
        }
        else
        {
            yield return StartCoroutine(RemoveTransparentEnergy(TransparentStones.Count - target));
        }
    }

    public void RemoveAllEnergy()
    {
        Debug.Log($"Removing All  Energy");
        foreach (GameObject stone in TransparentStones)
        {
            stone.transform.position -= Constants.EnergySpawnDisplacement * Stones.Count;
        }
        foreach (GameObject stone in Stones)
        {
            Destroy(stone);
        }
        Stones.Clear();
    }

    public void RemoveAllTransparentEnergy()
    {
        Debug.Log($"Removing All Transparent Energy");
        foreach (GameObject stone in TransparentStones)
        {
            Destroy(stone);
        }
        TransparentStones.Clear();
    }

    EnergyStone GetStoneComponent(GameObject stoneObject)
    {
        EnergyStone stone = stoneObject.GetComponent<EnergyStone>();
        Assert.IsNotNull(stone);
        return stone;
    }
}
