using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    Camera mainCam;
    float lastMainCamPosX;
    float cameraHalfWidth;

    [SerializeField] ParallaxLayer[] backgroundLayers;

    private void Awake()
    {
        mainCam = Camera.main;
        cameraHalfWidth = mainCam.orthographicSize * mainCam.aspect;
        InitialzieLayers();
    }

    private void FixedUpdate()
    {
        float currentCameraPositionX = mainCam.transform.position.x;
        float distanceToMove = currentCameraPositionX - lastMainCamPosX;
        lastMainCamPosX = currentCameraPositionX;

        float cameraLeftEdge = currentCameraPositionX - cameraHalfWidth;
        float cameraRightEdge = currentCameraPositionX + cameraHalfWidth;

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer.Move(distanceToMove);
            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
        }
    }

    void InitialzieLayers()
    {
        foreach (ParallaxLayer layer in backgroundLayers) 
        { 
            layer.CalculateImageWidth();
        }
    }
}
