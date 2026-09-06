using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UI_TreeConnectDetails
{
    public UI_TreeConnectHandler childNode;
    public NodeDirectionType direction;
    [Range(100f, 350f)] public float length;
    [Range(-50f, 50f)] public float rotation;
}

public class UI_TreeConnectHandler : MonoBehaviour
{
    private RectTransform myRectTransform => GetComponent<RectTransform>();
    [SerializeField] private UI_TreeConnectDetails[] connectionDetails;
    [SerializeField] private UI_TreeConnection[] connections;

    private Image connectionImage;
    private Color originalColor;

    private void Awake()
    {
        if (connectionImage != null)
        {
            originalColor = connectionImage.color;
        }
    }

    public UI_TreeNode[] GetChildNodes()
    {
        List<UI_TreeNode> childrenToReturn = new List<UI_TreeNode>();
        foreach (var node in connectionDetails)
        {
            if (node.childNode != null)
            {
                childrenToReturn.Add(node.childNode.GetComponent<UI_TreeNode>());
            }
        }
        return childrenToReturn.ToArray();
    }

    public void UpdateConnection()
    {
        for (int i = 0; i < connectionDetails.Length; i++)
        {
            var detail = connectionDetails[i];
            var connection = connections[i];
            Vector2 targetPosition = connection.GetConnectionPoint(myRectTransform);
            Image connectionImage = connection.GetConnectionImage();

            connections[i].DirectConnection(detail.direction, detail.length, detail.rotation);

            if (detail.childNode == null)
            {
                continue;
            }

            detail.childNode.SetPosition(targetPosition);
            detail.childNode.SetConnectionImage(connectionImage);
            detail.childNode.transform.SetAsLastSibling();
        }
    }

    public void UpdateAllConnections()
    {
        UpdateConnection();

        foreach (var node in connectionDetails)
        {
            if (node.childNode == null)
            {
                continue;
            }
            
            node.childNode?.UpdateConnection();
        }
    }

    public void UnlockConnectionImage(bool unlocked)
    {
        if (connectionImage == null)
        {
            return;
        }

        connectionImage.color = unlocked ? Color.white : originalColor;
    }

    public void SetConnectionImage(Image image) => connectionImage = image;

    public void SetPosition(Vector2 position) => myRectTransform.anchoredPosition = position;

    private void OnValidate()
    {
        if (connectionDetails.Length <= 0)
        {
            return;
        }

        if (connectionDetails.Length != connections.Length)
        {
            Debug.LogError("Connection details and connections arrays must have the same length.");
            return;
        }

        UpdateConnection();
    }
}
