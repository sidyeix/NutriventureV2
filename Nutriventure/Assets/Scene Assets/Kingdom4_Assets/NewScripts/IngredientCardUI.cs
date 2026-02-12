using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image frameImage;
    public Image enerlingImage;
    public Image rarityIcon;
    public Image smallIconImage;
    public GameObject lockIcon;
    public TextMeshProUGUI progressText;

    private KingdomFrameLibrary frameLibrary;

    public void Setup(
    IngredientDatabase.IngredientInfo info,
    IngredientDatabase db,
    KingdomFrameLibrary library)
{
    // FRAME BY KINGDOM
    frameImage.sprite = library.GetFrame(info.kingdom);

    // =========================
    // BIG ICON
    // =========================
    Sprite customIcon =
        library.GetEnerlingIcon(info.ingredientName);

    if (customIcon != null)
        enerlingImage.sprite = customIcon;
    else
        enerlingImage.sprite = info.enerlingSprite;

    // =========================
    // SMALL ICON (OPTIONAL)
    // =========================
    if (smallIconImage != null)
    {
        // Use same override system
        if (customIcon != null)
            smallIconImage.sprite = customIcon;
        else
            smallIconImage.sprite = info.enerlingSprite;

        // Show only if sprite assigned
        smallIconImage.gameObject.SetActive(
            smallIconImage.sprite != null);
    }

    // =========================
    // RARITY ICON
    // =========================
    Sprite customRarity =
        library.GetRarityIcon(info.rarity);

    if (customRarity != null)
        rarityIcon.sprite = customRarity;
    else
        rarityIcon.sprite =
            db.GetRarityIcon(info.rarity);

    // =========================
    // LOCK STATE
    // =========================
    bool unlocked = info.isUnlocked;

    lockIcon.SetActive(!unlocked);

    enerlingImage.color =
        unlocked ? Color.white : Color.black;

    if (smallIconImage != null)
        smallIconImage.color =
            unlocked ? Color.white : Color.black;

    progressText.text =
        unlocked ? "1/20" : "0/20";
}


}
