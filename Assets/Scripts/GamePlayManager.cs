using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Yachu.Server;
using Yachu.Server.Gameplay;
using Yachu.Server.Packets;
using Yachu.Server.Packets.Body;
using Yachu.Server.Util;

namespace Yachu.Client {
public class GamePlayManager : MonoSingleton<GamePlayManager> {
    [Header("UI")] 
    public Canvas _gamePlayCanvas;
    public GameUIScoreboard _scoreboard;
    public GameSelectManager _selectManager;
    public GameUICurrentPlayerText _currentTurnPlayerText;
    public GameUIDiceCount _diceCountText;
    public GameUIMadeText _madeText;
    public GameUITimer _timer;

    [Header("Gameplay")] 
    public GameCup _cup;
    public List<GameDice> _dices;
    public List<GameDiceHolder> _diceHolders;
    public Collider _diceBoardAreaCollider;

    public List<Quaternion> _diceFixedRotations;
    public Transform _diceFloatPosition;
    public float _diceFloatSpacing = 1f;
    public float _diceFixPositionSpeed = 50f;
    public float _diceFixRotationSpeed = 50f;

    public float _diceThrowMaxTime = 8f;
    public int _diceThrowCount = 0;
    public int _currentTurn = 0;
    public int _currentPlayerIndex;
    public float _currentTurnLeftTime = Constants.TimeLimitInTurn;
    public GamePlayState _state = GamePlayState.SceneLoading;
    public bool IsLocalTurn => _currentPlayerIndex == GameManager.Instance._thisClientIndex;

    [Header("Debugging")] 
    public List<Text> _diceDebugTexts;

    protected override void OnAwake() {
        RegisterPacketListeners();
        _scoreboard.gameObject.SetActive(false);
        _scoreboard.OnSlotInteract = OnSlotInteract;
        _scoreboard._scrollRect.onValueChanged.AddListener((_) => {
            _selectManager.Adjust();
        });
        for (int i = 0; i < 5; ++i) {
            _dices[i]._index = i;
        }

        for (int i = 0; i < 6; ++i) {
            _diceFixedRotations[i].Normalize();
        }
        _currentTurnPlayerText.Disable();
        _diceCountText.Disable();
        _madeText.Disable();
        _timer.Hide();
    }

    private SoundManager _soundManager;
    private Camera _camera;

    private void Start() {
        _camera = Camera.main;
        _soundManager = SoundManager.Instance;
    }

    private void RegisterPacketListeners() {
        PacketHandler<Unit>.RegisterListeners(new List<PacketListener<Unit>>() {
            new PacketListener<Unit>(PacketType.GameTurnStart, (packet, _) => TurnStart(packet.GetPacketData<GameTurnStartPacket>())),
            new PacketListener<Unit>(PacketType.GameCupUpdate,
                (packet, _) => RemoteUpdateCupPosition(packet.GetPacketData<GameCupUpdatePacket>())),
            new PacketListener<Unit>(PacketType.GameDiceUpdate,
                (packet, _) => RemoteUpdateDice(packet.GetPacketData<GameDiceUpdatePacket>())),
            new PacketListener<Unit>(PacketType.GameDiceThrow, (packet, _) => ThrowDice()),
            new PacketListener<Unit>(PacketType.GameDiceDetermined, (packet, _) => OnDiceDetermined(packet.GetPacketData<GameDiceDeterminedPacket>())),
            new PacketListener<Unit>(PacketType.GameSelect,
                (packet, _) => OnSelect(packet.GetPacketData<GameSelectPacket>())),
            new PacketListener<Unit>(PacketType.GameEnd, OnReceivedGameEndPacket),
        });
    }

    // 서버에서 다음 턴 신호가 왔을 때
    private void TurnStart(GameTurnStartPacket packet) {
        var gameManager = GameManager.Instance;
        var state = gameManager.State;
        if (state != GameState.Preparing && state != GameState.Playing) {
            return;
        }

        if (state == GameState.Preparing) {
            GameManager.Instance.State = GameState.Playing;
            _scoreboard.gameObject.SetActive(true);
            _scoreboard.Reset();
            _scoreboard.SetPlayerName(gameManager._currentRoomPlayers.ConvertAll(it => it.name));
            
        }
        
        
        var now = ExtraUtil.CurrentTimeInMillis;
        var timeElapsed = (now - packet.Timestamp) / 1000f;
        _currentTurnLeftTime = Constants.TimeLimitInTurn - timeElapsed;
        _diceThrowCount = 0;
        _currentTurn = packet.Turn;
        var playerCount = GameManager.Instance._currentRoomPlayers.Count;
        _currentPlayerIndex = _currentTurn % playerCount;
        _scoreboard.SetTurn(_currentTurn / playerCount + 1);
        _scoreboard.SetHighlight(_currentPlayerIndex);
        _scoreboard.UpdateScore(_currentPlayerIndex, null);
        ResetSelection();
        var isLocalTurn = IsLocalTurn;
        if (!isLocalTurn) {
            _currentTurnPlayerText.SetOtherPlayer(gameManager._currentRoomPlayers[_currentPlayerIndex].name);
        }
        else {
            _currentTurnPlayerText.SetSelf();
        }

        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            dice.IsFreeze = !isLocalTurn;
            dice.SetUnKeep();
            _keepingDicesIndex[i] = -1;
        }
        _cup.ControlByRemote = !isLocalTurn;

