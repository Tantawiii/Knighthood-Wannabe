using UnityEngine;

public class Object_NPC : MonoBehaviour, IInteractable
{
    protected Transform player;
    protected UI ui;
    protected Player_QuestManager questManager;

    [Header("Quest Info")]
    [SerializeField] private string npcTargetQuestID; // Enemy name, NPC name, Item name, etc.
    [SerializeField] protected RewardGiver npcType; // The type of NPC that gives the quest (e.g., Enemy, NPC, Item, etc.)
    [Space]
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

    protected virtual void Start()
    {
        questManager = Player.Instance.questManager;
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

    public virtual void Interact()
    {
        questManager.AddProgress(npcTargetQuestID);
        // questManager.TryGiveRewardFrom(npcType);
    }
}
