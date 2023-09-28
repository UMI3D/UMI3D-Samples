﻿/*
Copyright 2019 - 2021 Inetum
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.SimpleJSON;
using System.Collections.Generic;
using umi3d.cdk.collaboration;
using umi3d.common;
using umi3d.common.collaboration;
using umi3d.edk.collaboration.tracking;
using UnityEngine;

namespace umi3d.edk.collaboration
{
    public class UMI3DForgeEnvironmentServer : ForgeSocketBase, IForgeServer
    {
        private const DebugScope scope = DebugScope.EDK | DebugScope.Collaboration | DebugScope.Networking;

        #region Fields

        /// <summary>
        /// 
        /// </summary>
        public int maxNbPlayer = 64;

        /// <summary>
        /// 
        /// </summary>
        private UDPServer server;

        /// <summary>
        /// Forge server environmentType
        /// </summary>
        public string environmentType = "";

        /// <summary>
        /// Port given to clients if they find this server via a master server.
        /// </summary>
        public ushort connectionPort;

        public UMI3DStreamableSceneRelay streamableRelay;

        object timeLock = new object();
        public ulong Time
        {
            get
            {
                lock (timeLock)
                    return server.Time.Timestep;
            }
        }

        /// <inheritdoc/>
        public override NetWorker GetNetWorker()
        {
            return server;
        }

        #endregion

        /// <summary>
        /// Create a Forge server.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="masterServerHost"></param>
        /// <param name="masterServerPort"></param>
        /// <param name="natServerHost"></param>
        /// <param name="natServerPort"></param>
        /// <param name="maxNbPlayer"></param>
        /// <returns></returns>
        public static UMI3DForgeEnvironmentServer Create(string ip = "127.0.0.1", ushort connectionPort = 50043, ushort port = 15937, string masterServerHost = "", ushort masterServerPort = 15940, string natServerHost = "", ushort natServerPort = 15941, int maxNbPlayer = 64)
        {
            UMI3DForgeEnvironmentServer server = new GameObject("UMI3DForgeServer").AddComponent<UMI3DForgeEnvironmentServer>();
            server.ip = ip;
            server.port = port;
            server.masterServerHost = masterServerHost;
            server.masterServerPort = masterServerPort;
            server.natServerHost = natServerHost;
            server.natServerPort = natServerPort;
            server.maxNbPlayer = maxNbPlayer;
            server.connectionPort = connectionPort;
            server.streamableRelay = new UMI3DStreamableSceneRelay(server, () => { return server.server.Players; });

            return server;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticator"></param>
        public void Host(IUserAuthenticator authenticator = null)
        {
            server = new UDPServer(maxNbPlayer);

            if (authenticator != null)
                server.SetUserAuthenticator(authenticator);

            server.binaryMessageReceived += ReadBinary;
            server.onPingPong += SetRoundTripLatency;
            server.playerAccepted += PlayerAccepted;
            server.playerAuthenticated += PlayerAuthenticated;
            server.playerRejected += PlayerRejected;
            server.playerTimeout += PlayerTimeout;
            server.playerDisconnected += PlayerDisconnected;

            if (natServerHost.Trim().Length == 0)
                server.Connect(ip, port);
            else
                server.Connect(port: port, natHost: natServerHost, natPort: natServerPort);


            //When connected

            if (!server.IsBound)
            {
                UMI3DLogger.LogError("NetWorker failed to bind", scope);
                return;
            }

            if (mgr == null && networkManager == null)
            {
                UMI3DLogger.LogWarning("A network manager was not provided, generating a new one instead", scope);
                networkManager = new GameObject("Network Manager");
                mgr = networkManager.AddComponent<UMI3DNetworkManager>();
            }
            else if (mgr == null)
            {
                mgr = Instantiate(networkManager).GetComponent<UMI3DNetworkManager>();
            }

            // If we are using the master server we need to get the registration data
            JSONNode masterServerData = null;
            if (!string.IsNullOrEmpty(masterServerHost))
            {
                string serverId = UMI3DCollaborationServer.Instance.sessionId;
                string serverName = UMI3DCollaborationEnvironment.Instance.environmentName;// ok
                string type = string.IsNullOrEmpty(environmentType) ? UMI3DCollaborationEnvironment.Instance.environmentName : environmentType;
                string mode = string.IsNullOrEmpty(UMI3DCollaborationServer.Instance.iconServerUrl) ? "public/picture.png" : UMI3DCollaborationServer.Instance.iconServerUrl;
                string comment = UMI3DCollaborationServer.Instance.descriptionComment;

                masterServerData = (mgr as UMI3DNetworkManager).MasterServerRegisterData(server, connectionPort.ToString(), serverId, serverName, type, mode, comment);
            }
            mgr.Initialize(server, masterServerHost, masterServerPort, masterServerData);
            NetworkObject.Flush(server); //Called because we are already in the correct scene!
            playerCount = server.Players.Count;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            if (server != null) server.Disconnect(true);
        }

        #region signaling

        public void SendSignalingMessage(NetworkingPlayer player, UMI3DDto dto)
        {
            SendBinaryDataTo((int)DataChannelTypes.Signaling, player, dto.ToBson(), true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sender"></param>
        private void PlayerTimeout(NetworkingPlayer player, NetWorker sender)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sender"></param>
        private void PlayerAuthenticated(NetworkingPlayer player, NetWorker sender)
        {
            UMI3DLogger.Log("Player Authenticated", scope);
            //UMI3DLogger.Log($"Player { player.NetworkId } {player.Name} authenticated",scope);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sender"></param>
        private void PlayerAccepted(NetworkingPlayer player, NetWorker sender)
        {
            UMI3DLogger.Log("Player Accepted", scope);
            playerCount = server.Players.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sender"></param>
        private void PlayerRejected(NetworkingPlayer player, NetWorker sender)
        {
            UMI3DLogger.Log("Player rejected", scope);
            //TODO
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sender"></param>
        private void PlayerDisconnected(NetworkingPlayer player, NetWorker sender)
        {

        }

        #endregion
        protected override void _ReadBinary(NetworkingPlayer player, Binary frame, NetWorker sender)
        {
            switch (frame.GroupId)
            {
                case MessageGroupIds.AUTHENTICATION_FAILURE:
                    OnAuthenticationFailure(player, frame, sender);
                    break;
                default:
                    OnFrame(player, frame, sender);
                    break;
            }
        }

        protected virtual void OnAuthenticationFailure(NetworkingPlayer player, Binary frame, NetWorker sender) { }

        /// <inheritdoc/>
        protected void OnFrame(NetworkingPlayer player, Binary frame, NetWorker sender)
        {

        }

        #region data



        public void SendData(NetworkingPlayer player, byte[] data, bool reliable)
        {

        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="player"></param>
        /// <param name="data"></param>
        /// <param name="isReliable"></param>
        protected void SendBinaryDataTo(int channel, NetworkingPlayer player, byte[] data, bool isReliable)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="player"></param>
        /// <param name="data"></param>
        /// <param name="isRealiable"></param>
        public void RelayBinaryDataTo(int channel, NetworkingPlayer player, byte[] data, bool isRealiable)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <param name="isReliable"></param>
        protected void SendBinaryDataToAll(int channel, byte[] data, bool isReliable)
        {
            ulong timestep = NetworkManager.Instance.Networker.Time.Timestep;
            bool isTcpClient = NetworkManager.Instance.Networker is TCPClient;
            bool isTcp = NetworkManager.Instance.Networker is BaseTCP;
            var bin = new Binary(timestep, isTcpClient, data, BeardedManStudios.Forge.Networking.Receivers.Others, channel, isTcp);
            server.Send(bin, isReliable);
        }

        #region MonoBehaviour

        /// <summary>
        /// 
        /// </summary>
        private void Start()
        {
            inetum.unityUtils.QuittingManager.OnApplicationIsQuitting.AddListener(ApplicationQuit);
            // If not using TCP
            // Should it be done before Host() ???
            NetWorker.PingForFirewall(port);
        }

        /// <summary>
        /// 
        /// </summary>
        private void ApplicationQuit()
        {
            Stop();
        }

        #endregion
    }
}