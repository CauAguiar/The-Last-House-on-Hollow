using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper para criar automaticamente um layout de teclas de piano.
/// Facilita a criação rápida do puzzle de piano.
/// </summary>
public class PianoLayoutHelper : MonoBehaviour
{
    [Header("Configuração do Layout")]
    [SerializeField] private int numberOfWhiteKeys = 24; // Número total de teclas brancas (24 = ~3.5 oitavas)
    [SerializeField] private bool includeBlackKeys = true;
    [SerializeField] private int startingOctave = 2; // Oitava inicial (C2, C3, C4, etc.)
    [SerializeField]
    [Tooltip("Se true, o layout é gerado visualmente da direita para a esquerda (invertendo a âncora). O mapeamento de notas permanece correto.")]
    private bool layoutRightToLeft = false;
    
    [Header("Prefabs das Teclas")]
    [SerializeField] private GameObject whiteKeyPrefab;
    [SerializeField] private GameObject blackKeyPrefab;
    
    [Header("Sprites")]
    [SerializeField] private Sprite whiteKeyNormal;
    [SerializeField] private Sprite whiteKeyPressed;
    [SerializeField] private Sprite blackKeyNormal;
    [SerializeField] private Sprite blackKeyPressed;
    [SerializeField] private Sprite blackKeyLeft;
    [SerializeField] private Sprite blackKeyRight;
    
    [Header("Dimensões")]
    [SerializeField] private float whiteKeyWidth = 60f;
    [SerializeField] private float whiteKeyHeight = 260f;
    [SerializeField] private float whiteKeySpacing = 0f; // Espaçamento entre teclas brancas
    [SerializeField] private float blackKeyWidth = 40f;
    [SerializeField] private float blackKeyHeight = 130f;
    [SerializeField] private float blackKeyOffsetX = 0f; // Offset horizontal das teclas pretas
    [SerializeField] private float blackKeyOffsetY = 30f; // Offset vertical das teclas pretas
    [SerializeField] private float blackKeyPressedHeightIncrease = 10f; // Aumento na altura quando tecla preta é pressionada
    [SerializeField] private float blackKeyPerspectiveHeight = 130f; // Altura dos sprites de perspectiva (esquerda/direita)
    [SerializeField] private float blackKeyPerspectiveWidth = 40f; // Largura dos sprites de perspectiva (esquerda/direita)
    
    [Header("Container")]
    [SerializeField] private Transform keysContainer;
    
    private static readonly string[] NOTE_NAMES = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly bool[] IS_BLACK_KEY = { false, true, false, true, false, false, true, false, true, false, true, false };
    
