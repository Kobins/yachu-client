using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yachu.Client {
// https://tistory.jeon.sh/59
public class Dispatcher : MonoBehaviour {
    private static Dispatcher main = null;
    private readonly Queue<Action> queued = new Queue<Action>();

    private void Awake() {
        Init();
    }

    private void Update() {
        lock (queued) {
            while (queued.Count > 0) {
                queued.Dequeue().Invoke();
            }
        }
    }

    public static void Init() {
        if (main == null) {
            main = FindObjectOfType<Dispatcher>();
            if (main == null) {
                main = new GameObject("[Dispatcher]").AddComponent<Dispatcher>();
            }
        }

        DontDestroyOnLoad(main.gameObject);
    }

    public static void Register(Action action) {
        main.queued.Enqueue(action);
    }

    public static void Register(IEnumerator action) {
        Register(() => main.StartCoroutine(action));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDomain() {
        main = null;
    }
}
}