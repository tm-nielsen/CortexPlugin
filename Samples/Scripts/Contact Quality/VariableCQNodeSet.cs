using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using TMPro;

public class VariableCQNodeSet : CQNodeSet
{
    public GameObject nodePrefab;
    public Transform nodeList;

    Color[] nodeColours;

    public override void Init(Color[] colours)
    {
        nodes = new CQNode[0];
        nodeColours = colours;
    }

    public override void OnCQUpdate(DeviceInfo data)
    {
        int expectedNodeCount = data.cqHeaders.Count;
        if (nodes.Length != expectedNodeCount)
        {
            MakeNodes(data, expectedNodeCount);
        }

        foreach (CQNode node in nodes)
            node.UpdateQuality(data);
    }

    void MakeNodes(DeviceInfo data, int count)
    {
        nodes = new CQNode[count];
        nodes[0] = MakeNode("Ref");
        for (int i = 1; i < count; i++)
        {
            nodes[i] = MakeNode(data.cqHeaders[i - 1]);
        }

        foreach (CQNode node in nodes)
            node.Init(nodeColours);
    }

    CQNode MakeNode(string header)
    {
        CQNode node = Instantiate(nodePrefab, nodeList).GetComponentInChildren<CQNode>();
        node.channelID = header;
        return node;
    }
}
