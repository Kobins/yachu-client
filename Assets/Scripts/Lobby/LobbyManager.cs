using System;
using System.Collections.Generic;
using Yachu.Server.Packets;
using Yachu.Server.Packets.Body;
using UnityEngine;
using UnityEngine.UI;
using Yachu.Server.Util;

namespace Yachu.Client.Lobby {
public class LobbyManager : MonoSingleton<LobbyManager> {
    [SerializeField] private Canvas _lobbyCanvas;
    [SerializeField] private Camera _lobbyCamera;

    [SerializeField] private RectTransform _coverPanel;
    [SerializeField] private RectTransform _leftButtonsPanel;
    
    [SerializeField] private List<Button> _exitButton;
    public bool ExitButtonActive
    {
        get => _exitButton[0].gameObject.activeSelf;
        set
        {
            foreach (var b in _exitButton)
            {
                b.gameObject.SetActive(value);
            }
        }
    }
    public bool ExitButtonInteractable
    {
        get => _exitButton[0].interactable;
        set
        {
            foreach (var b in _exitButton)
            {
                b.interactable = value;
            }
        }
    }
    
    [Header("Settings")]
    [SerializeField] private List<Button> _settingsButton;
    public bool SettingsButtonActive
    {
        get => _settingsButton[0].gameObject.activeSelf;
        set
        {
            foreach (var b in _settingsButton)
            {
                b.gameObject.SetActive(value);
            }
        }
    }
    public bool SettingsButtonInteractable
    {
        get => _settingsButton[0].interactable;
        set
        {
            foreach (var b in _settingsButton)
            {
                b.interactable = value;
            }
        }
    }
    [SerializeField] private Button _settingsCloseButton;
    [SerializeField] private Image _settingsPanel;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _soundVolumeSlider;

    [Header("Login")] 
    [SerializeField] private RectTransform _loginButtonPanel;
    [SerializeField] private InputField _idInputField;
    [SerializeField] private InputField _passwordInputField;
    [SerializeField] private Button _loginButton;
    [SerializeField] private InputField _passwordCheckInputField;
    [SerializeField] private Button _registerButton;
    
    [Header("Connect")]
    [SerializeField] private RectTransform _connectPanel;
    [SerializeField] private Button _connectButton;
    [SerializeField] private InputField _connectEndPointInputField;
    // [SerializeField] private Button _openLANServerButton;
    // [SerializeField] private Text _connectingText;
    // [SerializeField] private Text _connectingSubText;
    // [HideInInspector] public string _connectingSubTextString;


    [Header("Name Change")] 
    [SerializeField] private RectTransform _nameChangePanel;
    [SerializeField] private InputField _nameChangeInputField;
    [SerializeField] private Text _statText;
    
    [Header("Lobby")]
    [SerializeField] private RectTransform _lobbyPanel;
    
    
    [Header("Main Menu")]
    [SerializeField] private Button _autoPlayButton;

    [Header("Alert")] 
    [SerializeField] private RectTransform _alertPanel;
    [SerializeField] private Text _alertText;
    [HideInInspector] public string _alertContext = "";
    
    [Header("Room")]
    [SerializeField] private RectTransform _roomButtonPanel;
    [SerializeField] private Button _roomStartOrReadyButton;
    [SerializeField] private Text _roomStartOrReadyButtonText;
    [SerializeField] private Button _roomExitButton;
    [SerializeField] private GameObject _roomRightPanel;
    [SerializeField] private Text _roomTitleText;
    [SerializeField] private List<LobbyUIPlayer> _roomPlayerList;
    public List<LobbyUIPlayer> RoomPlayerList => _roomPlayerList;

    [SerializeField] private float _roomStartDelay = 1.5f;
    [SerializeField] private Dropdown _playerCountDropdown;

    private float _roomStartLeftDelay = 0f;

    [Header("Game Result Panel")] 
    [SerializeField] private LobbyUIResultPanel _gameResultPanel;
    public string GameResultWinnerString { get; set; }
    /*
    public GameObject _gameResultPanel;
    public Text _gameResultWinner;
    public Button _gameResultCloseButton;
*/
    private const string NicknameKey = "saved_nickname";
    private SoundManager _soundManager;
    private AudioClip _selectSound;
    private void PlaySelectSound() => _soundManager.PlaySound(_selectSound);

