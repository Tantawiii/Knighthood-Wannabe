using UnityEngine;

public class Enemy_ArcherArrow : MonoBehaviour, ICounterable
{
    [SerializeField] private LayerMask whatIsTarget;

    private Collider2D col;
    private Rigidbody2D rb;
    private Entity_Combat combat;

    public bool CanBeCountered => true;

    public void SetupArrow(float xVelocity, Entity_Combat combat)
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();


        rb.linearVelocity = new Vector2(xVelocity, 0);
        this.combat = combat;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(((1 << collision.gameObject.layer) & whatIsTarget) != 0)
        {
            
        }
    }

    public void HandleCounter()
    {
    }
}
