using System;
using System.Collections;
using System.Collections.Generic;
using umi3d.edk;
using UnityEngine;

public class TestObjWiithRef : MonoBehaviour
{
    [SerializeField]
    public TestObjWiiithRef wiiithRef;

    [SerializeField]
    protected Transform tVar;
    public GameObject gVar;
    public UMI3DNode nVar;
    public MonoBehaviour mVar;
    public TestObjWiithRef wiithRef;
    public int iVar;
    public float[] fVars;
    public Transform[] transforms;

    public Vector2 vector2;
    public Vector2[] vector2s;

    public string sVar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[Serializable]
public class TestObjWiiithRef
{
    [SerializeField]
    public TestObjWiithRef wiithRef;

    [SerializeField]
    public int iVar;

    [SerializeField]
    public Vector2 vector2;
}