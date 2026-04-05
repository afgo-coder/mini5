using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Serializable]
    public class FighterInput
    {
        public KeyCode moveLeft = KeyCode.A;
        public KeyCode moveRight = KeyCode.D;
        public KeyCode jump = KeyCode.W;
        public KeyCode lightAttack = KeyCode.F;
        public KeyCode heavyAttack = KeyCode.G;
    }

    [Serializable]
    public class FighterRig
    {
        public string fighterName = "Player";
        public GameObject actor;
        public FighterInput input = new FighterInput();
    }

    public enum FighterState
    {
        Idle,
        Move,
        Jump,
        LightAttack,
        HeavyAttack,
        JumpLightAttack,
        JumpHeavyAttack,
        Hit,
        Win,
        Lose,
    }

    private enum MatchState
    {
        RoundIntro,
        Fighting,
        RoundOver,
    }

    private enum AttackType
    {
        None,
        Light,
        Heavy,
        JumpLight,
        JumpHeavy,
    }

    private class FighterRuntime
    {
        public Transform transform;
        public Rigidbody2D rigidbody;
        public Collider2D bodyCollider;
        public SpriteRenderer[] spriteRenderers;
        public Color[] originalSpriteColors;
        public Vector3 velocity;
        public Vector3 spawnPosition;
        public float horizontalInput;
        public bool jumpPressed;
        public bool lightPressed;
        public bool heavyPressed;
        public FighterState state = FighterState.Idle;
        public FighterState previousLoggedState = FighterState.Idle;
        public AttackType currentAttack = AttackType.None;
        public float attackTimer;
        public float hitStunTimer;
        public bool isGrounded = true;
        public float facing = 1f;
        public Vector3 baseScale = Vector3.one;
        public int currentHealth;
        public float hitEffectTimer;
        public float invincibilityTimer;
        public float blinkTimer;
        public bool blinkVisible = true;
    }

    [Header("Scene")]
    [SerializeField] private FighterRig player1 = new FighterRig
    {
        fighterName = "Player1",
        input = new FighterInput
        {
            moveLeft = KeyCode.A,
            moveRight = KeyCode.D,
            jump = KeyCode.W,
            lightAttack = KeyCode.F,
            heavyAttack = KeyCode.G,
        },
    };

    [SerializeField] private FighterRig player2 = new FighterRig
    {
        fighterName = "Player2",
        input = new FighterInput
        {
            moveLeft = KeyCode.LeftArrow,
            moveRight = KeyCode.RightArrow,
            jump = KeyCode.UpArrow,
            lightAttack = KeyCode.Keypad1,
            heavyAttack = KeyCode.Keypad2,
        },
    };

    [SerializeField] private Transform ground;
    [SerializeField] private FightHud fightHud;

    [Header("Input")]
    [SerializeField] private bool applyDefaultBindingsOnStart = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 9f;
    [SerializeField] private float jumpPower = 12f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float groundSnapDistance = 0.1f;
    [SerializeField] private float hitSlideDamping = 18f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private Vector2 groundCheckShrink = new Vector2(0.05f, 0.02f);

    [Header("Stage Bounds")]
    [SerializeField] private bool useCameraBoundsForWalls = true;
    [SerializeField] private bool useGroundBoundsForWalls = true;
    [SerializeField] private float wallInset = 0.25f;
    [SerializeField] private float manualLeftWallX = -8f;
    [SerializeField] private float manualRightWallX = 8f;

    [Header("Attack Timing")]
    [SerializeField] private float lightAttackDuration = 0.25f;
    [SerializeField] private float heavyAttackDuration = 0.45f;
    [SerializeField] private float jumpLightAttackDuration = 0.3f;
    [SerializeField] private float jumpHeavyAttackDuration = 0.5f;
    [SerializeField] private float attackMoveSlowMultiplier = 0.35f;

    [Header("Combat")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float attackHeightTolerance = 1.2f;
    [SerializeField] private float lightAttackRange = 1.6f;
    [SerializeField] private float heavyAttackRange = 2f;
    [SerializeField] private int lightAttackDamage = 8;
    [SerializeField] private int heavyAttackDamage = 14;
    [SerializeField] private int jumpLightAttackDamage = 10;
    [SerializeField] private int jumpHeavyAttackDamage = 16;
    [SerializeField] private float lightHitStun = 0.2f;
    [SerializeField] private float heavyHitStun = 0.35f;
    [SerializeField] private float jumpLightHitStun = 0.22f;
    [SerializeField] private float jumpHeavyHitStun = 0.38f;
    [SerializeField] private Vector2 lightKnockback = new Vector2(3.5f, 1.5f);
    [SerializeField] private Vector2 heavyKnockback = new Vector2(6f, 2f);
    [SerializeField] private Vector2 jumpLightKnockback = new Vector2(4f, 2.2f);
    [SerializeField] private Vector2 jumpHeavyKnockback = new Vector2(6.5f, 3f);
    [SerializeField] private float hitEffectDuration = 0.5f;
    [SerializeField] private float hitBlinkInterval = 0.1f;

    [Header("Round")]
    [SerializeField] private int startingRound = 1;
    [SerializeField] private int roundsToWinMatch = 3;
    [SerializeField] private float roundTime = 60f;
    [SerializeField] private float roundIntroDuration = 2.5f;
    [SerializeField] private float roundResultDuration = 2.5f;
    [SerializeField] private bool autoRestartRound = true;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.5f, -10f);
    [SerializeField] private float cameraSmoothSpeed = 8f;
    [SerializeField] private bool followJumpHeight = false;
    [SerializeField] private bool enableCameraFollow = false;
    [SerializeField] private float orthographicSize = 6f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool logInputEvents = true;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private FighterState player1State = FighterState.Idle;
    [SerializeField] private FighterState player2State = FighterState.Idle;

    private FighterRuntime player1Runtime;
    private FighterRuntime player2Runtime;
    private MatchState matchState = MatchState.RoundIntro;
    private float roundTimer;
    private float stateTimer;
    private int currentRound;
    private int player1RoundWins;
    private int player2RoundWins;
    private bool matchFinished;
    private float groundY;
    private float leftWallX;
    private float rightWallX;
    private float cameraBaseY;
    private Vector3 fixedCameraPosition;

    private void Start()
    {
        if (applyDefaultBindingsOnStart)
        {
            ApplyDefaultBindings();
        }

        player1Runtime = CreateRuntime(player1);
        player2Runtime = CreateRuntime(player2);
        groundY = ResolveGroundY();
        ResolveStageBounds();

        ConfigureCamera();
        SetupHud();
        SnapFightersToGround();

        currentRound = startingRound;
        StartRoundIntro();
        LogCurrentSetup();
    }

    private void OnValidate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (!Application.isPlaying)
        {
            ApplyCameraSettings();
        }
    }

    private void Update()
    {
        groundY = ResolveGroundY();
        ResolveStageBounds();
        UpdateMatchState();
        CaptureFighterInput(player1, player1Runtime);
        CaptureFighterInput(player2, player2Runtime);
        RefreshHud();
    }

    private void FixedUpdate()
    {
        groundY = ResolveGroundY();
        ResolveStageBounds();

        SimulateFighter(player1, player1Runtime, player2Runtime, ref player1State);
        SimulateFighter(player2, player2Runtime, player1Runtime, ref player2State);
    }

    private void LateUpdate()
    {
        UpdateMainCamera();
    }

    private FighterRuntime CreateRuntime(FighterRig rig)
    {
        FighterRuntime runtime = new FighterRuntime();

        if (rig.actor != null)
        {
            runtime.rigidbody = rig.actor.GetComponent<Rigidbody2D>();

            if (runtime.rigidbody == null)
            {
                runtime.rigidbody = rig.actor.GetComponentInChildren<Rigidbody2D>();
            }

            runtime.bodyCollider = rig.actor.GetComponent<Collider2D>();

            if (runtime.bodyCollider == null)
            {
                runtime.bodyCollider = rig.actor.GetComponentInChildren<Collider2D>();
            }

            if (runtime.rigidbody != null)
            {
                runtime.transform = runtime.rigidbody.transform;
            }
            else if (runtime.bodyCollider != null)
            {
                runtime.transform = runtime.bodyCollider.transform;
            }
            else
            {
                runtime.transform = rig.actor.transform;
            }

            runtime.baseScale = runtime.transform.localScale;
            runtime.spawnPosition = runtime.transform.position;
            runtime.currentHealth = maxHealth;
            runtime.spriteRenderers = rig.actor.GetComponentsInChildren<SpriteRenderer>(true);
            runtime.originalSpriteColors = new Color[runtime.spriteRenderers.Length];

            for (int i = 0; i < runtime.spriteRenderers.Length; i++)
            {
                runtime.originalSpriteColors[i] = runtime.spriteRenderers[i] != null
                    ? runtime.spriteRenderers[i].color
                    : Color.white;
            }

            if (runtime.rigidbody != null)
            {
                runtime.rigidbody.bodyType = RigidbodyType2D.Kinematic;
                runtime.rigidbody.gravityScale = 0f;
                runtime.rigidbody.freezeRotation = true;
                runtime.rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (enableDebugLogs && (runtime.rigidbody == null || runtime.bodyCollider == null))
            {
                Debug.LogWarning(
                    $"{rig.fighterName} setup warning: Rigidbody2D 또는 Collider2D를 찾지 못했습니다. " +
                    "Player 오브젝트 본체나 자식 오브젝트에 컴포넌트가 있는지 확인하세요."
                );
            }
        }

        return runtime;
    }

    private void CaptureFighterInput(FighterRig rig, FighterRuntime runtime)
    {
        if (runtime == null || runtime.transform == null)
        {
            return;
        }

        bool jumpPressed = Input.GetKeyDown(rig.input.jump);
        bool lightPressed = Input.GetKeyDown(rig.input.lightAttack);
        bool heavyPressed = Input.GetKeyDown(rig.input.heavyAttack);
        bool moveLeftPressed = Input.GetKeyDown(rig.input.moveLeft);
        bool moveRightPressed = Input.GetKeyDown(rig.input.moveRight);
        float horizontalInput = GetHorizontalInput(rig.input);

        LogInputEvents(rig, moveLeftPressed, moveRightPressed, jumpPressed, lightPressed, heavyPressed);
        runtime.horizontalInput = horizontalInput;
        runtime.jumpPressed |= jumpPressed;
        runtime.lightPressed |= lightPressed;
        runtime.heavyPressed |= heavyPressed;
    }

    private void SimulateFighter(
        FighterRig rig,
        FighterRuntime runtime,
        FighterRuntime opponent,
        ref FighterState debugState
    )
    {
        if (runtime == null || runtime.transform == null)
        {
            return;
        }

        if (matchState == MatchState.RoundOver)
        {
            runtime.horizontalInput = 0f;
            runtime.jumpPressed = false;
            runtime.lightPressed = false;
            runtime.heavyPressed = false;
            ApplyFacing(runtime);
            debugState = runtime.state;
            LogStateChange(rig, runtime);
            return;
        }

        UpdateHitState(runtime);
        UpdateHitEffect(runtime);

        bool canControl = matchState == MatchState.Fighting && runtime.hitStunTimer <= 0f && runtime.hitEffectTimer <= 0f;

        if (!canControl)
        {
            runtime.horizontalInput = 0f;
        }

        if (canControl && runtime.attackTimer <= 0f)
        {
            TryStartAttack(runtime, opponent);
        }
        else
        {
            UpdateAttackTimer(runtime);
        }

        float moveMultiplier = runtime.attackTimer > 0f ? attackMoveSlowMultiplier : 1f;

        if (runtime.hitStunTimer > 0f || runtime.hitEffectTimer > 0f)
        {
            runtime.velocity.x = Mathf.MoveTowards(runtime.velocity.x, 0f, hitSlideDamping * Time.fixedDeltaTime);
        }
        else
        {
            runtime.velocity.x = runtime.horizontalInput * moveSpeed * moveMultiplier;
        }

        if (Mathf.Abs(runtime.horizontalInput) > 0.01f && runtime.hitStunTimer <= 0f)
        {
            runtime.facing = Mathf.Sign(runtime.horizontalInput);
        }

        if (canControl && runtime.jumpPressed && runtime.isGrounded && runtime.attackTimer <= 0f)
        {
            runtime.velocity.y = jumpPower;
            runtime.isGrounded = false;
            runtime.state = FighterState.Jump;
        }

        runtime.velocity.y -= gravity * Time.fixedDeltaTime;
        ClampToGround(runtime);
        UpdateIdleOrMoveState(runtime, runtime.horizontalInput);
        ApplyFacing(runtime);
        ApplyMovement(runtime);

        debugState = runtime.state;
        LogStateChange(rig, runtime);

        runtime.jumpPressed = false;
        runtime.lightPressed = false;
        runtime.heavyPressed = false;
    }

    private void TryStartAttack(FighterRuntime attacker, FighterRuntime defender)
    {
        if (attacker.lightPressed)
        {
            StartAttack(attacker, defender, attacker.isGrounded ? AttackType.Light : AttackType.JumpLight);
            return;
        }

        if (attacker.heavyPressed)
        {
            StartAttack(attacker, defender, attacker.isGrounded ? AttackType.Heavy : AttackType.JumpHeavy);
        }
    }

    private void StartAttack(FighterRuntime attacker, FighterRuntime defender, AttackType attackType)
    {
        attacker.currentAttack = attackType;

        switch (attackType)
        {
            case AttackType.Light:
                attacker.state = FighterState.LightAttack;
                attacker.attackTimer = lightAttackDuration;
                break;
            case AttackType.Heavy:
                attacker.state = FighterState.HeavyAttack;
                attacker.attackTimer = heavyAttackDuration;
                break;
            case AttackType.JumpLight:
                attacker.state = FighterState.JumpLightAttack;
                attacker.attackTimer = jumpLightAttackDuration;
                break;
            case AttackType.JumpHeavy:
                attacker.state = FighterState.JumpHeavyAttack;
                attacker.attackTimer = jumpHeavyAttackDuration;
                break;
        }

        TryApplyAttackHit(attacker, defender, attackType);
    }

    private void UpdateAttackTimer(FighterRuntime runtime)
    {
        if (runtime.attackTimer <= 0f)
        {
            return;
        }

        runtime.attackTimer -= Time.fixedDeltaTime;

        if (runtime.attackTimer > 0f)
        {
            return;
        }

        runtime.attackTimer = 0f;
        runtime.currentAttack = AttackType.None;

        if (runtime.hitStunTimer > 0f)
        {
            runtime.state = FighterState.Hit;
        }
        else
        {
            runtime.state = runtime.isGrounded ? FighterState.Idle : FighterState.Jump;
        }
    }

    private void UpdateHitState(FighterRuntime runtime)
    {
        if (runtime.hitStunTimer <= 0f)
        {
            return;
        }

        runtime.hitStunTimer -= Time.fixedDeltaTime;

        if (runtime.hitStunTimer <= 0f)
        {
            runtime.hitStunTimer = 0f;

            if (runtime.attackTimer <= 0f)
            {
                runtime.state = runtime.isGrounded ? FighterState.Idle : FighterState.Jump;
            }
            return;
        }

        runtime.state = FighterState.Hit;
    }

    private void UpdateHitEffect(FighterRuntime runtime)
    {
        if (runtime.invincibilityTimer > 0f)
        {
            runtime.invincibilityTimer = Mathf.Max(0f, runtime.invincibilityTimer - Time.fixedDeltaTime);
        }

        if (runtime.hitEffectTimer <= 0f)
        {
            if (!runtime.blinkVisible)
            {
                runtime.blinkVisible = true;
                SetFighterBlinkTint(runtime, true);
            }

            runtime.blinkTimer = 0f;
            return;
        }

        runtime.hitEffectTimer = Mathf.Max(0f, runtime.hitEffectTimer - Time.fixedDeltaTime);
        runtime.blinkTimer -= Time.fixedDeltaTime;

        if (runtime.blinkTimer <= 0f)
        {
            runtime.blinkVisible = !runtime.blinkVisible;
            SetFighterBlinkTint(runtime, runtime.blinkVisible);
            runtime.blinkTimer = Mathf.Max(0.02f, hitBlinkInterval);
        }

        if (runtime.hitEffectTimer <= 0f)
        {
            runtime.blinkVisible = true;
            SetFighterBlinkTint(runtime, true);

            if (runtime.hitStunTimer <= 0f && runtime.attackTimer <= 0f && runtime.currentHealth > 0)
            {
                runtime.state = runtime.isGrounded ? FighterState.Idle : FighterState.Jump;
            }
        }
    }

    private void TryApplyAttackHit(FighterRuntime attacker, FighterRuntime defender, AttackType attackType)
    {
        if (
            defender == null ||
            defender.transform == null ||
            defender.currentHealth <= 0 ||
            defender.invincibilityTimer > 0f
        )
        {
            return;
        }

        Vector2 delta = (Vector2)(defender.transform.position - attacker.transform.position);
        float direction = Mathf.Sign(delta.x == 0f ? attacker.facing : delta.x);
        bool inFront = Mathf.Sign(attacker.facing) == direction || Mathf.Abs(delta.x) < 0.05f;
        bool inHeight = Mathf.Abs(delta.y) <= attackHeightTolerance;
        float attackRange = GetAttackRange(attackType);

        if (!inFront || !inHeight || Mathf.Abs(delta.x) > attackRange)
        {
            return;
        }

        ApplyDamage(defender, GetAttackDamage(attackType));
        ApplyKnockback(defender, GetKnockback(attackType), attacker.facing);
        defender.hitStunTimer = GetHitStun(attackType);
        defender.hitEffectTimer = hitEffectDuration;
        defender.invincibilityTimer = hitEffectDuration;
        defender.blinkTimer = 0f;
        defender.blinkVisible = false;
        SetFighterBlinkTint(defender, false);
        defender.currentAttack = AttackType.None;
        defender.attackTimer = 0f;
        defender.state = FighterState.Hit;

        if (enableDebugLogs)
        {
            Debug.Log($"{GetRuntimeName(attacker)} hit {GetRuntimeName(defender)} with {attackType}");
        }
    }

    private void ApplyDamage(FighterRuntime runtime, int damage)
    {
        runtime.currentHealth = Mathf.Max(0, runtime.currentHealth - damage);

        if (runtime.currentHealth <= 0)
        {
            runtime.hitStunTimer = 0f;
        }
    }

    private void ApplyKnockback(FighterRuntime runtime, Vector2 knockback, float attackerFacing)
    {
        runtime.velocity.x = knockback.x * Mathf.Sign(attackerFacing);
        runtime.velocity.y = knockback.y;
        runtime.isGrounded = false;
    }

    private float GetAttackRange(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Heavy:
            case AttackType.JumpHeavy:
                return heavyAttackRange;
            default:
                return lightAttackRange;
        }
    }

    private int GetAttackDamage(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Light:
                return lightAttackDamage;
            case AttackType.Heavy:
                return heavyAttackDamage;
            case AttackType.JumpLight:
                return jumpLightAttackDamage;
            case AttackType.JumpHeavy:
                return jumpHeavyAttackDamage;
            default:
                return 0;
        }
    }

    private float GetHitStun(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Light:
                return lightHitStun;
            case AttackType.Heavy:
                return heavyHitStun;
            case AttackType.JumpLight:
                return jumpLightHitStun;
            case AttackType.JumpHeavy:
                return jumpHeavyHitStun;
            default:
                return 0f;
        }
    }

    private Vector2 GetKnockback(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Light:
                return lightKnockback;
            case AttackType.Heavy:
                return heavyKnockback;
            case AttackType.JumpLight:
                return jumpLightKnockback;
            case AttackType.JumpHeavy:
                return jumpHeavyKnockback;
            default:
                return Vector2.zero;
        }
    }

    private void UpdateIdleOrMoveState(FighterRuntime runtime, float horizontalInput)
    {
        if (runtime.hitStunTimer > 0f || runtime.attackTimer > 0f)
        {
            return;
        }

        if (runtime.currentHealth <= 0)
        {
            return;
        }

        if (!runtime.isGrounded)
        {
            runtime.state = FighterState.Jump;
            return;
        }

        runtime.state = Mathf.Abs(horizontalInput) > 0.01f ? FighterState.Move : FighterState.Idle;
    }

    private void ClampToGround(FighterRuntime runtime)
    {
        bool foundGround = TryGetGroundY(runtime, out float hitGroundY);

        if (foundGround && runtime.velocity.y <= 0f)
        {
            float bottomY = GetBodyBottom(runtime);

            if (bottomY <= hitGroundY + groundSnapDistance)
            {
                float offsetToCenter = runtime.transform.position.y - bottomY;
                Vector3 position = runtime.transform.position;
                position.y = hitGroundY + offsetToCenter;
                SetRuntimePosition(runtime, position);
                runtime.velocity.y = 0f;
                runtime.isGrounded = true;
                return;
            }
        }

        runtime.isGrounded = false;
    }

    private void ApplyFacing(FighterRuntime runtime)
    {
        Vector3 scale = runtime.baseScale;
        scale.x = Mathf.Abs(scale.x) * runtime.facing;
        runtime.transform.localScale = scale;
    }

    private void SetFighterBlinkTint(FighterRuntime runtime, bool normalColor)
    {
        if (runtime.spriteRenderers == null || runtime.originalSpriteColors == null)
        {
            return;
        }

        for (int i = 0; i < runtime.spriteRenderers.Length; i++)
        {
            if (runtime.spriteRenderers[i] != null)
            {
                runtime.spriteRenderers[i].color = normalColor
                    ? runtime.originalSpriteColors[i]
                    : new Color(0.55f, 0.55f, 0.55f, runtime.originalSpriteColors[i].a);
            }
        }
    }

    private float GetHorizontalInput(FighterInput input)
    {
        float value = 0f;

        if (Input.GetKey(input.moveLeft))
        {
            value -= 1f;
        }

        if (Input.GetKey(input.moveRight))
        {
            value += 1f;
        }

        return Mathf.Clamp(value, -1f, 1f);
    }

    private void ApplyMovement(FighterRuntime runtime)
    {
        Vector2 frameVelocity = new Vector2(runtime.velocity.x, runtime.velocity.y);

        if (runtime.rigidbody != null)
        {
            Vector2 nextPosition = runtime.rigidbody.position + (frameVelocity * Time.fixedDeltaTime);
            nextPosition.x = GetClampedX(runtime, nextPosition.x);
            runtime.rigidbody.MovePosition(nextPosition);
            return;
        }

        Vector3 nextTransformPosition = runtime.transform.position + (Vector3)(frameVelocity * Time.fixedDeltaTime);
        nextTransformPosition.x = GetClampedX(runtime, nextTransformPosition.x);
        runtime.transform.position = nextTransformPosition;
    }

    private void SetRuntimePosition(FighterRuntime runtime, Vector3 position)
    {
        position.x = GetClampedX(runtime, position.x);

        if (runtime.rigidbody != null)
        {
            runtime.rigidbody.position = new Vector2(position.x, position.y);
        }
        else
        {
            runtime.transform.position = position;
        }
    }

    private bool TryGetGroundY(FighterRuntime runtime, out float hitGroundY)
    {
        hitGroundY = groundY;

        if (runtime.bodyCollider == null)
        {
            return runtime.transform.position.y <= groundY + groundSnapDistance;
        }

        Bounds bounds = runtime.bodyCollider.bounds;
        Vector2 castOrigin = bounds.center;
        Vector2 castSize = new Vector2(bounds.size.x, bounds.size.y) - groundCheckShrink;
        castSize.x = Mathf.Max(0.05f, castSize.x);
        castSize.y = Mathf.Max(0.05f, castSize.y);

        RaycastHit2D hit = Physics2D.BoxCast(
            castOrigin,
            castSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayers
        );

        if (hit.collider == null)
        {
            return false;
        }

        hitGroundY = hit.collider.bounds.max.y;
        return true;
    }

    private float GetBodyBottom(FighterRuntime runtime)
    {
        if (runtime.bodyCollider != null)
        {
            return runtime.bodyCollider.bounds.min.y;
        }

        return runtime.transform.position.y;
    }

    private float ResolveGroundY()
    {
        if (ground == null)
        {
            return 0f;
        }

        Renderer groundRenderer = ground.GetComponent<Renderer>();

        if (groundRenderer != null)
        {
            return groundRenderer.bounds.max.y;
        }

        Collider groundCollider = ground.GetComponent<Collider>();

        if (groundCollider != null)
        {
            return groundCollider.bounds.max.y;
        }

        Collider2D groundCollider2D = ground.GetComponent<Collider2D>();

        if (groundCollider2D != null)
        {
            return groundCollider2D.bounds.max.y;
        }

        return ground.position.y;
    }

    private void ResolveStageBounds()
    {
        if (useCameraBoundsForWalls && mainCamera != null && mainCamera.orthographic)
        {
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            leftWallX = mainCamera.transform.position.x - halfWidth + wallInset;
            rightWallX = mainCamera.transform.position.x + halfWidth - wallInset;
            return;
        }

        if (!useGroundBoundsForWalls || ground == null)
        {
            leftWallX = Mathf.Min(manualLeftWallX, manualRightWallX);
            rightWallX = Mathf.Max(manualLeftWallX, manualRightWallX);
            return;
        }

        Bounds bounds = default;
        bool hasBounds = false;

        Renderer groundRenderer = ground.GetComponent<Renderer>();

        if (groundRenderer != null)
        {
            bounds = groundRenderer.bounds;
            hasBounds = true;
        }
        else
        {
            Collider2D groundCollider2D = ground.GetComponent<Collider2D>();

            if (groundCollider2D != null)
            {
                bounds = groundCollider2D.bounds;
                hasBounds = true;
            }
            else
            {
                Collider groundCollider = ground.GetComponent<Collider>();

                if (groundCollider != null)
                {
                    bounds = groundCollider.bounds;
                    hasBounds = true;
                }
            }
        }

        if (!hasBounds)
        {
            leftWallX = Mathf.Min(manualLeftWallX, manualRightWallX);
            rightWallX = Mathf.Max(manualLeftWallX, manualRightWallX);
            return;
        }

        leftWallX = bounds.min.x + wallInset;
        rightWallX = bounds.max.x - wallInset;
    }

    private float GetClampedX(FighterRuntime runtime, float targetX)
    {
        float halfWidth = 0f;

        if (runtime.bodyCollider != null)
        {
            halfWidth = runtime.bodyCollider.bounds.extents.x;
        }

        float minX = leftWallX + halfWidth;
        float maxX = rightWallX - halfWidth;

        if (minX > maxX)
        {
            float center = (leftWallX + rightWallX) * 0.5f;
            return center;
        }

        return Mathf.Clamp(targetX, minX, maxX);
    }

    private void ConfigureCamera()
    {
        ApplyCameraSettings();

        if (mainCamera != null)
        {
            fixedCameraPosition = mainCamera.transform.position;
            cameraBaseY = mainCamera.transform.position.y;
        }
    }

    [ContextMenu("Apply Camera Settings")]
    private void ApplyCameraSettings()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && mainCamera.orthographic)
        {
            mainCamera.orthographicSize = orthographicSize;
        }
    }

    [ContextMenu("Use Current Camera Size")]
    private void UseCurrentCameraSize()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && mainCamera.orthographic)
        {
            orthographicSize = mainCamera.orthographicSize;
        }
    }

    private void SetupHud()
    {
        if (fightHud == null)
        {
            fightHud = FindFirstObjectByType<FightHud>();
        }

        if (fightHud == null)
        {
            GameObject hudObject = new GameObject("FightHud");
            fightHud = hudObject.AddComponent<FightHud>();
        }

        fightHud.Initialize(player1.fighterName, player2.fighterName, maxHealth, roundsToWinMatch);
    }

    private void ApplyDefaultBindings()
    {
        player1.input.moveLeft = KeyCode.A;
        player1.input.moveRight = KeyCode.D;
        player1.input.jump = KeyCode.W;
        player1.input.lightAttack = KeyCode.F;
        player1.input.heavyAttack = KeyCode.G;

        player2.input.moveLeft = KeyCode.LeftArrow;
        player2.input.moveRight = KeyCode.RightArrow;
        player2.input.jump = KeyCode.UpArrow;
        player2.input.lightAttack = KeyCode.Keypad1;
        player2.input.heavyAttack = KeyCode.Keypad2;
    }

    private void UpdateMainCamera()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (!enableCameraFollow)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = orthographicSize;
            }

            mainCamera.transform.position = fixedCameraPosition;
            return;
        }

        if (player1Runtime == null || player2Runtime == null)
        {
            return;
        }

        if (player1Runtime.transform == null || player2Runtime.transform == null)
        {
            return;
        }

        Vector3 center = (player1Runtime.transform.position + player2Runtime.transform.position) * 0.5f;
        float targetY = followJumpHeight ? center.y + cameraOffset.y : cameraBaseY;
        Vector3 targetPosition = new Vector3(center.x + cameraOffset.x, targetY, cameraOffset.z);
        float t = 1f - Mathf.Exp(-cameraSmoothSpeed * Time.deltaTime);

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            t
        );
    }

    private void SnapFightersToGround()
    {
        SnapFighterToGround(player1Runtime);
        SnapFighterToGround(player2Runtime);
    }

    private void SnapFighterToGround(FighterRuntime runtime)
    {
        if (runtime == null || runtime.transform == null)
        {
            return;
        }

        if (!TryGetGroundY(runtime, out float hitGroundY))
        {
            return;
        }

        float bottomY = GetBodyBottom(runtime);
        float offsetToCenter = runtime.transform.position.y - bottomY;
        Vector3 position = runtime.transform.position;
        position.y = hitGroundY + offsetToCenter;
        SetRuntimePosition(runtime, position);
        runtime.velocity = Vector3.zero;
        runtime.isGrounded = true;
    }

    private void UpdateMatchState()
    {
        switch (matchState)
        {
            case MatchState.RoundIntro:
                stateTimer -= Time.deltaTime;

                if (fightHud != null)
                {
                    string introMessage = stateTimer > 0.8f ? $"ROUND {currentRound}" : "FIGHT";
                    fightHud.ShowCenterMessage(introMessage, true);
                }

                if (stateTimer <= 0f)
                {
                    matchState = MatchState.Fighting;
                    roundTimer = roundTime;

                    if (fightHud != null)
                    {
                        fightHud.ShowCenterMessage(string.Empty, false);
                    }
                }
                break;

            case MatchState.Fighting:
                roundTimer = Mathf.Max(0f, roundTimer - Time.deltaTime);

                if (player1Runtime.currentHealth <= 0 || player2Runtime.currentHealth <= 0)
                {
                    FinishRound(GetWinnerByHealthZero());
                    return;
                }

                if (roundTimer <= 0f)
                {
                    FinishRound(GetWinnerByTimeOut());
                }
                break;

            case MatchState.RoundOver:
                stateTimer -= Time.deltaTime;

                if (stateTimer <= 0f && autoRestartRound)
                {
                    if (matchFinished)
                    {
                        ResetMatch();
                    }
                    else
                    {
                        currentRound++;
                        ResetFightersForRound();
                        StartRoundIntro();
                    }
                }
                break;
        }
    }

    private void StartRoundIntro()
    {
        matchState = MatchState.RoundIntro;
        stateTimer = roundIntroDuration;
        roundTimer = roundTime;
        ResetFightersForRound();
    }

    private void ResetFightersForRound()
    {
        ResetSingleFighter(player1Runtime, 1f);
        ResetSingleFighter(player2Runtime, -1f);
        SnapFightersToGround();
    }

    private void ResetSingleFighter(FighterRuntime runtime, float facing)
    {
        if (runtime == null || runtime.transform == null)
        {
            return;
        }

        SetRuntimePosition(runtime, runtime.spawnPosition);
        runtime.velocity = Vector3.zero;
        runtime.horizontalInput = 0f;
        runtime.jumpPressed = false;
        runtime.lightPressed = false;
        runtime.heavyPressed = false;
        runtime.attackTimer = 0f;
        runtime.hitStunTimer = 0f;
        runtime.hitEffectTimer = 0f;
        runtime.invincibilityTimer = 0f;
        runtime.blinkTimer = 0f;
        runtime.blinkVisible = true;
        runtime.currentAttack = AttackType.None;
        runtime.currentHealth = maxHealth;
        runtime.isGrounded = true;
        runtime.facing = facing;
        runtime.state = FighterState.Idle;
        runtime.previousLoggedState = FighterState.Idle;
        SetFighterBlinkTint(runtime, true);
        ApplyFacing(runtime);
    }

    private FighterRuntime GetWinnerByHealthZero()
    {
        if (player1Runtime.currentHealth <= 0 && player2Runtime.currentHealth <= 0)
        {
            return null;
        }

        return player1Runtime.currentHealth > 0 ? player1Runtime : player2Runtime;
    }

    private FighterRuntime GetWinnerByTimeOut()
    {
        if (player1Runtime.currentHealth == player2Runtime.currentHealth)
        {
            return null;
        }

        return player1Runtime.currentHealth > player2Runtime.currentHealth ? player1Runtime : player2Runtime;
    }

    private void FinishRound(FighterRuntime winner)
    {
        matchState = MatchState.RoundOver;
        stateTimer = roundResultDuration;
        matchFinished = false;

        player1Runtime.velocity = Vector3.zero;
        player2Runtime.velocity = Vector3.zero;

        if (winner == null)
        {
            player1Runtime.state = FighterState.Idle;
            player2Runtime.state = FighterState.Idle;

            if (fightHud != null)
            {
                fightHud.ShowCenterMessage("DRAW", true);
            }

            return;
        }

        FighterRuntime loser = winner == player1Runtime ? player2Runtime : player1Runtime;
        winner.state = FighterState.Win;
        loser.state = FighterState.Lose;

        if (winner == player1Runtime)
        {
            player1RoundWins++;
        }
        else
        {
            player2RoundWins++;
        }

        matchFinished = player1RoundWins >= roundsToWinMatch || player2RoundWins >= roundsToWinMatch;

        if (fightHud != null)
        {
            fightHud.ShowCenterMessage(
                matchFinished ? $"{GetRuntimeName(winner)} MATCH WIN" : $"{GetRuntimeName(winner)} ROUND WIN",
                true
            );
        }
    }

    private void RefreshHud()
    {
        if (fightHud == null || player1Runtime == null || player2Runtime == null)
        {
            return;
        }

        fightHud.SetHealth(player1Runtime.currentHealth, player2Runtime.currentHealth);
        fightHud.SetTimer(Mathf.CeilToInt(roundTimer));
        fightHud.SetRound(currentRound);
        fightHud.SetRoundWins(player1RoundWins, player2RoundWins);
    }

    private void ResetMatch()
    {
        player1RoundWins = 0;
        player2RoundWins = 0;
        currentRound = startingRound;
        matchFinished = false;
        StartRoundIntro();
    }

    private string GetRuntimeName(FighterRuntime runtime)
    {
        if (runtime == player1Runtime)
        {
            return player1.fighterName;
        }

        if (runtime == player2Runtime)
        {
            return player2.fighterName;
        }

        return "Player";
    }

    private void LogCurrentSetup()
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log(
            "GameManager ready. " +
            "Player1 controls: A/D move, W jump, F light, G heavy. " +
            "Player2 controls: Left/Right move, Up jump, Keypad1 light, Keypad2 heavy."
        );
    }

    private void LogInputEvents(
        FighterRig rig,
        bool moveLeftPressed,
        bool moveRightPressed,
        bool jumpPressed,
        bool lightPressed,
        bool heavyPressed
    )
    {
        if (!enableDebugLogs || !logInputEvents)
        {
            return;
        }

        if (moveLeftPressed)
        {
            Debug.Log($"{rig.fighterName} input: Move Left");
        }

        if (moveRightPressed)
        {
            Debug.Log($"{rig.fighterName} input: Move Right");
        }

        if (jumpPressed)
        {
            Debug.Log($"{rig.fighterName} input: Jump");
        }

        if (lightPressed)
        {
            Debug.Log($"{rig.fighterName} input: Light Attack");
        }

        if (heavyPressed)
        {
            Debug.Log($"{rig.fighterName} input: Heavy Attack");
        }
    }

    private void LogStateChange(FighterRig rig, FighterRuntime runtime)
    {
        if (!enableDebugLogs || !logStateChanges)
        {
            return;
        }

        if (runtime.previousLoggedState == runtime.state)
        {
            return;
        }

        Debug.Log($"{rig.fighterName} state: {runtime.previousLoggedState} -> {runtime.state}");
        runtime.previousLoggedState = runtime.state;
    }
}
