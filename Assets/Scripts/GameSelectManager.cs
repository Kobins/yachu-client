using System;
using UnityEngine;
using UnityEngine.UI;
using Yachu.Server.Packets.Body;

namespace Yachu.Client {
public class GameSelectManager : MonoBehaviour {

    public Canvas _canvas;
    private Camera _camera;
    public Image _selection;
    public float _selectionMoveSpeed = 20f;

    public Vector2 _floatingDiceSize;
    public Vector2 _keepingDiceSize;

    public Vector2 _cupSize;
    
    private void Awake() {
        _camera = Camera.main; 
        ResetSelection();
    }

    private bool _moveImmediately = false;
    private bool _isAnchoredPosition = false;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector2 _targetSize = Vector2.zero;
    private void Update() {
        if (!_selection.gameObject.activeInHierarchy) {
            return;   
        }

        var dt = Time.deltaTime;
        var t = _selection.rectTransform;
        if (_moveImmediately) {
            if (_isAnchoredPosition) {
                t.anchoredPosition = _targetPosition;
            }
            else {
                t.position = _targetPosition;
            }

            t.sizeDelta = _targetSize;
            _moveImmediately = false;
        }
        else {
            if (_isAnchoredPosition) {
                t.anchoredPosition = Vector3.Lerp(t.anchoredPosition, _targetPosition, dt * _selectionMoveSpeed);
            }
            else {
                t.position = Vector3.Lerp(t.position, _targetPosition, dt * _selectionMoveSpeed);
            }
            t.sizeDelta = Vector2.Lerp(t.sizeDelta, _targetSize, dt * _selectionMoveSpeed);
        }
    }

    public GameSelectPacket.SelectType? _currentSelectType = null;
    public int _currentSelectData = -1;

    public bool SelectDice(GameDice dice) {
        _isAnchoredPosition = false;
        _adjuster = null;
        var screenPoint = _camera.WorldToScreenPoint(dice.transform.position);
        SetSelection(screenPoint, dice.IsKeeping ? _keepingDiceSize : _floatingDiceSize);
        
        var changed = _currentSelectType != GameSelectPacket.SelectType.Dice || _currentSelectData != dice._index;
        _currentSelectType = GameSelectPacket.SelectType.Dice;
        _currentSelectData = dice._index;

        return changed;
    }

    public bool SelectCup(GameCup cup) {
        _isAnchoredPosition = false;
        _adjuster = null;
        var screenPoint = _camera.WorldToScreenPoint(cup.transform.position);
        SetSelection(screenPoint, _cupSize);

        var changed = _currentSelectType != GameSelectPacket.SelectType.Cup;
        _currentSelectType = GameSelectPacket.SelectType.Cup;
        _currentSelectData = -1;

        return changed;
    }

    private static readonly Vector2 ReferenceResolution = new Vector2(1280, 720);
    public bool SelectScoreboardSlot(GameUIScoreColumn scoreType) {
        _adjuster = () => {
            _isAnchoredPosition = false;
            var panelTransform = scoreType._panel.rectTransform;
            var panelRect = panelTransform.rect;
            var panelSize = new Vector2(panelRect.width, panelRect.height);
            var position = panelTransform.position;
            // position.x = position.x / Screen.width * ReferenceResolution.x;
            // position.y = position.y / Screen.height * ReferenceResolution.y;
            // var screenPoint = position + new Vector3(panelSize.x/2, 0);
            var screenPoint = position;
            var size = panelSize + new Vector2(20f, 20f);
            SetSelection(screenPoint, size);
        };
        _adjuster();

        var changed = _currentSelectType != GameSelectPacket.SelectType.ScoreBoard ||
                      _currentSelectData != scoreType._index;

        _currentSelectType = GameSelectPacket.SelectType.ScoreBoard;
        _currentSelectData = scoreType._index;
        return changed;
    }

    private void SetSelection(Vector3 screenPoint, Vector2 size) {
        if (!_selection.gameObject.activeSelf) {
            _moveImmediately = true;
        }
        _selection.gameObject.SetActive(true);
        _targetPosition = screenPoint;
        _targetSize = size;
    }

    private Action _adjuster;
    public void Adjust() {
        _adjuster?.Invoke();
    }

    public void ResetSelection() {
        _adjuster = null;
        _selection.gameObject.SetActive(false);
    }
}
}