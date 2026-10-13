//Adam Montgomery
//Gemini generated code
//10.9.2026

using UnityEngine;
//using HoldTheHill.Features.Towers; // Teammate's namespace, implement LATER

[CreateAssetMenu(fileName = "New Tower", menuName = "Hold The Hill/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Shop Display")]
    public string towerName;
    public int honeydewCost;
    public Sprite towerIcon;

    //[Header("Placement Prefab")]
    //[Tooltip("Drag the teammate's Tower prefab here.")]
    //public Tower towerPrefab; 
}