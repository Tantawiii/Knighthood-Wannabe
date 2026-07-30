using UnityEngine;

public class Skill_SwordThrow : Skill_Base
{
    private SkillObject_Sword currentSword;
    private float currentThrowPower;

    [Header("Regular Sword Upgrade")]
    [SerializeField] private GameObject swordPrefab;
    [Range(0, 10)]
    [SerializeField] private float regularThrowPower = 6f;

    [Header("Pierce Sword Upgrade")]
    [SerializeField] private GameObject pierceSwordPrefab;
    public int pierceAmountOfEnemies = 2; // The number of enemies the sword can pierce through before returning to the player
    [Range(0, 10)]
    [SerializeField] private float pierceThrowPower = 5f; // The throw power for the piercing sword, can be adjusted in the inspector

    [Header("Spin Sword Upgrade")]
    [SerializeField] private GameObject spinSwordPrefab;
    [Range(5, 10)]
    public int maxDistance = 5; // The maximum distance the sword can travel from the player before stopping
    public float attackPerSecond = 6f; // The number of attacks the sword can perform per second while spinning
    public float maxSpinDuration = 2f; // The duration for which the sword will spin before returning to the player
    [Range(0, 10)]
    [SerializeField] private float spinThrowPower = 5f; // The throw power for the spinning sword, can be adjusted in the inspector

    [Header("Bounce Sword Upgrade")]
    [SerializeField] private GameObject bounceSwordPrefab;
    public int bounceCount = 5; // The number of times the sword can bounce before returning to the player
    public float bounceSpeed = 12f; // The speed at which the sword bounces
    [Range(0, 10)]
    [SerializeField] private float bounceThrowPower = 5f; // The throw power for the bouncing sword, can be adjusted in the inspector

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
        UpdateThrowPower(); // Update the throw power based on the currently unlocked upgrade

        if(currentSword != null)
        {
            currentSword.GetSwordBackToPlayer(); // If a sword is already thrown, call the method to bring it back to the player
            return false; // Cannot use the skill if a sword is already thrown and active
        }
        
        return base.CanUseSkill();
    }

    public void ThrowSword()
    {
        swordPrefab = GetSwordPrefab(); // Get the appropriate sword prefab based on unlocked upgrades
        GameObject swordInstance = Instantiate(swordPrefab, predictionDots[1].position, Quaternion.identity);

        currentSword = swordInstance.GetComponent<SkillObject_Sword>();
        currentSword.SetupSword(this, GetThrowPower());
    }

    private GameObject GetSwordPrefab()
    {
        if (Unlocked(SkillUpgradeType.SwordThrow))
            return swordPrefab;
        else if (Unlocked(SkillUpgradeType.SwordThrow_Pierce))
            return pierceSwordPrefab;   
        else if (Unlocked(SkillUpgradeType.SwordThrow_Spin))
            return spinSwordPrefab;
        else if (Unlocked(SkillUpgradeType.SwordThrow_Bounce))
            return bounceSwordPrefab;

        Debug.LogWarning("No sword upgrade is unlocked. Please unlock a sword upgrade to use the Sword Throw skill.");
        return null; // Return null if no sword upgrade is unlocked
    }

    private void UpdateThrowPower()
    {
        switch (upgradeType)
        {
            case SkillUpgradeType.SwordThrow:
                currentThrowPower = regularThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Pierce:
                currentThrowPower = pierceThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Spin:
                currentThrowPower = spinThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Bounce:
                currentThrowPower = bounceThrowPower;
                break;
            default:
                currentThrowPower = regularThrowPower; // Default to regular throw power if no upgrade is unlocked
                break;
        }
    }

    private Vector2 GetThrowPower() => confirmedDirection * (currentThrowPower * 10); // Multiply by 10 to scale the throw power to a more suitable range for the game physics

    public void PredictTrajectory(Vector2 direction)
    {
        for (int i = 0; i < predictionDots.Length; i++)
        {
            predictionDots[i].position = CalculateTrajectoryPoint(direction, i * predictionDotSpacing);
        }
    }

    private Vector2 CalculateTrajectoryPoint(Vector2 direction, float time)
    {
        float scaleThrowPower = currentThrowPower * 10;

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
