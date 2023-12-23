using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yachu.Server.Gameplay;
using Random = UnityEngine.Random;

namespace Yachu.Client {
public class GameUIScoreboard : MonoBehaviour {
    public ScrollRect _scrollRect;
    
    public List<GameUIScoreType> _types;
    public List<string> _typeNames;
    public List<Sprite> _typeIcons;

    public List<YachuScoreStorage> _storages;

    public Text _turnText;
    public Image _leftPlayerPanel;
    public Image _rightPlayerPanel;
    public Text _leftPlayerNameText;
    public Text _rightPlayerNameText;

    public Color _subtotalSumBonusCompleteColor = new Color(251f/255f, 243f/255f, 136f/255f, 1f);
    public Color _subtotalSumBonusFailColor = Color.white;
    public Text _leftSubtotalSumText;
    public Text _rightSubtotalSumText;
    public Text _leftSubtotalBonusText;
    public Text _rightSubtotalBonusText;

    public Image _leftTotalSumPanel;
    public Text _leftTotalSumText;
    public Image _rightTotalSumPanel;
    public Text _rightTotalSumText;

    public GameUIScoreboardColumn _leftColumn;
    public GameUIScoreboardColumn _rightColumn;


    public Color _iconColor = new Color(41f / 255f, 41f / 255f, 41f / 255f, 1f);
    public Color _normalPanelColor = new Color(1f, 1f, 1f, 1f);
    public Color _highlightPanelColor = new Color(255f / 255f, 226f / 255f, 104f / 255f, 1f);
    public Color _markedScoreTextColor = new Color(41f / 255f, 41f / 255f, 41f / 255f, 1f);
    public Color _calculatedScoreTextColor = new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f);

    [ContextMenu("Fill Icons and Names")]
    public void Fill() {
        var rand = Random.Range(0, _types.Count);
        for (int i = 0; i < _types.Count; i++) {
            var typeName = _typeNames[i];
            var typeIcon = _typeIcons[i];

            var type = _types[i];

            type.gameObject.name = typeName;
            type._icon.sprite = typeIcon;
            type._nameText.name = "Text";
            type._nameText.text = typeName;
            type._descriptorText.text = YachuScoreType.Of((YachuScoreTypeEnum) i).Description;
            type._descriptor.gameObject.SetActive(i == rand);

            for (int c = 0; c < _size; ++c) {
                var column = type.GetColumn(c);
                column._text.color = c == 0 ? _markedScoreTextColor : _calculatedScoreTextColor;
            }
        }
    }

    private readonly int _size = 2;

    public enum SlotInteractType {
        Click,
        MouseOver,
        MouseExit,
    }

    
    private void OnEnable() {
        _storages = new List<YachuScoreStorage>(_size);
        for (int i = 0; i < _size; ++i) {
            _storages.Add(new YachuScoreStorage());
        }

        for (int i = 0; i < _types.Count; ++i) {
            var type = _types[i];
            type.Initialize(i);
            for (int c = 0; c < _size; ++c) {
                var column = type.GetColumn(c);
                var index = i;
                var columnCaptured = c;
                column.Initialize(i, c);
                column.OnClick += () => {
                    OnSlotInteract?.Invoke(SlotInteractType.Click, index, type, columnCaptured, column);
                };
                column.OnMouseEnter += () => {
                    OnSlotInteract?.Invoke(SlotInteractType.MouseOver, index, type, columnCaptured, column);
                };
                column.OnMouseExit += () =>
                {
                    OnSlotInteract?.Invoke(SlotInteractType.MouseExit, index, type, columnCaptured, column);
                };
            }
        }
    }

    public delegate void OnSlotInteractEvent(
        SlotInteractType type,
        int index, GameUIScoreType scoreType,
        int column, GameUIScoreColumn scoreColumn
    );

    public OnSlotInteractEvent OnSlotInteract { get; set; }



    public void Reset() {
        for (int i = 0; i < _types.Count; i++) {
            var type = _types[i];
            type._descriptor.gameObject.SetActive(false);
            for (int index = 0; index < type._size; ++index) {
                type.SetNumber(index, null, _normalPanelColor);
            }
        }
        for (int i = 0; i < _size; ++i) {
            _storages[i].Reset();
            UpdateScore(i, null);
        }
        SetHighlight(0);
    }

    private Image GetTotalSumPanel(int index) {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftTotalSumPanel : _rightTotalSumPanel;
    }

    private Image GetPlayerPanel(int index) {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftPlayerPanel : _rightPlayerPanel;
    }
    public void SetHighlight(int index) {
        foreach (var type in _types) {
            for (int i = 0; i < type._size; ++i) {
                type.SetPanelColor(i, i == index ? _highlightPanelColor : _normalPanelColor);
            }
        }

        for (int i = 0; i < _size; ++i) {
            var sumPanel = GetTotalSumPanel(i);
            if(sumPanel) sumPanel.color = i == index ? _highlightPanelColor : _normalPanelColor;
            var playerPanel = GetPlayerPanel(i);
            if (playerPanel) playerPanel.color = i == index ? _highlightPanelColor : _normalPanelColor;
        }
    }

