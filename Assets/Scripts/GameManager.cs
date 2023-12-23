using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Yachu.Server.Packets;
using Yachu.Server.Packets.Body;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Yachu.Client.Lobby;
using Yachu.Server.Util;

namespace Yachu.Client {
public enum GameState {
    /// <summary>
    /// 서버와 연결 없음
    /// </summary>
    NotConnected,

    /// <summary>
    /// 접속 시도 중
    /// </summary>
    Connecting,
    
    /// <summary>
    /// 서버와 접속됨; 비로그인 상태
    /// </summary>
    NotLoggedIn,
    
    /// <summary>
    /// 로그인 또는 회원가입 요청 중
    /// </summary>
    LoggingIn,

    /// <summary>
    /// 로비 메인 화면
    /// </summary>
    Lobby,

    /// <summary>
    /// 방 안
    /// </summary>
    RoomWaiting,

    /// <summary>
    /// 씬 로드 중
    /// </summary>
    Preparing,

    /// <summary>
    /// 게임 플레이 중
    /// </summary>
    Playing,
}

public class GameManager : MonoSingleton<GameManager> {
    public GameState State { get; set; } = GameState.NotConnected;

    private LobbyManager _lobbyManager;
    private GamePlayManager _gamePlayManager;

    [Header("Configuration")] 
    public float _fakeLoadingTime = 0.5f;

    [Header("Client")] 
    public ClientData ClientData;
    public UserData UserData;

    // 방 관련 정보
    [Header("Room")]
    public int _roomNumber;
    public RoomState _roomState;
    public string _roomName;
    public int _roomArenaSizeIndex;
    public short _maxPlayer = Constants.MaxPlayerInRoom;
    public List<ClientData> _currentRoomPlayers = new List<ClientData>(Constants.MaxPlayerInRoom);
    public List<UserData> _currentRoomPlayersUserData = new List<UserData>(Constants.MaxPlayerInRoom);
    public int _thisClientIndex;


    private bool _updateLobby = false;
    private bool _enableLobbyManager = false;

    protected override void OnAwake() {
        _gamePlayManager = GamePlayManager.Instance;
        _lobbyManager = LobbyManager.Instance;
        RegisterPacketListeners();
        Dispatcher.Init();
    }

    // Start is called before the first frame update
    void Start() {
        UpdateLobby(true);
        // ConnectToServer("127.0.0.1");
    }

    public void ConnectToServer(string address) {
        if (State != GameState.NotConnected) {
            return;
        }

        IPAddress ipAddress;
        try {
            ipAddress = IPAddress.Parse(address);
        }
        catch (Exception e) {
            // _lobbyManager._connectingSubTextString = $"유효하지 않은 IP 주소입니다.";
            _updateLobby = true;
            return;
        }

        State = GameState.Connecting;
        _lobbyManager.UpdateState(this);
        NetworkManager.Instance.Connect(ipAddress, 10020, socketError => {
            if (socketError == SocketError.Success) {
                NetworkManager.Instance.Send(new C2SHandshakePacket {
                    Version = Constants.ProtocolVersion,
                });
            }
            else {
                State = GameState.NotConnected;
                // _lobbyManager._connectingSubTextString = $"SocketError: {socketError}";
            }

            _updateLobby = true;
        });
    }

    public void Login(string name, string rawPassword)
    {
        if (State != GameState.NotLoggedIn)
        {
            return;
        }

        State = GameState.LoggingIn;
        _lobbyManager.UpdateState(this);
        NetworkManager.Instance.Send(new C2SLoginPacket(name, rawPassword));
    }
    public void Register(string name, string rawPassword)
    {
        if (State != GameState.NotLoggedIn)
        {
            return;
        }

        State = GameState.LoggingIn;
        _lobbyManager.UpdateState(this);
        NetworkManager.Instance.Send(new C2SRegisterPacket(name, rawPassword));
    }

    public void Logout()
    {
        if (State != GameState.Lobby)
        {
            return;
        }

        ClientData = ClientData.Empty;
        State = GameState.NotLoggedIn;
        _lobbyManager.UpdateState(this);
        NetworkManager.Instance.Send(new C2SLogoutPacket());
    }

    // Update is called once per frame
    void Update() {
        if (_updateLobby) {
            Debug.Log("GameManager.Update() => _updateLobby == true");
            if (_enableLobbyManager) {
                _lobbyManager.gameObject.SetActive(true);
                _enableLobbyManager = false;
                Debug.Log("_lobbyManager.gameObject.SetActive(true) called by force enabler");
            }

            _updateLobby = false;
            _lobbyManager.UpdateState(this);
        }
    }

