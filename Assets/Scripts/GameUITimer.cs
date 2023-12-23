using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {
[RequireComponent(typeof(RectTransform))]
public class GameUITimer : MonoBehaviour {
    [HideInInspector] public RectTransform _transform;
    public Image _timerCircle;
    public Text _text;

    public float _showSpeed = 15f;
    
    private Vector2 _initialPosition;
    private Vector2 _hidePosition;
    private void Awake() {
        _transform = GetComponent<RectTransform>();
        _initialPosition = _transform.anchoredPosition;
        _hidePosition = _initialPosition + Vector2.down * _transform.rect.height;
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        _transform.anchoredPosition = _hidePosition;
        gameObject.SetActive(true);
    }
    public void UpdateTimer(float ratio, int count) {
        if (!gameObject.activeSelf) {
            Show();
        }
        _timerCircle.fillAmount = Mathf.Clamp01(ratio);
        _text.text = count.ToString();
    }

    private void Update() {
        _transform.anchoredPosition = Vector2.Lerp(_transform.anchoredPosition, _initialPosition, _showSpeed * Time.deltaTime);
    }
}
}