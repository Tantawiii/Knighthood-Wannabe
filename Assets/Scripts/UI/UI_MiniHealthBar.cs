using UnityEngine;

public class UI_MiniHealthBar : MonoBehaviour
{
    Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    private void OnEnable()
    {
        entity.onFlipped += HandleFlip;
    }

    private void OnDisable()
    {
        entity.onFlipped -= HandleFlip;
    }

    void HandleFlip() => transform.rotation = Quaternion.identity;

    // Update is called once per frame
    //void Update()
    //{
    //    //Quite heavy load on the FPS, does it all the time even if not required
    //    transform.rotation = Quaternion.identity;
    //}
}
