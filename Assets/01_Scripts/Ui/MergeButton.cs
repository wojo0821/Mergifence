using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MergeButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private AudioClip mergeSfx = null;
    [SerializeField] private GameObject mergeableObjectPrefab = null;
    public GameObject[] mergeableObjects;

    public async void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySfx(mergeSfx);

        if (MergeAi.Instance != null && MergeAi.Instance.IsModelLoaded)
        {
            // 겹쳐진 아이템 이름 배열 (예: ["물", "불"])
            List<string> mergeableNamesList = new List<string>();
            foreach (GameObject mergeableObject in mergeableObjects)
            {
                mergeableNamesList.Add(mergeableObject.GetComponentInChildren<TextMeshProUGUI>().text);
            }
            string resultWord = await MergeAi.Instance.MergeIngredientsAsync(mergeableNamesList.ToArray());

            Debug.Log($"생성된 결과 단어: {resultWord}");
            GameObject newMergeableObject = Instantiate(this.mergeableObjectPrefab, transform.parent);
            newMergeableObject.transform.position = transform.position;
            newMergeableObject.transform.SetAsLastSibling();
            newMergeableObject.GetComponentInChildren<TextMeshProUGUI>().text = resultWord;
            foreach (GameObject mergeableObject in mergeableObjects)
            {
                Destroy(mergeableObject);
            }
            Destroy(gameObject);
        }
    }
}
