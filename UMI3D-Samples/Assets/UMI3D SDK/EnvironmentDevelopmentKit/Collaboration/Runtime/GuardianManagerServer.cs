
using System.Collections.Generic;
using System.Linq;

using umi3d.edk.binding;
using inetum.unityUtils;
using System;
using UnityEngine.Events;
using umi3d.common;
using umi3d.common.lbe;
using UnityEngine;
using umi3d.edk;
using umi3d.edk.collaboration;

namespace umi3d.edk
{
    public class GuardianManagerServer : Singleton<GuardianManagerServer>
    {
        private bool FirstUserManager = false;
        public UserGuardianDto userGuardianDto;
        public List<ulong> userId = new List<ulong>();



        void Start()
        {
        }

        public void OnUserJoin(UMI3DUser user, UserGuardianDto userGuardDto)
        {

            userId.Add(user.Id());

            if (userGuardianDto == null)
            {
                userGuardianDto = userGuardDto;

                Debug.Log("<color=cyan> userGuardianDto client : " + userGuardianDto.anchorAR.Count + "</color>");


                Debug.Log("<color=cyan>Save Guardian in server</color>");
            }
            else
            {
                Debug.Log("<color=cyan>New User</color>");

                SetGuardienUserManager(user);
            }         
        }

        public void GetGuardianUserManager(UMI3DUser user, UserGuardianDto userGuardianDto)
        {
            Debug.Log("OK GET GUARDIAN");
        }

        public void SetGuardienUserManager(UMI3DUser user)
        {

            Debug.Log("<color=green> userguardiandto : " + userGuardianDto.anchorAR.Count + "</color>");

            SendGuardianRequest sendGuardianRequest = new SendGuardianRequest(userGuardianDto);
            sendGuardianRequest.users = new HashSet<UMI3DUser> { user };

            Debug.Log("<color=green> SendGuardianRequest : " + sendGuardianRequest.guardianDataDto.anchorAR.Count + "</color>");

            Transaction transaction = new Transaction()
            {
                reliable = true,
                
            };

            transaction.AddIfNotNull(sendGuardianRequest);

            UMI3DServer.Dispatch(transaction);

            Debug.Log("<color=green> Send guardian for new user </color>");
        }

        public void GetARAnchor(UMI3DUser user, ulong trackableId, Vector3Dto position, Vector4Dto rotation)
        {

        }

        public void SetARAnchor(UMI3DUser user, ulong trackableId, Vector3Dto position, Vector4Dto rotation)
        {
            
        }
    }
}
