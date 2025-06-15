using UnityEngine;
public class AssetLoader : MonoBehaviour
{
    public static AssetLoader Instance;
    public Material PlayerCardBack;
    public Material EnemyCardBack;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}