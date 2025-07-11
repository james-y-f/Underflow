using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;
    public TextMeshProUGUI TooltipText;

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
        TooltipText = transform.Find("TooltipText").GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(TooltipText);
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowTooltip(string message)
    {
        TooltipText.text = message;
        gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        TooltipText.text = string.Empty;
        gameObject.SetActive(false);
    }
}
