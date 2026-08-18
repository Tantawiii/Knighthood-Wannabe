using UnityEngine;

public class Object_NPC : MonoBehaviour
{
    protected Transform player;
    protected UI ui;

    [SerializeField] private Transform npc;
    [SerializeField] private GameObject interactTooltip;

    private bool facingRight = true;
    
    [Header("Floating Tooltip Movement")]
    [SerializeField] float floatSpeed = 8f;
    [SerializeField] float floatRange = .1f;
    Vector3 startPosition;

    protected virtual void Awake()
    {
        ui = FindFirstObjectByType<UI>();
        startPosition = interactTooltip.transform.position;
        interactTooltip.SetActive(false);
    }

    protected virtual void Update()
    {
        HandleNpcFlip();
        HandleToolTipFloat();
    }

    private void HandleToolTipFloat()
    {
        if(interactTooltip == null)
            return;
        
        if(interactTooltip.activeSelf)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
            interactTooltip.transform.position = startPosition + new Vector3(0, yOffset);
        }
    }

    private void HandleNpcFlip()
    {
        if(player == null || npc == null)
            return;
        
        if(npc.position.x < player.position.x && !facingRight)
        {
            npc.Rotate(0f, 180f, 0f);
            facingRight = !facingRight;
        }
        else if(npc.position.x > player.position.x && facingRight)
        {
            npc.Rotate(0f, 180f, 0f);
            facingRight = !facingRight;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        player = collision.transform;
        interactTooltip.SetActive(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        interactTooltip.SetActive(false);
    }
}
