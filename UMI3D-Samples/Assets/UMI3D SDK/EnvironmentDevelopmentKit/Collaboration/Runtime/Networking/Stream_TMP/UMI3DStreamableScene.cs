using System.Collections;
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using umi3d.common.userCapture.tracking;
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.collaboration.tracking;
using UnityEngine;

public class UMI3DStreamableScene
{
    HashSet<IEntity> entities;

    public IReadOnlyList<IEntity> Entities => entities.ToList();

    public UMI3DStreamableScene()
    {
        this.entities = new HashSet<IEntity>();
    }


    public bool Add(IEntity entity)
    {
        return entities.Add(entity);
    }

    public bool Remove(IEntity entity)
    {
        return entities.Remove(entity);
    }

}

//public class UMI3DStreamableSceneRelay : UMI3DRelay<UMI3DDto>
//{
//    public UMI3DStreamableSceneRelay(IForgeServer server) : base(server)
//    {
//    }

//    protected override byte[] GetMessage(List<UMI3DDto> frames)
//    {
//        if (UMI3DEnvironment.Instance.useDto)
//            return (new UMI3DDtoListDto<UMI3DDto>() { values = frames }).ToBson();
//        else
//            return UMI3DSerializer.WriteCollection(frames).ToBytes();
//    }
//}
