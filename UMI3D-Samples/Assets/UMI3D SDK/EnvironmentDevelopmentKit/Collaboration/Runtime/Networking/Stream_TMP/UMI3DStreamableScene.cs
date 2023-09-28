using BeardedManStudios.Forge.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using umi3d.common.collaboration;
using umi3d.edk;
using umi3d.edk.collaboration;
using umi3d.edk.collaboration.tracking;

public class UMI3DStreamableScene
{
    HashSet<UMI3DEntity> entities;

    public IReadOnlyList<UMI3DEntity> Entities => entities.ToList();
    public UMI3DStreamableSceneRelay relay;

    public UMI3DStreamableScene(UMI3DStreamableSceneRelay relay)
    {
        this.entities = new HashSet<UMI3DEntity>();
        this.relay = relay;
    }

    public bool Add(UMI3DEntity entity)
    {
        return entities.Add(entity);
    }

    public bool Remove(UMI3DEntity entity) => entities.Remove(entity);

    public UMI3DDto getDto()
    {
        return new UMI3DDto();
    }

    public UMI3DDto getFrameDto()
    {
        return new UMI3DDto();
    }

}

public class UMI3DStreamableSceneRelay : UMI3DRelay<NetworkingPlayer, UMI3DStreamableScene, UMI3DDto>
{
    IForgeServer server;
    protected DataChannelTypes dataChannel = DataChannelTypes.Data;
    private Func<IEnumerable<NetworkingPlayer>> getTargets;

    public UMI3DStreamableSceneRelay(IForgeServer server, Func<IEnumerable<NetworkingPlayer>> getTargets) : base()
    {
        this.getTargets = getTargets;
        this.server = server;
    }

    protected override IEnumerable<NetworkingPlayer> GetTargets()
    {
        return getTargets();
    }

    protected override ulong GetTime()
    {
        return server.Time;
    }

    protected override void Send(NetworkingPlayer to, List<UMI3DDto> frames, bool force)
    {
        server.RelayBinaryDataTo((int)dataChannel, to, UMI3DSerializer.WriteCollection(frames).ToBytes(), force);
    }
}