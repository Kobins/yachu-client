using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yachu.Server.Packets.Body;
using Yachu.Server.Util;

namespace Yachu.Client {
[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class GameDice : MonoBehaviour {
    [HideInInspector]
    public Rigidbody _rigidbody;
    [HideInInspector]
    public AudioSource _audioSource;
    
    public int _index;
    public int _number;

    [Header("Debug")]
    public Image _debugImage;
    public Text _debugText;
    private Camera _camera;
    
    private static readonly List<Vector3> Faces = new List<Vector3>() {
        Vector3.forward,
        Vector3.down,
        Vector3.left,
        Vector3.right,
        Vector3.up,
        Vector3.back,
    };

    private void Awake() {
        // _camera = Camera.main;
        _rigidbody = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    public void Initialize(int index) {
        _index = index;
    }

    public void SetUnKeep() => KeepingIndex = -1;
    public bool IsKeeping => KeepingIndex >= 0;
    public int KeepingIndex { get; set; } = -1;
    public bool IsFreeze {
        get => _rigidbody.isKinematic;
        set {
            if (value) {
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }
            else {
                _rigidbody.isKinematic = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rigidbody.useGravity = true;
                _moved = 0f;
                _rotationDot = 1f;
            }
        }
    }

    public float Moved => _moved;
    public float RotatedDot => _rotationDot;
    
    private float _smoothingDelay = 25f;
    private float _moved = 0f;
    private float _rotationDot = 0f;
    private const float Epsilon = 0.0001f;
    private const float RotationEpsilon = (1 - Epsilon);
    public bool TransformChanged => !IsFreeze && (_moved > Epsilon || _rotationDot < RotationEpsilon);
    // public bool TransformChanged => !IsFreeze && (_rigidbody.velocity.sqrMagnitude <= 0.01 && _rigidbody.angularVelocity.sqrMagnitude <= 0.01);
    private Vector3 _lastPosition = Vector3.zero;
    private Quaternion _lastRotation = Quaternion.identity;

    private void FixedUpdate() {
        var gamePlayManager = GamePlayManager.Instance;
        // 로컬 턴일 때
        if (!IsFreeze) {
            if (GameManager.Instance.State != GameState.Playing || gamePlayManager.IsLocalTurn) {
                var newPosition = _rigidbody.position;
                var newRotation = _rigidbody.rotation;

                _moved = (newPosition - _lastPosition).sqrMagnitude;
                _rotationDot = Quaternion.Dot(newRotation, _lastRotation);

                _lastPosition = newPosition;
                _lastRotation = newRotation;

                if (_moved > 10f) {
                    _rigidbody.velocity = Vector3.zero;
                }
            }
        }

        // 원격 턴일 때 주사위 이동
        if (GameManager.Instance.State == GameState.Playing 
            && !gamePlayManager.IsLocalTurn
            && (gamePlayManager._state == GamePlayState.CupShaking || gamePlayManager._state == GamePlayState.DiceThrowing)
        ) {
            _rigidbody.position = (Vector3.Lerp(_rigidbody.position, _remotePosition, Time.deltaTime * _smoothingDelay));
            _rigidbody.rotation = (Quaternion.Slerp(_rigidbody.rotation, _remoteRotation, Time.deltaTime * _smoothingDelay));
        }
    }

    public Tuple<Vector3, Quaternion> TransformForSendToRemote {
        get {
            var thisTransform = transform;
            return new Tuple<Vector3, Quaternion>(thisTransform.position - _remoteOffset, thisTransform.rotation);
        }
    }
    private readonly Vector3 _remoteOffset = new Vector3(4f, 0f, 0f);
    private Vector3 _remotePosition = Vector3.zero;
    private Quaternion _remoteRotation = Quaternion.identity;
    public void RemoteUpdate(DiceTransform remoteTransform) {
        var (x, y, z) = remoteTransform.GetPosition();
        _remotePosition = new Vector3(x, y, z) + _remoteOffset;
            
        remoteTransform.GetRotation(out var q);
        _remoteRotation = new Quaternion(q[0], q[1], q[2], q[3]);
    }


    private readonly Vector3 _worldUp = Vector3.up;
    public int CalculateNumber() {
        var t = transform;
        var forward = t.forward;
        var up = t.up;
        var right = t.right;
        var faces = new List<Vector3>() {
            forward,
            -up,
            -right,
            right,
            up,
            -forward,
        };

        // 위쪽하고 가장 방향이 일치하는 번호 찾기
        int nearest = 0;
        float nearestDot = Vector3.Dot(_worldUp, faces[0]);
        for (int i = 1; i < 6; i++) {
            // 내적 결과: 위쪽 방향과의 cos값
            // 가까울 수록 1, 멀 수록 -1
            var dot = Vector3.Dot(_worldUp, faces[i]);
            if (dot > nearestDot) {
                nearest = i;
                nearestDot = dot;
            }
        }
        _number = nearest + 1;
        return _number;
    }

    private DiceHitMaterialType GetHitMaterialType(Component c) {
        if (c.CompareTag("Dice")) {
            return DiceHitMaterialType.Dice;
        }
        if (c.CompareTag("Cup")) {
            return DiceHitMaterialType.Cup;
        }
        if (c.CompareTag("DiceBoard")) {
            return DiceHitMaterialType.BoardFloor;
        }
        if (c.CompareTag("DiceBoardWall")) {
            return DiceHitMaterialType.BoardWall;
        }

        return DiceHitMaterialType.TypeCount;
    }

    [Header("Sounds")] 
    public float _weakPower = 1f;
    public float _hardPower = 10f;
    public float _maxPower = 100f;
    private DiceHitSoundType GetSoundType(float power) {
        return power <= _weakPower ? DiceHitSoundType.Weak 
            : power <= _hardPower ? DiceHitSoundType.Hard 
            : power <= _maxPower ? DiceHitSoundType.Hardest : DiceHitSoundType.TypeCount;
    }

    public bool PlaySound(DiceHitSoundType soundType, DiceHitMaterialType materialType) {
        var sound = SoundManager.Instance[materialType, soundType];
        if (sound == null) {
            return false;
        }
        _audioSource.PlayOneShot(sound);
        return true;
    }
    private DiceHitMaterialType _lastMaterialType = DiceHitMaterialType.TypeCount;
    private DiceHitSoundType _lastSoundType = DiceHitSoundType.TypeCount;
    private void OnCollisionEnter(Collision collision) {
        var power = collision.impulse;

        var soundType = GetSoundType(power.magnitude);
        if (soundType == DiceHitSoundType.TypeCount) {
            return;
        }
        var hitMaterialType = GetHitMaterialType(collision.collider);
        if (hitMaterialType == DiceHitMaterialType.TypeCount) {
            return;
        }

        if (!PlaySound(soundType, hitMaterialType)) return;
        
        _lastMaterialType = hitMaterialType;
        _lastSoundType = soundType;
        /*
        var popup = DebugUIPopup.Generate(GamePlayManager.Instance._gamePlayCanvas);
        popup.Initialize(power.magnitude.ToString(), collision.GetContact(0).point);
        */
    }

    public bool PopLastSound(out DiceHitMaterialType materialType, out DiceHitSoundType soundType) {
        if (_lastMaterialType == DiceHitMaterialType.TypeCount || _lastSoundType == DiceHitSoundType.TypeCount) {
            materialType = DiceHitMaterialType.TypeCount;
            soundType = DiceHitSoundType.TypeCount;
            return false;
        }

        materialType = _lastMaterialType;
        soundType = _lastSoundType;
        _lastMaterialType = DiceHitMaterialType.TypeCount;
        _lastSoundType = DiceHitSoundType.TypeCount;
        return true;
    }

    public struct Triple<T1, T2, T3> {
        public T1 First;
        public T2 Second;
        public T3 Third;
        public Triple(T1 first, T2 second, T3 third) {
            First = first;
            Second = second;
            Third = third;
        }

        public void Deconstruct(out T1 first, out T2 second, out T3 third) {
            first = First;
            second = Second;
            third = Third;
        }
    }

    private void Update() {
        ProjectDirection();
    }

    private static void ApplyToMatrix(ref Matrix4x4 matrix, int face, Vector3 vector) {
        var v4 = new Vector4(vector.x, vector.y, vector.z);
        switch (face) {
            case 0:
                // forward
                matrix.SetColumn(2, v4);
                return;
            case 1:
                // -up
                matrix.SetColumn(1, -v4);
                return;
            case 2:
                // -right
                matrix.SetColumn(0, -v4);
                return;
            case 3:
                // right
                matrix.SetColumn(0, v4);
                return;
            case 4:
                // up
                matrix.SetColumn(1, v4);
                return;
            case 5:
                // -forward
                matrix.SetColumn(2, -v4);
                return;
        }
    }
    private List<Triple<int, Vector3, float>> _dotFaces = new List<Triple<int, Vector3, float>>(6);
    public Transform testTransform;
    public bool useTest = false;
    private Matrix4x4 matrix;
    public Vector3 row1;
    public Vector3 row2;
    public Vector3 row3;
    public void ProjectDirection() {
        if(!useTest) return;
        var t = transform;
        var forward = t.forward;
        var up = t.up;
        var right = t.right;
        var faces = new List<Vector3>() {
            forward,
            -up,
            -right,
            right,
            up,
            -forward,
        };

        _dotFaces.Clear();
        for (int i = 0; i < 6; ++i) {
            _dotFaces.Add(new Triple<int, Vector3, float>(i, faces[i], Vector3.Dot(_worldUp, faces[i])));
        }
        _dotFaces.Sort((a, b) => -a.Third.CompareTo(b.Third));

        // 가장 업 벡터와 가까운 벡터: 주사위의 윗 면
        var first = _dotFaces[0];
        var firstAxis = first.Second;
        
        // 두 번째로 가까운 벡터를 xz평면에 투영 -> 기울기를 없앰
        var second = _dotFaces[1];
        var secondAxis = second.Second;
        var position = t.position;
        Debug.DrawRay(position, forward * 10, Color.blue);
        Debug.DrawRay(position, right * 10, Color.red);
        Debug.DrawRay(position, up * 10, Color.green);
        Debug.DrawRay(position, firstAxis * 10, new Color(1f, 1f, 1f));
        Debug.DrawRay(position, secondAxis * 10, new Color(1f, 0f, 0.38f));
        Debug.DrawRay(position, _worldUp * 10, Color.yellow);
        var secondProjection = secondAxis;
        secondProjection.y = 0f; secondProjection.Normalize();
        Debug.DrawRay(position, secondProjection * 10, Color.magenta);
        
        // 나머지는 업 벡터와 투영 벡터를 외적해 구하기
        var projectionRight = Vector3.Cross(Vector3.up, secondProjection);
        projectionRight.Normalize();
        Debug.DrawRay(position, projectionRight * 10, Color.cyan);

        if (testTransform) {
            matrix = new Matrix4x4();
            ApplyToMatrix(ref matrix, first.First, Vector3.up);
            ApplyToMatrix(ref matrix, second.First, secondProjection);
            
            // 외적한 벡터를 회전행렬에 반영할 때, 나머지 4개 면 중 가장 가까운 면에 회전행렬 반영
            var third = _dotFaces[2];
            var thirdDot = Vector3.Dot(projectionRight, third.Second);
            for (int i = 3; i < 6; ++i) {
                float dot = Vector3.Dot(projectionRight, _dotFaces[i].Second);
                // 내적이기때문에 클 수록 가까움
                if (dot >= thirdDot) {
                    thirdDot = dot;
                    third = _dotFaces[i];
                }
            }
            ApplyToMatrix(ref matrix, third.First, projectionRight);
            Quaternion newRotation = QuaternionUtil.FromMatrix(matrix);
            testTransform.rotation = newRotation;

            row1 = matrix.GetRow(0);
            row2 = matrix.GetRow(1);
            row3 = matrix.GetRow(2);
            Debug.DrawRay(testTransform.position, matrix.GetColumn(2) * 3, Color.blue);
            Debug.DrawRay(testTransform.position, matrix.GetColumn(0) * 3, Color.red);
            Debug.DrawRay(testTransform.position, matrix.GetColumn(1) * 3, Color.green);
        }

    }
}
}