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
using UnityEngine;

/// <summary>
/// Synchronized webView url from <see cref="currentMasterUser"/>
/// </summary>
public class SynchronizeWebView : MonoBehaviour
{
    [SerializeField]
    private UMI3DWebView webView;

    private void OnEnable()
    {
        UMI3DServer.Instance.OnUserActive.AddListener(OnUserActive);
    }

    private void OnDisable()
    {
        UMI3DServer.Instance.OnUserActive.RemoveListener(OnUserActive);
    }

    private void OnUserActive(UMI3DUser user)
    {
        Transaction t = new () { reliable = true };
        t.AddIfNotNull(webView.objectIsAdmin.SetValue(user, true));
        t.Dispatch();
    }
}
