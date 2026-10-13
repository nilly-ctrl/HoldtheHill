//Adam Montgomery
//Gemini generated code
//10.9.2026

using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public Image iconImage;
    public Button cardButton; 

    private TowerData myTowerData;

    public void SetupCard(TowerData data)
    {
        myTowerData = data;
        
        nameText.text = myTowerData.DisplayName;
        costText.text = myTowerData.BaseCost.ToString();
        iconImage.sprite = myTowerData.Icon;

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        Debug.Log($"Player clicked {myTowerData.DisplayName}. Placement logic pending indev merge.");
    }
}