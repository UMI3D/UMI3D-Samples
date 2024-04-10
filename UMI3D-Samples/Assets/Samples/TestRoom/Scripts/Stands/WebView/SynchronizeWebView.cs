/*
Copyright 2019 - 2023 Inetum
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

using umi3d.edk;
using umi3d.edk.interaction;
using UnityEngine;
using static umi3d.edk.interaction.AbstractInteraction;

/// <summary>
/// Synchronized webView url from <see cref="currentMasterUser"/>
/// </summary>
public class SynchronizeWebView : MonoBehaviour
{
    [SerializeField]
    private UMI3DWebView webView;

    [SerializeField]
    private UMI3DEvent ev;

    private UMI3DUser currentMasterUser;

    private void OnEnable()
    {
        WebViewManager.Instance.onUserChangedUrlEvent.AddListener(OnUserUrlChanged);

        if (UMI3DServer.Exists)
        {
            UMI3DServer.Instance.OnUserLeave.AddListener(OnUserLeave);
        }

        ev.onTrigger.AddListener(OnTrigger);
    }

    private void OnDisable()
    {
        WebViewManager.Instance.onUserChangedUrlEvent.RemoveListener(OnUserUrlChanged);

        if (UMI3DServer.Exists)
        {
            UMI3DServer.Instance.OnUserLeave.RemoveListener(OnUserLeave);
        }

        ev.onTrigger.RemoveListener(OnTrigger);
    }

    private void OnUserUrlChanged(UMI3DUser user, ulong webViewId,  string url)
    {
        if ((user == currentMasterUser) && (webViewId == webView?.Id()))
        {
            Transaction transaction = new Transaction { reliable = false };

            var op = webView.objectUrl.SetValue(url);
            op.users.Remove(currentMasterUser);
            transaction.AddIfNotNull(op);

            transaction.Dispatch();
        }
    }

    private void OnUserLeave(UMI3DUser user)
    {
        if (user == currentMasterUser)
            currentMasterUser = null;
    }

    private void OnTrigger(InteractionEventContent content)
    {
        currentMasterUser = content.user;
    }
}
