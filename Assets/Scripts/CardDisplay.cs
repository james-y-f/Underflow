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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CardRef = GetComponent<Card>().Template;
        Title.text = CardRef.title;
        Energy.text = CardRef.energyCost.ToString();
        Description.text = CardRef.description;
    }

}
