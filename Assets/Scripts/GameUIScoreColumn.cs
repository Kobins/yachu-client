using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Yachu.Client {

public class GameUIScoreColumn : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public int _calculatedScore;
    public Image _panel;
    public Text _text;

    [ContextMenu("Bind")]
    private void Bind()
    {
        _panel = GetComponent<Image>();
        _text = GetComponentInChildren<Text>();
    }
    
    public int _index;
    public int _column;

    public int _markFontSize = 56;
    public readonly float _markAnimationSpeed = 8f;
    private int _initialFontSize;
    private void Awake() {
        _initialFontSize = _text.fontSize;
    }

    public void Initialize(int index, int column) {
        _index = index;
        _column = column;
    }
    
    public Action OnClick { get; set; }
    public Action OnMouseEnter { get; set; }
    public Action OnMouseExit { get; set; }

    
    public void OnPointerClick(PointerEventData eventData) {
        OnClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        OnMouseEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnMouseExit?.Invoke();
    }


    public void SetScore(int? score) {
        if (score == null) {
            _calculatedScore = 0;
            _text.text = "";
        }
        else {
            _calculatedScore = score.Value;
            _text.text = score.ToString();
        }
    }

    public void Mark() {
        _text.fontSize = _markFontSize;
        StartCoroutine(MarkCoroutine());
    }

    private IEnumerator MarkCoroutine() {
        float size = _text.fontSize;
        while (_text.fontSize != _initialFontSize) {
            size = Mathf.Lerp(size, _initialFontSize, _markAnimationSpeed * Time.deltaTime);
            _text.fontSize = (int)size;
            yield return null;
        }    
    }
    
}
}