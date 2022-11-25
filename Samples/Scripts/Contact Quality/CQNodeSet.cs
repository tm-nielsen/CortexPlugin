using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using System;
using System.Linq;
using TMPro;

public class CQNodeSet : MonoBehaviour
{
    protected CQNode[] nodes;


    public virtual void Init(Color[] nodeColours)
    {
        List<CQNode> nodeList = new List<CQNode>();
        foreach (Transform child in transform)
        {
            CQNode node = child.GetComponent<CQNode>();
            if (node)
                nodeList.Add(node);
        }
        nodes = nodeList.ToArray();

        foreach (CQNode node in nodes)
            node.Init(nodeColours);
    }

    public virtual void OnCQUpdate(DeviceInfo data)
    {
        foreach (CQNode node in nodes)
            node.UpdateQuality(data);
    }

    public bool CanDisplay(List<string> cqHeaders)
    {
        string overall = ChannelStringList.ChannelToString(Channel_t.CHAN_CQ_OVERALL);
        if (cqHeaders.Contains(overall))
            cqHeaders.Remove(overall);

        List<CQNode> displayNodes = nodes.Where(node => !node.IsReferenceNode()).ToList();

        if (cqHeaders.Count != displayNodes.Count)
            return false;


        foreach (CQNode node in displayNodes)
        {
            if (!cqHeaders.Contains(node.channelID))
                return false;
        }

        return true;
    }
}
