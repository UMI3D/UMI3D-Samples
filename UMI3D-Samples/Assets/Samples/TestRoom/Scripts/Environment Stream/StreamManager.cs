using System.Collections;
using System.Collections.Generic;
using umi3d.cdk.collaboration;
using umi3d.common;
using UnityEngine;
using UnityEngine.UI;

public class StreamManager : MonoBehaviour
{
    public InputField ServerIp;
    public Button StartButton;
    public Text StartButtonText;

    bool status = false;

    MediaDto media = null;
    UMI3DWorldControllerClient1 wcClient = null;
    UMI3DEnvironmentClient1 nvClient = null;

    private void Start()
    {
        StartButton.onClick.AddListener(StartButtonClicked);
        SetUi();
    }

    void StartButtonClicked()
    {
        DisableButtonFor(1000);
        StartStop();
    }

    async void DisableButtonFor(int millisecond)
    {
        StartButton.enabled = false;
        await UMI3DAsyncManager.Delay(millisecond);
        StartButton.enabled = true;
    }

    void StartStop()
    {
        status = !status;
        SetUi();
        if (status)
            _Start();
        else
            _Stop();
    }

    void SetUi()
    {
        StartButtonText.text = status ? "Stop Server" : "Start Server";
        ServerIp.enabled = !status;
    }

    async void _Start()
    {

        media = new MediaDto()
        {
            name = "other server",
            url = ServerIp.text
        };
        wcClient = new UMI3DWorldControllerClient1(media);
        if(await wcClient.Connect())
        {
            nvClient = await wcClient.ConnectToEnvironment();
        }

        //Get connection DTO
        // create environmnetClient
        //connect client
    }

    async void _Stop()
    {
        if (nvClient != null) {
            await nvClient.Logout(); 
            wcClient.Logout();
                }
    }
}
