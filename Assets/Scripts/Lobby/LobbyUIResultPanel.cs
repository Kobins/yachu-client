using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {
public class LobbyUIResultPanel : MonoBehaviour {
    public Text _text;
    public Button _closeButton;

    private void Awake() {
        _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void SetWinner(string winnerNameOrNull) {
        gameObject.SetActive(true);
        if (winnerNameOrNull == null) {
            _text.text = "무승부 ...";
        }
        else {
            _text.text = $"{winnerNameOrNull}의 승리!";
        }
        
    }

    public void Disable() {
        gameObject.SetActive(false);
    }
}
}