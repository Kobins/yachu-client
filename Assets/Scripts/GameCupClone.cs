using UnityEngine;

namespace Yachu.Client {
public class GameCupClone : MonoBehaviour {
    public GameCup _original;
    
    public void OnCoverDisabled() {
        // Debug.Log("OnCoverDisabled called");
        _original.OnCoverDisabled();
    }

    public void OnStartDisappear() {
        _original.OnStartDisappear();
    }
    
    public void OnDiceThrowEnd() {
        // Debug.Log("OnDiceThrowEnd called");
        _original.OnDiceThrowEnd();
    }
}
}