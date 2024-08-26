using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigureStand : MonoBehaviour
{
    [SerializeField]
    GameObject[] prefab;

    [SerializeField]
    GameObject center;

    [SerializeField]
    bool update = false;

    private void OnValidate()
    {
        if (update)
            update = false;



    }
}
