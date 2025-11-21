using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    [Header("Comportamento")]
    [Tooltip("Se true, permite re-pressionar a tecla mesmo que ela esteja no estado 'pressionado'; a tecla ainda irá voltar ao normal após 'pressedDuration'.")]
    [SerializeField] private bool neverLockKey = true;
    
    [Header("Feedback Visual")]
    [SerializeField] private float pressedDuration = 0.2f; // Tempo que a tecla fica pressionada
    [SerializeField] private Color hoverTint = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private float blackKeyPressedHeightIncrease = 10f; // Aumento na altura da tecla preta quando pressionada
    [SerializeField] private float blackKeyPerspectiveHeight = 130f; // Altura dos sprites de perspectiva (esquerda/direita)
    [SerializeField] private float blackKeyPerspectiveWidth = 40f; // Largura dos sprites de perspectiva (esquerda/direita)
    
    private bool isPressed = false;
    private float pressedTimer = 0f;
    private Color originalColor;
    private Coroutine colorCoroutine;
    private bool hasPersistentColor = false;
    private Color persistentColor;
    // Removido: Outline e cor de outline. Agora só usa keyImage.color
    // Overlay para teclas pretas
    private Image overlayImage;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Opacidade do overlay para teclas pretas (0 = transparente, 1 = opaco)")]
    private float overlayAlpha = 0.01f;
    // Guarda o alpha anterior para detectar mudanças em runtime
    private float lastOverlayAlpha = -1f;
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
    // Allow remapping note name at runtime (used if the visual layout is inverted)
    public void SetNoteName(string newNote)
    {
        noteName = newNote;
        // Keep note sound consistent with naming convention used by layout helper
        noteSoundName = $"piano_{newNote}";
    }
    // Expose pressed duration so UI manager can sync overlay flash with key press duration
    public float PressedDuration => pressedDuration;
    // Public accessor so UI manager can check persistence
    public bool HasPersistentColor() => hasPersistentColor;
    
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

        // Outline removido: não é mais usado
        // Se for tecla preta, procura ou cria overlay
        if (keyType == KeyType.Black)
        {
            Transform overlayTransform = transform.Find("Overlay");
            if (overlayTransform == null)
            {
                // Cria overlay se não existir
                GameObject overlayObj = new GameObject("Overlay");
                overlayObj.transform.SetParent(transform, false);
                overlayImage = overlayObj.AddComponent<Image>();
                overlayImage.raycastTarget = false;
                overlayImage.color = Color.clear;
                // Ajusta tamanho e ordem: fixa o overlay no centro com o mesmo tamanho da tecla
                RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
                RectTransform keyRect = GetComponent<RectTransform>();
                if (overlayRect != null && keyRect != null)
                {
                    overlayRect.localScale = Vector3.one;
                    overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                    overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
                    overlayRect.anchoredPosition = Vector2.zero;
                    overlayRect.sizeDelta = keyRect.sizeDelta;
                }
                overlayObj.transform.SetAsLastSibling();
            }
            else
            {
                overlayImage = overlayTransform.GetComponent<Image>();
            }
            if (overlayImage != null)
                overlayImage.enabled = false;
            lastOverlayAlpha = overlayAlpha;
        }
    }
    
    private void Update()
    {
        // Timer para despressionar a tecla automaticamente
        // Use unscaledDeltaTime para garantir que teclas liberem mesmo quando o jogo está em pausa (timeScale = 0)
        if (isPressed)
        {
            pressedTimer -= Time.unscaledDeltaTime;
            if (pressedTimer <= 0f)
            {
                ReleaseKey();
            }
        }

        // Se o overlay estiver visível e o usuário mudou o overlayAlpha no Inspector em runtime,
        // atualizamos a cor alpha para refletir imediatamente a mudança.
        if (keyType == KeyType.Black && overlayImage != null && overlayImage.enabled)
        {
            if (!Mathf.Approximately(overlayAlpha, lastOverlayAlpha))
            {
                Color c = overlayImage.color;
                c.a = overlayAlpha;
                overlayImage.color = c;
                lastOverlayAlpha = overlayAlpha;
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
        // Não aplicar hover tint se:
        // - a tecla está pressionada,
        // - a tecla tem cor persistente (ex: já marcada como correta),
        // - ou se for tecla preta (evitar colorir sprite compartilhado)
        if (isPressed || keyImage == null || hasPersistentColor || keyType == KeyType.Black) return;
        keyImage.color = hoverTint;
    }
    
    /// <summary>
    /// Remove o feedback visual ao sair com o mouse.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Somente restaura a cor para o original se não estivermos no estado 'pressed' e não houver cor persistente
        if (isPressed || keyImage == null || hasPersistentColor) return;
        if (keyType == KeyType.Black) return; // black keys use overlay for color; não tocar no sprite
        keyImage.color = originalColor;
    }
    
    /// <summary>
    /// Pressiona a tecla (chamado ao clicar).
    /// </summary>
    public void PressKey()
    {
        // If the puzzle is already solved in the UI, ignore further input completely
        if (PianoUIManager.Instance != null && PianoUIManager.Instance.IsSolved()) return;
        // Se a tecla estiver bloqueada (isPressed) e NÃO estivermos no modo de re-pressão, ignoramos a nova pressão
        if (!neverLockKey && isPressed) return;
        
        isPressed = true;
        // Sempre usamos 'pressedDuration' — isso garante que a tecla voltará ao estado normal
        // após o tempo configurado. Quando 'neverLockKey' == true, pressionar enquanto já
        // está pressionada apenas reinicia o timer, permitindo re-pressões.
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
        
        // Restaura a cor original, exceto quando existe uma cor persistente marcada
        if (keyImage != null)
        {
            if (hasPersistentColor)
            {
                // Non-black keys receive the persistent color; black keys keep their outline
                if (keyType != KeyType.Black)
                {
                    keyImage.color = persistentColor;
                }
            }
            else
            {
                keyImage.color = originalColor;
            }
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
            if (isPressed || hasPersistentColor)
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
        // Garante que a cor foi restaurada e cancela flashes
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
        // Clear any persistent color state on force release
        hasPersistentColor = false;
        if (keyType == KeyType.Black)
        {
            if (overlayImage != null)
            {
                overlayImage.enabled = false;
                overlayImage.color = new Color(0f, 0f, 0f, 0f); // inicialmente transparente
            }
        }
        else
        {
            if (keyImage != null)
            {
                keyImage.color = originalColor;
            }
        }
        UpdateKeySprite();
    }

    /// <summary>
    /// Muda a cor da tecla por um período e depois restaura a cor original.
    /// Cancela corrotinas de cor anteriores para evitar sobreposição.
    /// </summary>
    public void FlashColor(Color color, float duration)
    {
        // Se já tem cor persistente, não faz flash
        if (hasPersistentColor) return;
        if (keyType == KeyType.Black)
        {
            if (overlayImage != null)
            {
                overlayImage.color = new Color(color.r, color.g, color.b, overlayAlpha);
                overlayImage.enabled = true;
                lastOverlayAlpha = overlayAlpha;
                if (colorCoroutine != null)
                {
                    StopCoroutine(colorCoroutine);
                    colorCoroutine = null;
                }
                colorCoroutine = StartCoroutine(FlashOverlayCoroutine(duration));
            }
        }
        else
        {
            if (keyImage == null) return;
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                colorCoroutine = null;
            }
            colorCoroutine = StartCoroutine(FlashColorCoroutine(color, duration));
        }
        UpdateKeySprite();
    }

    private IEnumerator FlashColorCoroutine(Color color, float duration)
    {
        keyImage.color = color;
        yield return new WaitForSecondsRealtime(duration);
        keyImage.color = originalColor;
        colorCoroutine = null;
    }

    private IEnumerator FlashOverlayCoroutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (!hasPersistentColor && overlayImage != null)
        {
            overlayImage.enabled = false;
            overlayImage.color = Color.clear;
        }
        colorCoroutine = null;
    }

    // Removido: RemoveOutlineAfter. Não usa mais outline.

    /// <summary>
    /// Define uma cor persistente (ex: VERDE para nota correta) até que
    /// a tecla seja resetada. Cancela quaisquer corrotinas de flash.
    /// </summary>
    public void SetPersistentColor(Color color)
    {
        // Cancela qualquer flash que esteja ocorrendo
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
        if (keyType == KeyType.Black)
        {
            if (overlayImage != null)
            {
                overlayImage.color = new Color(color.r, color.g, color.b, overlayAlpha);
                overlayImage.enabled = true;
                lastOverlayAlpha = overlayAlpha;
            }
        }
        else
        {
            if (keyImage == null) return;
            keyImage.color = color;
        }
        hasPersistentColor = true;
        persistentColor = color;
        UpdateKeySprite();
    }
}
