using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class CardDisplay : MonoBehaviour
{
    CardTemplate CardRef;
    TMP_Text Title;
    TMP_Text Energy;
    TMP_Text Description;

    void Awake()
    {
        Transform tf = gameObject.transform;
        Title = tf.Find("Title").GetComponent<TMP_Text>();
        Energy = tf.Find("Energy").GetComponent<TMP_Text>();
        Description = tf.Find("Description").GetComponent<TMP_Text>();
    }
    public void UpdateDisplay(CardTemplate info)
    {
        Title.text = info.title;
        Energy.text = info.energyCost.ToString();
        Description.text = info.description;
    }

}
