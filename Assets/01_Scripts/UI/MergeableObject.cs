using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MergeableObject : DraggableItem
{
    private GameObject[] overlappedObjects;

    [SerializeField] private GameObject mergeButton = null;
    private GameObject installedButton;
    private bool isButtonInstalled = false; //버튼이 설치 되있는가

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        overlappedObjects = GetOverlappedObject();

        if (overlappedObjects.Length > 0)
        {
            Vector3 sumPos = rectTransform.position;
            foreach (GameObject overlappedObject in overlappedObjects)
            {
                overlappedObject.GetComponent<MergeableObject>().DeleteMergeButton();
                sumPos += overlappedObject.transform.position;
            }
            Vector3 centerPos = sumPos / (overlappedObjects.Length + 1);

            if (isButtonInstalled == false)
            {
                isButtonInstalled = true;
                installedButton = Instantiate(mergeButton, parentCanvas.transform);
                installedButton.transform.SetAsLastSibling();
                installedButton.transform.position = centerPos;
            }
            else
            {
                installedButton.transform.position = centerPos;
                installedButton.transform.SetAsLastSibling();
            }
        }
        else
        {
            DeleteMergeButton();
        }
    }
    public GameObject[] GetOverlappedObject()
    {
        List<GameObject> targetList = new List<GameObject>();
        // 1. 내 월드 사각형 구하기
        Vector3[] myCorners = new Vector3[4];
        rectTransform.GetWorldCorners(myCorners);
        Rect myRect = new Rect(myCorners[0].x, myCorners[0].y, myCorners[2].x - myCorners[0].x, myCorners[2].y - myCorners[0].y);
        // 2. 같은 캔버스 안에 있는 모든 Image(UI)들을 가져와서 하나씩 검사
        Image[] allImages = parentCanvas.GetComponentsInChildren<Image>();
        foreach (Image targetImage in allImages)
        {
            // 나 자신이나 내 자식 오브젝트는 제외
            if (targetImage.transform.IsChildOf(transform)) continue;
            // 상대방이 MergeableObject가 아니면 제외
            if (targetImage.GetComponent<MergeableObject>() == null) continue;
            // 상대방의 월드 사각형 구하기
            Vector3[] targetCorners = new Vector3[4];
            targetImage.rectTransform.GetWorldCorners(targetCorners);
            Rect targetRect = new Rect(targetCorners[0].x, targetCorners[0].y, targetCorners[2].x - targetCorners[0].x, targetCorners[2].y - targetCorners[0].y);
            // ✅ Overlaps로 겹쳤는지 확인! 겹쳤다면 그 게임오브젝트를 즉시 반환!
            if (myRect.Overlaps(targetRect))
            {
                targetList.Add(targetImage.gameObject);
            }
        }
        return targetList.ToArray(); // 겹친 오브젝트가 없음
    }
    public void DeleteMergeButton()
    {
        if (isButtonInstalled == true)
        {
            isButtonInstalled = false;
            Destroy(installedButton);
        }
    }
}