    private void TryConnect(string address)
    {
        _connectButton.interactable = false;
        _connectEndPointInputField.interactable = false;
        // _openLANServerButton.interactable = false;
        GameManager.Instance.ConnectToServer(address);
    }
    private void Start()
    {
        _soundManager = SoundManager.Instance;
        _selectSound = _soundManager.SelectSound;
        // 접속 버튼 클릭 시 각종 버튼 비활성화 & 서버 연결 시도
        _connectButton.onClick.AddListener(() =>
        {
            TryConnect(_connectEndPointInputField.text.Trim());
            PlaySelectSound();
        });
        /*
        _openLANServerButton.onClick.AddListener(() => {
            _nameChangeInputField.interactable = false;
            _connectButton.interactable = false;
            _connectEndPointInputField.interactable = false;
            _openLANServerButton.interactable = false;
            LANServerManager.Instance.OpenServer((result) => {
                if (result) {
                    GameManager.Instance.ConnectToServer("127.0.0.1");
                }
                else {
                    GameManager.Instance.UpdateLobby();
                }
            });
            PlaySelectSound();
        });
        */

        _idInputField.onValueChanged.AddListener(_ => RefreshLoginAndRegisterButton());
        _passwordInputField.onValueChanged.AddListener(_ => RefreshLoginAndRegisterButton());
        _passwordCheckInputField.onValueChanged.AddListener(_ => RefreshLoginAndRegisterButton());
        _loginButton.onClick.AddListener(() =>
        {
            _idInputField.interactable = false;
            _passwordInputField.interactable = false;
            _loginButton.interactable = false;
            _passwordCheckInputField.interactable = false;
            _registerButton.interactable = false;
            GameManager.Instance.Login(_idInputField.text.Trim(), _passwordInputField.text.Trim());
        });
        _registerButton.onClick.AddListener(() =>
        {
            _idInputField.interactable = false;
            _passwordInputField.interactable = false;
            _loginButton.interactable = false;
            _passwordCheckInputField.interactable = false;
            _registerButton.interactable = false;
            GameManager.Instance.Register(_idInputField.text.Trim(), _passwordInputField.text.Trim());
        });
        
        // GameManager.Instance.ClientData.name = PlayerPrefs.GetString(NicknameKey, "");
        // 이름 변경 시 패킷 전송
        _nameChangeInputField.onEndEdit.AddListener((newName) => {
            if (NetworkManager.Instance.ConnectState == ConnectState.Connected) {
                _nameChangeInputField.interactable = false;
                NetworkManager.Instance.Send(new NameChangePacket {NewName = newName});
            }
            else {
                GameManager.Instance.ClientData.name = newName;
            }
            PlayerPrefs.SetString(NicknameKey, newName);
            PlaySelectSound();
        });
        
        // 자동 참가 버튼 클릭시 Enter 전송, 버튼 비활성화
        _autoPlayButton.onClick.AddListener(() => {
            _autoPlayButton.interactable = false;
            NetworkManager.Instance.Send(new C2SRoomAutoEnter());
            PlaySelectSound();
        });
        
        // 기본 설정창 비활성화
        _settingsPanel.gameObject.SetActive(false);
        // 설정 버튼 클릭 시 설정창 활성화 및 slider 값 초기화 
        foreach (var b in _settingsButton)
        {
            b.onClick.AddListener(() =>
            {
                _settingsPanel.gameObject.SetActive(true);
                _musicVolumeSlider.value = _soundManager.MusicVolume;
                _soundVolumeSlider.value = _soundManager.SoundVolume;
                PlaySelectSound();
            });
        }
        // 설정창 슬라이더 조정 시 실제 볼륨 변경
        _musicVolumeSlider.onValueChanged.AddListener((value) => {
            _soundManager.MusicVolume = value;
        });
        _soundVolumeSlider.onValueChanged.AddListener((value) => {
            _soundManager.SoundVolume = value;
        });
        // 설정창 닫기 버튼 클릭 시 닫음
        _settingsCloseButton.onClick.AddListener(() => {
            _settingsPanel.gameObject.SetActive(false);
            PlaySelectSound();
        });

        // 방 나가기 버튼 클릭 시 패킷 전송
        _roomExitButton.onClick.AddListener(() => {
            _roomExitButton.interactable = false;
            NetworkManager.Instance.Send(new Packet(PacketType.RoomExit));
            PlaySelectSound();
        });
        // 시작 또는 준비버튼 활성화 중에 누르면 ...
        _roomStartOrReadyButton.onClick.AddListener(() => {
            _roomStartOrReadyButton.interactable = false;
            var selfIndex = GameManager.Instance._thisClientIndex;
            // 방장이면 게임 시작
            if (selfIndex == 0) {
                NetworkManager.Instance.Send(new Packet(PacketType.RoomStart));
            }
            // 유저면 준비 또는 준비해제
            else {
                var newReady = !_roomPlayerList[selfIndex].Ready;
                _roomPlayerList[selfIndex].Ready = newReady;
                NetworkManager.Instance.Send(new RoomReadyPacket {
                    Index = GameManager.Instance._thisClientIndex, 
                    Ready = newReady
                });
                // 준비 시 설정창 못열도록
                SettingsButtonInteractable = !newReady;
            }
            PlaySelectSound();
        });

        // 게임 종료 버튼
        foreach (var b in _exitButton)
        {
            b.onClick.AddListener(() => {
                PlaySelectSound();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            });
        }
        
        // 로비 Canvas 기본 활성화
        _lobbyCanvas.gameObject.SetActive(true);
        // 커버 패널 기본 활성화
        _coverPanel.gameObject.SetActive(true);
        // 게임 결과창 기본 비활성화
        _gameResultPanel.Disable();

        if (CommandLineReader.GetArguments().TryGetValue("host", out var host))
        {
            TryConnect(host);
        }
    }

