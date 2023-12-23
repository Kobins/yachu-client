using UnityEngine;

namespace Yachu.Client {
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {
    private static T _instance = null;

    public static T Instance {
        get {
            if (ReferenceEquals(_instance, null)) {
                _instance = FindObjectOfType<T>();
                if (ReferenceEquals(_instance, null)) {
                    Debug.Log($"Cannot find MonoSingleton {nameof(T)}, create one but not works properly");
                    var gameObject = new GameObject(nameof(T));
                    _instance = gameObject.AddComponent<T>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    private void Awake() {
        if (_instance == null) {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this) {
            // Debug.LogError("duplicate singleton detected", this);
            Destroy(gameObject);
            return;
        }

        OnAwake();
    }

    protected virtual void OnAwake() {
    }
}
}