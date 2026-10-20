using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;

public class MergeAi : MonoBehaviour
{
    public static MergeAi Instance { get; private set; }

    [SerializeField] private LLMAgent llmAgent;
    [SerializeField] private LLM llm;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (llmAgent == null) llmAgent = FindAnyObjectByType<LLMAgent>();
        if (llm == null) llm = FindAnyObjectByType<LLM>();

        ConfigureLLM();
    }

    private void ConfigureLLM()
    {
        if (llm != null)
        {
            // GPU 가속 99레이어 오프로딩 (CUDA / Vulkan)
            llm.numGPULayers = 99;

            if (llm.contextSize > 2048)
            {
                llm.contextSize = 2048;
            }
        }

        if (llmAgent != null)
        {
            llmAgent.cachePrompt = true;
            // 단어 1개만 나오도록 최대 8토큰으로 엄격 제한
            llmAgent.numPredict = 8;
            llmAgent.temperature = 0.6f;

            // Qwen3.5 4B 맞춤형 시스템 프롬프트 (규칙 및 조합 공식 고정)
            llmAgent.systemPrompt =
                "/no_think\n" +
                "당신은 인피니티 크래프트(Infinite Craft) 게임의 단어 조합 엔진입니다.\n" +
                "두 개념이 결합되어 생기는 새로운 한국어 명사 한 단어만 답하세요.\n\n" +
                "[절대 규칙]\n" +
                "1. 생각 과정(<think> 태그)을 절대로 출력하지 마세요.\n" +
                "2. 마크다운(**), 따옴표(\"), 문장, 설명, 조사(은/는/이/가)를 절대 쓰지 마세요.\n" +
                "3. 오직 '단어 하나'만 출력하세요.\n\n" +
                "[조합 공식]\n" +
                "불 + 공기 = 연기\n" +
                "물 + 불 = 증기\n" +
                "물 + 흙 = 진흙\n" +
                "흙 + 불 = 용암\n" +
                "공기 + 흙 = 먼지\n" +
                "물 + 공기 = 비\n" +
                "불 + 모래 = 유리\n" +
                "용암 + 물 = 돌\n" +
                "물 + 물 = 바다\n" +
                "불 + 불 = 화산\n" +
                "식물 + 불 = 재\n" +
                "나무 + 불 = 숯\n" +
                "비 + 흙 = 식물\n" +
                "공기 + 에너지 = 바람";
        }
    }

    /// <summary>
    /// 두 개의 단어를 합성합니다.
    /// 매 호출마다 대화 히스토리를 리셋하여 무한 반복해도 깨끗한 품질을 유지하며,
    /// 5초 타임아웃 안전장치를 통해 UI 멈춤 현상을 방지합니다.
    /// </summary>
    public async Task<string> MergeAsync(string itemA, string itemB)
    {
        if (llmAgent == null)
        {
            llmAgent = FindAnyObjectByType<LLMAgent>();
            if (llmAgent == null)
            {
                Debug.LogError("[MergeAi] LLMAgent를 찾을 수 없습니다.");
                return GetFallback(itemA, itemB);
            }
        }

        try
        {
            // 1. 이전 대화 기록을 완벽하게 초기화 (반복 시 품질 저하/오염 원천 차단)
            await llmAgent.ClearHistory();

            // 2. 입력 쿼리: 예시 패턴대로 뒤의 단어 하나만 즉시 채우도록 '= ' 추가
            string query = $"{itemA} + {itemB} = ";

            // 3. 5초 타임아웃 안전장치 구축 ("합성 중..." 무한 대기 방지)
            Task<string> chatTask = llmAgent.Chat(query, null, null, false);
            Task timeoutTask = Task.Delay(5000);

            Task completedTask = await Task.WhenAny(chatTask, timeoutTask);

            if (completedTask == chatTask)
            {
                string response = await chatTask;
                return ParseResult(response, itemA, itemB);
            }
            else
            {
                Debug.LogWarning($"[MergeAi] '{itemA} + {itemB}' AI 응답 시간 초과(5초). 기본값으로 대체합니다.");
                return GetFallback(itemA, itemB);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MergeAi] 합성 중 오류 발생: {ex.Message}");
            return GetFallback(itemA, itemB);
        }
    }

    private string ParseResult(string raw, string itemA, string itemB)
    {
        if (string.IsNullOrWhiteSpace(raw)) return GetFallback(itemA, itemB);

        // 1. <think> 태그 내용물 제거
        string cleaned = Regex.Replace(raw, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase).Trim();

        // 2. 첫 줄만 취득
        string firstLine = cleaned.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        // 3. 등호(=), 화살표(->), 콜론(:) 뒤의 단어만 취득
        if (firstLine.Contains("="))
            firstLine = firstLine.Substring(firstLine.LastIndexOf('=') + 1).Trim();
        if (firstLine.Contains("->"))
            firstLine = firstLine.Substring(firstLine.LastIndexOf("->", StringComparison.Ordinal) + 2).Trim();
        if (firstLine.Contains(":"))
            firstLine = firstLine.Substring(firstLine.LastIndexOf(':') + 1).Trim();

        // 4. 마크다운 볼드(**), 따옴표(", '), 특수문자 완벽 제거
        string resultWord = Regex.Replace(firstLine, @"[\*\""\'\‘\’\“\”\`\#\$\%\^\&\(\)\[\]\{\}\<\>\!\?\.\,\;\:\/\\]", "").Trim();

        // 5. 공백이 있으면 첫 단어만 취득
        if (resultWord.Contains(" "))
        {
            string[] parts = resultWord.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) resultWord = parts[0];
        }

        // 6. 끝에 붙은 불필요한 조사/어미(입니다, 은, 는, 이, 가, 을, 를 등) 제거
        string[] particles = { "입니다", "이다", "으로", "에서", "은", "는", "이", "가", "을", "를", "와", "과", "로" };
        foreach (var p in particles)
        {
            if (resultWord.Length > p.Length + 1 && resultWord.EndsWith(p))
            {
                resultWord = resultWord.Substring(0, resultWord.Length - p.Length);
                break;
            }
        }

        resultWord = resultWord.Trim();

        // 7. 단순 결합("불공기") 또는 입력 단어와 동일한 경우 방어 (Fallback 적용)
        string simple1 = (itemA + itemB).Replace(" ", "");
        string simple2 = (itemB + itemA).Replace(" ", "");
        string cleanCheck = resultWord.Replace(" ", "");

        if (string.IsNullOrWhiteSpace(resultWord) ||
            cleanCheck.Equals(itemA, StringComparison.OrdinalIgnoreCase) ||
            cleanCheck.Equals(itemB, StringComparison.OrdinalIgnoreCase) ||
            cleanCheck.Equals(simple1, StringComparison.OrdinalIgnoreCase) ||
            cleanCheck.Equals(simple2, StringComparison.OrdinalIgnoreCase))
        {
            return GetFallback(itemA, itemB);
        }

        return resultWord;
    }

    private string GetFallback(string itemA, string itemB)
    {
        if ((itemA == "불" && itemB == "공기") || (itemA == "공기" && itemB == "불")) return "연기";
        if ((itemA == "물" && itemB == "불") || (itemA == "불" && itemB == "물")) return "증기";
        if ((itemA == "물" && itemB == "흙") || (itemA == "흙" && itemB == "물")) return "진흙";
        if ((itemA == "흙" && itemB == "불") || (itemA == "불" && itemB == "흙")) return "용암";
        if ((itemA == "공기" && itemB == "흙") || (itemA == "흙" && itemB == "공기")) return "먼지";
        if ((itemA == "물" && itemB == "공기") || (itemA == "공기" && itemB == "물")) return "비";
        if ((itemA == "불" && itemB == "모래") || (itemA == "모래" && itemB == "불")) return "유리";
        if ((itemA == "물" && itemB == "물")) return "바다";
        if ((itemA == "불" && itemB == "불")) return "화산";

        return "에너지";
    }
}
