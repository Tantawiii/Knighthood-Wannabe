using UnityEngine;

public class VFX_AutoController : MonoBehaviour
{
    [SerializeField] bool autoDestroy = true;
    [SerializeField] float destroyDelay = 1;
    [Space]
    [SerializeField] bool randomOffset = true;
    [SerializeField] bool randomRotation = true;

    [Header("Random Position")]
    [SerializeField] float xMinOffset = -.3f;
    [SerializeField] float xMaxOffset = .3f;
    [Space]
    [SerializeField] float yMinOffset = -.3f;
    [SerializeField] float yMaxOffset = .3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyRandomOffset();
        ApplyRandomRotation();

        if (autoDestroy)
            Destroy(gameObject, destroyDelay);
    }

    void ApplyRandomOffset()
    {
        if (!randomOffset)
            return;

        float xOffset = Random.Range(xMinOffset, xMaxOffset);
        float yOffset = Random.Range(yMinOffset, yMaxOffset);

        transform.position += new Vector3(xOffset, yOffset);
    }

    void ApplyRandomRotation()
    {
        if (!randomRotation) return;

        float zRotation = Random.Range(0, 360);
        transform.Rotate(0,0, zRotation);
    }
}
