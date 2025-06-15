using UnityEngine;
using UnityEngine.Assertions;

public class TlingRandomizer : MonoBehaviour
{
    Renderer Render;
    void Awake()
    {
        Render = gameObject.GetComponent<Renderer>();
        Assert.IsNotNull(Render);
    }

    void Start()
    {
        Render.material.mainTextureOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

}
