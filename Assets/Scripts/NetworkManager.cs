using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Yachu.Server;
using Yachu.Server.Packets;
using UnityEngine;
using Yachu.Client.Lobby;
using Yachu.Server.Util;

namespace Yachu.Client {
public enum ConnectState {
    NotConnected,
    Connecting,
    Connected,
}

public class NetworkManager : MonoSingleton<NetworkManager> {
    private Socket _socket;
    public ConnectState ConnectState { get; private set; } = ConnectState.NotConnected;

    private SocketAsyncEventArgs _receiveEvent;
    private PacketBuilder _packetBuilder;
    private readonly object _receivedPacketQueueLock = new object();
    private Queue<Packet> _receivedPacketQueue;
    private PacketHandler<Unit> _packetHandler;
    private byte[] _receiveBuffer;

    protected override void OnAwake() {
        Log.Printer = UnityLogPrinter.Instance;
        Initialize();
    }

    private void Update() {
        if (ConnectState == ConnectState.Connected) {
            ProcessPacket();
        }
    }

    private void Initialize() {
        _receivedPacketQueue = new Queue<Packet>(50);
        _receiveBuffer = new byte[Constants.SocketBufferSize];
        _packetBuilder = new PacketBuilder();

        _packetHandler = PacketHandler<Unit>.Instance;

        _receiveEvent = new SocketAsyncEventArgs();
        _receiveEvent.Completed += OnReceiveCompleted;
        _receiveEvent.UserToken = this;
        _receiveEvent.SetBuffer(_receiveBuffer, 0, Constants.SocketBufferSize);
    }

    public void StartReceive() {
        bool pending = _socket.ReceiveAsync(_receiveEvent);
        if (!pending) OnReceiveCompleted(this, _receiveEvent);
    }

    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e) {
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
            // Debug.Log($"offset: {e.Offset}, {e.BytesTransferred} bytes transferred");
            _packetBuilder.OnReceive(e.Buffer, e.Offset, e.BytesTransferred, AddReceivePacket);
            StartReceive();
            return;
        }

        if (e.BytesTransferred <= 0) {
            Debug.LogError($"Zero byte received, disconnecting");
        }
        else {
            Debug.LogError($"SocketError while receiving: {e.SocketError}, disconnecting");
        }
        
        LobbyManager.Instance.AlertMessage("서버와의 연결이 해제됐습니다."+$"({(e.BytesTransferred > 0 ? $" ({e.SocketError})" : "")})");

        OnDisconnect();
    }

    private void AddReceivePacket(Packet packet) {
        lock (_receivedPacketQueueLock) {
            _receivedPacketQueue.Enqueue(packet);
        }
    }

    private void ProcessPacket() {
        if (_receivedPacketQueue.Count <= 0) return;
        lock (_receivedPacketQueueLock) {
            while (_receivedPacketQueue.Count > 0) {
                var packet = _receivedPacketQueue.Dequeue();
                Debug.Log($"received packet {(PacketType) packet.Type}({packet.Length} bytes)");
                _packetHandler.HandlePacket(packet);
            }

            _receivedPacketQueue.Clear();
        }
    }

    private Action<SocketError> _onSuccessCallback;

    // public void ConnectToServer(string address, int port, Action<SocketError> onSuccessCallback) {
    // Connect("127.0.0.1", 10020, onSuccessCallback);
    // }
    public void Connect(IPAddress address, int port = 10020, Action<SocketError> onSuccessCallback = null) {
        if (ConnectState != ConnectState.NotConnected) {
            Debug.LogError("Tried connecting server while connecting server, ignored");
            return;
        }

        ConnectState = ConnectState.Connecting;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;

        var endPoint = new IPEndPoint(address, port);

        var connectEvent = new SocketAsyncEventArgs();
        connectEvent.Completed += OnConnect;
        _onSuccessCallback = onSuccessCallback;
        connectEvent.RemoteEndPoint = endPoint;

        bool pending = _socket.ConnectAsync(connectEvent);
        LobbyManager.Instance.AlertMessage($"{address}에 연결하는 중 ...");
        if (!pending) OnConnect(null, connectEvent);
        
    }

    private void OnConnect(object sender, SocketAsyncEventArgs e) {
        if (e.SocketError == SocketError.Success) {
            // 연결 성공
            ConnectState = ConnectState.Connected;
            StartReceive();
            Debug.Log($"<color=green>Successfully Connected to Server!</color>");
            LobbyManager.Instance.AlertMessage($"서버 접속에 성공했습니다!");
        }
        else {
            ConnectState = ConnectState.NotConnected;
            Debug.LogError($"Failed to connect server: {e.SocketError}");
            LobbyManager.Instance.AlertMessage($"서버 접속에 실패했습니다 : {e.SocketError}");
        }

        _onSuccessCallback.Invoke(e.SocketError);

        _onSuccessCallback = null;
        e.Completed -= OnConnect;
    }

    public void Send<T>(PacketData<T> packet) where T : PacketData<T> {
        Send(packet.ToPacket());
    }

    public void Send(Packet packet) {
        if (_socket == null || !_socket.Connected) {
            return;
        }

        var sendEvent = SocketAsyncEventArgsPool.Instance.Pop();
        if (sendEvent == null) {
            Debug.LogError("SocketAsyncEventArgsPool returned null", this);
            return;
        }

        sendEvent.Completed += OnSendCompleted;
        sendEvent.UserToken = this;

        var sendData = packet.Data;
        var length = packet.Length;
        sendEvent.SetBuffer(sendData, 0, length);

        bool pending = _socket.SendAsync(sendEvent);
        if (!pending) OnSendCompleted(null, sendEvent);
    }

    private void OnSendCompleted(object sender, SocketAsyncEventArgs e) {
        if (e.SocketError == SocketError.Success) {
            // 전송 성공
        }
        else {
            Debug.LogError($"Failed to send data to server: {e.SocketError}, disconnecting");
            OnDisconnect();
        }

        e.Completed -= OnSendCompleted;
        e.SetBuffer(null, 0, 0);
        SocketAsyncEventArgsPool.Instance.Push(e);
    }

    private void OnDisconnect() {
        ConnectState = ConnectState.NotConnected;
        Debug.Log("Disconnected from server");
        _socket.Disconnect(false);
        _socket.Close();

        GameManager.Instance.OnDisconnected();
    }

    private void OnApplicationQuit() {
        if (ConnectState != ConnectState.NotConnected)
            OnDisconnect();
    }
}

public class UnityLogPrinter : Singleton<UnityLogPrinter>, ILogPrinter {
    public void Print<T>(Log.LogType type, string prefix, T message) {
        switch (type) {
            case Log.LogType.NORMAL:
                Debug.Log(message);
                break;
            case Log.LogType.ERROR:
                Debug.LogError(message);
                break;
        }
    }
}
}