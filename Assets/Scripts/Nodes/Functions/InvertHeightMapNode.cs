﻿using UnityEngine;
using XNode;

[CreateNodeMenu("Functions/Invert HeightMap")]
public class InvertHeightMapNode : Node
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public NodeVariables.NextTask PreviousTask;
    [Output] public NodeVariables.NextTask NextTask;
    public override object GetValue(NodePort port)
    {
        return null;
    }
    public void RunNode()
    {
        MapIO mapIO = GameObject.FindGameObjectWithTag("MapIO").GetComponent<MapIO>();
        mapIO.InvertHeightmap();
    }
}