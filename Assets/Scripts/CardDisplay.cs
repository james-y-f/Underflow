using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class CardDisplay : MonoBehaviour
{
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
    public void UpdateDisplay(CardInfo info)
    {
        Title.text = info.Title;
        Energy.text = info.EnergyCost.ToString();
        Description.text = info.Description;
    }

}
