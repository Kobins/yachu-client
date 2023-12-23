using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yachu.Client {
public class DebugUIPopup : MonoBehaviour {
    public Text _text;

    public void Initialize(string text, Vector3 worldPosition, float duration = 1f, bool log = true) {
        var camera = Camera.main;

        var position = camera.WorldToScreenPoint(worldPosition);
        _text.text = text;
        transform.position = position;
        Destroy(gameObject, duration);

        if (log) {
            Debug.Log($"{text}");
        }
    }

    private static readonly string Key = "Prefabs/DebugPopup";
    private static DebugUIPopup _prefab = null;
    public static DebugUIPopup Generate(Canvas parentCanvas) {
        return Instantiate(_prefab ??= Resources.Load<GameObject>(Key).GetComponent<DebugUIPopup>(), parentCanvas.transform);
    }
}
}