        StartCupShaking();
        _soundManager.PlayCupShakingMusic();
    }

    private class LastSlotInteraction {
        public int index;
        public GameUIScoreType scoreType;
        public int column;
        public GameUIScoreColumn scoreColumn;
    }

    private LastSlotInteraction _lastSlotInteraction = null;
    private void OnSlotInteract(GameUIScoreboard.SlotInteractType type, int index, GameUIScoreType scoreType, int column, GameUIScoreColumn scoreColumn) {
        // 내 턴일 때에만 처리함
        if (!IsLocalTurn) {
            // 내 턴이 아닐 때 Slot 설명 볼 수 있게 하기
            Debug.Log($"OnSlotInteract :: Not my turn, Type: {type}, Index: {index}, _currentSelectType: {_selectManager._currentSelectType}");
            
            // 근데 이미 상대가 ScoreBoard를 보고 있으면 안 됨
            if (_selectManager._currentSelectType == GameSelectPacket.SelectType.ScoreBoard)
            {
                return;
            }

            // 마우스 올린 경우 설명 보기
            if (type == GameUIScoreboard.SlotInteractType.MouseOver)
            {
                UpdateScoreboardDescriptor(index);   
            }
            // 마우스 뗀 경우 설명 해제 
            else if (type == GameUIScoreboard.SlotInteractType.MouseExit)
            {
                UpdateScoreboardDescriptor();
            }
            return;
         
        }

        // 내 column일 때에만 처리함
        if (_currentPlayerIndex != column) { return; }

        // 이미 채워져 있으면 무시
        if (_scoreboard._storages[_currentPlayerIndex][(YachuScoreTypeEnum) index] != null) {
            return;
        }

        // Selecting일 때에만 처리함
        if (_state != GamePlayState.Selecting) {
            // Selecting 아닐 때 본 게 있으면
            _lastSlotInteraction = new LastSlotInteraction {
                index = index,
                scoreType = scoreType,
                column = column,
                scoreColumn = scoreColumn,
            };
            return;
        }
        
        switch (type) {
            case GameUIScoreboard.SlotInteractType.Click: {
                MarkScore(index, scoreColumn._calculatedScore);
                ResetSelection();
                break;
            }
            case GameUIScoreboard.SlotInteractType.MouseOver: {
                SelectScoreboard(scoreColumn);
                break;
            }
        }
    }

    private void StartCupShaking() {
        if (_state == GamePlayState.Selecting && _dices.All(it => it.IsKeeping)) {
            return;
        }

        if (_diceThrowCount >= 3) {
            return;
        }
        _diceCountText.SetLeftCount(3 - _diceThrowCount);
        _diceCountText.Follow(_cup._textPosition);
        DisableMadeText();
        _state = GamePlayState.CupShaking;
        _cupHolding = false;
        _cup.StartShake();
        var isLocalTurn = IsLocalTurn;
        foreach (var dice in _dices) {
            if (_diceThrowCount > 0 && dice.IsKeeping) {
                continue;
            }

            dice._rigidbody.rotation = Quaternion.identity;
            dice._rigidbody.position = _cup._diceInitializePosition.position;
            // 로컬 턴이면 주사위 직접 움직임, 원격 턴이면 원격이 움직여줌
            dice.IsFreeze = !isLocalTurn;
            if (_diceThrowCount == 0) {
                dice.SetUnKeep();
            }
        }
        
        if (isLocalTurn) {
            NetworkManager.Instance.Send(_selectPacketCached.SetCup(true, true));    
        }
        ResetSelection();
    }

    private void CancelCupShaking() {
        if (_diceThrowCount == 0) return;
        _cupHolding = false;
        _cup.CancelShake();
        _diceCountText.Unfollow();
        StartSelecting();
        
        if (IsLocalTurn) {
            NetworkManager.Instance.Send(_selectPacketCached.SetCup(true, false));
        }
    }
    
