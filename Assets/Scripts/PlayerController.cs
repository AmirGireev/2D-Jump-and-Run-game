using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    public static System.Action OnPlayerDied;

    // Komponenten
    Rigidbody2D rb;
    BoxCollider2D box;
    Animator anim;
    SpriteRenderer sr;
    AudioSource sfx;
    Camera cam;

    // Allgemein
    float facing = 1f;
    float defaultGravity;
    Vector3 respawnPoint;

    // ---------- Tasten ----------

    [Header("Tasten (im Inspector aenderbar)")]
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode dashKey = KeyCode.CapsLock;
    public KeyCode dashKeyAlt = KeyCode.Q;
    public KeyCode slideKey = KeyCode.S;
    public KeyCode slideKeyAlt = KeyCode.LeftControl;
    public KeyCode slideKeyAlt2 = KeyCode.LeftShift;
    public KeyCode shootStraightKey = KeyCode.Space;
    public KeyCode teleportKey = KeyCode.E;

    [Header("Controller (PS4-Belegung, im Inspector aenderbar)")]
    public KeyCode padJumpButton = KeyCode.JoystickButton1;      // Kreuz
    public KeyCode padDashButton = KeyCode.JoystickButton2;      // Kreis
    public KeyCode padDashAltButton = KeyCode.JoystickButton4;   // L1
    public KeyCode padSlideButton = KeyCode.JoystickButton3;     // Dreieck
    public KeyCode padShootButton = KeyCode.JoystickButton0;     // Quadrat
    public KeyCode padShootAltButton = KeyCode.JoystickButton5;  // R1
    public KeyCode padTeleportButton = KeyCode.JoystickButton7;  // R2
    [Range(0.1f, 0.9f)] public float stickDeadzone = 0.35f;
    public string aimAxisX = "RightStickX";
    public string aimAxisY = "RightStickY";
    bool rightStickAvailable;

    // ---------- Bewegung ----------

    [Header("Laufen")]
    public float moveSpeed = 8f;
    public float acceleration = 25f;
    public float deceleration = 25f;

    [Header("Springen")]
    public float jumpForce = 14f;
    public int maxAirJumps = 1;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
    int airJumpsLeft;
    float coyoteCounter, jumpBufferCounter;

    [Header("Wand")]
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(9f, 14f);
    public float wallJumpLockTime = 0.15f;
    public float wallCheckDistance = 0.6f;
    public float wallStickTime = 0.15f;
    bool isTouchingWall, isWallSliding;
    float wallStickCounter, wallJumpLockCounter;

    [Header("Turnaround")]
    public float turnaroundTime = 0.2f;
    private float turnaroundCounter;
    private bool isTurningAround;

    // ---------- Aktionen ----------

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.4f;
    public float doubleTapTime = 0.10f;
    public Image dashBar;
    private float lastPressA = -10f;
    private float lastPressD = -10f;
    bool isDashing;
    bool canDash = true;
    float dashCounter, dashCooldownCounter;

    [Header("Dash-Nachbilder")]
    public bool dashAfterimages = true;
    public float afterimageInterval = 0.03f;
    public float afterimageFadeTime = 0.25f;
    public Color afterimageColor = new Color(0.6f, 0.85f, 1f, 0.6f);
    float afterimageTimer;

    [Header("Slide")]
    public float slideSpeed = 14f;
    public float slideTime = 0.45f;
    public float slideCooldown = 0.3f;
    [Range(0.3f, 0.8f)] public float slideHeightFactor = 0.5f;
    bool isSliding;
    float slideCounter, slideCooldownCounter;
    Vector2 normalSize, normalOffset;

    [Header("Schuss")]
    public GameObject projectilePrefab;
    public float shootOffset = 0.6f;
    public float shootCooldown = 0.3f;
    public LayerMask projectileLayer;
    public Image shootBar;
    Projectile activeProjectile;
    float shootCounter, lastShotTime;

    [Header("Teleport")]
    public float teleportDelay = 0.1f;
    public float teleportRecharge = 1.5f;

    // ---------- Boden ----------

    [Header("Checks")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    public LayerMask groundLayer;
    bool isGrounded;
    float airVelY;

    // ---------- Sound ----------

    [Header("Sounds und Effekte")]
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip slideSound;
    public AudioClip landSound;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public float landSoundMinFallSpeed = 3f;

    // ---------- Unity ----------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        box = GetComponent<BoxCollider2D>();
        cam = Camera.main;
        normalSize = box.size;
        normalOffset = box.offset;
        defaultGravity = rb.gravityScale;
        respawnPoint = transform.position;
        rightStickAvailable = AxisExists(aimAxisX) && AxisExists(aimAxisY);

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // Pause
        if (Time.timeScale == 0f) return;

        float dt = Time.deltaTime;
        float x = ReadMoveInput();

        // Boden check
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // Landeton
        if (!isGrounded) airVelY = rb.linearVelocity.y;
        else if (!wasGrounded)
        {
            if (airVelY < -landSoundMinFallSpeed) PlaySfx(landSound);
            airVelY = 0f;
        }

        // Wand check
        isTouchingWall = Physics2D.Raycast(transform.position, Vector2.right * facing, wallCheckDistance, groundLayer);

        // Timer
        dashCooldownCounter -= dt;
        shootCounter -= dt;
        slideCooldownCounter -= dt;
        wallJumpLockCounter -= dt;
        if (JumpDown())
        {
            jumpBufferCounter = jumpBufferTime;
            isTurningAround = false;
        }
        else jumpBufferCounter -= dt;

        // Dash aktiv
        if (isDashing)
        {
            dashCounter -= dt;

            // Nachbilder
            if (dashAfterimages)
            {
                afterimageTimer -= dt;
                if (afterimageTimer <= 0f)
                {
                    SpawnAfterimage();
                    afterimageTimer = afterimageInterval;
                }
            }

            if (dashCounter <= 0f) EndDash();
            return;
        }

        // Turnaround
        if (isTurningAround)
        {
            turnaroundCounter -= dt;

            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, deceleration * dt), rb.linearVelocity.y);

            if (turnaroundCounter <= 0f)
            {
                isTurningAround = false;

                if (x != 0f) SetFacing(x);
                else SetFacing(-facing);
            }
        }

        // Blickrichtung
        if (!isTurningAround && x != 0f && wallJumpLockCounter <= 0f && !isSliding && !isDashing)
        {
            bool tryingToLeaveWall = isTouchingWall && (x != facing);

            if (tryingToLeaveWall)
            {
                wallStickCounter -= dt;
                if (wallStickCounter <= 0f)
                {
                    SetFacing(x);
                }
            }
            else
            {
                wallStickCounter = wallStickTime;

                if (x != facing && isGrounded && Mathf.Abs(rb.linearVelocity.x) > 2f)
                {
                    StartTurnaround();
                }
                else
                {
                    SetFacing(x);
                }
            }
        }
        else
        {
            wallStickCounter = wallStickTime;
        }

        // Coyote Airjump
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            airJumpsLeft = maxAirJumps;
        }
        else
        {
            coyoteCounter -= dt;
            if (isTouchingWall) airJumpsLeft = maxAirJumps;
        }

        if ((isGrounded || isTouchingWall) && dashCooldownCounter <= 0f) canDash = true;

        // Dash start
        if (canDash && dashCooldownCounter <= 0f && !isSliding)
        {
            // Dash Tasten
            if (Input.GetKeyDown(dashKey) || Input.GetKeyDown(dashKeyAlt)
                || Input.GetKeyDown(padDashButton) || Input.GetKeyDown(padDashAltButton))
            {
                StartDash();
                return;
            }

            // Doppeltipp D
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Time.time - lastPressD <= doubleTapTime)
                {
                    StartDash();
                    return;
                }
                lastPressD = Time.time;
            }

            // Doppeltipp A
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Time.time - lastPressA <= doubleTapTime)
                {
                    StartDash();
                    return;
                }
                lastPressA = Time.time;
            }
        }

        // Slide
        if (isSliding)
        {
            slideCounter -= dt;

            float vx = slideCounter > 0f ? facing * slideSpeed : x * moveSpeed * 0.5f;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

            bool wantsOut = slideCounter <= 0f || jumpBufferCounter > 0f || !isGrounded;
            if (wantsOut && CanStand()) EndSlide();

            if (isSliding)
            {
                HandleShooting();
                return;
            }
        }
        else if ((Input.GetKeyDown(slideKey) || Input.GetKeyDown(slideKeyAlt) || Input.GetKeyDown(slideKeyAlt2)
                  || Input.GetKeyDown(padSlideButton))
                 && isGrounded && slideCooldownCounter <= 0f)
        {
            StartSlide();
            HandleShooting();
            return;
        }

        // Laufen
        if (!isTurningAround && wallJumpLockCounter <= 0f)
        {
            float targetSpeed = x * moveSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * dt);
            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
        }

        // Springen
        if (jumpBufferCounter > 0f)
        {
            if (coyoteCounter > 0f) Jump();
            else if (isTouchingWall && !isGrounded) WallJump();
            else if (airJumpsLeft > 0) { airJumpsLeft--; Jump(); }
        }

        // Jump cut
        if (JumpUp() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // Wall slide
        isWallSliding = isTouchingWall && rb.linearVelocity.y <= -wallSlideSpeed;
        if (isWallSliding)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));

        // Schuss
        HandleShooting();

        // UI Balken
        if (dashBar != null)
            dashBar.fillAmount = 1f - Mathf.Clamp01(dashCooldownCounter / dashCooldown);

        if (shootBar != null)
            shootBar.fillAmount = 1f - Mathf.Clamp01(shootCounter / shootCooldown);
    }

    void LateUpdate()
    {
        // Animator
        if (anim != null)
        {
            anim.SetBool("isTurningAround", isTurningAround);
            anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
            anim.SetFloat("vSpeed", rb.linearVelocity.y);
            anim.SetBool("isGrounded", isGrounded);
            anim.SetBool("isSliding", isSliding);
            anim.SetBool("isWallSliding", isWallSliding);
        }
    }

    // ---------- Input ----------

    static bool AxisExists(string axisName)
    {
        if (string.IsNullOrEmpty(axisName)) return false;
        try { Input.GetAxisRaw(axisName); return true; }
        catch (System.ArgumentException) { return false; }
    }

    float ReadMoveInput()
    {
        float raw = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(raw) < stickDeadzone) return 0f;
        return Mathf.Sign(raw);
    }

    bool JumpDown() { return Input.GetKeyDown(jumpKey) || Input.GetKeyDown(padJumpButton); }
    bool JumpUp() { return Input.GetKeyUp(jumpKey) || Input.GetKeyUp(padJumpButton); }

    // ---------- Springen ----------

    void Jump()
    {
        PlaySfx(jumpSound);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }

    void WallJump()
    {
        PlaySfx(jumpSound);
        float dir = -facing;
        rb.linearVelocity = new Vector2(dir * wallJumpForce.x, wallJumpForce.y);
        airJumpsLeft = maxAirJumps;
        SetFacing(dir);
        wallJumpLockCounter = wallJumpLockTime;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }

    // ---------- Blickrichtung ----------

    void SetFacing(float dir)
    {
        facing = dir;
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
    }

    void StartTurnaround()
    {
        isTurningAround = true;
        turnaroundCounter = turnaroundTime;
    }

    // ---------- Dash ----------

    void StartDash()
    {
        PlaySfx(dashSound);
        afterimageTimer = 0f;
        isDashing = true;
        canDash = false;
        dashCounter = dashTime;
        dashCooldownCounter = dashCooldown;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = new Vector2(facing * moveSpeed, 0f);
    }

    void SpawnAfterimage()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject g = new GameObject("Afterimage");
        g.transform.position = sr.transform.position;
        g.transform.rotation = sr.transform.rotation;
        g.transform.localScale = sr.transform.lossyScale;

        SpriteRenderer gr = g.AddComponent<SpriteRenderer>();
        gr.sprite = sr.sprite;
        gr.flipX = sr.flipX;
        gr.flipY = sr.flipY;
        gr.sharedMaterial = sr.sharedMaterial;
        gr.sortingLayerID = sr.sortingLayerID;
        gr.sortingOrder = sr.sortingOrder - 1;
        gr.color = afterimageColor;

        StartCoroutine(FadeAfterimage(gr));
    }

    System.Collections.IEnumerator FadeAfterimage(SpriteRenderer gr)
    {
        Color start = afterimageColor;
        float t = 0f;
        while (t < afterimageFadeTime && gr != null)
        {
            t += Time.deltaTime;
            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t / afterimageFadeTime);
            gr.color = c;
            yield return null;
        }
        if (gr != null) Destroy(gr.gameObject);
    }

    // ---------- Slide ----------

    void StartSlide()
    {
        PlaySfx(slideSound);
        isSliding = true;
        slideCounter = slideTime;
        slideCooldownCounter = slideCooldown;

        float newHeight = normalSize.y * slideHeightFactor;
        box.size = new Vector2(normalSize.x, newHeight);
        box.offset = new Vector2(normalOffset.x, normalOffset.y - (normalSize.y - newHeight) / 2f);

        rb.linearVelocity = new Vector2(facing * slideSpeed, rb.linearVelocity.y);
    }

    void EndSlide()
    {
        isSliding = false;
        box.size = normalSize;
        box.offset = normalOffset;
    }

    bool CanStand()
    {
        Vector2 full = FullSize();
        float feetY = box.bounds.min.y;

        float skinWidth = 0.05f;
        float fullHeight = full.y - skinWidth;
        float width = full.x * 0.9f;

        Vector2 checkCenter = new Vector2(box.bounds.center.x, feetY + skinWidth + (fullHeight / 2f));
        Vector2 checkSize = new Vector2(width, fullHeight);

        return !Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundLayer);
    }

    Vector2 FullSize()
    {
        return new Vector2(normalSize.x * Mathf.Abs(transform.lossyScale.x),
                           normalSize.y * Mathf.Abs(transform.lossyScale.y));
    }

    // ---------- Schuss und Teleport ----------

    void HandleShooting()
    {
        bool aimPressed = Input.GetMouseButtonDown(0);
        bool straightPressed = Input.GetKeyDown(shootStraightKey);
        bool padPressed = Input.GetKeyDown(padShootButton) || Input.GetKeyDown(padShootAltButton);
        bool tpPressed = Input.GetKeyDown(teleportKey) || Input.GetKeyDown(padTeleportButton);
        bool shootPressed = aimPressed || straightPressed || padPressed;
        if (!shootPressed && !tpPressed) return;

        // Teleport
        if (activeProjectile != null)
        {
            if (Time.time - lastShotTime >= teleportDelay && (!isSliding || CanStand()))
                TeleportToProjectile();
            return;
        }

        if (!shootPressed) return;

        if (shootCounter > 0f) return;

        if (aimPressed) Shoot(GetAimDirection());
        else if (padPressed) Shoot(GetPadAimDirection());
        else Shoot(Vector2.right * facing);
    }

    Vector2 GetAimDirection()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector2.right * facing;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.01f) return Vector2.right * facing;
        return dir.normalized;
    }

    Vector2 GetPadAimDirection()
    {
        // Rechter Stick
        if (rightStickAvailable)
        {
            Vector2 r = new Vector2(Input.GetAxisRaw(aimAxisX), Input.GetAxisRaw(aimAxisY));
            if (r.magnitude > 0.4f) return r.normalized;
        }

        // Linker Stick
        Vector2 l = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (l.magnitude > 0.5f) return l.normalized;

        return Vector2.right * facing;
    }

    void Shoot(Vector2 dir)
    {
        if (projectilePrefab == null) return;
        shootCounter = shootCooldown;
        lastShotTime = Time.time;

        Vector2 origin = transform.position;

        // Ball Radius
        float r = 0.15f;
        Projectile prefabProj = projectilePrefab.GetComponent<Projectile>();
        if (prefabProj != null) r = prefabProj.radius;

        // Startpunkt
        Vector2 spawnPos = origin + dir * shootOffset;
        RaycastHit2D hit = Physics2D.CircleCast(origin, r, dir, shootOffset, groundLayer);
        if (hit.collider != null)
            spawnPos = hit.centroid + hit.normal * 0.05f;

        GameObject p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = p.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Init(dir, projectileLayer);
            activeProjectile = proj;
        }
        if (anim != null)
        {
            anim.SetTrigger("Shoot");
        }
    }

    void TeleportToProjectile()
    {
        if (isSliding) EndSlide();

        // Position
        transform.position = activeProjectile.transform.position;

        // Stopp
        rb.linearVelocity = Vector2.zero;

        // Cooldowns
        canDash = true;
        dashCooldownCounter = 0f;
        shootCounter = teleportRecharge;

        // Ball weg
        Destroy(activeProjectile.gameObject);
        activeProjectile = null;
    }

    // ---------- Respawn ----------

    public void Die()
    {
        if (isDashing) EndDash();
        if (isSliding) EndSlide();
        rb.linearVelocity = Vector2.zero;

        transform.position = respawnPoint;
        OnPlayerDied?.Invoke();
    }

    public void SetCheckpoint(Vector3 pos)
    {
        respawnPoint = pos;
    }

    // ---------- Sound ----------

    void PlaySfx(AudioClip c)
    {
        if (c != null && sfx != null) sfx.PlayOneShot(c, sfxVolume);
    }

    // ---------- Gizmos ----------

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * facing * wallCheckDistance);
    }
}
