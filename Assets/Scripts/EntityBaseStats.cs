using UnityEngine;

[CreateAssetMenu(fileName = "New Entity Stats", menuName = "Entity Stats")]
public class EntityBaseStats : ScriptableObject
{
    public string Name;
    public int BaseViewSize;
    public int BaseEnergy;
    public DeckTemplate Deck;
    public bool ShuffleAtStart;
}
