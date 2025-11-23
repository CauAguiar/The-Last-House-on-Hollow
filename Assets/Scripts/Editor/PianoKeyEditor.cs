using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor helper para configurar teclas do piano automaticamente.
/// Adiciona botões úteis no Inspector do PianoKey.
/// </summary>
[CustomEditor(typeof(PianoKey))]
public class PianoKeyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        PianoKey pianoKey = (PianoKey)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ações Rápidas", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Testar Som da Nota"))
        {
            TestNoteSound(pianoKey);
        }
        
        if (GUILayout.Button("Pressionar Tecla (Preview)"))
        {
            if (Application.isPlaying)
            {
                pianoKey.PressKey();
            }
            else
            {
                EditorGUILayout.HelpBox("Entre em Play Mode para testar a tecla.", MessageType.Info);
            }
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Dica: Configure as teclas adjacentes para ativar o efeito de perspectiva das teclas pretas.", MessageType.Info);
    }
    
    private void TestNoteSound(PianoKey key)
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Entre em Play Mode para testar o som.", MessageType.Warning);
            return;
        }
        
        // Testa o som através do AudioManager
        SerializedObject so = new SerializedObject(key);
        SerializedProperty noteSoundProp = so.FindProperty("noteSoundName");
        
        if (noteSoundProp != null && !string.IsNullOrEmpty(noteSoundProp.stringValue))
        {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(noteSoundProp.stringValue);
                }
                else
                {
                    // AudioManager not found in scene (editor test)
                }
        }
        else
        {
            // Nome do som não configurado (editor)
        }
    }
}
#endif
