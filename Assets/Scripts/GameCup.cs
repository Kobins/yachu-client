using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yachu.Server.Packets.Body;

namespace Yachu.Client {
// [RequireComponent(typeof(Animator))]
public class GameCup : MonoBehaviour {

    private Rigidbody _rigidbody;
    private Animator _animator;
    public Transform _cupClone;

    public Collider[] _colliders;
    
    public GameObject _textPosition;
    public Collider _areaCollider;
    public Transform _diceInitializePosition;
    public GameObject _cover;
    private static readonly int BoolAutoShake = Animator.StringToHash("auto_shake");
    private static readonly int TriggerCancelShake = Animator.StringToHash("cancel_shake");
    private static readonly int TriggerStartShake = Animator.StringToHash("shake");
    private static readonly int TriggerDiceThrow = Animator.StringToHash("throw");

    private void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = _cupClone.GetComponent<Animator>();
    }

    public void StartShake() {
        _animator.SetTrigger(TriggerStartShake);
        _cover.gameObject.SetActive(true);
        if (!ControlByRemote) {
            UseCollider = true;
        }
    }

    public void CancelShake() {
        AutoShaking = false;
        _animator.SetTrigger(TriggerCancelShake);
        if (!ControlByRemote) {
            UseCollider = false;
        }
    }

    private bool _useCollider = true;

    public bool UseCollider {
        get => _useCollider;
        set {
            _useCollider = value;

            for (int i = 0; i < _colliders.Length; i++) {
                _colliders[i].enabled = value;
            }
        }
    }

    private bool _isAutoShaking = false;
    public bool AutoShaking {
        get => _isAutoShaking;
        set {
            if (_isAutoShaking != value) {
                _animator.SetBool(BoolAutoShake, value);
            }
            _isAutoShaking = value;
        }
    }

    public void ClampDice(GameDice dice) {
        if(dice.IsFreeze || dice.IsKeeping) return;
        if (_areaCollider && _areaCollider.gameObject.activeSelf) {
            var diceRigidbody = dice._rigidbody;
            var dicePosition = diceRigidbody.position;
        
            var closestPoint = _areaCollider.ClosestPoint(dicePosition);
            if (dicePosition != closestPoint) {
                diceRigidbody.position = (_diceInitializePosition.position);
                diceRigidbody.velocity = Vector3.zero;
                diceRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private Action _coverDisabledCallback;
    private Action _diceThrowEndCallback;
    public void DiceThrow(Action coverDisabledCallback, Action diceThrowEndCallback) {
        _coverDisabledCallback = coverDisabledCallback;
        _diceThrowEndCallback = diceThrowEndCallback;
        _animator.SetTrigger(TriggerDiceThrow);
        // AutoShaking = false;
    }

    public void OnCoverDisabled() {
        // Debug.Log("OnCoverDisabled called");
        _coverDisabledCallback?.Invoke();
        _coverDisabledCallback = null;
        AutoShaking = false;
    }
    public void OnStartDisappear() {
        if (!ControlByRemote) {
            UseCollider = false;
        }
    }

    public void OnDiceThrowEnd() {
        // Debug.Log("OnDiceThrowEnd called");
        _diceThrowEndCallback?.Invoke();
        _diceThrowEndCallback = null;
    }

    private bool _controlByRemote = false;
    public bool ControlByRemote {
        get => _controlByRemote;
        set {
            _controlByRemote = value;
            if (value) {
                _animator.enabled = false;
                UseCollider = false;
            }
            else {
                _animator.enabled = true;
                _animator.Rebind();
                UseCollider = true;
            }
        }
    }

    private void FixedUpdate() {
        if(_controlByRemote) return;
        var targetTransform = _cupClone;
        var targetPosition = targetTransform.position;
        var targetRotation = targetTransform.rotation;
        var sqrDistance = (targetPosition - _rigidbody.position).sqrMagnitude;
        if (sqrDistance > 0) {
            _rigidbody.MovePosition(targetTransform.position);
        }

        var rotationDot = Quaternion.Dot(targetRotation, _rigidbody.rotation);
        if (rotationDot < 1) {
            _rigidbody.MoveRotation(targetTransform.rotation);
        }
    }

    public Tuple<Vector3, Quaternion> TransformForSendToRemote {
        get {
            var thisTransform = transform;
            return new Tuple<Vector3, Quaternion>(thisTransform.position - _remoteOffset, thisTransform.rotation);
        }
    }
    private readonly Vector3 _remoteOffset = new Vector3(8f, 0f, 0f);
    private Vector3 _remotePosition = Vector3.zero;
    private Quaternion _remoteRotation = Quaternion.identity;
    public void RemoteUpdate(Vector3 position, Quaternion rotation) {
        _remotePosition = position + _remoteOffset;
        _remoteRotation = rotation;
    }

    private float _smoothingDelay = 25f;
    private void Update() {
        if(!ControlByRemote) return;
        var gamePlayManager = GamePlayManager.Instance;
        var thisTransform = transform;
        if (GameManager.Instance.State == GameState.Playing && !gamePlayManager.IsLocalTurn) {
            thisTransform.position = (Vector3.Lerp(thisTransform.position, _remotePosition, Time.deltaTime * _smoothingDelay));
            thisTransform.rotation = (Quaternion.Slerp(thisTransform.rotation, _remoteRotation, Time.deltaTime * _smoothingDelay));
        }
    }
}
}