using UnityEngine;

[CreateAssetMenu(fileName = "PortraitData_", menuName = "Puzzle/Portrait Data")]
public class PortraitData : ScriptableObject
{
    public string portraitID;      
    public Sprite image;          
    public string inspectionText; 
}