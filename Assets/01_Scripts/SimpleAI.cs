using System.IO;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField] private string modelFileName = "Qwen3.5-2B-Q4_K_M.gguf";
    [SerializeField] private uint contextSize = 2048;
    [SerializeField] private int gpuLayerCount = 0; // CPU 백엔드면 0 유지

    private LLamaWeights _model;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    private ChatSession _session;

    private bool _isInitialized = false;
    private bool _isGenerating = false;

    private async void Start()
    {
        await InitializeModelAsync();

        // 초기화 완료 후 테스트 질문 전송

        if (_isInitialized)
        {
            await AskQuestionAsync("자기소개를 간단히 해줘.");
        }
    }

    /// <summary>
    /// 모델 가중치 로드 및 세션 초기화 (백그라운드 스레드)
    /// </summary>
    private async Task InitializeModelAsync()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);

        if (!File.Exists(modelPath))
        {
            Debug.LogError($"[SimpleAI] 모델 파일을 찾을 수 없습니다: {modelPath}");
            return;
        }

        Debug.Log("[SimpleAI] 모델 로딩 시작...");

        await Task.Run(() =>
        {
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = gpuLayerCount
            };

            // 가중치 및 컨텍스트 생성
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);

            // 실행기 및 대화 세션 설정
            _executor = new InteractiveExecutor(_context);

            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.System, "당신은 친절하고 도움이 되는 AI 어시스턴트입니다. 한국어로 답변해주세요.");

            _session = new ChatSession(_executor, chatHistory);
        });

        _isInitialized = true;
        Debug.Log("[SimpleAI] 모델 로딩 완료!");
    }

    /// <summary>
    /// AI에게 질문하고 실시간(스트리밍)으로 답변 받기
    /// </summary>
    public async Task<string> AskQuestionAsync(string userPrompt)
    {
        if (!_isInitialized || _isGenerating) return string.Empty;

        _isGenerating = true;
        Debug.Log($"[User]: {userPrompt}");

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 256,
            SamplingPipeline = new DefaultSamplingPipeline(),
            AntiPrompts = new[] { "User:" }
        };

        var responseBuilder = new StringBuilder();

        // 스트리밍 방식으로 한 토큰씩 전달받음
        await foreach (var token in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userPrompt), inferenceParams))
        {
            responseBuilder.Append(token);
            // 필요 시 UI 텍스트 컴포넌트에 실시간 갱신 가능
        }

        string fullResponse = responseBuilder.ToString();
        Debug.Log($"[AI]: {fullResponse}");

        _isGenerating = false;
        return fullResponse;
    }

    /// <summary>
    /// 네이티브 리소스 정리
    /// </summary>
    private void OnDestroy()
    {
        _context?.Dispose();
        _model?.Dispose();
        Debug.Log("[SimpleAI] 리소스 정리 완료");
    }
}
