using UnityEngine;

public class Skill_SwordThrow : Skill_Base
{
    private SkillObject_Sword currentSword;

    [Header("Regular Sword Upgrade")]
    [SerializeField] private GameObject swordPrefab;
    [Range(0f, 10f)]
    [SerializeField] private float throwPower = 6f;

    [Header("Trajectory Prediction")]
    [SerializeField] private GameObject predictionDotPrefab;
    [SerializeField] private int predictionDotCount = 20;
    [SerializeField] private float predictionDotSpacing = 0.05f;
    private float swordGravity;
    private Transform[] predictionDots;
    private Vector2 confirmedDirection;

    protected override void Awake()
    {
        base.Awake();

        swordGravity = swordPrefab.GetComponent<Rigidbody2D>().gravityScale;
        predictionDots = GenerateDots();
    }

    public override bool CanUseSkill()
    {
        if(currentSword != null)
        {
            return false; // Cannot use the skill if a sword is already thrown and active
        }
        
        return base.CanUseSkill();
    }

    public void ThrowSword()
    {
        GameObject swordInstance = Instantiate(swordPrefab, predictionDots[1].position, Quaternion.identity);

        currentSword = swordInstance.GetComponent<SkillObject_Sword>();
        currentSword.SetupSword(this, GetThrowPower());
    }

    private Vector2 GetThrowPower() => confirmedDirection * (throwPower * 10); // Multiply by 10 to scale the throw power to a more suitable range for the game physics

    public void PredictTrajectory(Vector2 direction)
    {
        for (int i = 0; i < predictionDots.Length; i++)
        {
            predictionDots[i].position = CalculateTrajectoryPoint(direction, i * predictionDotSpacing);
        }
    }

    private Vector2 CalculateTrajectoryPoint(Vector2 direction, float time)
    {
        float scaleThrowPower = throwPower * 10;

        Vector2 initialVelocity = direction * scaleThrowPower; // This gives us the initial velocity - The Starting speed and direction of the throw

        Vector2 gravityEffect = 0.5f * Physics2D.gravity * swordGravity * (time * time); // Gravity effect on the sword down over time, the more it's in the air, the more it will fall down

        // We calculate how far the sword will travel after time 'time'
        // by combining the initial throw direction and the effect of gravity pull.
        Vector2 predictedPosition = (initialVelocity * time) + gravityEffect; 

        Vector2 playerPosition = transform.root.position; // Get the player's position to use as starting point for the prediction

        return playerPosition + predictedPosition; // Return the final predicted position of the sword after time 'time'
    }

    public void SetConfirmedDirection(Vector2 direction) => confirmedDirection = direction;

    public void EnableDot(bool enable)
    {
        foreach (var dot in predictionDots)
        {
            dot.gameObject.SetActive(enable);
        }
    }

    private Transform[] GenerateDots()
    {
        Transform[] dots = new Transform[predictionDotCount];
        for (int i = 0; i < predictionDotCount; i++)
        {
            dots[i] = Instantiate(predictionDotPrefab, transform.position, Quaternion.identity, transform).transform;
            dots[i].gameObject.SetActive(false);
        }
        return dots;
    }
}
