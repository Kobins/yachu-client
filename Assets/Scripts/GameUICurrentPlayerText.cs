using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {

public class GameUICurrentPlayerText : MonoBehaviour {
    public Text _text;

    public void SetSelf() {
        SetOtherPlayer(null);
    }
    
    public void SetOtherPlayer(string playerName) {
        gameObject.SetActive(true);
        if (playerName == null) {
            _text.text = "당신의 차례!";
        }
        else {
            _text.text = $"{playerName}의 차례 ...";
        }
    }

    public void Disable() {
        gameObject.SetActive(false);
    }
}
}