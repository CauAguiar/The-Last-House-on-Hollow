using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que guarda o conteúdo de todas as páginas do diário do Tim.
/// </summary>
[CreateAssetMenu(fileName = "JournalData", menuName = "Diary/Journal Data")]
public class JournalData : ScriptableObject
{
    [System.Serializable]
    public class JournalPage
    {
        public int pageId;           // ID da página
        [TextArea(3, 8)]
        public string pageContent;   // Texto da página
    }

    public List<JournalPage> pages = new List<JournalPage>();
}