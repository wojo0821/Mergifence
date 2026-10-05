using System.IO;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using UnityEngine;

public class MergeAi : MonoBehaviour
{
    public static MergeAi Instance { get; private set; }

    private string modelFileName = "gemma-4-E2B_q4_0-it.gguf";
    private uint contextSize = 512; // 단발성 생성이므로 512로 CPU 속도 및 메모리 최적화

    private LLamaWeights model;
    private StatelessExecutor executor; // 이전 대화 상태를 누적하지 않는 가벼운 실행기
    private ModelParams parameters;

    public bool IsModelLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await LoadModelAsync();
    }

    // 모델 로드 (게임 시작 시 1회 실행)
    private async Task LoadModelAsync()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
        if (!File.Exists(modelPath))
        {
            Debug.LogError($"[MergeAi] 모델 파일을 찾을 수 없습니다: {modelPath}");
            return;
        }

        Debug.Log($"[MergeAi] Gemma 4 모델 로딩 시작: {modelFileName}");

        int threadCount = Mathf.Max(1, System.Environment.ProcessorCount - 1);

        await Task.Run(() =>
        {
            parameters = new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = 0, // 순수 CPU 구동
                Threads = threadCount // 백그라운드 스레드에서도 안전한 CPU 코어 수
            };

            model = LLamaWeights.LoadFromFile(parameters);
            executor = new StatelessExecutor(model, parameters);
        });

        IsModelLoaded = true;
        Debug.Log("[MergeAi] Gemma 4 모델 로딩 완료!");
    }

    // 단어 합성 추론 요청 함수
    public async Task<string> MergeIngredientsAsync(string[] ingredients)
    {
        if (!IsModelLoaded)
        {
            Debug.LogWarning("[MergeAi] 모델이 아직 로드되지 않았습니다.");
            return "로딩 중...";
        }

        string items = string.Join(" + ", ingredients);

        // ✅ Few-shot 예시: 유저의 입력(불+물 등)과 중복되어 혼란을 주지 않도록 다른 예시로 구성
        string prompt =
            "<start_of_turn>user\n" +
            "당신은 연금술 단어 합성 게임의 AI입니다. 두 단어를 융합한 새로운 명사 단어 1개만 답하세요. 절대로 단어를 단순히 이어붙이지 마세요 (예: 불+물=불물 금지).\n" +
            "흙 + 불<end_of_turn>\n" +
            "<start_of_turn>model\n" +
            "용암<end_of_turn>\n" +
            "<start_of_turn>user\n" +
            "나무 + 불<end_of_turn>\n" +
            "<start_of_turn>model\n" +
            "숯<end_of_turn>\n" +
            "<start_of_turn>user\n" +
            "바람 + 물<end_of_turn>\n" +
            "<start_of_turn>model\n" +
            "파도<end_of_turn>\n" +
            "<start_of_turn>user\n" +
            $"{items}<end_of_turn>\n" +
            "<start_of_turn>model\n";

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 64,
            AntiPrompts = new[] { "<end_of_turn>", "\n", "<start_of_turn>", "=", "+", "<" }, // '<' 나오면 즉시 정지
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Temperature = 0.6f // 더 정밀하고 정확한 출력을 위해 온도를 약간 낮춤
            }
        };

        StringBuilder resultBuilder = new StringBuilder();

        // 백그라운드 스레드에서 추론 실행 (Unity 메인 스레드 프리징 방지)
        await Task.Run(async () =>
        {
            await foreach (var token in executor.InferAsync(prompt, inferenceParams))
            {
                resultBuilder.Append(token);
            }
        });

        // 텍스트 정리 (태그 시작 기호인 '<' 또는 '+', '=', 줄바꿈 앞까지만 순수 단어로 추출)
        string rawText = resultBuilder.ToString();
        string[] parts = rawText.Split(new[] { '\n', '+', '=', '<' }, System.StringSplitOptions.RemoveEmptyEntries);
        string cleanResult = parts.Length > 0 ? parts[0].Trim() : rawText.Trim();

        Debug.Log($"[MergeAi] 합성 완료: {items} -> {cleanResult}");
        return cleanResult;
    }

    // 메모리 해제
    private void OnDestroy()
    {
        model?.Dispose();
    }
}
