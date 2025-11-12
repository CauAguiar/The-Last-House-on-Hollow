using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Representa uma tecla individual do piano.
/// Gerencia a aparência visual (pressionada/não pressionada) e emite o som correspondente.
/// </summary>
public class PianoKey : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuração da Tecla")]
    [SerializeField] private string noteName; // Ex: "C", "D#", "E", etc.
    [SerializeField] private KeyType keyType = KeyType.White;
    
    [Header("Sprites da Tecla")]
    [SerializeField] private Image keyImage;
    [SerializeField] private Sprite whiteKeyNormal;
    [SerializeField] private Sprite whiteKeyPressed;
    [SerializeField] private Sprite blackKeyNormal;
    [SerializeField] private Sprite blackKeyPressed;
    [SerializeField] private Sprite blackKeyLeft; // Perspectiva esquerda (quando tecla branca à direita é pressionada)
    [SerializeField] private Sprite blackKeyRight; // Perspectiva direita (quando tecla branca à esquerda é pressionada)
    
    [Header("Teclas Adjacentes (para perspectiva)")]
    [SerializeField] private PianoKey leftWhiteKey; // Tecla branca à esquerda
    [SerializeField] private PianoKey rightWhiteKey; // Tecla branca à direita
    [SerializeField] private PianoKey leftBlackKey; // Tecla preta à esquerda (se houver)
    [SerializeField] private PianoKey rightBlackKey; // Tecla preta à direita (se houver)
    
    [Header("Áudio")]
    [SerializeField] private string noteSoundName; // Nome do som no SoundBank
    
    [Header("Feedback Visual")]
    [SerializeField] private float pressedDuration = 0.2f; // Tempo que a tecla fica pressionada
    [SerializeField] private Color hoverTint = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private float blackKeyPressedHeightIncrease = 10f; // Aumento na altura da tecla preta quando pressionada
    [SerializeField] private float blackKeyPerspectiveHeight = 130f; // Altura dos sprites de perspectiva (esquerda/direita)
    [SerializeField] private float blackKeyPerspectiveWidth = 40f; // Largura dos sprites de perspectiva (esquerda/direita)
    
    private bool isPressed = false;
    private float pressedTimer = 0f;
    private Color originalColor;
    private PerspectiveState currentPerspective = PerspectiveState.Normal;
    private Vector2 originalSize;
    
    public enum KeyType
    {
        White,
        Black
    }
    
    public enum PerspectiveState
    {
        Normal,
        Left,
        Right
    }
    
    public string NoteName => noteName;
    public KeyType Type => keyType;
    
    private void Awake()
    {
        if (keyImage == null)
        {
            keyImage = GetComponent<Image>();
        }
        
        if (keyImage != null)
        {
            originalColor = keyImage.color;
        }
        
        // Armazena o tamanho original da tecla
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            originalSize = rect.sizeDelta;
        }
        
        UpdateKeySprite();
    }
    
    private void Update()
    {
        // Timer para despressionar a tecla automaticamente
        if (isPressed)
        {
            pressedTimer -= Time.deltaTime;
            if (pressedTimer <= 0f)
            {
                ReleaseKey();
            }
        }
    }
    
    /// <summary>
    /// Chamado quando a tecla é clicada.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        PressKey();
    }
    
    /// <summary>
    /// Feedback visual ao passar o mouse.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPressed && keyImage != null)
        {
            keyImage.color = hoverTint;
        }
    }
    
    /// <summary>
    /// Remove o feedback visual ao sair com o mouse.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed && keyImage != null)
        {
            keyImage.color = originalColor;
        }
    }
    
    /// <summary>
    /// Pressiona a tecla (chamado ao clicar).
    /// </summary>
    public void PressKey()
    {
        if (isPressed) return;
        
        isPressed = true;
        pressedTimer = pressedDuration;
        
        // Aumenta a altura da tecla preta quando pressionada
        if (keyType == KeyType.Black)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(originalSize.x, originalSize.y + blackKeyPressedHeightIncrease);
            }
        }
        
        // Atualiza o visual
        UpdateKeySprite();
        
        // Toca o som da nota
        PlayNoteSound();
        
        // Notifica o PianoUIManager que uma tecla foi pressionada
        if (PianoUIManager.Instance != null)
        {
            PianoUIManager.Instance.OnKeyPressed(this);
        }
        
        // Se for tecla branca, atualiza perspectiva das teclas pretas adjacentes
        if (keyType == KeyType.White)
        {
            UpdateAdjacentBlackKeysPerspective();
        }
    }
    
    /// <summary>
    /// Solta a tecla (volta ao estado normal).
    /// </summary>
    public void ReleaseKey()
    {
        if (!isPressed) return;
        
        isPressed = false;
        
        // Restaura o tamanho original da tecla preta
        if (keyType == KeyType.Black)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = originalSize;
            }
        }
        
        // Restaura a cor original
        if (keyImage != null)
        {
            keyImage.color = originalColor;
        }
        
        // Atualiza o visual
        UpdateKeySprite();
        
        // Se for tecla branca, restaura perspectiva das teclas pretas adjacentes
        if (keyType == KeyType.White)
        {
            ResetAdjacentBlackKeysPerspective();
        }
    }
    
    /// <summary>
    /// Atualiza a perspectiva das teclas pretas adjacentes quando uma tecla branca é pressionada.
    /// </summary>
    private void UpdateAdjacentBlackKeysPerspective()
    {
        if (leftBlackKey != null)
        {
            leftBlackKey.SetPerspective(PerspectiveState.Right);
        }
        
        if (rightBlackKey != null)
        {
            rightBlackKey.SetPerspective(PerspectiveState.Left);
        }
    }
    
    /// <summary>
    /// Restaura a perspectiva das teclas pretas adjacentes quando uma tecla branca é solta.
    /// </summary>
    private void ResetAdjacentBlackKeysPerspective()
    {
        if (leftBlackKey != null)
        {
            leftBlackKey.SetPerspective(PerspectiveState.Normal);
        }
        
        if (rightBlackKey != null)
        {
            rightBlackKey.SetPerspective(PerspectiveState.Normal);
        }
    }
    
    /// <summary>
    /// Define a perspectiva da tecla preta (usada por teclas brancas adjacentes).
    /// </summary>
    public void SetPerspective(PerspectiveState perspective)
    {
        if (keyType != KeyType.Black) return;
        
        currentPerspective = perspective;
        UpdateKeySprite();
    }
    
    /// <summary>
    /// Atualiza o sprite da tecla baseado no estado atual.
    /// </summary>
    private void UpdateKeySprite()
    {
        if (keyImage == null) return;
        
        RectTransform rect = GetComponent<RectTransform>();
        
        if (keyType == KeyType.White)
        {
            keyImage.sprite = isPressed ? whiteKeyPressed : whiteKeyNormal;
        }
        else // Black key
        {
            if (isPressed)
            {
                keyImage.sprite = blackKeyPressed;
            }
            else
            {
                switch (currentPerspective)
                {
                    case PerspectiveState.Left:
                        keyImage.sprite = blackKeyLeft;
                        // Ajusta largura e altura para o sprite de perspectiva
                        if (rect != null)
                        {
                            rect.sizeDelta = new Vector2(blackKeyPerspectiveWidth, blackKeyPerspectiveHeight);
                        }
                        break;
                    case PerspectiveState.Right:
                        keyImage.sprite = blackKeyRight;
                        // Ajusta largura e altura para o sprite de perspectiva
                        if (rect != null)
                        {
                            rect.sizeDelta = new Vector2(blackKeyPerspectiveWidth, blackKeyPerspectiveHeight);
                        }
                        break;
                    default:
                        keyImage.sprite = blackKeyNormal;
                        // Restaura tamanho original completo (largura + altura)
                        if (rect != null)
                        {
                            rect.sizeDelta = new Vector2(originalSize.x, originalSize.y);
                        }
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Toca o som da nota musical.
    /// </summary>
    private void PlayNoteSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(noteSoundName))
        {
            AudioManager.Instance.PlaySFX(noteSoundName);
        }
    }
    
    /// <summary>
    /// Força a tecla a soltar (usado para reset).
    /// </summary>
    public void ForceRelease()
    {
        ReleaseKey();
        currentPerspective = PerspectiveState.Normal;
        UpdateKeySprite();
    }
}
