using LLMUnity;
using UnityEngine;

public class TestAi : MonoBehaviour
{
    public LLMAgent llmAgent;

    void Start()
    {
        // "안녕!"이라는 메시지를 보내고, 답변을 HandleReply 함수로 받음
        _ = llmAgent.Chat("안녕! 오늘 날씨가 어때?", HandleReply, ReplyCompleted);
    }

    // AI가 답변을 생성할 때 호출되는 함수 (실시간 스트리밍 출력)
    void HandleReply(string reply)
    {
        Debug.Log(reply);
    }

    // 답변 생성이 완전히 끝났을 때 호출되는 함수 (선택 사항)
    void ReplyCompleted()
    {
        Debug.Log("답변 완료!");
    }
}