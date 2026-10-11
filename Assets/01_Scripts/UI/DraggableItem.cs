using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    protected RectTransform rectTransform;
    protected Image image;
    protected TextMeshProUGUI text;
    protected Canvas parentCanvas;

    [SerializeField] private AudioClip clickSfxDown = null;
    [SerializeField] private AudioClip clickSfxUp = null;

    protected Vector2 initialPosition;

    protected float outOfScreenOffset;

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>();
        text = image.GetComponentInChildren<TextMeshProUGUI>();
        outOfScreenOffset = Screen.width * 0.02f;
    }


    //이미지 클릭시 바로 사운드 재생
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySfx(clickSfxDown);
    }
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySfx(clickSfxUp);
    }

    public void SetText(string newText)
    {
        if (text == null && image != null)
        {
            text = image.GetComponentInChildren<TextMeshProUGUI>();
        }
        else if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (text != null)
        {
            text.text = newText;
        }

        gameObject.name = newText;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        initialPosition = rectTransform.anchoredPosition;

        transform.SetAsLastSibling();

        image.color = new Color(image.color.r, image.color.g, image.color.b, 0.6f);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 0.6f);
        image.raycastTarget = false;
    }
    public virtual void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        image.raycastTarget = true;

        // 만약에 있는 곳이 삭제 위치라면 삭제 혹은 캔버스 밖+오프셋 이라면 원래 위치로
        if (eventData.pointerCurrentRaycast.gameObject != null && eventData.pointerCurrentRaycast.gameObject.CompareTag("Delete"))
        {
            Destroy(gameObject);
        }
        else if (eventData.position.x < 0 + outOfScreenOffset || eventData.position.x > Screen.width - outOfScreenOffset || eventData.position.y < 0 + outOfScreenOffset || eventData.position.y > Screen.height - outOfScreenOffset)
        {
            rectTransform.anchoredPosition = initialPosition;
        }
    }
}
