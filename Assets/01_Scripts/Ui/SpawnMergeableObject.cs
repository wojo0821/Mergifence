using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnMergeableObject : DraggableItem
{
    [SerializeField] private GameObject mergeableObjectPrefab = null;
    [SerializeField] private AudioClip dropSfx = null;

    public override void OnEndDrag(PointerEventData eventData)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        image.raycastTarget = true;
        // 포인터 위치가 mergeableObject를 설치 가능한 위치라면 그 위치에 prefab생성
        if (eventData.pointerCurrentRaycast.gameObject != null && eventData.pointerCurrentRaycast.gameObject.CompareTag("CanDrop"))
        {
            GameObject newObject = Instantiate(mergeableObjectPrefab, parentCanvas.transform);
            newObject.transform.position = transform.position;
            newObject.transform.SetAsLastSibling();
            newObject.GetComponent<DraggableItem>().SetText(text.text);

            AudioManager.Instance.PlaySfx(dropSfx);
        }
        // 원래 위치로 되돌림
        rectTransform.anchoredPosition = initialPosition;
    }
}