    private List<PianoKey> createdKeys = new List<PianoKey>();
    
#if UNITY_EDITOR
    /// <summary>
    /// Cria o layout de teclas automaticamente (apenas no Editor).
    /// </summary>
    [ContextMenu("Gerar Layout de Teclas")]
    public void GenerateKeyLayout()
    {
        if (keysContainer == null)
        {
            Debug.LogError("Keys Container não definido!");
            return;
        }
        
        // Limpa teclas anteriores
        ClearKeys();
        
        List<PianoKey> whiteKeys = new List<PianoKey>();
        List<PianoKey> blackKeys = new List<PianoKey>();
        
        // Piano real: C, D, E, F, G, A, B (7 teclas brancas por oitava)
        // Padrão de notas de um piano: C, C#, D, D#, E, F, F#, G, G#, A, A#, B
        int currentOctave = startingOctave;
        int noteInOctave = 0; // Índice da nota dentro da oitava (0-11)
        int whiteKeysCreated = 0;
        
        // Primeira passagem: gera todas as notas em ordem normal para contar
        List<(string noteName, bool isBlack, int octave)> notesToCreate = new List<(string, bool, int)>();
        
        while (whiteKeysCreated < numberOfWhiteKeys)
        {
            bool isBlack = IS_BLACK_KEY[noteInOctave];
            
            // Se for preta e não queremos incluir, pula
            if (isBlack && !includeBlackKeys)
            {
                noteInOctave++;
                if (noteInOctave >= 12)
                {
                    noteInOctave = 0;
                    currentOctave++;
                }
                continue;
            }
            
            string noteName = NOTE_NAMES[noteInOctave] + currentOctave.ToString();
            notesToCreate.Add((noteName, isBlack, currentOctave));
            
            if (!isBlack)
            {
                whiteKeysCreated++;
            }
            
            // Avança para a próxima nota
            noteInOctave++;
            if (noteInOctave >= 12)
            {
                noteInOctave = 0;
                currentOctave++;
            }
        }
        
        // Segunda passagem: cria as teclas da ESQUERDA para DIREITA (ordem natural)
        int currentWhiteIndex = 0;
        for (int i = 0; i < notesToCreate.Count; i++)
        {
            var noteData = notesToCreate[i];
            int whiteKeyIndex = currentWhiteIndex;

            PianoKey key = CreateKey(noteData.noteName, noteData.isBlack, whiteKeyIndex, noteData.octave);

            if (key != null)
            {
                createdKeys.Add(key);

                if (noteData.isBlack)
                {
                    blackKeys.Add(key);
                }
                else
                {
                    whiteKeys.Add(key);
                    currentWhiteIndex++;
                }
            }
        }
        
        // Conecta teclas adjacentes
        // Reorder sibling indices so white keys are left-to-right increasing pitch
        // (some setups may end up with reversed creation order). This preserves
        // the intended pitch mapping while keeping black keys rendered above.
        whiteKeys.Sort((a, b) => a.GetComponent<RectTransform>().anchoredPosition.x.CompareTo(
            b.GetComponent<RectTransform>().anchoredPosition.x));

        for (int i = 0; i < whiteKeys.Count; i++)
        {
            whiteKeys[i].transform.SetSiblingIndex(i);
        }

        // Put black keys on top but maintain left-to-right order
        blackKeys.Sort((a, b) => a.GetComponent<RectTransform>().anchoredPosition.x.CompareTo(
            b.GetComponent<RectTransform>().anchoredPosition.x));

        for (int i = 0; i < blackKeys.Count; i++)
        {
            blackKeys[i].transform.SetSiblingIndex(whiteKeys.Count + i);
        }

        ConnectAdjacentKeys(whiteKeys, blackKeys);
        
        // IMPORTANTE: Garante que teclas pretas fiquem na frente das brancas
        foreach (PianoKey blackKey in blackKeys)
        {
            blackKey.transform.SetAsLastSibling();
        }
        
        // Layout de piano criado (log removed)
    }
    
    /// <summary>
    /// Cria uma tecla individual.
    /// </summary>
    private PianoKey CreateKey(string noteName, bool isBlack, int whiteKeyIndex, int octave)
    {
        GameObject prefab = isBlack ? blackKeyPrefab : whiteKeyPrefab;
        
        // Cria GameObject
        GameObject keyObj = new GameObject($"Key_{noteName}");
        keyObj.transform.SetParent(keysContainer);
        keyObj.transform.localScale = Vector3.one;
        
        // Adiciona Image component
        Image image = keyObj.AddComponent<Image>();
        image.sprite = isBlack ? blackKeyNormal : whiteKeyNormal;
        image.raycastTarget = true; // Importante para detectar cliques
        
        // Configura RectTransform (ancora sempre no topo-esquerdo para manter o mapeamento)
        RectTransform rect = keyObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); // Ancora no topo esquerdo
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        
        if (isBlack)
        {
            rect.sizeDelta = new Vector2(blackKeyWidth, blackKeyHeight);
            
            // Posiciona entre as teclas brancas (considera espaçamento e offset X)
            float baseX = whiteKeyIndex * (whiteKeyWidth + whiteKeySpacing);
            // If layout is right-to-left, compute the reversed position relative to total span
            if (layoutRightToLeft)
            {
                float totalSpan = (numberOfWhiteKeys - 1) * (whiteKeyWidth + whiteKeySpacing);
                baseX = totalSpan - baseX;
            }
            float xPos = baseX + (whiteKeyWidth / 2f) + blackKeyOffsetX;
            rect.anchoredPosition = new Vector2(xPos, -blackKeyOffsetY);
            
            // Tecla preta criada (log removed)
        }
        else
        {
            rect.sizeDelta = new Vector2(whiteKeyWidth, whiteKeyHeight);
            float baseXWhite = whiteKeyIndex * (whiteKeyWidth + whiteKeySpacing);
            if (layoutRightToLeft)
            {
                float totalSpanWhite = (numberOfWhiteKeys - 1) * (whiteKeyWidth + whiteKeySpacing);
                baseXWhite = totalSpanWhite - baseXWhite;
            }
            rect.anchoredPosition = new Vector2(baseXWhite, 0);
        }
        
