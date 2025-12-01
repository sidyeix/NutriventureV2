using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BookUIEntry : MonoBehaviour
{
    public Image bookIconImage;
    public Button bookButton;
    
    private string bookId;
    private string bookName;
    private Sprite bookIcon;
    private BookUIManager bookManager;
    
    public void Initialize(string id, string name, Sprite icon, BookUIManager manager)
    {
        bookId = id;
        bookName = name;
        bookIcon = icon;
        bookManager = manager;
            
        if (bookIconImage != null && icon != null)
            bookIconImage.sprite = icon;
            
        if (bookButton != null)
            bookButton.onClick.AddListener(OnBookClicked);
    }
    
    private void OnBookClicked()
    {
        bookManager.OpenBook(bookId, bookName, bookIcon);
    }
}