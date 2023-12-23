using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {
[RequireComponent(typeof(RectTransform))]
public class GameUIDiceCount : MonoBehaviour {
    private Camera _camera;
    private RectTransform _transform;
    public float _lerpSpeed = 20f;
    public Text _text;

    private Vector3 _initialPosition;
    private void Awake() {
        _camera = Camera.main;
        _transform = GetComponent<RectTransform>();
        _initialPosition = _transform.anchoredPosition;
    }

    private Transform _targetObject;
    public void Follow(GameObject worldObject) {
        _targetObject = worldObject.transform;
    }

    public void Unfollow() {
        _targetObject = null;
    }

    public void SetLeftCount(int count) {
        gameObject.SetActive(true);
        _text.text = $"{count} 번 남음";
    }

    public Vector3 _followOffset = new Vector3(0f, 30f);
    private void LateUpdate() {
        if (_targetObject) {
            var rect = _transform.rect;
            var offset = new Vector2(rect.width / 2, rect.height / 2);
            var targetPosition = _camera.WorldToScreenPoint(_targetObject.position) + (Vector3)offset + _followOffset;
            _transform.position = Vector3.Lerp(
                _transform.position, 
                targetPosition, 
                Time.deltaTime * _lerpSpeed
            );
        }
        else {
            _transform.anchoredPosition = Vector3.Lerp(
                _transform.anchoredPosition, 
                _initialPosition, 
                Time.deltaTime * _lerpSpeed
            );
        }
    }

    public void Disable() {
        gameObject.SetActive(false);
    }
}
}