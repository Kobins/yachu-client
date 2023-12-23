using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {
[RequireComponent(typeof(Animator))]
public class GameUIMadeText : MonoBehaviour {
    private Camera _camera;
    private Animator _animator;
    public Text _text;
    private static readonly int Start = Animator.StringToHash("start");

    private void Awake() {
        _camera = Camera.main;
        _animator = GetComponent<Animator>();
    }

    public void Made(Vector3 worldPosition, string text) {
        if (!gameObject.activeSelf) {
           gameObject.SetActive(true); 
        }
        var position = _camera.WorldToScreenPoint(worldPosition);
        _text.rectTransform.position = position;
                                        // + new Vector3(0f, _text.rectTransform.rect.height, 0f);
        _text.text = text;
        
        _animator.SetTrigger(Start);
    }

    public void OnAnimationEnd() {
        Disable();
    }

    public void Disable() {
        if (gameObject.activeSelf) {
            _animator.ResetTrigger(Start);
            gameObject.SetActive(false);
        }
    }
}
}