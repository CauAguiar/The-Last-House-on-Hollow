using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundBank", menuName = "Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public string soundName;
        public AudioClip clip;
    }

    [Header("Lista de Sons")]
    public List<SoundEntry> sounds = new List<SoundEntry>();

    private Dictionary<string, AudioClip> soundDictionary;

    private void OnEnable()
    {
        soundDictionary = new Dictionary<string, AudioClip>();
        foreach (var s in sounds)
        {
            if (!soundDictionary.ContainsKey(s.soundName))
                soundDictionary.Add(s.soundName, s.clip);
        }
    }

    public AudioClip GetClip(string soundName)
    {
        if (soundDictionary != null && soundDictionary.ContainsKey(soundName))
        {
            return soundDictionary[soundName];
        }

        Debug.LogWarning($"Som '{soundName}' n?o encontrado no SoundBank!");
        return null;
    }
}
