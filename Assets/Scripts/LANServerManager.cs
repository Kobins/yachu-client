using System;
using System.Threading.Tasks;
using UnityEngine;
using Yachu.Server;
using Yachu.Server.Util;

namespace Yachu.Client {
public class LANServerManager : MonoSingleton<LANServerManager> {
    public YachuGameServer Server { get; private set; } = null;

    public async void OpenServer(Action<bool> callback) {
        if (Server != null) {
            callback.Invoke(false);
            return;
        }

        await Task.Run(() => {
            Server = new YachuGameServer(dedicate: false);
            Server.Start();
        });
        callback.Invoke(true);
    }

    public void StopServer() {
        if (Server != null && Server.State != YachuGameServer.ServerState.Disposing) {
            Server.Dispose();
        }
        Server = null;
    }

    private float _timeElapsed = 0f;
    private void Update() {
        if (Server == null || Server.State != YachuGameServer.ServerState.Running) {
            return;
        }

        _timeElapsed += Time.deltaTime;
        if (_timeElapsed >= Constants.TickInSeconds) {
            _timeElapsed -= Constants.TickInSeconds;
            Server.Tick();
        }
    }

    private void OnDestroy() {
        if (Server != null && Server.State != YachuGameServer.ServerState.Disposing) {
            Server.Dispose();
        }

        Server = null;
    }
}

}