    private void ClampDiceToDiceBoard(GameDice dice) {
        if(dice.IsFreeze || dice.IsKeeping) return;
        var diceRigidbody = dice._rigidbody;
        var dicePosition = diceRigidbody.position;
        
        var closestPoint = _diceBoardAreaCollider.ClosestPoint(dicePosition);
        if (dicePosition != closestPoint) {
            diceRigidbody.position = closestPoint;
            diceRigidbody.AddForce(closestPoint - dicePosition);
        }
    }

    private void DebugUpdateDice() {
        for (int i = 0; i < _diceDebugTexts.Count; ++i) {
            var dice = _dices[i];
            if (dice.IsFreeze) {
                _diceDebugTexts[i].text = "Freeze";
                continue;
            }

            _diceDebugTexts[i].text = $"move: {dice.Moved:F5}, rotateDot: {dice.RotatedDot:F5}";
        }
    }
    
    private void FixedUpdate() {
        var gameManager = GameManager.Instance;
        if (gameManager.State != GameState.Playing) {
            for (int i = 0; i < 5; ++i) {
                _cup.ClampDice(_dices[i]);
            }
            DebugUpdateDice();
            return;
        }

        if (IsLocalTurn) {
            switch (_state) {
                case GamePlayState.CupShaking: {
                    // 애니메이션이 끝나지 않은 동안은 주사위를 가둠
                    if (_cup._cover.activeSelf /* && !_cupAnimationEnd */) {
                        for (int i = 0; i < 5; ++i) {
                            _cup.ClampDice(_dices[i]);
                        }
                    }

                    break;
                }
                case GamePlayState.DiceThrowing: {
                    if (_cup._cover.activeSelf) {
                        for (int i = 0; i < 5; ++i) {
                            _cup.ClampDice(_dices[i]);
                        }
                    }
                    if (_cupAnimationEnd) {
                        for (int i = 0; i < 5; ++i) {
                            ClampDiceToDiceBoard(_dices[i]);  
                        }
                    }

                    break;
                }
            }
            DebugUpdateDice();
        }
    }

