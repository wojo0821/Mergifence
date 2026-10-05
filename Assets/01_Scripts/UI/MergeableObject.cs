using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MergeableObject : DraggableItem
{
    private GameObject[] overlappedObjects;

    [SerializeField] private GameObject mergeButton = null;
    private GameObject installedButton;
    private bool isButtonInstalled = false; //버튼이 설치 되있는가

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        UpdateMergeButton();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        UpdateMergeButton();
    }
    public GameObject[] GetOverlappedObject()
    {
        List<GameObject> targetList = new List<GameObject>();
        // 1. 내 월드 사각형 구하기
        Vector3[] myCorners = new Vector3[4];
        rectTransform.GetWorldCorners(myCorners);
        Rect myRect = new Rect(myCorners[0].x, myCorners[0].y, myCorners[2].x - myCorners[0].x, myCorners[2].y - myCorners[0].y);
        // 2. 같은 캔버스 안에 있는 모든 MergeableObject들을 가져와서 하나씩 검사(본인도 포함)
        MergeableObject[] allObjects = parentCanvas.GetComponentsInChildren<MergeableObject>();
        foreach (MergeableObject targetObject in allObjects)
        {
            // 상대방의 월드 사각형 구하기
            Vector3[] targetCorners = new Vector3[4];
            targetObject.rectTransform.GetWorldCorners(targetCorners);
            Rect targetRect = new Rect(targetCorners[0].x, targetCorners[0].y, targetCorners[2].x - targetCorners[0].x, targetCorners[2].y - targetCorners[0].y);
            // Overlaps로 겹쳤는지 확인! 겹쳤다면 그 게임오브젝트를 즉시 반환!
            if (myRect.Overlaps(targetRect))
            {
                targetList.Add(targetObject.gameObject);
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
    void UpdateMergeButton()
    {
        List<string> namesList = new List<string>();
        overlappedObjects = GetOverlappedObject();

        if (overlappedObjects.Length > 1)
        {
            Vector3 sumPos = Vector3.zero;
            foreach (GameObject overlappedObject in overlappedObjects)
            {
                if (!overlappedObject.transform.IsChildOf(transform))
                {
                    overlappedObject.GetComponent<MergeableObject>().DeleteMergeButton();
                }
                sumPos += overlappedObject.transform.position;
                namesList.Add(overlappedObject.GetComponentInChildren<TextMeshProUGUI>().text);
            }
            Vector3 centerPos = sumPos / (overlappedObjects.Length);

            if (isButtonInstalled == false)
            {
                isButtonInstalled = true;
                installedButton = Instantiate(mergeButton, parentCanvas.transform);
                installedButton.transform.SetAsLastSibling();
                installedButton.GetComponent<MergeButton>().mergeableObjects = overlappedObjects;
                installedButton.transform.position = centerPos;
            }
            else
            {
                installedButton.GetComponent<MergeButton>().mergeableObjects = overlappedObjects;
                installedButton.transform.position = centerPos;
                installedButton.transform.SetAsLastSibling();
            }
        }
        else
        {
            DeleteMergeButton();
        }
    }
}
