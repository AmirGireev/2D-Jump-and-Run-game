using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Tasten (im Inspector aenderbar)")]
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode dashKey = KeyCode.CapsLock;
    public KeyCode dashKeyAlt = KeyCode.Q;
    public KeyCode slideKey = KeyCode.S;              // Rutschen / Ducken
    public KeyCode slideKeyAlt = KeyCode.LeftControl; // auf "None" stellen, wenn du sie nicht willst
    public KeyCode slideKeyAlt2 = KeyCode.LeftShift;
    public KeyCode shootStraightKey = KeyCode.Space;  // geradeaus schiessen
    // Linksklick = Richtung Maus schiessen, A/D oder Pfeile = laufen
    // Schuss-Taste nochmal druecken, solange der Ball da ist = zum Ball teleportieren

    [Header("Laufen")]
    public float moveSpeed = 8f;
    public float acceleration = 25f; // Wie schnell er anläuft
    public float deceleration = 25f; // Wie schnell er abbremst

    [Header("Springen")]
    public float jumpForce = 14f;
    public int maxAirJumps = 1;               // 1 = Double Jump
    public float coyoteTime = 0.12f;          // kurz nach der Kante noch springen
    public float jumpBufferTime = 0.12f;      // Sprung kurz vor der Landung merken
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f; // Taste loslassen = kleinerer Sprung

    [Header("Wand")]
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(9f, 14f);
    public float wallJumpLockTime = 0.15f;    // so lange kein Steuern nach Wandsprung
    public float wallCheckDistance = 0.6f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.4f;

    [Header("Slide")]
    public float slideSpeed = 14f;
    public float slideTime = 0.45f;
    public float slideCooldown = 0.3f;
    [Range(0.3f, 0.8f)] public float slideHeightFactor = 0.5f; // so hoch ist der Collider beim Rutschen

    [Header("Schuss")]
    public GameObject projectilePrefab;
    public float shootOffset = 0.6f;          // Abstand vom Spieler, an dem der Schuss startet
    public float shootCooldown = 0.3f;

    [Header("Teleport")]
    public float teleportDelay = 0.1f;        // kurz nach dem Schuss noch kein Teleport (gegen versehentlichen Doppelklick)
    public float teleportRecharge = 1.5f;     // so lange kein neuer Schuss nach einem Teleport

    [Header("Checks")]
    public Transform groundCheck;             // leeres Kind-Objekt an den Fuessen
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;             // Layer "Ground" auswaehlen

    Rigidbody2D rb;
    BoxCollider2D box;
    Vector2 normalSize, normalOffset;         // Collider-Groesse im Stehen
    Projectile activeProjectile;
    Camera cam;
    float facing = 1f;
    float defaultGravity;
    Vector3 respawnPoint;

    bool isGrounded, isTouchingWall, isWallSliding, isDashing, isSliding, canDash = true;
    int airJumpsLeft;
    float coyoteCounter, jumpBufferCounter, wallJumpLockCounter;
    float dashCounter, dashCooldownCounter, shootCounter, lastShotTime;
    float slideCounter, slideCooldownCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        cam = Camera.main;
        normalSize = box.size;
        normalOffset = box.offset;
        defaultGravity = rb.gravityScale;
        respawnPoint = transform.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float x = Input.GetAxisRaw("Horizontal");

        // Timer runterzaehlen
        dashCooldownCounter -= dt;
        shootCounter -= dt;
        slideCooldownCounter -= dt;
        wallJumpLockCounter -= dt;
        if (Input.GetKeyDown(jumpKey)) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= dt;

        // Waehrend des Dashs nichts anderes tun
        if (isDashing)
        {
            dashCounter -= dt;
            if (dashCounter <= 0f) EndDash();
            return;
        }

        // Blickrichtung (beim Rutschen fix)
        if (x != 0f && wallJumpLockCounter <= 0f && !isSliding) SetFacing(Mathf.Sign(x));

        // Checks
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isTouchingWall = Physics2D.Raycast(transform.position, Vector2.right * facing, wallCheckDistance, groundLayer);

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            airJumpsLeft = maxAirJumps;
        }
        else
        {
            coyoteCounter -= dt;
            if (isTouchingWall) airJumpsLeft = maxAirJumps; // an der Wand wieder Air Jump
        }

        if ((isGrounded || isTouchingWall) && dashCooldownCounter <= 0f) canDash = true;

        // Dash (Feststelltaste oder Q)
        if ((Input.GetKeyDown(dashKey) || Input.GetKeyDown(dashKeyAlt)) && canDash && dashCooldownCounter <= 0f && !isSliding)
        {
            StartDash();
            return;
        }

        // Slide
        if (isSliding)
        {
            slideCounter -= dt;

            // Solange Zeit uebrig ist: schnell rutschen. Danach (falls die Decke noch zu niedrig ist): langsam kriechen
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
        else if ((Input.GetKeyDown(slideKey) || Input.GetKeyDown(slideKeyAlt) || Input.GetKeyDown(slideKeyAlt2))
                 && isGrounded && slideCooldownCounter <= 0f)
        {
            StartSlide();
            HandleShooting();
            return;
        }

        // Laufen
        if (wallJumpLockCounter <= 0f)
        {
            float targetSpeed = x * moveSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.deltaTime);
            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
        }

        // Springen
        if (jumpBufferCounter > 0f)
        {
            if (coyoteCounter > 0f) Jump();
            else if (isTouchingWall && !isGrounded) WallJump();
            else if (airJumpsLeft > 0) { airJumpsLeft--; Jump(); }
        }

        // Sprungtaste frueh loslassen = niedrigerer Sprung
        if (Input.GetKeyUp(jumpKey) && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // Wall Slide
        isWallSliding = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && x == facing;
        if (isWallSliding)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));

        // Schuss und Teleport
        HandleShooting();
    }

    // Eine Taste fuer beides: Ist kein Ball unterwegs, wird geschossen.
    // Ist einer unterwegs (oder liegt noch), teleportiert dieselbe Taste dorthin.
    void HandleShooting()
    {
        bool aimPressed = Input.GetMouseButtonDown(0);              // Linksklick: Richtung Maus
        bool straightPressed = Input.GetKeyDown(shootStraightKey);  // Leertaste: geradeaus
        if (!aimPressed && !straightPressed) return;

        if (activeProjectile != null)
        {
            // Beim Rutschen nur teleportieren, wenn genug Platz zum Aufstehen ist
            if (Time.time - lastShotTime >= teleportDelay && (!isSliding || CanStand()))
                TeleportToProjectile();
            return;
        }

        if (shootCounter > 0f) return;

        if (aimPressed) Shoot(GetAimDirection());
        else Shoot(Vector2.right * facing);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }

    void WallJump()
    {
        float dir = -facing;
        rb.linearVelocity = new Vector2(dir * wallJumpForce.x, wallJumpForce.y);
        SetFacing(dir);
        wallJumpLockCounter = wallJumpLockTime;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }

    void StartDash()
    {
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

    void StartSlide()
    {
        isSliding = true;
        slideCounter = slideTime;
        slideCooldownCounter = slideCooldown;

        // Collider flacher machen, die Unterkante bleibt am Boden
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

    // Ist ueber dem Spieler genug Platz, um wieder aufzustehen?
    bool CanStand()
    {
        return !Physics2D.OverlapBox(FullCenter(), FullSize() * 0.9f, 0f, groundLayer);
    }

    Vector2 FullSize()
    {
        return new Vector2(normalSize.x * Mathf.Abs(transform.lossyScale.x),
                           normalSize.y * Mathf.Abs(transform.lossyScale.y));
    }

    Vector2 FullCenter()
    {
        return transform.TransformPoint(normalOffset);
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

    void Shoot(Vector2 dir)
    {
        if (projectilePrefab == null) return;
        shootCounter = shootCooldown;
        lastShotTime = Time.time;

        Vector2 origin = transform.position;

        // Groesse des Balls aus dem Prefab lesen
        float r = 0.15f;
        Projectile prefabProj = projectilePrefab.GetComponent<Projectile>();
        if (prefabProj != null) r = prefabProj.radius;

        // Startpunkt: shootOffset vor dem Spieler. Liegt dort Boden oder Wand (z.B. beim Zielen nach
        // unten), startet der Ball direkt vor der Flaeche und nicht darin.
        Vector2 spawnPos = origin + dir * shootOffset;
        RaycastHit2D hit = Physics2D.CircleCast(origin, r, dir, shootOffset, groundLayer);
        if (hit.collider != null)
            spawnPos = hit.centroid + hit.normal * 0.05f;

        GameObject p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = p.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Init(dir, groundLayer);
            activeProjectile = proj;
        }
    }

    void TeleportToProjectile()
    {
        if (isSliding) EndSlide();

        Vector2 target = activeProjectile.transform.position;
        Vector2 origin = transform.position;
        Vector2 colliderOffset = FullCenter() - origin;
        Vector2 size = FullSize() * 0.95f;

        // Ist am Zielpunkt eine Wand im Weg, den Punkt schrittweise zum Spieler zurueckschieben
        Vector2 toOrigin = origin - target;
        float dist = toOrigin.magnitude;
        Vector2 stepDir = dist > 0.01f ? toOrigin / dist : Vector2.zero;
        Vector2 pos = target;
        float moved = 0f;

        while (Physics2D.OverlapBox(pos + colliderOffset, size, 0f, groundLayer) && moved < dist)
        {
            pos += stepDir * 0.1f;
            moved += 0.1f;
        }

        // Kein freier Platz gefunden: nicht teleportieren
        if (Physics2D.OverlapBox(pos + colliderOffset, size, 0f, groundLayer)) return;

        transform.position = pos;
        rb.linearVelocity = Vector2.zero;

        // Dash ist wieder da, der Double Jump bleibt wie er ist
        canDash = true;
        dashCooldownCounter = 0f;

        // Der Schuss muss erst wieder aufladen
        shootCounter = teleportRecharge;

        Destroy(activeProjectile.gameObject);
        activeProjectile = null;
    }

    void SetFacing(float dir)
    {
        facing = dir;
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
    }

    // Wird von Spikes und Gegnern aufgerufen
    public void Die()
    {
        if (isDashing) EndDash();
        if (isSliding) EndSlide();
        rb.linearVelocity = Vector2.zero;
        transform.position = respawnPoint;
    }

    public void SetCheckpoint(Vector3 pos)
    {
        respawnPoint = pos;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * facing * wallCheckDistance);
    }
}