    /// <summary>
    /// 모든 플레이어가 준비중인지 검사합니다.
    /// </summary>
    private bool AllPlayersReady {
        get {
            for (int i = 0; i < GameManager.Instance._currentRoomPlayers.Count; i++) {
                var player = _roomPlayerList[i];
                if (!player.Host && !player.Ready) return false;
            }

            return true;
        }
        
    }
    /// <summary>
    /// 인원수가 2명 이상이고 모든 플레이어게 준비중인지 검사합니다.
    /// </summary>
    private bool CanStartGame => GameManager.Instance._currentRoomPlayers.Count >= 2 && AllPlayersReady;

    private void RefreshLoginAndRegisterButton()
    {
        _idInputField.interactable = true;
        _passwordInputField.interactable = true;
        var isIdValid = !string.IsNullOrWhiteSpace(_idInputField.text); 
        var isPasswordValid = !string.IsNullOrWhiteSpace(_passwordInputField.text);
        _loginButton.interactable = isIdValid && isPasswordValid;
        _passwordCheckInputField.interactable = isIdValid && isPasswordValid;
        var isPasswordCheckValid = !string.IsNullOrWhiteSpace(_passwordCheckInputField.text) 
                                   && _passwordCheckInputField.text == _passwordInputField.text;
        _registerButton.interactable = isPasswordCheckValid;
    }
    /// <summary>
    /// 현재 게임 상태 기반으로 로비 UI를 갱신합니다.
    /// </summary>
    /// <param name="gameManager"></param>
    public void UpdateState(GameManager gameManager) {
        // 전송된 Alert Message가 있으면 보여줌
        if (!string.IsNullOrEmpty(_alertContext))
        {
            _alertPanel.gameObject.SetActive(false);
            _alertPanel.gameObject.SetActive(true);
            _alertText.text = _alertContext;
            _alertContext = "";
            LayoutRebuilder.ForceRebuildLayoutImmediate(_alertPanel);
        }
        var state = gameManager.State;
        switch (state) {
            // 연결 안 된 상태
            // 서버 접속 & 설정 & 게임 종료만 가능.
            case GameState.NotConnected:
                _coverPanel.gameObject.SetActive(true);   // 커버 패널만 활성화
                _connectPanel.gameObject.SetActive(true); // 서버 주소 & 접속버튼만 가능
                SettingsButtonActive = (true);            // 설정 버튼 활성화 
                ExitButtonActive = (true);                // 게임 종료 버튼 활성화
                
                _connectButton.interactable = true;
                _connectEndPointInputField.interactable = true;
                ExitButtonInteractable = true;
                SettingsButtonInteractable = true;
                
                _nameChangePanel.gameObject.SetActive(false);
                _loginButtonPanel.gameObject.SetActive(false);
                _roomButtonPanel.gameObject.SetActive(false);
                _roomRightPanel.SetActive(false);

                break;
            // 접속 응답 대기중
            case GameState.Connecting:
                _coverPanel.gameObject.SetActive(true);   // 커버 패널만 활성화
                _connectPanel.gameObject.SetActive(true); // 서버 주소 & 접속버튼만 가능
                SettingsButtonActive = (true);            // 설정 버튼 활성화 
                ExitButtonActive = (true);                // 게임 종료 버튼 활성화
                
                // 접속 중에는 interactable 비활성화
                _connectButton.interactable = false;
                _connectEndPointInputField.interactable = false;
                ExitButtonInteractable = false;
                SettingsButtonInteractable = false;
                
                _nameChangePanel.gameObject.SetActive(false);
                _loginButtonPanel.gameObject.SetActive(false);
                _lobbyPanel.gameObject.SetActive(false);
                _roomButtonPanel.gameObject.SetActive(false);
                _roomRightPanel.SetActive(false);

                // _openLANServerButton.interactable = false;
                // _connectingText.text = "서버 연결 중 ...";
                // _connectingSubText.text = "";
                // _connectingText.color = Color.yellow;
                // _buttonsParent.gameObject.SetActive(false);
                
                break;
            // 서버와 연결 확립, 로그인 안 됨
            case GameState.NotLoggedIn:
                _coverPanel.gameObject.SetActive(true);       // 여전히 커버 패널 사용중
                _loginButtonPanel.gameObject.SetActive(true); // 로그인 & 회원가입 가능
                SettingsButtonActive = (true);                // 설정 버튼 활성화 
                ExitButtonActive = (true);                    // 게임 종료 버튼 활성화

                RefreshLoginAndRegisterButton();
                ExitButtonInteractable = true;
                SettingsButtonInteractable = true;
                
                _connectPanel.gameObject.SetActive(false);
                _nameChangePanel.gameObject.SetActive(false);
                _lobbyPanel.gameObject.SetActive(false);
                _roomButtonPanel.gameObject.SetActive(false);
                _roomRightPanel.SetActive(false);
                break;
            case GameState.LoggingIn:
                _coverPanel.gameObject.SetActive(true);       // 여전히 커버 패널 사용중
                _loginButtonPanel.gameObject.SetActive(true); // 로그인 & 회원가입 가능
                SettingsButtonActive = (true);                // 설정 버튼 활성화 
                ExitButtonActive = (true);                    // 게임 종료 버튼 활성화
                
                // 요청 중에는 모든 상호작용 불가능
                _idInputField.interactable = false;
                _passwordInputField.interactable = false;
                _loginButton.interactable = false;
                _passwordCheckInputField.interactable = false;
                _registerButton.interactable = false;
                ExitButtonInteractable = false;
                SettingsButtonInteractable = false;
                
                _connectPanel.gameObject.SetActive(false);
                _nameChangePanel.gameObject.SetActive(false);
                _lobbyPanel.gameObject.SetActive(false);
                _roomButtonPanel.gameObject.SetActive(false);
                _roomRightPanel.SetActive(false);
                break;
            case GameState.Lobby:
                _lobbyPanel.gameObject.SetActive(true);      // 로비 버튼 패널 활성화
                _nameChangePanel.gameObject.SetActive(true); // 이름 변경 가능
                SettingsButtonActive = (true);               // 설정 버튼 활성화 
                ExitButtonActive = (true);                   // 게임 종료 버튼 활성화
                
                _autoPlayButton.interactable = true;
                SettingsButtonInteractable = true;
                ExitButtonInteractable = true;
                _nameChangeInputField.text = gameManager.ClientData.name;
                _nameChangeInputField.interactable = true;
                _statText.text = $"<color=yellow>{gameManager.UserData.PlayCount}전</color> " +
                                 $"<color=lime>{gameManager.UserData.WinCount}승</color> " +
                                 $"<color=red>{gameManager.UserData.LoseCount}패</color>";
                
                _coverPanel.gameObject.SetActive(false);
                _roomButtonPanel.gameObject.SetActive(false);
                _roomRightPanel.SetActive(false);
                
                break;
            case GameState.RoomWaiting:
                if ((GameResultWinnerString?.Length ?? 0) > 0) {
                    _gameResultPanel.SetWinner(GameResultWinnerString);
                    GameResultWinnerString = null;
                }
                _roomButtonPanel.gameObject.SetActive(true);
                _roomRightPanel.SetActive(true);
                SettingsButtonActive = (true);
                
                var selfPlayer = _roomPlayerList[gameManager._thisClientIndex];
                SettingsButtonInteractable = selfPlayer.Host || !selfPlayer.Ready;
                _roomExitButton.interactable = true;
                _roomTitleText.text = gameManager._roomName;
                int i;
                for (i = 0; i < gameManager._currentRoomPlayers.Count; i++) {
                    _roomPlayerList[i]._button.gameObject.SetActive(true);
                    _roomPlayerList[i]._button.interactable = true;
                    _roomPlayerList[i].Host = i == 0;
                    var suffixBuilder = new List<string>();

                    if (i == gameManager._thisClientIndex) {
                        suffixBuilder.Add("나");
                    }
                    // gameManager._currentRoomPlayersUserData[i]

                    var suffix = suffixBuilder.Count > 0
                        ? $" ({string.Join(", ", suffixBuilder)})"
                        : "";

                    var userData = gameManager._currentRoomPlayersUserData[i];
                    var stat = $" <color=#9A7623>{userData.PlayCount}</color>/" +
                               $"<color=green>{userData.WinCount}</color>/" +
                               $"<color=red>{userData.LoseCount}</color>";
                    _roomPlayerList[i]._playerName.text = gameManager._currentRoomPlayers[i].name + suffix + stat;
                }

                for (; i < gameManager._maxPlayer; i++) {
                    _roomPlayerList[i]._button.gameObject.SetActive(true);
                    _roomPlayerList[i]._button.interactable = false;
                    _roomPlayerList[i]._playerName.text = "비어 있음";
                    _roomPlayerList[i].Reset();
                }

                for (; i < Constants.MaxPlayerInRoom; i++) {
                    _roomPlayerList[i].Reset();
                    _roomPlayerList[i]._button.gameObject.SetActive(false);
                }

                if (gameManager._thisClientIndex == 0) {
                    _roomStartOrReadyButtonText.text = "게임 시작";
                    _roomStartOrReadyButton.interactable = CanStartGame;
                }
                else {
                    var text = _roomPlayerList[gameManager._thisClientIndex].Ready
                        ? "준비 해제"
                        : "준비";
                    _roomStartOrReadyButtonText.text = text;
                    _roomStartOrReadyButton.interactable = true;
                }
                
                _nameChangePanel.gameObject.SetActive(false);
                _coverPanel.gameObject.SetActive(false);
                _lobbyPanel.gameObject.SetActive(false);
                ExitButtonActive = (false);
                
                break;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_leftButtonsPanel);
    }

