using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))] // Garante que o Animator está presente
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Velocidade de Movimento do Personagem")]
    private float speed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer; // Para virar o personagem
    private PlayerControls controls;
    private Vector2 moveInput;

    void Awake()
    {
        // Pega as referências dos componentes
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configura o Input System
        controls = new PlayerControls();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }

    // Usamos Update para lógica que não envolve física, como animação e input
    void Update()
    {
        UpdateAnimationParameters();
    }

    // FixedUpdate é o ideal para manipular o Rigidbody
    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }

    private void UpdateAnimationParameters()
    {
        if (moveInput.sqrMagnitude > 0.1f) // Se o personagem está se movendo
        {
            animator.SetBool("isMoving", true);

            // Envia a direção atual para o Blend Tree de caminhada
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", moveInput.y);

            // Guarda a última direção para o Blend Tree de idle
            animator.SetFloat("lastHorizontal", moveInput.x);
            animator.SetFloat("lastVertical", moveInput.y);
        }
        else // Se o personagem está parado
        {
            animator.SetBool("isMoving", false);
        }

        // Vira o sprite para a esquerda ou direita
        if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true; // Vira para a esquerda
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false; // Vira para a direita (padrão)
        }
    }
}

