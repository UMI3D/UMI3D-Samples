
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

        void Start()
        {
            UMI3DServerService = UMI3DServer.Instance;
            UMI3DServerService.OnUserJoin.AddListener(OnUserJoin);
        }

        // Méthode appelée lorsque l'utilisateur rejoint
        void OnUserJoin(UMI3DUser user)
        {
            // Vérifier si c'est le premier utilisateur
            if (UMI3DServerService.Users().Count() == 1 && FirstUserManager == false)
            {
                FirstUserManager = true;

                // Récupérer le guardian de l'utilisateur
               // GetGuardianUserManager(user);
            }

            else if (UMI3DServerService.Users().Count() > 1 && FirstUserManager == true)
            {
                // Set le guardian au nouveau client
                //SetGuardienUserManager(user);
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


        }    
    }
}
