using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yachu.Client {

public class GameUIScoreType : MonoBehaviour {
    public readonly int _size = 2;
    public int _index;
    public Image _icon;
    public Text _nameText;

    public RectTransform _descriptor;
    public Text _descriptorText;

    public GameUIScoreColumn _leftScoreColumn;
    public GameUIScoreColumn _rightScoreColumn;

    [ContextMenu("Bind")]
    private void Bind()
    {
        _icon = transform.GetChild(0).GetChild(1).GetComponent<Image>();
        _nameText = transform.GetChild(0).GetChild(2).GetComponent<Text>();
        _descriptor = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        _descriptorText = _descriptor.GetComponentInChildren<Text>();
        _leftScoreColumn = transform.GetChild(1).GetComponent<GameUIScoreColumn>();
        _rightScoreColumn = transform.GetChild(2).GetComponent<GameUIScoreColumn>();
    }
    
    public void Initialize(int index) {
        _index = index;
    }
    
    public void SetNumber(int index, int? score, Color textColor) {
        if (index < 0 || index >= _size) {
            return;
        }

        var panel = GetColumn(index);
        if (score == null) {
            panel._text.text = "";
        }
        else {
            panel._text.text = score.ToString();
        }

        panel._text.color = textColor;
    }

    public void SetPanelColor(int index, Color panelColor) {
        if (index < 0 || index >= _size) {
            return;
        }
        var panel = GetColumn(index);
        panel._panel.color = panelColor;
    }
    
    public GameUIScoreColumn GetColumn(int index) {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftScoreColumn : _rightScoreColumn;
    }
}
}