using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Som de Passos")]
    public AudioClip passosClip;

    [Tooltip("Tempo entre cada som de passo enquanto o jogador se move")]
    public float footstepInterval = 0.4f;
    private float footstepTimer;

    [Header("Movement Speeds")]
    [SerializeField]
    [Tooltip("Velocidade de Movimento do Personagem")]
    private float walkSpeed = 5f;

    [SerializeField]
    [Tooltip("Velocidade de Corrida do Personagem")]
    private float sprintSpeed = 8f;

    [Header("Collision")]
    [SerializeField]
    [Tooltip("Distância extra para a detecção de colisão, ajuda a evitar ficar preso.")]
    private float collisionOffset = 0.02f;

    [SerializeField]
    [Tooltip("Filtro para especificar com quais camadas o jogador deve colidir.")]
    private ContactFilter2D movementFilter;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerControls controls;
    private Vector2 moveInput;
    private bool isSprinting = false;

    public static PlayerMovement Instance;

    // Lista para armazenar os resultados das detecções de colisão
    private List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controls = new PlayerControls();

        // Assinatura dos eventos de movimento
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Assinatura dos eventos de corrida (Sprint)
        controls.Player.Sprint.performed += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }

    void Update()
    {
        UpdateAnimationAndSpriteFlip();
        HandleFootsteps();
    }

    void FixedUpdate()
    {
        if (moveInput == Vector2.zero)
        {
            return;
        }

        bool success = TryMove(moveInput);

        // Se o movimento diagonal falhar, tenta mover nos eixos individuais para deslizar nas paredes
        if (!success)
        {
            success = TryMove(new Vector2(moveInput.x, 0));
            if (!success)
            {
                TryMove(new Vector2(0, moveInput.y));
            }
        }
    }

    /// <summary>
    /// Tenta mover o personagem na direção do input, verificando colisões antes.
    /// </summary>
    private bool TryMove(Vector2 direction)
    {
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        int count = rb.Cast(
            direction,
            movementFilter,
            castCollisions,
            currentSpeed * Time.fixedDeltaTime + collisionOffset
        );

        if (count == 0)
        {
            rb.MovePosition(rb.position + direction * currentSpeed * Time.fixedDeltaTime);
            return true;
        }

        return false;
    }

    private void UpdateAnimationAndSpriteFlip()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            Vector2 direction = moveInput.normalized;
            animator.SetFloat("Horizontal", direction.x);
            animator.SetFloat("Vertical", direction.y);
            animator.SetFloat("lastHorizontal", direction.x);
            animator.SetFloat("lastVertical", direction.y);
        }

        if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        if (!isMoving)
        {
            AudioManager.Instance.sfxSource.Stop();
        }
    }

    private void HandleFootsteps()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f && !AudioManager.Instance.sfxSource.isPlaying)
            {
                footstepTimer = footstepInterval;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("PassoMadeira");
                }
            }
        }
        else
        {

            footstepTimer = 0f;

            if (AudioManager.Instance != null && AudioManager.Instance.sfxSource.isPlaying)
            {
                AudioManager.Instance.sfxSource.Stop();
            }
        }
    }
}

