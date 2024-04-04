
using System.Collections.Generic;
using System.Linq;

using umi3d.edk.binding;
using inetum.unityUtils;
using System;
using UnityEngine.Events;
using umi3d.common;
using umi3d.common.lbe;
using UnityEngine;

namespace umi3d.edk
{
    public class GuardianManager : Singleton<GuardianManager>
    {
        private bool FirstUserManager = false;

        private IBindingService bindingHelperService;
        private IUMI3DServer UMI3DServerService;
        private UserGuardianDto firstUserGuardianDto;
        private Dictionary<UMI3DUser, UserGuardianDto> userGuardianMap = new Dictionary<UMI3DUser, UserGuardianDto>();

        public UserGuardianDto userGuardianDto;

        void Start()
        {
            UMI3DServerService = UMI3DServer.Instance;
            //UMI3DServerService.OnUserJoin.AddListener(OnUserJoin);
        }

        // Méthode appelée lorsque l'utilisateur rejoint
        public void OnUserJoin(UMI3DUser user, UserGuardianDto userGuardDto)
        {
            if(userGuardianDto == null)
            {
                userGuardianDto = userGuardDto;
                Debug.Log("Save Guardian in server");
            }
            else
            {
                SetGuardienUserManager(user, userGuardianDto);
            }         
        }

        public void GetGuardianUserManager(UMI3DUser user, UserGuardianDto userGuardianDto)
        {
            // Sauvegarder la liste des users du guardian ici
            Debug.Log("OK GET GUARDIAN"); 
        }

        public void SetGuardienUserManager(UMI3DUser user, UserGuardianDto userGuardianDto)
        {
            // Sauvegarder la liste des users du guardian ici
            Debug.Log("Send guardian for new user");

        }    
    }
}
