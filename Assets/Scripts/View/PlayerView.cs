using UnityEngine;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    public Text nameText;
    public Text hpText;

    public void Bind(PlayerModel model)
    {
        nameText.text = model.playerName;
        hpText.text = "HP: " + model.hp;
    }
}