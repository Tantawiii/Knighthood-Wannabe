using System.Collections;
using UnityEngine;

public class Player_VFX : Entity_VFX
{
    [Header("Image Echo VFX")]
    [Range(.01f, .2f)]
    [SerializeField] private float imageEchoInterval = .05f;
    [SerializeField] private GameObject imageEchoPrefab;
    
    private Coroutine imageEchoCoroutine;

    public void DoImageEcho(float duration)
    {
        if (imageEchoCoroutine != null)
            StopCoroutine(imageEchoCoroutine);

        imageEchoCoroutine = StartCoroutine(ImageEchoCo(duration));
    }

    private IEnumerator ImageEchoCo(float duration)
    {
        float timeTracker = 0f;
        while (timeTracker < duration)
        {
            CreateImageEcho();

            yield return new WaitForSeconds(imageEchoInterval);
            timeTracker += imageEchoInterval;
        }
    }

    private void CreateImageEcho()
    {
        GameObject echo = Instantiate(imageEchoPrefab, transform.position, transform.rotation);
        echo.GetComponentInChildren<SpriteRenderer>().sprite = spriteRenderer.sprite;
    }
}