    private void RegisterPacketListeners() {
        PacketHandler<Unit>.RegisterListeners(new List<PacketListener<Unit>> {
            // 초기 유저 데이터 받기
            new PacketListener<Unit>(PacketType.Handshake, (packet, _) => {
                var data = packet.GetPacketData<S2CHandshakePacket>();
                State = GameState.NotLoggedIn;
                Debug.Log($"Handshake Success");
                UpdateLobby();
            }),
            // 로그인 응답
            new PacketListener<Unit>(PacketType.Login, (packet, _) => {
                var data = packet.GetPacketData<S2CLoginPacket>();
                if (data.Result == S2CLoginPacket.LoginResult.Success)
                {
                    State = GameState.Lobby;
                    ClientData = data.ClientData;
                    Debug.Log($"Successfully logged in: {ClientData.name}({ClientData.guid})");
                }
                else
                {
                    State = GameState.NotLoggedIn;
                    Debug.Log($"Failed to login: {data.Result.ToString()}");
                }
                UpdateLobby();
            }),
            // 회원가입 응답
            new PacketListener<Unit>(PacketType.Register, (packet, _) => {
                var data = packet.GetPacketData<S2CRegisterPacket>();
                if (data.Result == S2CRegisterPacket.RegisterResult.Success)
                {
                    State = GameState.Lobby;
                    ClientData = data.ClientData;
                    Debug.Log($"Successfully registered in: {ClientData.name}({ClientData.guid})");
                }
                else
                {
                    State = GameState.NotLoggedIn;
                    Debug.Log($"Failed to register: {data.Result.ToString()}");
                }
                UpdateLobby();
            }),
            // 유저 정보 갱신
            new PacketListener<Unit>(PacketType.UserDataUpdate, (packet, _) => {
                var data = packet.GetPacketData<S2CUserDataUpdatePacket>();
                UserData = data.UserData;
                UpdateLobby();
            }),
            new PacketListener<Unit>(PacketType.NameChange, (packet, _) => {
                var data = packet.GetPacketData<NameChangePacket>();
                ClientData.name = data.NewName;
                UpdateLobby();
            }),
            new PacketListener<Unit>(PacketType.AlertMessage, (packet, _) => {
                var data = packet.GetPacketData<S2CAlertPacket>();
                var content = data.Content;
                _lobbyManager.AlertMessage(content);
            }),
            // 방 정보 초기화
            new PacketListener<Unit>(PacketType.RoomEnter, (packet, _) => {
                var data = packet.GetPacketData<S2CRoomEnterPacket>();
                SetRoomData(data.RoomData);
                _roomState = RoomState.Waiting;
                State = GameState.RoomWaiting;
                // _lobbyManager.OnNewPlayer();
                UpdateLobby();
            }),
            
            new PacketListener<Unit>(PacketType.RoomUpdate, (packet, _) => {
                var data = packet.GetPacketData<S2CRoomUpdatePacket>();
                SetRoomData(data.RoomData);
                // _lobbyManager.OnNewPlayer();
                UpdateLobby();
            }),
            new PacketListener<Unit>(PacketType.RoomExit, (packet, _) => {
                if (State != GameState.RoomWaiting) {
                    Debug.Log("Tried exit room when state is not RoomWaiting");
                    return;
                }

                State = GameState.Lobby;
                UpdateLobby();
            }),
            // 새 유저 입장
            new PacketListener<Unit>(PacketType.RoomNewUser, _lobbyManager.OnNewPlayer),
            // 유저 퇴장
            new PacketListener<Unit>(PacketType.RoomExitUser, (packet, _) => {
                var data = packet.GetPacketData<S2CRoomUserExitPacket>();
                _currentRoomPlayers.RemoveAt(data.Index);
                _thisClientIndex = _currentRoomPlayers.IndexOf(ClientData);
                UpdateLobby();
            }),
            // 레디 신호
            new PacketListener<Unit>(PacketType.RoomReady, _lobbyManager.OnReady),
            // 게임 시작 신호
            new PacketListener<Unit>(PacketType.RoomStart, (packet, _) => {
                Debug.Log("received RoomStart");
                var data = packet.GetPacketData<RoomStartPacket>();
                StartGame(data);
            }),
        });
    }

    private void SetRoomData(RoomData roomData) {
        _maxPlayer = roomData.maxPlayer;
        _roomNumber = roomData.number;
        _roomName = roomData.Name;
        _roomArenaSizeIndex = roomData.arenaSizeIndex;
        _currentRoomPlayers.Clear();
        _currentRoomPlayersUserData.Clear();
        for (int i = 0; i < roomData.playerCount; i++) {
            var client = roomData.Clients[i];
            var userData = roomData.UserDatas[i];
            var ready = roomData.Ready[i];
            _currentRoomPlayers.Add(client);
            _currentRoomPlayersUserData.Add(userData);
            if (client.guid == ClientData.guid) {
                _thisClientIndex = i;
            }

            _lobbyManager.RoomPlayerList[i].Ready = ready;
        }
    }

    public void UpdateLobby(bool forceEnable = false) {
        _updateLobby = true;
        _enableLobbyManager = _enableLobbyManager || forceEnable;
        // _lobbyManager.UpdateState(this);
    }

    public void OnDisconnected() {
        State = GameState.NotConnected;
        // _lobbyManager._connectingSubTextString = $"서버와의 연결이 끊어졌습니다.";
        UpdateLobby(true);
    }

    public void StartGame(RoomStartPacket data) {
        Debug.Log("StartGame()");
        if (State != GameState.RoomWaiting) {
            return;
        }
        State = GameState.Preparing;
        _lobbyManager.gameObject.SetActive(false);
        StartCoroutine(LoadGameScene(data));
    }

    private IEnumerator LoadGameScene(RoomStartPacket data) {
        
        NetworkManager.Instance.Send(new Packet(PacketType.SceneLoadDone));
        yield return null;
    }

    public void EndGame(GameEndPacket data) {
        State = GameState.RoomWaiting;
        _lobbyManager.GameResultWinnerString = data.Index >= 0 ? data.Client.name : null;
        UpdateLobby(true);
        StartCoroutine(LoadLobbyScene(data));
    }

    private IEnumerator LoadLobbyScene(GameEndPacket data) {
        yield return null;
    }
}
}