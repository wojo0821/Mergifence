using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnMergeableObject : DraggableItem
{
    [SerializeField] private GameObject mergeableObjectPrefab = null;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // 1. base.OnBeginDrag는 호출하지 않음 (호출 시 자기 자신이 투명해지거나 움직임)
        // 2. 새 오브젝트 생성
        GameObject newObject = Instantiate(mergeableObjectPrefab, parentCanvas.transform);
        newObject.transform.position = transform.position; // 또는 eventData.position
        newObject.transform.SetAsLastSibling();
        MergeableObject draggable = newObject.GetComponent<MergeableObject>();
        if (draggable != null)
        {
<<<<<<< HEAD
            draggable.SetText(text.text);
=======
            GameObject newObject = Instantiate(mergeableObjectPrefab, parentCanvas.transform);
            newObject.transform.position = eventData.position;
            newObject.transform.SetAsLastSibling();
            newObject.GetComponent<DraggableItem>().SetText(text.text);

            AudioManager.Instance.PlaySfx(dropSfx);
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
        }
        // 3. 드래그 대상을 새로 생성된 오브젝트로 위임
        eventData.pointerDrag = newObject;
        // 4. 새 오브젝트의 OnBeginDrag를 직접 실행하여 드래그 상태 초기화
        ExecuteEvents.Execute(newObject, eventData, ExecuteEvents.beginDragHandler);
    }
}
