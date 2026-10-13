//Adam Montgomery
//Gemini generated code
//10.9.2026

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerShopManager : MonoBehaviour
{
    [Header("Configuration")]
    public List<TowerData> availableTowers; 
    public GameObject towerCardPrefab;      
    public Transform shopContainer;         
    public ScrollRect shopScrollRect;       

    void Start()
    {
        PopulateShop();
    }

    void PopulateShop()
    {
        // Clean out any placeholder cards in the container
        foreach (Transform child in shopContainer)
        {
            Destroy(child.gameObject);
        }

        // Generate a new card for every TowerData in the list
        foreach (TowerData tower in availableTowers)
        {
            GameObject newCard = Instantiate(towerCardPrefab, shopContainer);
            
            TowerCardUI cardUI = newCard.GetComponent<TowerCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(tower);
            }
        }

        // Force the layout to rebuild so the scrollbar doesn't break
        Canvas.ForceUpdateCanvases();
        shopScrollRect.verticalNormalizedPosition = 1f;
    }
}