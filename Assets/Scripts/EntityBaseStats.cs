using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityStats", menuName = "Scriptable Objects/EntityStats")]
public class EntityBaseStats : ScriptableObject
{
    public int BASE_ENERGY;
    public int BASE_VIEWSIZE;
    public List<CardTemplate> STARTING_DECK;
}
