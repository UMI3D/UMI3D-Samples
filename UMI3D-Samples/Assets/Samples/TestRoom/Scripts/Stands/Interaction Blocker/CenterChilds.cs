using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterChilds : MonoBehaviour
{
    [SerializeField]
    [Range(0, 10)]
    int virtualChildren = 0;
    [SerializeField]
    Vector3 From;
    [SerializeField]
    Vector3 To;
    [SerializeField]
    bool update = false;

    private void OnValidate()
    {
        if (update)
            update = false;
        int childCount = transform.childCount;
        int count = childCount + virtualChildren;
        if (count <= 0) return;

        if(count == 1)
        {
            Transform child = transform.GetChild(0);
            child.localPosition = From + (To - From)/2;
            return;
        }

        Vector3 delta = (To - From) / (count-1);
        Vector3 pos = From + delta * virtualChildren/2;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.localPosition = pos;
            pos += delta;
        }
    }
}