    private void Update() {
        // if (_roomStartLeftDelay > 0 && GameManager.Instance._currentRoomPlayers.Count >= 2 &&
            // GameManager.Instance._thisClientIndex == 0 && CanStartGame) {
            // _roomStartLeftDelay -= Time.deltaTime;
            // if (_roomStartLeftDelay <= 0) {
                // _roomStartOrReadyButton.interactable = true;
                // _roomDescriptionText.text = "게임 시작 버튼을 눌러 시작!";
            // }
        // }
    }

    public void AlertMessage(string context)
    {
        _alertContext = context;
        GameManager.Instance.UpdateLobby();
    }
    
    public void OnNewPlayer(Packet packet, Unit _) {
        var gameManager = GameManager.Instance;
        var data = packet.GetPacketData<S2CRoomNewUserPacket>();
        var newPlayerIndex = gameManager._currentRoomPlayers.Count;
        gameManager._currentRoomPlayers.Add(data.NewPlayer);
        gameManager._currentRoomPlayersUserData.Add(data.NewPlayerUserData);
        _roomPlayerList[newPlayerIndex].Reset();
        gameManager.UpdateLobby();
        // if (GameManager.Instance._thisClientIndex != 0) {
            // _roomDescriptionText.text = "방장을 기다리세요 ...";
        // }
        // else {
            // _roomDescriptionText.text = "잠시만 기다리세요 ...";
        // }
    }

    public void OnReady(Packet packet, Unit _) {
        var data = packet.GetPacketData<RoomReadyPacket>();
        var index = data.Index;
        var ready = data.Ready;

        _roomPlayerList[index].Ready = ready;
        GameManager.Instance.UpdateLobby();
    }
}
}