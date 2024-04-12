using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using umi3d.common;
using umi3d.common.lbe;
using umi3d.edk;
using System;
using umi3d.common.lbe.description;

namespace umi3d.common
{
    public class SendGuardianRequest : Operation
    {
        public UserGuardianDto guardianDataDto;

        public SendGuardianRequest(UserGuardianDto guardianData)
        {
            if (guardianData == null)
            {
                Debug.Log("<color=yellow> Guardian data is null. </color>");
                throw new ArgumentNullException(nameof(guardianData));
            }

            Debug.Log("<color=yellow> Guardian data is not null. </color>");


            this.guardianDataDto = guardianData;
        }

        public override Bytable ToBytable(UMI3DUser user)
        {
            if (user == null)
            {
                Debug.Log("<color=yellow> UMI3DUser is null.</color>");
                return null;
            }

            Debug.Log("<color=yellow> Write Dto </color>");

            return UMI3DSerializer.Write(GetOperationKey())
                + UMI3DSerializer.Write(guardianDataDto);
        }

        protected virtual SendGuardianRequestDto CreateDto() 
        {
            Debug.Log("<color=yellow> Create Dto </color>");

            SendGuardianRequestDto dto = new SendGuardianRequestDto();
            dto.guardianData = new UserGuardianDto();
            dto.guardianData.anchorAR = new List<ARAnchorDto>(); 
            return dto;
        }

        protected virtual void WriteProperties(SendGuardianRequestDto dto) 
        {
            if (dto == null)
            {
                Debug.Log("<color=yellow> Dto is null. </color>");
                return;
            }

            Debug.Log("<color=yellow> Write properties Dto </color>");
            
            foreach (var anchor in guardianDataDto.anchorAR)
            {
                dto.guardianData.anchorAR.Add(anchor);
            }

            Debug.Log("<color=yellow> Write properties : " + guardianDataDto.anchorAR.Count + "</color>");
            Debug.Log("<color=yellow> Write properties Dto :  " + dto.guardianData.anchorAR.Count + "</color>");
        }

        public override AbstractOperationDto ToOperationDto(UMI3DUser user)
        {
            if (user == null)
            {
                Debug.Log("<color=yellow> UMI3DUser is null. </color>");
                return null;
            }

            Debug.Log("<color=yellow> To Operation Dto </color>");

            SendGuardianRequestDto dto = CreateDto();

            if (dto == null)
            {
                Debug.Log("<color=yellow> Dto creation failed. </color>");
                return null;
            }


            WriteProperties(dto);
            Debug.Log("<color=yellow> To Operation Dto" + dto + "</color>");


            return dto;
        }

        protected virtual uint GetOperationKey()
        {
            return UMI3DOperationKeys.SetGuardianRequest;
        }
    }
}