        // Adiciona componente PianoKey
        PianoKey pianoKey = keyObj.AddComponent<PianoKey>();
        
        // Configura via Reflection (não ideal, mas funciona no Editor)
        SerializedObject so = new SerializedObject(pianoKey);
        so.FindProperty("noteName").stringValue = noteName;
        so.FindProperty("keyType").enumValueIndex = isBlack ? 1 : 0;
        so.FindProperty("keyImage").objectReferenceValue = image;
        
        // Configura sprites
        so.FindProperty("whiteKeyNormal").objectReferenceValue = whiteKeyNormal;
        so.FindProperty("whiteKeyPressed").objectReferenceValue = whiteKeyPressed;
        so.FindProperty("blackKeyNormal").objectReferenceValue = blackKeyNormal;
        so.FindProperty("blackKeyPressed").objectReferenceValue = blackKeyPressed;
        so.FindProperty("blackKeyLeft").objectReferenceValue = blackKeyLeft;
        so.FindProperty("blackKeyRight").objectReferenceValue = blackKeyRight;
        
        // Configura altura aumentada da tecla preta quando pressionada
        so.FindProperty("blackKeyPressedHeightIncrease").floatValue = blackKeyPressedHeightIncrease;
        
        // Configura dimensões dos sprites de perspectiva
        so.FindProperty("blackKeyPerspectiveHeight").floatValue = blackKeyPerspectiveHeight;
        so.FindProperty("blackKeyPerspectiveWidth").floatValue = blackKeyPerspectiveWidth;
        
        // Configura som
        so.FindProperty("noteSoundName").stringValue = $"piano_{noteName}";
        
        so.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(pianoKey);
        
