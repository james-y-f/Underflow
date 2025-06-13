using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class EnergyDisplay : MonoBehaviour
{
    [SerializeField] GameObject BaseEnergyStonePrefab;
    [SerializeField] GameObject TempEnergyStonePrefab;
    Transform SpawnPoint;
    List<GameObject> BaseStones;
    public int TotalBaseEnergy = 0;
    int ActiveBaseEnergy = 0;
    List<GameObject> TempStones;
    List<GameObject> DimmedTempStones;

    void Awake()
    {
        SpawnPoint = transform;
        BaseStones = new List<GameObject>();
        TempStones = new List<GameObject>();
        DimmedTempStones = new List<GameObject>();
    }

    public void SpawnBaseEnergy(int amount)
    {
        TotalBaseEnergy = amount;
        Assert.IsTrue(TotalBaseEnergy > 0);
        Debug.Log($"Spawning {TotalBaseEnergy} Base Energy");
        for (int i = 0; i < TotalBaseEnergy; i++)
        {
            GameObject newStone = Instantiate(BaseEnergyStonePrefab,
                SpawnPoint.position + Constants.BaseEnergySpawnHeightAdjustment + BaseStones.Count * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            GetColorController(newStone).SetDimmed(true);
            BaseStones.Add(newStone);
        }
    }

    IEnumerator GainBaseEnergy(int amount = int.MaxValue)
    {
        if (amount < 1) yield break;
        if (amount == int.MaxValue)
        {
            amount = TotalBaseEnergy;
        }
        Debug.Log($"Activating {amount} Base Energy");
        for (int i = 0; i < Math.Min(amount, TotalBaseEnergy - ActiveBaseEnergy); i++)
        {
            int gainIndex = ActiveBaseEnergy + i;
            GetColorController(BaseStones[gainIndex]).SetDimmed(false);
            yield return StartCoroutine(GetColorController(BaseStones[gainIndex]).FlashColor(Constants.FlashHighlight));
        }
        ActiveBaseEnergy = Math.Min(TotalBaseEnergy, ActiveBaseEnergy + amount);
    }

    IEnumerator SpendBaseEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Spending {amount} Base Energy");
        for (int i = 0; i < Math.Min(amount, ActiveBaseEnergy); i++)
        {
            int spendIndex = ActiveBaseEnergy - i - 1;
            GetColorController(BaseStones[spendIndex]).SetDimmed(true);
            yield return StartCoroutine(GetColorController(BaseStones[spendIndex]).FlashColor(Constants.FlashHighlight));
        }
        ActiveBaseEnergy = Math.Max(0, ActiveBaseEnergy - amount);
    }

    IEnumerator AddTempEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Adding {amount} Temp Energy");
        for (int i = 0; i < amount; i++)
        {
            foreach (GameObject stone in DimmedTempStones)
            {
                stone.transform.position += Constants.EnergySpawnDisplacement;
            }
            GameObject newStone = Instantiate(TempEnergyStonePrefab,
                SpawnPoint.position + (BaseStones.Count + TempStones.Count) * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            TempStones.Add(newStone);
            yield return StartCoroutine(GetColorController(newStone).FlashColor(Constants.FlashHighlight));
        }
    }

    IEnumerator RemoveTempEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Removing {amount} Temp Energy");
        for (int i = 0; i < amount; i++)
        {
            foreach (GameObject stone in DimmedTempStones)
            {
                stone.transform.position -= Constants.EnergySpawnDisplacement;
            }
            GameObject removedStone = TempStones[TempStones.Count - 1];
            yield return StartCoroutine(GetColorController(removedStone).FlashColor(Constants.FlashHighlight));
            TempStones.Remove(removedStone);
            Destroy(removedStone);
        }
    }

    public IEnumerator SetActiveEnergy(int target)
    {
        Assert.IsTrue(target >= 0);
        int currentEnergy = ActiveBaseEnergy + TempStones.Count;
        if (currentEnergy == target) yield break;
        Debug.Log($"Setting Energy To {target}");
        int difference = Math.Abs(target - currentEnergy);
        if (currentEnergy < target)
        {
            int baseGainAmount = Math.Min(difference, TotalBaseEnergy - ActiveBaseEnergy);
            int tempGainAmount = difference - baseGainAmount;
            yield return StartCoroutine(GainBaseEnergy(baseGainAmount));
            yield return StartCoroutine(AddTempEnergy(tempGainAmount));
        }
        else
        {
            int tempLoseAmount = Math.Min(difference, TempStones.Count);
            int baseLoseAmount = difference - tempLoseAmount;
            yield return StartCoroutine(RemoveTempEnergy(tempLoseAmount));
            yield return StartCoroutine(SpendBaseEnergy(baseLoseAmount));
        }
    }

    IEnumerator AddDimmedTempEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Adding {amount} Dimmed Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject newStone = Instantiate(TempEnergyStonePrefab,
                SpawnPoint.position + (BaseStones.Count + TempStones.Count + DimmedTempStones.Count) * Constants.EnergySpawnDisplacement,
                SpawnPoint.rotation);
            DimmedTempStones.Add(newStone);
            GetColorController(newStone).SetDimmed(true);
            yield return StartCoroutine(GetColorController(newStone).FlashColor(Constants.FlashHighlight));
        }
    }

    IEnumerator RemoveDimmedTempEnergy(int amount)
    {
        if (amount < 1) yield break;
        Debug.Log($"Removing {amount} Dimmed Energy");
        for (int i = 0; i < amount; i++)
        {
            GameObject removedStone = DimmedTempStones[DimmedTempStones.Count - 1];
            yield return StartCoroutine(GetColorController(removedStone).FlashColor(Constants.FlashHighlight));
            DimmedTempStones.Remove(removedStone);
            Destroy(removedStone);
        }
    }

    public IEnumerator SetDimmedTempEnergy(int target)
    {
        Assert.IsTrue(target >= 0);
        if (DimmedTempStones.Count == target) yield break;
        Debug.Log($"Setting Dimmed Energy To {target}");
        if (DimmedTempStones.Count < target)
        {
            yield return StartCoroutine(AddDimmedTempEnergy(target - DimmedTempStones.Count));
        }
        else
        {
            yield return StartCoroutine(RemoveDimmedTempEnergy(DimmedTempStones.Count - target));
        }
    }

    public IEnumerator RemoveUnusedEnergy()
    {
        yield return RemoveTempEnergy(TempStones.Count);
        yield return SpendBaseEnergy(ActiveBaseEnergy);
    }

    public IEnumerator StartTurnCoroutine(int target)
    {
        Assert.IsTrue(TotalBaseEnergy + DimmedTempStones.Count == target);
        yield return StartCoroutine(GainBaseEnergy(TotalBaseEnergy));
        foreach (GameObject dimmedStone in DimmedTempStones)
        {
            TempStones.Add(dimmedStone);
        }
        DimmedTempStones = new List<GameObject>();
        Assert.IsTrue(DimmedTempStones.Count == 0);
        foreach (GameObject stone in TempStones)
        {
            GetColorController(stone).SetDimmed(false);
            yield return StartCoroutine(GetColorController(stone).FlashColor(Constants.FlashHighlight));
        }
    }

    ColorController GetColorController(GameObject stoneObject)
    {
        ColorController controller = stoneObject.GetComponent<ColorController>();
        Assert.IsNotNull(controller);
        return controller;
    }
}
