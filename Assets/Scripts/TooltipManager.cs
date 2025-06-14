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
        Cursor.visible = true;
        gameObject.SetActive(false);
    }

    public void ShowTooltip(string message)
    {
        gameObject.SetActive(true);
        TooltipText.text = message;
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
        TooltipText.text = string.Empty;
    }
}
