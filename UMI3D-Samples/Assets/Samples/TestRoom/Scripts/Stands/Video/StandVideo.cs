using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using umi3d.edk;
using umi3d.edk.interaction;
using UnityEngine;

public class StandVideo : MonoBehaviour
{
    public PlayPause PlayPause;
    public UMI3DVideoPlayer Player;

    // Start is called before the first frame update
    async void Start()
    {
        await Task.Yield();
        var t = new Transaction();
        t.AddIfNotNull(PlayPause.Play.SetAvailable());
        t.AddIfNotNull(PlayPause.Pause.SetUnavailable());
        t.AddIfNotNull(PlayPause.Stop.SetUnavailable());
        t.Dispatch();

        PlayPause.Play.Event.onTrigger.AddListener(OnPlay);
        PlayPause.Stop.Event.onTrigger.AddListener(OnStop);
        PlayPause.Pause.Event.onTrigger.AddListener(OnPause);

        if(Player == null)
            Player = this.GetComponent<UMI3DVideoPlayer>();



    }

    private void OnPause(AbstractInteraction.InteractionEventContent arg0)
    {
        var t = new Transaction();
        t.AddIfNotNull(PlayPause.Play.SetAvailable());
        t.AddIfNotNull(PlayPause.Pause.SetUnavailable());
        t.AddIfNotNull(PlayPause.Stop.SetAvailable());
        t.AddIfNotNull(Player.objectPlaying.SetValue(false));
        t.Dispatch();
    }

    private void OnStop(AbstractInteraction.InteractionEventContent arg0)
    {
        var t = new Transaction();
        t.AddIfNotNull(PlayPause.Play.SetAvailable());
        t.AddIfNotNull(PlayPause.Pause.SetUnavailable());
        t.AddIfNotNull(PlayPause.Stop.SetUnavailable());
        t.AddIfNotNull(Player.objectPlaying.SetValue(false));
        t.AddIfNotNull(Player.objectPauseTime.SetValue(0));
        t.Dispatch();
    }

    private void OnPlay(AbstractInteraction.InteractionEventContent arg0)
    {
        var t = new Transaction();
        t.AddIfNotNull(PlayPause.Play.SetUnavailable());
        t.AddIfNotNull(PlayPause.Pause.SetAvailable());
        t.AddIfNotNull(PlayPause.Stop.SetAvailable());
        t.AddIfNotNull(Player.objectPlaying.SetValue(true));
        t.Dispatch();
    }
}
