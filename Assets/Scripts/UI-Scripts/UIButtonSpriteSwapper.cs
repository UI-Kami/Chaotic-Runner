using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Replaces the Animator-based button hover/click system with a simple
/// sprite-swap approach. Attach this to any UI button GameObject that has
/// an Image component. Assign the three sprites (Normal, Hover, Clicked)
/// in the Inspector and remove the Animator component.
///
/// On click the button locks to the Clicked sprite and stays there until
/// the player returns to the menu (scene reload resets it via Awake).
/// </summary>
[RequireComponent(typeof(Image))]
public class UIButtonSpriteSwapper : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    [Header("Button Sprites")]
    [Tooltip("The default (idle) sprite shown when the button is not interacted with.")]
    public Sprite normalSprite;

    [Tooltip("The sprite shown when the mouse hovers over the button.")]
    public Sprite hoverSprite;

    [Tooltip("The sprite shown while the button is being held/clicked.")]
    public Sprite clickedSprite;

    private Image _image;
    private bool _isHovered = false;
    private bool _isClicked = false;

    void Awake()
    {
        _image = GetComponent<Image>();

        // Set initial sprite — resets whenever the scene (re)loads
        _isClicked = false;
        _isHovered = false;
        if (normalSprite != null)
            _image.sprite = normalSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        // Don't change sprite if already locked to clicked state
        if (_isClicked) return;
        if (hoverSprite != null)
            _image.sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        // Don't change sprite if already locked to clicked state
        if (_isClicked) return;
        if (normalSprite != null)
            _image.sprite = normalSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isClicked = true;
        if (clickedSprite != null)
            _image.sprite = clickedSprite;
    }

    // OnPointerUp intentionally NOT restoring the sprite —
    // the clicked frame stays locked until the scene reloads.

    /// <summary>
    /// Resets to the normal sprite. Call this manually if you need to
    /// un-lock the clicked state without reloading the scene.
    /// </summary>
    public void ResetToNormal()
    {
        _isClicked = false;
        _isHovered = false;
        if (_image != null && normalSprite != null)
            _image.sprite = normalSprite;
    }
}
