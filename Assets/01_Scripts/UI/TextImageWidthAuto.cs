using TMPro;
using UnityEngine;

[ExecuteAlways] // 씬뷰에서도 작동
public class TextImageWidthAuto : MonoBehaviour
{
    //TMP 글자 Width에 따라서 Image의 Width를 늘림

    [Header("연결")]
    private RectTransform imageRectTransform;
    private TMP_Text textComponent;

    [Header("설정")]
    [SerializeField] private float padding = 40f;
    [SerializeField] private float minWidth = 50f;

    private void Awake()
    {
        InitComponents();
    }
    private void Update()
    {
        if (Application.isPlaying) return;
        UpdateWidth();
    }
    private void InitComponents()
    {
        imageRectTransform = GetComponent<RectTransform>();
        textComponent = GetComponentInChildren<TMP_Text>();
    }
    public void UpdateWidth()
    {
        if (imageRectTransform == null || textComponent == null)
        {
            InitComponents();
            if (imageRectTransform == null || textComponent == null) return;
        }
        float textWidth = textComponent.preferredWidth;
        textComponent.rectTransform.sizeDelta = new Vector2(textWidth, textComponent.rectTransform.sizeDelta.y);
        float finalImageWidth = Mathf.Max(minWidth, textWidth + padding);
        imageRectTransform.sizeDelta = new Vector2(finalImageWidth, imageRectTransform.sizeDelta.y);
    }
}
