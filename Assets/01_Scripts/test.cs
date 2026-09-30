using System.IO;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    [SerializeField] private string _modelPath = "model.gguf";
    private LLamaWeights _model;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    private ChatSession _session;

    private async void Start()
    {
        // 1. 모델 파일 위치 찾기
        string path = Path.Combine(Application.streamingAssetsPath, _modelPath);

        if (!File.Exists(path))
        {
            Debug.LogError("model.gguf 파일이 Assets/StreamingAssets 폴더에 없습니다!");
            return;
        }

        Debug.Log("AI 로딩 중... 잠시만 기다려주세요.");

        // 2. AI 뇌 켜기
        await Task.Run(() =>
        {
            var parameters = new ModelParams(path) { ContextSize = 1024, GpuLayerCount = 0 };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _session = new ChatSession(_executor);
        });

        Debug.Log("AI 로딩 완료! 이제 질문을 던집니다.");

        // 3. 딱 한 마디 질문 던져보기
        var options = new InferenceParams()
        {
            MaxTokens = 64, // 짧게 1~2문장만 답하도록 제한
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.6f }
        };

        // 4. 답변 받아서 콘솔에 찍기
        await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, "안녕! 너는 누구야?"), options))
        {
            Debug.Log($"AI의 한마디: {text}");
        }
    }

    private void OnDestroy()
    {
        _context?.Dispose();
        _model?.Dispose();
    }
}
