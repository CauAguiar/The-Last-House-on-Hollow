using UnityEngine;

public class DoorController : InteractableBase
{
    [Header("Configuração da Porta")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetSpawnID;

    [Header("Modo de Operação")]
    [Tooltip("Se true, a porta só abre quando o jogador interage (menu de contexto). Caso contrário, a porta abre por trigger (OnTriggerEnter2D).")]
    [SerializeField] private bool requireInteraction = false;

    [Header("Tranca (opcional)")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private InventoryItem requiredKey;
    
    [Header("Unlocked Visuals & SFX")]
    [Tooltip("Sprite a ser aplicado ao objeto quando a porta for destrancada (opcional).")]
    [SerializeField] private Sprite unlockedSprite;
    [Tooltip("Nome do som no SoundBank para tocar quando a porta for destrancada.")]
    [SerializeField] private string unlockSfxName;
    [Tooltip("Se >0, toca apenas um trecho (slice) começando em segundos")]
    [SerializeField] private float unlockSfxStart = 0f;
    [SerializeField] private float unlockSfxDuration = 0f;
    [SerializeField] private float unlockSfxVolume = 1f;
    
    [Header("Persistência e Identificação")]
    [SerializeField]
    [Tooltip("ID único desta porta para persistência de estado entre cenas. Use 'Generate Unique ID' no menu de contexto do componente.")]
    private string uniqueId;

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null && GameStateManager.Instance.IsDoorUnlocked(uniqueId))
        {
            isLocked = false;
            // Se a porta já estiver destrancada no estado persistido, aplica o sprite de destrancada
            if (unlockedSprite != null)
            {
                if (spriteRenderer != null)
                    spriteRenderer.sprite = unlockedSprite;
                else
                    Debug.LogWarning("DoorController.Start: spriteRenderer é nulo, não foi possível aplicar unlockedSprite.");
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (requireInteraction && isLocked) return;

        if (other.CompareTag("Player"))
        {
            if (isLocked)
            {
                if (InteractionManager.Instance != null)
                {
                    InteractionManager.Instance.ShowDialogue("A porta está trancada.");
                }
                return;
            }

            PlayerPrefs.SetString("NextSpawnPointID", targetSpawnID);
            PlayerPrefs.Save();

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneLoader.Instance.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("DoorController: sceneToLoad não foi especificada.");
            }
        }
    }

    public override void Interact()
    {
        // Se já estiver destrancada, abre/teleporta sem exibir o painel de interação
        if (!isLocked)
        {
            PlayerPrefs.SetString("NextSpawnPointID", targetSpawnID);
            PlayerPrefs.Save();

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneLoader.Instance.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("DoorController: sceneToLoad não foi especificada.");
            }
            return;
        }

        // Porta está trancada: exibe painel de interação padrão
        base.Interact();
    }

    public override bool CanShowContextMenu()
    {
        // Apenas permite o menu de contexto quando a porta estiver trancada.
        return isLocked;
    }

    public override void OnUseItem(InventoryItem item)
    {
        if (item == null)
        {
            base.OnUseItem(item);
            return;
        }

        if (requiredKey != null && item == requiredKey)
        {
            isLocked = false;

            if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.MarkDoorUnlocked(uniqueId);
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(requiredKey);
            }

            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("A porta foi destrancada.");
            }

            // Toca SFX de destrancamento se configurado
            if (!string.IsNullOrEmpty(unlockSfxName) && AudioManager.Instance != null)
            {
                if (unlockSfxDuration > 0f)
                {
                    AudioManager.Instance.PlaySFXSlice(unlockSfxName, unlockSfxStart, unlockSfxDuration, unlockSfxVolume, AudioManager.Category.SFX);
                }
                else
                {
                    AudioManager.Instance.PlaySFX(unlockSfxName, AudioManager.Category.SFX, unlockSfxVolume);
                }
            }

            // Aplica sprite de destrancada (se configurado)
            if (unlockedSprite != null)
            {
                SetSprite(unlockedSprite);
            }
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    /// <summary>
    /// Aplica um novo sprite ao renderer deste interactable.
    /// Útil para alterar visual quando a porta é destrancada dinamicamente.
    /// </summary>
    public void SetSprite(Sprite newSprite)
    {
        if (newSprite == null)
        {
            Debug.LogWarning("DoorController.SetSprite: novo sprite é nulo.");
            return;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
        }
        else
        {
            Debug.LogWarning("DoorController.SetSprite: spriteRenderer é nulo no objeto " + gameObject.name);
        }
    }
}
