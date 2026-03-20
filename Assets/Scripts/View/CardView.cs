using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    public Text nameText;
    public Text descText;

    private CardModel model;

    public void Init(CardModel model)
    {
        this.model = model;
        Refresh();
    }

    public void Refresh()
    {
        nameText.text = model.cardName;
        descText.text = model.description;
    }
}