        return pianoKey;
    }
    
    /// <summary>
    /// Conecta as teclas adjacentes para o efeito de perspectiva.
    /// </summary>
    private void ConnectAdjacentKeys(List<PianoKey> whiteKeys, List<PianoKey> blackKeys)
    {
        // Cria um dicionário de todas as teclas por nome completo (com oitava)
        Dictionary<string, PianoKey> allKeysByName = new Dictionary<string, PianoKey>();
        
        foreach (var whiteKey in whiteKeys)
        {
            SerializedObject so = new SerializedObject(whiteKey);
            string noteName = so.FindProperty("noteName").stringValue;
            allKeysByName[noteName] = whiteKey;
        }
        
        foreach (var blackKey in blackKeys)
        {
            SerializedObject so = new SerializedObject(blackKey);
            string noteName = so.FindProperty("noteName").stringValue;
            allKeysByName[noteName] = blackKey;
        }
        
        // Conecta cada tecla branca com suas adjacentes
        foreach (var whiteKey in whiteKeys)
        {
            SerializedObject so = new SerializedObject(whiteKey);
            string noteName = so.FindProperty("noteName").stringValue;
            
            // Encontra teclas brancas adjacentes (esquerda e direita)
            (string leftWhite, string rightWhite) = GetAdjacentWhiteNotes(noteName);
            
            if (leftWhite != null && allKeysByName.ContainsKey(leftWhite))
            {
                so.FindProperty("leftWhiteKey").objectReferenceValue = allKeysByName[leftWhite];
            }
            
            if (rightWhite != null && allKeysByName.ContainsKey(rightWhite))
            {
                so.FindProperty("rightWhiteKey").objectReferenceValue = allKeysByName[rightWhite];
            }
            
            // Encontra teclas pretas adjacentes (esquerda e direita)
            (string leftBlack, string rightBlack) = GetAdjacentBlackNotes(noteName);
            
            if (leftBlack != null && allKeysByName.ContainsKey(leftBlack))
            {
                so.FindProperty("leftBlackKey").objectReferenceValue = allKeysByName[leftBlack];
            }
            
            if (rightBlack != null && allKeysByName.ContainsKey(rightBlack))
            {
                so.FindProperty("rightBlackKey").objectReferenceValue = allKeysByName[rightBlack];
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(whiteKey);
        }
        
        // Conecta cada tecla preta com suas adjacentes brancas
        foreach (var blackKey in blackKeys)
        {
            SerializedObject so = new SerializedObject(blackKey);
            string noteName = so.FindProperty("noteName").stringValue;
            
            // Encontra teclas brancas adjacentes
            (string leftWhite, string rightWhite) = GetAdjacentWhiteNotesForBlack(noteName);
            
            if (leftWhite != null && allKeysByName.ContainsKey(leftWhite))
            {
                so.FindProperty("leftWhiteKey").objectReferenceValue = allKeysByName[leftWhite];
            }
            
            if (rightWhite != null && allKeysByName.ContainsKey(rightWhite))
            {
                so.FindProperty("rightWhiteKey").objectReferenceValue = allKeysByName[rightWhite];
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(blackKey);
        }
    }
    
    /// <summary>
    /// Retorna as teclas brancas adjacentes para uma tecla branca.
    /// </summary>
    private (string left, string right) GetAdjacentWhiteNotes(string fullNoteName)
    {
        // Extrai a nota base e a oitava
        string noteBase = fullNoteName.Substring(0, fullNoteName.Length - 1);
        int octave = int.Parse(fullNoteName.Substring(fullNoteName.Length - 1));
        
        string leftNote = null;
        string rightNote = null;
        
        switch (noteBase)
        {
            case "C":
                leftNote = $"B{octave - 1}";
                rightNote = $"D{octave}";
                break;
            case "D":
                leftNote = $"C{octave}";
                rightNote = $"E{octave}";
                break;
            case "E":
                leftNote = $"D{octave}";
                rightNote = $"F{octave}";
                break;
            case "F":
                leftNote = $"E{octave}";
                rightNote = $"G{octave}";
                break;
            case "G":
                leftNote = $"F{octave}";
                rightNote = $"A{octave}";
                break;
            case "A":
                leftNote = $"G{octave}";
                rightNote = $"B{octave}";
                break;
            case "B":
                leftNote = $"A{octave}";
                rightNote = $"C{octave + 1}";
                break;
        }
        
        return (leftNote, rightNote);
    }
    
    /// <summary>
    /// Retorna as teclas pretas adjacentes para uma tecla branca.
    /// </summary>
    private (string left, string right) GetAdjacentBlackNotes(string fullNoteName)
    {
        // Extrai a nota base e a oitava
        string noteBase = fullNoteName.Substring(0, fullNoteName.Length - 1);
        int octave = int.Parse(fullNoteName.Substring(fullNoteName.Length - 1));
        
        string leftBlack = null;
        string rightBlack = null;
        
        switch (noteBase)
        {
            case "C":
                rightBlack = $"C#{octave}";
                break;
            case "D":
                leftBlack = $"C#{octave}";
                rightBlack = $"D#{octave}";
                break;
            case "E":
                leftBlack = $"D#{octave}";
                break;
            case "F":
                rightBlack = $"F#{octave}";
                break;
            case "G":
                leftBlack = $"F#{octave}";
                rightBlack = $"G#{octave}";
                break;
            case "A":
                leftBlack = $"G#{octave}";
                rightBlack = $"A#{octave}";
                break;
            case "B":
                leftBlack = $"A#{octave}";
                break;
        }
        
        return (leftBlack, rightBlack);
    }
    
    /// <summary>
    /// Retorna as teclas brancas adjacentes para uma tecla preta.
    /// </summary>
    private (string left, string right) GetAdjacentWhiteNotesForBlack(string fullNoteName)
    {
        // Extrai a nota base e a oitava
        int lastHashIndex = fullNoteName.LastIndexOf('#');
        string noteBase = fullNoteName.Substring(0, lastHashIndex + 1);
        int octave = int.Parse(fullNoteName.Substring(lastHashIndex + 1));
        
        string leftWhite = null;
        string rightWhite = null;
        
        switch (noteBase)
        {
            case "C#":
                leftWhite = $"C{octave}";
                rightWhite = $"D{octave}";
                break;
            case "D#":
                leftWhite = $"D{octave}";
                rightWhite = $"E{octave}";
                break;
            case "F#":
                leftWhite = $"F{octave}";
                rightWhite = $"G{octave}";
                break;
            case "G#":
                leftWhite = $"G{octave}";
                rightWhite = $"A{octave}";
                break;
            case "A#":
                leftWhite = $"A{octave}";
                rightWhite = $"B{octave}";
                break;
        }
        
        return (leftWhite, rightWhite);
    }
    
    /// <summary>
    /// Remove todas as teclas criadas.
    /// </summary>
    [ContextMenu("Limpar Teclas")]
    public void ClearKeys()
    {
        if (keysContainer == null) return;
        
        int childCount = keysContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(keysContainer.GetChild(i).gameObject);
        }
        
        createdKeys.Clear();
        // Teclas removidas! (log removed)
    }
#endif
}