    private Text GetPlayerNameText(int index) {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftPlayerNameText : _rightPlayerNameText;
    }
    public void SetPlayerName(List<string> players) {
        for (int i = 0; i < players.Count; ++i) {
            var text = GetPlayerNameText(i);
            if (text) text.text = players[i];
        }
    }

    public void SetTurn(int turn) {
        _turnText.text = $"{turn}/{_types.Count(it => it.gameObject.activeSelf)}";
    }

    public Text GetSubtotalSumText(int index)
    {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftSubtotalSumText : _rightSubtotalSumText;
    }
    public Text GetSubtotalBonusText(int index)
    {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftSubtotalBonusText : _rightSubtotalBonusText;
    }
    public Text GetTotalSumText(int index)
    {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftTotalSumText : _rightTotalSumText;
    }

    public Text GetScoreColumn(int index)
    {
        if (index < 0 || index >= _size) {
            return null;
        }
        return index == 0 ? _leftTotalSumText : _rightTotalSumText;
    }

    public void UpdateScore(int index, List<int> dicesOrNull, YachuScoreTypeEnum typeEnum = YachuScoreTypeEnum.TypeCount) {
        var storage = _storages[index];
        var count = (int) YachuScoreTypeEnum.TypeCount;

        for (int i = 0; i < count; i++) {
            var obj = _types[i];
            var panel = obj.GetColumn(index);
            var scoreType = (YachuScoreTypeEnum) i;
            var score = storage?[scoreType];
            if (score == null) {
                if (dicesOrNull == null || dicesOrNull.Count < 5) {
                    panel._text.text = "";
                }
                else {
                    panel.SetScore(scoreType.Of().Calculator(dicesOrNull));
                    panel._text.color = _calculatedScoreTextColor;
                }
            }
            else {
                panel.SetScore(score.Value);
                panel._text.color = _markedScoreTextColor;
                if (typeEnum != YachuScoreTypeEnum.TypeCount && (int)typeEnum == i) {
                    panel.Mark();
                }
            }
        }

        var subtotalSumText = GetSubtotalSumText(index);
        var subtotalBonusText = GetSubtotalBonusText(index);
        var totalSumText = GetTotalSumText(index);
        if (storage == null) {
            subtotalSumText.text = "0";
            subtotalSumText.color = _subtotalSumBonusFailColor;
            subtotalBonusText.text = "";
            totalSumText.text = "";
        }
        else {
            var bonusDetermined = storage.BonusDetermined;
            subtotalSumText.text = $"{storage.Subtotal}";
            subtotalSumText.color = bonusDetermined && storage.Bonus > 0 ? _subtotalSumBonusCompleteColor : _subtotalSumBonusFailColor;
            subtotalBonusText.text = storage.BonusDetermined ? $"+{storage.Bonus}" : "";
            totalSumText.text = storage.Total.ToString();
        }
        
    }

    public void UpdateUI() {
        
    }


    public int testIndex = 3;
    public int testColumn = 0;
    public Image testImage;
    public Vector3 testVector;
    public Vector3[] testArray = new Vector3[4];

    private static readonly Vector2 ReferenceResolution = new Vector2(1280, 720);
    [ContextMenu("Test")]
    public void Test() {
        var column = _types[testIndex].GetColumn(testColumn);
        var t = column._panel.rectTransform;

        var panelRect = t.rect;
        var panelSize = new Vector2(panelRect.width, panelRect.height);
        Debug.Log($"panelSize: {panelSize}");
        var position = t.position;
        Debug.Log($"raw position: {position}");
        // position.x = position.x / Screen.width * ReferenceResolution.x;
        // position.y = position.y / Screen.height * ReferenceResolution.y;
        Debug.Log($"adjusted position: {position}");
        var offset = new Vector3(0f, 0f);
        // var offset = new Vector3((panelSize.x / 2) / Screen.width * ReferenceResolution.x, 0f);
        // var offset = new Vector3((panelSize.x / 2), 0f);
        Debug.Log($"offset: {offset}");
        testImage.rectTransform.position = position + offset;
        Debug.Log($"position: {testImage.rectTransform.position}");
        testImage.rectTransform.sizeDelta = t.sizeDelta + new Vector2(20f, 20f);
        // testVector = Camera.main.ScreenToWorldPoint(t.position);


        // testVector.x *= Screen.width;
        // testVector.y *= Screen.height;

        // Debug.DrawLine(testArray[0], testArray[1], Color.cyan, 3f);
        // Debug.DrawLine(testArray[1], testArray[2], Color.cyan, 3f);
        // Debug.DrawLine(testArray[2], testArray[3], Color.cyan, 3f);
        // Debug.DrawLine(testArray[3], testArray[0], Color.cyan, 3f);
    }
}
}