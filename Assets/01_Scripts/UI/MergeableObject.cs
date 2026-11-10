using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MergeableObject : DraggableItem
{
    private GameObject[] overlappedObjects;
    private bool isMerging = false;
    [SerializeField] private float mergingTime = 0;
    [SerializeField] private AudioClip mergingClip = null;
    private float currentMergingTime;
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD

    // 합성 진행 중 여부 플래그
    public bool isMerging { get; private set; } = false;
=======
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (isMerging) return;
        base.OnPointerDown(eventData);

        if (GetOverlappedObject().Length > 0)
        {
            isMerging = true;
            AudioManager.Instance.PlayHoldSound(mergingClip);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        isMerging = false;
        AudioManager.Instance.StopHoldSound();
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (isMerging) return;
        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (isMerging) return;
        base.OnDrag(eventData);

        isMerging = false;
        AudioManager.Instance.StopHoldSound();
    }
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (isMerging) return;
        base.OnEndDrag(eventData);

        // 드래그 종료 시 하이라이트된 상대방 스케일 복구
        if (currentOverlappedObject != null)
        {
            currentOverlappedObject.transform.localScale = Vector3.one;
            currentOverlappedObject = null;
        }

        GameObject overlapped = GetOverlappedObject();
        if (overlapped != null)
        {
            MergeableObject targetObject = overlapped.GetComponent<MergeableObject>();
            if (targetObject != null && !targetObject.isMerging)
            {
                StartMerge(targetObject);
            }
        }
    }

    private async void StartMerge(MergeableObject target)
    {
        isMerging = true;
        target.isMerging = true;

        string myWord = text.text;
        string targetWord = target.text.text;

        string originalText = myWord;
        SetText("합성 중...");

        string resultWord = await MergeAi.Instance.MergeAsync(myWord, targetWord);

        if (!string.IsNullOrEmpty(resultWord))
        {
            // 합성 성공: 자신은 새로운 단어로 변경되고 상대방은 제거
            SetText(resultWord);
            Destroy(target.gameObject);
        }
        else
        {
            // 실패 시 원래 단어로 복구
            SetText(originalText);
            target.isMerging = false;
        }

        isMerging = false;
    }

    public GameObject GetOverlappedObject()
=======
    public GameObject[] GetOverlappedObject()
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
    {
=======
    public GameObject[] GetOverlappedObject()
    {
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
    public GameObject[] GetOverlappedObject()
    {
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
    public GameObject[] GetOverlappedObject()
    {
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
    public GameObject[] GetOverlappedObject()
    {
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
        List<GameObject> targetList = new List<GameObject>();
        // 1. 내 월드 사각형 구하기
        Vector3[] myCorners = new Vector3[4];
        rectTransform.GetWorldCorners(myCorners);
        Rect myRect = new Rect(myCorners[0].x, myCorners[0].y, myCorners[2].x - myCorners[0].x, myCorners[2].y - myCorners[0].y);
        // 2. 같은 캔버스 안에 있는 모든 MergeableObject들을 가져와서 하나씩 검사(본인 및 이미 합성 중인 오브젝트 제외)
        MergeableObject[] allObjects = parentCanvas.GetComponentsInChildren<MergeableObject>();
        foreach (MergeableObject targetObject in allObjects)
        {
            if (targetObject == this || targetObject.isMerging) continue;
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

    private void Update()
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        if (!isUsing || isMerging) return;
=======
        if (!isMerging) return;
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
        if (!isMerging) return;
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
        if (!isMerging) return;
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
        if (!isMerging) return;
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)
=======
        if (!isMerging) return;
>>>>>>> parent of 560d699 (refactor: ui재구성 및 스크립트 재구성)

        currentMergingTime += Time.deltaTime;

        if (currentMergingTime >= mergingTime)
        {
            Debug.Log("합성");
        }
    }
}
