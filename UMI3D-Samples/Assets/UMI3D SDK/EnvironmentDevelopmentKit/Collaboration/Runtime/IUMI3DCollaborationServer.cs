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

using System.Threading.Tasks;
using umi3d.common;
using umi3d.common.collaboration.dto.networking;
using umi3d.common.collaboration.dto.signaling;

namespace umi3d.edk.collaboration
{
    /// <summary>
    /// Manager for the UMI3D server in a collaborative context.
    /// </summary>
    public interface IUMI3DCollaborationServer : IUMI3DServer
    {
        bool IsResourceServerSetup { get; }

        /// <summary>
        /// Is the server active?
        /// </summary>
        bool isRunning { get; }

        void ClearIP();

        void Init();

        PendingTransactionDto IsThereTransactionPending(UMI3DCollaborationAbstractContentUser user);

        void NotifyUnregistered(UMI3DCollaborationAbstractContentUser user);

        void Ping(UMI3DCollaborationAbstractContentUser user);

        Task Register(RegisterIdentityDto identityDto, bool resourcesOnly = false);

        void SetIP(string ip);

        EnvironmentConnectionDto ToDto();

    }
}