    private bool _cupHolding = false;
    private bool _diceDetermined = false;

    
    private const float TimerRevealTime = 15f;
    private bool TimeCalculation() {
        if (_currentTurnLeftTime > 0) {
            _currentTurnLeftTime -= Time.deltaTime;

            var leftTimeInteger = Mathf.CeilToInt(_currentTurnLeftTime);
            if (_currentTurnLeftTime <= TimerRevealTime) {
                _timer.UpdateTimer(_currentTurnLeftTime / TimerRevealTime, leftTimeInteger);
            }
            if (_currentTurnLeftTime < 0) {
                _currentTurnLeftTime = 0f;
                return true;
            }
        }

        return false;
    }
    private void Update() {
        var gameManager = GameManager.Instance;
        if (gameManager.State != GameState.Playing) {
            return;
        }

        var dt = Time.deltaTime;
        if (IsLocalTurn) {
            switch (_state) {
                // TODO 입력 받아서 컵 흔들기
                case GamePlayState.CupShaking: {
                    // 시간 초과 시 그냥 주사위 던져버림
                    if (TimeCalculation()) {
                        ThrowDice();
                        return;
                    }
                    // 컵 흔들기 취소
                    if (Input.GetKeyDown(KeyCode.Escape)) {
                        CancelCupShaking();
                    }

                    // 키보드로 흔들기 -> 누르고 있으면 자동 흔들기
                    if (Input.GetKeyDown(KeyCode.Return)) {
                        _cup.AutoShaking = true;
                    }
                    // 떼면 바로 던짐
                    else if (Input.GetKeyUp(KeyCode.Return)) {
                        ThrowDice();
                    }

                    RaycastHit hit;
                    if (Input.GetMouseButtonDown(0)) {
                        if (RaycastMouse(out hit)) {
                            var hitTransform = hit.transform;
                            if (hitTransform.CompareTag("Cup")) {
                                _cup.AutoShaking = true;
                            }
                            // 다른 거 건드리면 취소
                            else if (hitTransform.CompareTag("Background") 
                                     || hitTransform.CompareTag("DiceBoard") 
                                     || hitTransform.CompareTag("DiceBoardWall") 
                                     || hitTransform.CompareTag("DiceHolder")
                            ) {
                                CancelCupShaking();
                            }
                        }
                    }

                    // 흔드는 중에 마우스 뗌, 여전히 컵이나 주사위를 마우스로 가리키고 있으면 던짐
                    // 아니고 마우스는 뗐는데 바깥에 있으면 안 던짐
                    if (_cup.AutoShaking && Input.GetMouseButtonUp(0)) {
                        if (RaycastMouse(out hit)) {
                            var hitTransform = hit.transform;
                            if (hitTransform.CompareTag("Cup") || hitTransform.CompareTag("Dice")) {
                                ThrowDice();
                            }
                            else {
                                _cup.AutoShaking = false;
                            }
                        }
                        else {
                            _cup.AutoShaking = false;
                        }
                    }

                    break;
                }
                case GamePlayState.DiceThrowing: {
                    if (IsLocalTurn) {
                        var prevTime = _throwTime;
                        _throwTime += Time.deltaTime;
                        // 전부 안 움직이고, 컵 애니메이션 끝나면
                        if (!_diceDetermined && _cupAnimationEnd && (_dices.All(it => !it.TransformChanged) || _throwTime > _diceThrowMaxTime)) {
                            OnDiceDetermined();
                            _diceDetermined = true;
                        }
                    }

                    break;
                }
                case GamePlayState.Selecting: {
                    // 시간 초과 시 자동 마킹
                    if (TimeCalculation()) {
                        // 메이드가 있으면
                        if (_highestType != null) {
                            var (type, score) = _highestType;
                            MarkScore((int)type.TypeEnum, score);
                        }
                        // 남는 것 중 최대 점수에 할당
                        else {
                            var dicesSorted = SortedDicesNumber;
                            var scores = YachuScoreType.Types
                                    .Where(it => _scoreboard._storages[_currentPlayerIndex][it.TypeEnum] == null).ToList()
                                    .ConvertAll(it => new Tuple<YachuScoreType, int>(it, it.Calculator(dicesSorted)))
                                    .OrderByDescending(it => it.Item2).ToList()
                                ;
                            if (scores.Count <= 0) {
                                Debug.Log("오류 발생, 턴이 끝나지 않았는데 체크할 점수가 없음");
                                TurnEnd();
                            }
                            else {
                                var (type, score) = scores[0];
                                MarkScore((int)type.TypeEnum, score);
                            }
                        }
                        return;
                    }
                    if (RaycastMouse(out var hit)) {
                        var hitTransform = hit.transform;
                        // 주사위 키핑/언키핑
                        if (hitTransform.CompareTag("Dice")) {
                            if (_diceThrowCount < 3) {
                                var dice = hitTransform.GetComponent<GameDice>();
                                if (dice) {
                                    if (Input.GetMouseButtonDown(0)) {
                                        ToggleKeepDice(dice);
                                        ResetSelection();
                                    }
                                    else {
                                        SelectDice(dice);
                                    }
                                }
                            }
                        }
                        else if (hitTransform.CompareTag("Cup")) {
                            if (_diceThrowCount < 3 && _dices.Any(it => !it.IsKeeping)) {
                                var cup = hitTransform.GetComponent<GameCup>();
                                if (cup) {
                                    if (Input.GetMouseButtonDown(0)) {
                                        StartCupShaking();
                                    }
                                    else {
                                        SelectCup();
                                    }
                                }
                            }
                        }
                    }

                    break;
                }
            }

            // 주사위 움직임 동기화
            UpdateCupAndDiceToRemote();
        }
        else {
            TimeCalculation();
        }

        
        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            // Freeze된 주사위만 반영
            if ((!dice.IsKeeping || (_state != GamePlayState.CupShaking && _state != GamePlayState.DiceThrowing)) &&
                (!dice.IsFreeze || _state != GamePlayState.Selecting)) {
                continue;
            }
            var targetPosition = _dicePositions[i];
            var targetRotation = _diceFixedRotations[dice._number - 1];

            var diceRigidbody = dice._rigidbody;
            var dicePosition = diceRigidbody.position;
            var diceRotation = diceRigidbody.rotation;

            var sqrDistance = (targetPosition - dicePosition).sqrMagnitude;
            if (sqrDistance <= 0.001) {
                diceRigidbody.position = targetPosition;
                _diceSelectable[i] = true;
            }
            else {
                diceRigidbody.position = (Vector3.Lerp(dicePosition, targetPosition,
                    dt * _diceFixPositionSpeed));
                _diceSelectable[i] = false;
            }
            
            diceRigidbody.rotation = (Quaternion.Slerp(diceRotation, targetRotation,
                dt * _diceFixRotationSpeed));

            

        }
    }

    private static readonly RaycastHit EmptyHit = new RaycastHit(); 
    private bool RaycastMouse(
        out RaycastHit hitInfo,
        float maxDistance = Mathf.Infinity,
        int layerMask = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal
    ) {
        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        var result = Physics.Raycast(ray, out var hit, maxDistance, layerMask, interaction);
        if (result) {
            hitInfo = hit;
        }
        else {
            hitInfo = EmptyHit;
        }

        return result;
    }

    private float _updateTimeElapsed = 0f;

    private readonly GameCupUpdatePacket _cupUpdatePacketCache = new GameCupUpdatePacket {
        CupTransform = new DiceTransform()
    };
    private readonly GameDiceUpdatePacket _diceUpdatePacketCache = new GameDiceUpdatePacket {
        Sounds = new byte[5],
        Transforms = new DiceTransform[5],
    };

    private readonly float[] _quaternionArray = new float[4];

    private Vector3 _lastSentCupPosition = Vector3.zero;
    private Quaternion _lastSentCupRotation = Quaternion.identity;
    private void UpdateCupAndDiceToRemote() {
        if (_state != GamePlayState.CupShaking && _state != GamePlayState.DiceThrowing && _state != GamePlayState.Selecting) {
            return;
        }

        _updateTimeElapsed += Time.deltaTime;
        if (_updateTimeElapsed < Constants.TickInSeconds) {
            return;
        }

        _updateTimeElapsed -= Constants.TickInSeconds;

        var (position, rotation) = _cup.TransformForSendToRemote;
        if ((position - _lastSentCupPosition).sqrMagnitude > 0 || (Quaternion.Dot(rotation, _lastSentCupRotation) < 1)) {
            _lastSentCupPosition = position;
            _lastSentCupRotation = rotation;
            _cupUpdatePacketCache.CupTransform.SetPosition(position.x, position.y, position.z);
            rotation.Normalize();
            _quaternionArray[0] = rotation[0];
            _quaternionArray[1] = rotation[1];
            _quaternionArray[2] = rotation[2];
            _quaternionArray[3] = rotation[3];
            _cupUpdatePacketCache.CupTransform.SetRotation(_quaternionArray);
            NetworkManager.Instance.Send(_cupUpdatePacketCache);
        }

        if (_state != GamePlayState.CupShaking && _state != GamePlayState.DiceThrowing) {
            return;
        }

        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            var diceRigidbody = dice._rigidbody;
            var (dicePosition, diceRotation) = dice.TransformForSendToRemote;
            // var dicePosition = diceRigidbody.position;
            // var diceRotation = diceRigidbody.rotation;
            var diceTransform = _diceUpdatePacketCache.Transforms[i];
            diceTransform.SetPosition(dicePosition.x, dicePosition.y, dicePosition.z);
            diceRotation.Normalize();
            _quaternionArray[0] = diceRotation[0];
            _quaternionArray[1] = diceRotation[1];
            _quaternionArray[2] = diceRotation[2];
            _quaternionArray[3] = diceRotation[3];
            diceTransform.SetRotation(_quaternionArray);
            _diceUpdatePacketCache.Transforms[i] = diceTransform;
            if (dice.PopLastSound(out var materialType, out var soundType)) {
                _diceUpdatePacketCache.SetSound(i, soundType, materialType);
            }
            else {
                _diceUpdatePacketCache.ClearSound(i);
            }
        }

        NetworkManager.Instance.Send(_diceUpdatePacketCache);
    }

    private float _throwTime = 0f;
    private bool _cupAnimationEnd;

    private void ThrowDice() {
        if (_state != GamePlayState.CupShaking) return;
        _soundManager.StopMusic();
        _throwTime = 0f;
        ++_diceThrowCount;
        _diceCountText.SetLeftCount(3 - _diceThrowCount);
        _diceCountText.Unfollow();
        _timer.Hide();
        _currentTurnLeftTime = 0f;
        _state = GamePlayState.DiceThrowing;
        _cupAnimationEnd = false;
        _diceDetermined = false;
        _cup.DiceThrow(() => {
            if (!IsLocalTurn) {
                return;
            }
            _cup._cover.gameObject.SetActive(false);
            var cupDirection = _cup.transform.up;
            foreach (var dice in _dices) {
                dice._rigidbody.AddForce(cupDirection * 3f, ForceMode.Impulse);
            }
            _soundManager.PlaySound(_soundManager.DiceThrowSound);
        }, () => {
            _cupAnimationEnd = true;
        });
        
        if (IsLocalTurn) {
            NetworkManager.Instance.Send(new GameDiceThrowPacket {  });
        }
        else {
            StartCoroutine(PlayThrowSoundAfter());
        }
        ResetSelection();
    }

    private IEnumerator PlayThrowSoundAfter(float time = 0.416666f) {
        yield return new WaitForSeconds(time);
        _soundManager.PlaySound(_soundManager.DiceThrowSound);
    }

    private void OnDiceDetermined(GameDiceDeterminedPacket packetOrNull = null) {
        StartCoroutine(DiceDetermineCoroutine(packetOrNull));
    }

    private IEnumerator DiceDetermineCoroutine(GameDiceDeterminedPacket packetOrNull = null) {
        yield return new WaitForSeconds(packetOrNull != null ? 0f : 2f);
        StartSelecting(packetOrNull);
    }

    private List<GameDice> SortedDices => _dices.OrderBy(it => it._number).ToList();
    private List<int> SortedDicesNumber => _dices.ConvertAll(it => it._number).OrderBy(it => it).ToList();
    private void StartSelecting(GameDiceDeterminedPacket packetOrNull = null) {
        var prevState = _state;
        _state = GamePlayState.Selecting;

        var isLocalTurn = IsLocalTurn;
        var numbers = new byte[5];
        // 원격에서 패킷 온 경우 숫자 설정
        if (!isLocalTurn && packetOrNull != null) {
            for (int i = 0; i < 5; ++i) {
                _dices[i]._number = packetOrNull.numbers[i];
            }

            numbers = packetOrNull.numbers;
        }
        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            // 로컬인 경우에만
            if (isLocalTurn) {
                // 주사위 던진 직후면 숫자 설정
                if (prevState == GamePlayState.DiceThrowing) {
                    numbers[i] = (byte) dice.CalculateNumber();
                }
                // 아니면 원래 숫자 사용
                else {
                    numbers[i] = (byte) dice._number;
                }
            }
            // 물리 정지
            dice.IsFreeze = true;
            
            // 주사위 전부 던졌으면 강제 홀드
            if (_diceThrowCount >= 3 && !dice.IsKeeping) {
                ToggleKeepDice(dice, true);
            }
        }

        var sorted = SortedDicesNumber;
        _scoreboard.UpdateScore(_currentPlayerIndex, SortedDicesNumber);
        
        
        // 던진 직후의 Selecting 전환일 경우
        if (prevState == GamePlayState.DiceThrowing) {
            var now = ExtraUtil.CurrentTimeInMillis;
            var timeElapsed = (now - (packetOrNull?.Timestamp ?? now)) / 1000f;
            _currentTurnLeftTime = Constants.TimeLimitInTurn - timeElapsed;
            // 주사위 결정 신호 전송
            if (isLocalTurn) {
                NetworkManager.Instance.Send(new GameDiceDeterminedPacket {
                    Timestamp = now,
                    numbers = numbers,
                });
            }

            UpdateMadeText(sorted);
            _soundManager.PlaySelectingMusic(_highestType != null);
            // StartCoroutine(SelectingStartCoroutine(true));
        }
        // CancelShake로 인한 Selecting 전환일 경우
        else {
            // EnableMadeText();
            // 일단 주사위 이동 멈춤
            // StartCoroutine(SelectingStartCoroutine(false));
        }

        CalculateDicePositions();

        
        
        if (isLocalTurn) {
            // 마지막에 기록된 마우스 Over 처리
            if (_lastSlotInteraction != null) {
                OnSlotInteract(GameUIScoreboard.SlotInteractType.MouseOver, 
                    _lastSlotInteraction.index,
                    _lastSlotInteraction.scoreType,
                    _lastSlotInteraction.column,
                    _lastSlotInteraction.scoreColumn
                );
                _lastSlotInteraction = null;
            }
        }
        
        
        
    }

    private Tuple<YachuScoreType, int> _highestType;
    private void UpdateMadeText(List<int> dices) {
        var storage = _scoreboard._storages[_currentPlayerIndex];
        var special = YachuScoreType.Special
            .Where(it => storage[it.TypeEnum] == null).ToList()
            .ConvertAll(it => new Tuple<YachuScoreType, int>(it, it.Calculator(dices)));
        _highestType = null;
        foreach (var tuple in special) {
            // 0보단 높고
            if (tuple.Item2 <= 0) {
                continue;
            }
            // 최대값인 튜플
            if (_highestType == null) {
                _highestType = tuple;
                continue;
            }

            if (_highestType.Item2 < tuple.Item2) {
                _highestType = tuple;
            }
        }

        if (_highestType != null) {
            _madeText.Made(_diceFloatPosition.position + new Vector3(0, 0, 1), _highestType.Item1.Text);
            EnableMadeText();
        }
        else {
            DisableMadeText();
        }
        
    }

    private void EnableMadeText() {
        if(_highestType == null) return;
        _madeText.gameObject.SetActive(true);
    }
    private void DisableMadeText() {
        _madeText.gameObject.SetActive(false);
    }

    private void OnSelect(GameSelectPacket packet) {
        
        var selectType = packet.Selection;
        var data = packet.Data;

        switch (selectType) {
            case GameSelectPacket.SelectType.Dice: {
                if (!packet.TryGetDice(out var interact, out var indexes)) {
                    return;
                }
                
                if (interact) {
                    for (int i = 0; i < 5; ++i) _keepingDicesIndex[i] = indexes[i];
                    UpdateKeepDiceByIndexes();
                    ResetSelection();
                }
                else {
                    var dice = _dices[indexes[0]];
                    SelectDice(dice, true);
                }
                break;
            }
            case GameSelectPacket.SelectType.Cup: {
                if (packet.IsStartShake()) {
                    StartCupShaking();
                    ResetSelection();
                }
                else if(packet.IsCancelShake()) {
                    CancelCupShaking();
                    ResetSelection();
                }
                else {
                    SelectCup();
                }
                break;
            }
            case GameSelectPacket.SelectType.ScoreBoard: {
                if (!packet.TryGetScoreBoard(out var mark, out var index, out var score)) {
                    return;
                }

                if (mark) {
                    MarkScore((int)index, (int)score);
                    ResetSelection();
                }
                else {
                    var type = _scoreboard._types[(int) index];
                    var column = type.GetColumn(_currentPlayerIndex);
                    SelectScoreboard(column);
                }
                break;
            }
        }
    }

    private void MarkScore(int index, int score) {
        var typeEnum = (YachuScoreTypeEnum) index;
        var type = typeEnum.Of();
        _scoreboard._storages[_currentPlayerIndex][typeEnum] = score;
        _scoreboard.UpdateScore(_currentPlayerIndex, null, typeEnum);
        if (IsLocalTurn) {
            NetworkManager.Instance.Send(_selectPacketCached.SetScoreboard(true, index, score));
        }

        var soundType = score == 0 ? SoundManager.ScoreMarkType.Zero 
            : type.IsSpecial ? SoundManager.ScoreMarkType.Special 
            : SoundManager.ScoreMarkType.Normal;
        _soundManager.StopMusic();
        _soundManager.PlaySound(_soundManager.GetScoreMarkSound(soundType));
        TurnEnd();
    }
    
    // 각 배열의 숫자에는 주사위 index가 들어있음
    // dice.KeepingIndex는 이 배열 상의 index를 의미함
    private readonly int[] _keepingDicesIndex = new int[5] {-1, -1, -1, -1, -1};

    private void ToggleKeepDice(GameDice dice, bool force = false) {
        if (!force && !_diceSelectable[dice._index]) {
            return;
        }
        if (dice.IsKeeping) {
            _keepingDicesIndex[dice.KeepingIndex] = -1;
            dice.SetUnKeep();
        }
        else {
            for (int i = 0; i < 5; ++i) {
                if (_keepingDicesIndex[i] < 0) {
                    _keepingDicesIndex[i] = dice._index;
                    dice.KeepingIndex = i;
                    break;
                }
            }
        }
        UpdateKeepDice();
    }

    // _keepingDicesIndex 기준으로 재설ㅈ어
    private void UpdateKeepDiceByIndexes() {
        // 일단 전부 해제
        for (int i = 0; i < 5; ++i) {
            _dices[i].SetUnKeep();
        }
        for (int i = 0; i < 5; ++i) {
            var diceIndex = _keepingDicesIndex[i];
            if(diceIndex < 0) continue;
            _dices[diceIndex].KeepingIndex = i;
        } 
        UpdateKeepDice();
    }

    private void UpdateKeepDice() {
        CalculateDicePositions();
        if (IsLocalTurn) {
            NetworkManager.Instance.Send(_selectPacketCached.SetDiceKeep(_keepingDicesIndex));
        }
        _soundManager.PlaySound(_soundManager.DiceKeepToggleSound);
    }


    private readonly bool[] _diceSelectable = new bool[5];
    private readonly Vector3[] _dicePositions = new Vector3[5];

    private void CalculateDicePositions() {
        var floating = new List<int>(5);
        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            if (!dice.IsKeeping) {
                floating.Add(i);
            }
            else {
                _dicePositions[i] = _diceHolders[dice.KeepingIndex]._diceHoldPosition.position;
            }
        }

        floating = floating.OrderBy(it => _dices[it]._number).ThenBy(it => _dices[it]._index).ToList();
        // floating.Sort((i1, i2) => _dices[i1]._number - _dices[i2]._number);

        var floatingCount = floating.Count;
        var left = ((floatingCount - 1) / 2f) * -_diceFloatSpacing;
        for (var i = 0; i < floatingCount; ++i) {
            var diceIndex = floating[i];
            _dicePositions[diceIndex] = _diceFloatPosition.position + Vector3.right * (left + i * _diceFloatSpacing);
        }
    }

    private readonly GameSelectPacket _selectPacketCached = new GameSelectPacket { };

    private void SelectDice(GameDice dice, bool force = false) {
        if (!force && !_diceSelectable[dice._index]) {
            return;
        }

        if (_selectManager.SelectDice(dice)) {
            PlaySelectSound();
            UpdateScoreboardDescriptor();
            if(IsLocalTurn)
                NetworkManager.Instance.Send(_selectPacketCached.SetDiceSelect(dice._index));
        }
    }

    private void SelectCup() {
        if (_selectManager.SelectCup(_cup)) {
            PlaySelectSound();
            UpdateScoreboardDescriptor();
            if(IsLocalTurn)
                NetworkManager.Instance.Send(_selectPacketCached.SetCup(false));
        }
        
    }

    private void SelectScoreboard(GameUIScoreColumn scoreColumn) {
        if (_selectManager.SelectScoreboardSlot(scoreColumn)) {
            PlaySelectSound();
            UpdateScoreboardDescriptor(scoreColumn._index);
            if(IsLocalTurn)
                NetworkManager.Instance.Send(_selectPacketCached.SetScoreboard(false, scoreColumn._index));
        }
    }

    private void UpdateScoreboardDescriptor(int targetIndex = -1) {
        var count = _scoreboard._types.Count;
        for (int i = 0; i < count; ++i) {
            _scoreboard._types[i]._descriptor.gameObject.SetActive(i == targetIndex);
        }
    }

    private void PlaySelectSound() {
        _soundManager.PlaySound(_soundManager.SelectSound);
    }

    private void ResetSelection() {
        _selectManager.ResetSelection();
        UpdateScoreboardDescriptor();
    }
    
    // 클라이언트 개인의 턴 종료 신호
    // 모든 클라이언트가 종료 신호를 보내야 TurnStart 호출됨
    private void TurnEnd() {
        // const int maxTurn = 2;
        const int maxTurn = (int) YachuScoreTypeEnum.TypeCount;
        _currentTurnLeftTime = 0f;
        _timer.Hide();
        var playerCount = GameManager.Instance._currentRoomPlayers.Count;
        // 게임 종료 판정
        if ((_currentTurn + 1) / playerCount >= maxTurn) {
            _state = GamePlayState.GameEnding;
            var largestScore = _scoreboard._storages[0].Total;
            var winners = new List<int>(playerCount) { 0 };
            for (int i = 1; i < playerCount; ++i) {
                var score = _scoreboard._storages[i].Total;
                if (score == largestScore) {
                    winners.Add(i);
                }else if (score > largestScore) {
                    winners.Clear();
                    winners.Add(i);
                    largestScore = score;
                }
            }

            var winner = winners.Count == 1 ? winners[0] : GamePlayer.GameEndFlagDraw;
            StartCoroutine(GameEndCoroutine(2f, winner));
            return;

        }

        _state = GamePlayState.TurnEnding;
        StartCoroutine(TurnEndCoroutine(2f));
    }

    private IEnumerator TurnEndCoroutine(float wait) {
        yield return new WaitForSeconds(wait);
        NetworkManager.Instance.Send(new GameTurnEndPacket());
    }

    private IEnumerator GameEndCoroutine(float wait, int winner) {
        yield return new WaitForSeconds(wait);
        NetworkManager.Instance.Send(new GameEndPacket {
            Index = (short)winner
        });
    }
    private void OnReceivedGameEndPacket(Packet packet, Unit _) {
        var data = packet.GetPacketData<GameEndPacket>();
        _scoreboard.gameObject.SetActive(false);
        _cup.ControlByRemote = false;
        for (int i = 0; i < 5; ++i) {
            var dice = _dices[i];
            dice.IsFreeze = false;
            dice.SetUnKeep();
        }
        _diceCountText.Disable();
        _currentTurnPlayerText.Disable();
        _madeText.Disable();
        _timer.Hide();
        _soundManager.StopMusic();
        ResetSelection();
        GameManager.Instance.EndGame(data);
    }
    
    
    
    // private IEnumerator SelectingStartCoroutine(bool smooth) {
    // yield return null;
    // }

    private void RemoteUpdateCupPosition(GameCupUpdatePacket data) {
        var (x, y, z) = data.CupTransform.GetPosition();
        data.CupTransform.GetRotation(out var rotation);
        _cup.RemoteUpdate(new Vector3(x, y, z), new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]));
    }

    private void RemoteUpdateDice(GameDiceUpdatePacket data) {
        for (int i = 0; i < 5; ++i) {
            // Debug.Log($"[{i}]: pos: ({data.transforms[i].x}, {data.transforms[i].y}, {data.transforms[i].z})");
            var dice = _dices[i];
            var remoteTransform = data.Transforms[i];
            if (data.GetSound(i, out var soundType, out var materialType)) {
                dice.PlaySound(soundType, materialType);
            }
            dice.RemoteUpdate(remoteTransform);
        }
    }
}
}