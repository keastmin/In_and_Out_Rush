using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace Dev
{
    public class HostMigration : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        [SerializeField] private StageManager _stageManagerPrefab;

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            Debug.Log("Host Migration started, shutting down the old Runner...");
            await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

            var newRunner = Instantiate(_runnerPrefab);
            newRunner.AddCallbacks(this);

            StartGameResult result = await newRunner.StartGame(new StartGameArgs()
            {
                // SessionName = SessionName,              // ignored, peer never disconnects from the Photon Cloud
                GameMode = GameMode.Host,                    // ignored, Game Mode comes with the HostMigrationToken
                HostMigrationToken = hostMigrationToken,   // contains all necessary info to restart the Runner
                HostMigrationResume = HostMigrationResume, // this will be invoked to resume the simulation
                SceneManager = newRunner.GetComponent<NetworkSceneManagerDefault>() // 기본 씬 매니저 사용
                // other args
            });

            // Check StartGameResult as usual
            if (result.Ok == false) {
                Debug.LogWarning(result.ShutdownReason);
            } else {
                Debug.Log("Done");
            }
        }

        void HostMigrationResume(NetworkRunner runner)
        {
            Debug.Log($"[DEBUG] Resume Start. IsServer: {runner.IsServer}, GameMode: {runner.GameMode}");
            Debug.Log("Host Migration Resume called, restoring the old NetworkObjects...");
            // Get a temporary reference for each NO from the old Host
            foreach (var resumeNO in runner.GetResumeSnapshotNetworkObjects())
            {
                Debug.Log($"resume 되는 네트워크 오브젝트: {resumeNO}");
                if (
                    // Extract any NetworkBehavior used to represent the position/rotation of the NetworkObject
                    // this can be either a NetworkTransform or a NetworkRigidBody, for example
                    resumeNO.TryGetBehaviour<NetworkTransform>(out var posRot)) {

                    runner.Spawn(resumeNO, position: posRot.transform.position, rotation: posRot.transform.rotation, onBeforeSpawned: (runner, newNO) =>
                    {
                        // One key aspects of the Host Migration is to have a simple way of restoring the old NetworkObjects state
                        // If all state of the old NetworkObject is all what is necessary, just call the NetworkObject.CopyStateFrom
                        newNO.CopyStateFrom(resumeNO);

                        // and/or

                        // If only partial State is necessary, it is possible to copy it only from specific NetworkBehaviours
                        // if (resumeNO.TryGetBehaviour<NetworkBehaviour>(out var myCustomNetworkBehaviour))
                        // {
                        //     newNO.GetComponent<NetworkBehaviour>().CopyStateFrom(myCustomNetworkBehaviour);
                        // }
                    });
                }
            }
            Debug.Log("Host Migration Resume done, all NetworkObjects should be restored");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            // Can check if the Runner is being shutdown because of the Host Migration
            if (shutdownReason == ShutdownReason.HostMigration) {
            // ...
                Debug.Log("Host Migration in progress...");
            } else {
            // Or a normal Shutdown
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // Debug.Log("Scene Load Done");
            // runner.Spawn(_stageManagerPrefab);
        }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}