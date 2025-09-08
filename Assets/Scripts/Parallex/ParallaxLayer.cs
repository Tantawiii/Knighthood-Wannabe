using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [SerializeField] Transform background;
    [SerializeField] float parallaxMultiplier;
    [SerializeField] float parallaxOffset = 10;

    float imageFullWidth;
    float imageHalfWidth;

    public void CalculateImageWidth()
    {
        imageFullWidth = background.GetComponent<SpriteRenderer>().bounds.size.x;
        imageHalfWidth = imageFullWidth / 2;
    }

    public void Move(float distanceToMove)
    {
        background.position += Vector3.right * (distanceToMove * parallaxMultiplier);
        //Or background.position += new Vector3 (distanceToMove * parallaxMultiplier, 0, 0);
    }

    public void LoopBackground(float cameraLeftEdge, float cameraRightEdge)
    {
        float imageRightEdge = (background.position.x + imageHalfWidth) - parallaxOffset;
        float imageLeftEdge = (background.position.x - imageHalfWidth) + parallaxOffset;

        if(imageRightEdge < cameraLeftEdge)
            background.position += Vector3.right * imageFullWidth;
        else if(imageLeftEdge > cameraRightEdge)
            background.position += Vector3.right * -imageFullWidth;
    }
}
