using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using UnityEngine;

public class MergeAi : MonoBehaviour
{
    public static MergeAi Instance { get; private set; }

    public enum CraftLanguage
    {
        Korean,  // 한국어 출력 (기본값)
        English, // 영어 출력
        Auto     // 입력 단어 언어에 맞춤 (한글 포함 시 한국어)
    }

    [Header("모델 설정")]
    [SerializeField] private string modelFileName = "gemma-4-E2B-it-qat-UD-Q4_K_XL.gguf";
    [Tooltip("KV 캐시 크기. NoKvSlot 에러 방지를 위해 최소 1024 이상")]
    [SerializeField] private uint contextSize = 1024;
    [SerializeField] private float temperature = 0.3f;

    [Header("출력 설정")]
    [Tooltip("합성 결과 언어 설정 (한국어 / 영어 / 자동 감지)")]
    [SerializeField] private CraftLanguage outputLanguage = CraftLanguage.Korean;

    [Tooltip("결과 텍스트에 이모지를 함께 포함할지 여부 (예: '💨 증기' vs '증기')")]
    [SerializeField] private bool includeEmoji = true;

    private LLamaWeights model;
    private StatelessExecutor executor;
    private ModelParams parameters;

    public bool IsModelLoaded { get; private set; } = false;

    // JSON 응답 매핑용 DTO
    [Serializable]
    private class CraftResultJson
    {
        public string result;
        public string emoji;
    }

    // 외부에서도 구조화된 결과로 접근할 수 있는 데이터 클래스
    public class CraftResult
    {
        public string ResultName { get; set; }
        public string Emoji { get; set; }
        public string FormattedText => string.IsNullOrEmpty(Emoji) ? ResultName : $"{Emoji} {ResultName}";

        public override string ToString() => FormattedText;
    }

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
        string trimmedFileName = modelFileName.Trim();
        string modelPath = Path.Combine(Application.streamingAssetsPath, trimmedFileName);

        if (!File.Exists(modelPath))
        {
            string[] ggufFiles = Directory.GetFiles(Application.streamingAssetsPath, "*.gguf");
            if (ggufFiles.Length > 0)
            {
                modelPath = ggufFiles[0];
                Debug.LogWarning($"[MergeAi] 지정된 모델 파일({trimmedFileName})을 찾지 못해 대체 파일을 로드합니다: {Path.GetFileName(modelPath)}");
            }
            else
            {
                Debug.LogError($"[MergeAi] 모델 파일을 찾을 수 없습니다: {modelPath}");
                return;
            }
        }

        Debug.Log($"[MergeAi] 모델 로딩 시작: {Path.GetFileName(modelPath)}");

        int threadCount = Mathf.Max(1, Environment.ProcessorCount - 1);
        // 인스펙터에 작은 값(512 등)이 저장되어 있어도 NoKvSlot 에러를 방지하는 최소값 보장
        uint effectiveContextSize = Math.Max(contextSize, 1024u);

        await Task.Run(() =>
        {
            parameters = new ModelParams(modelPath)
            {
                ContextSize = effectiveContextSize,
                BatchSize = 512, // 안전한 배치 크기
                GpuLayerCount = 0, // 순수 CPU 구동
                Threads = threadCount
            };

            model = LLamaWeights.LoadFromFile(parameters);
            executor = new StatelessExecutor(model, parameters);
        });

        IsModelLoaded = true;
        Debug.Log($"[MergeAi] 모델 로딩 완료! (ContextSize: {effectiveContextSize})");
    }

    /// <summary>
    /// 단어 합성 요청 함수 (UI와 연동)
    /// </summary>
    public async Task<string> MergeIngredientsAsync(string[] ingredients)
    {
        CraftResult craftResult = await MergeIngredientsDetailedAsync(ingredients);
        if (craftResult == null)
        {
            return "???";
        }

        return includeEmoji ? craftResult.FormattedText : craftResult.ResultName;
    }

    /// <summary>
    /// 단어 및 이모지가 분리된 상세 객체로 합성 결과를 반환하는 함수
    /// </summary>
    public async Task<CraftResult> MergeIngredientsDetailedAsync(string[] ingredients)
    {
        if (!IsModelLoaded)
        {
            Debug.LogWarning("[MergeAi] 모델이 아직 로드되지 않았습니다.");
            return new CraftResult { ResultName = "로딩 중...", Emoji = "⏳" };
        }

        if (ingredients == null || ingredients.Length == 0)
        {
            return new CraftResult { ResultName = "미확인", Emoji = "❓" };
        }

        // 1. 입력 전처리: 재료 텍스트에 포함된 기존 이모지/특수기호 제거
        List<string> cleanIngredients = ingredients
            .Select(i => CleanIngredientInput(i))
            .Where(i => !string.IsNullOrEmpty(i))
            .ToList();

        if (cleanIngredients.Count < 2)
        {
            string single = cleanIngredients.FirstOrDefault() ?? "물질";
            return new CraftResult { ResultName = single, Emoji = "✨" };
        }

        // 출력 언어 판별 (설정값 또는 재료들에 한글 포함 여부 감지)
        bool useKorean = DetermineIfKorean(cleanIngredients);

        // 2. 다중 재료(N개) 지원 경량화 프롬프트 빌드 (Gemma / Phi 등 모델 템플릿 호환)
        string prompt = BuildPrompt(cleanIngredients, useKorean);

        var antiPrompts = new List<string> { "<end_of_turn>", "<start_of_turn>", "<|end|>", "<|user|>", "\n\n", "}" };

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 50, // 짧은 JSON 1개 생성을 위한 최적 토큰 수
            AntiPrompts = antiPrompts,
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Temperature = temperature
            }
        };

        StringBuilder resultBuilder = new StringBuilder();

        // 3. 백그라운드 추론
        await Task.Run(async () =>
        {
            await foreach (var token in executor.InferAsync(prompt, inferenceParams))
            {
                resultBuilder.Append(token);
                // 닫는 중괄호가 나오면 즉시 정지하여 속도 최적화
                if (token.Contains("}"))
                {
                    break;
                }
            }
        });

        string rawResponse = resultBuilder.ToString().Trim();
        Debug.Log($"[MergeAi] 원시 응답: {rawResponse}");

        // 4. JSON 파싱 및 후처리 정규화 필터 적용
        CraftResult finalResult = ParseAndNormalizeResult(rawResponse, cleanIngredients, useKorean);

        string combinedNames = string.Join(" + ", cleanIngredients);
        Debug.Log($"[MergeAi] 최종 합성 결과: {combinedNames} -> {finalResult.FormattedText}");
        return finalResult;
    }

    /// <summary>
    /// 출력 언어 결정 (설정 또는 모든 재료 중 한글 포함 여부 감지)
    /// </summary>
    private bool DetermineIfKorean(List<string> ingredients)
    {
        if (outputLanguage == CraftLanguage.Korean) return true;
        if (outputLanguage == CraftLanguage.English) return false;

        // Auto 모드: 입력 단어들 중 하나라도 한글이 포함되어 있으면 한국어로 처리
        return ingredients.Any(i => Regex.IsMatch(i, @"[가-힣]"));
    }

    /// <summary>
    /// 토큰을 대폭 절약한 경량화 프롬프트 생성 (2개 및 3개 이상의 다중 재료 지원)
    /// </summary>
    private string BuildPrompt(List<string> ingredients, bool isKorean)
    {
        string instructions;
        string fewShots;

        if (isKorean)
        {
            instructions =
                "당신은 인피니트 크래프트 스타일의 연금술 조합 AI입니다.\n" +
                "주어진 모든 재료들을 하나로 융합하여 가장 직관적이고 어울리는 하나의 '한국어 명사' 결과를 도출하세요.\n" +
                "규칙:\n" +
                "- 반드시 단일 JSON 포맷으로 출력: {\"result\": \"결과명사\", \"emoji\": \"이모지\"}\n" +
                "- 'result'는 반드시 1~3단어의 자연스러운 한국어 명사여야 합니다. (서술어, 문장, 영어 금지)\n" +
                "- 단순히 재료 단어들을 그대로 이어붙이지 마세요\n" +
                "- 'emoji'는 결과에 어울리는 유니코드 이모지 딱 1개여야 합니다.";

            fewShots =
                "재료: 물 + 불 -> {\"result\": \"증기\", \"emoji\": \"💨\"}\n" +
                "재료: 흙 + 불 -> {\"result\": \"용암\", \"emoji\": \"🌋\"}\n" +
                "재료: 나무 + 불 -> {\"result\": \"숯\", \"emoji\": \"🪵\"}\n" +
                "재료: 물 + 불 + 흙 -> {\"result\": \"온천\", \"emoji\": \"♨️\"}\n" +
                "재료: 사람 + 바다 -> {\"result\": \"인어\", \"emoji\": \"🧜\"}\n" +
                "재료: 사람 + 검 + 방패 -> {\"result\": \"기사\", \"emoji\": \"🛡️\"}\n" +
                "재료: 배트맨 + 자동차 -> {\"result\": \"배트모빌\", \"emoji\": \"🦇\"}";
        }
        else
        {
            instructions =
                "You are the crafting engine for an Infinite Craft style game.\n" +
                "Merge all given ingredients into one intuitive, witty Title Case noun.\n" +
                "Rules:\n" +
                "- Output MUST be exactly one JSON: {\"result\": \"Title Case Noun\", \"emoji\": \"Emoji\"}\n" +
                "- 'result' must be 1 to 3 words maximum. No sentences, verbs, or adjectives.\n" +
                "- Do NOT simply concatenate words.\n" +
                "- 'emoji' must be exactly ONE standard Unicode emoji.";

            fewShots =
                "Items: Water + Fire -> {\"result\": \"Steam\", \"emoji\": \"💨\"}\n" +
                "Items: Earth + Water -> {\"result\": \"Plant\", \"emoji\": \"🌱\"}\n" +
                "Items: Water + Fire + Earth -> {\"result\": \"Geyser\", \"emoji\": \"♨️\"}\n" +
                "Items: Human + Sword + Shield -> {\"result\": \"Knight\", \"emoji\": \"🛡️\"}\n" +
                "Items: Batman + Car -> {\"result\": \"Batmobile\", \"emoji\": \"🦇\"}";
        }

        string joinedIngredients = string.Join(" + ", ingredients);
        string currentInput = isKorean
            ? $"재료: {joinedIngredients} ->"
            : $"Items: {joinedIngredients} ->";

        bool isGemma = modelFileName.IndexOf("gemma", StringComparison.OrdinalIgnoreCase) >= 0;

        if (isGemma)
        {
            return $"<start_of_turn>user\n{instructions}\n\n예시:\n{fewShots}\n\n{currentInput}<end_of_turn>\n<start_of_turn>model\n";
        }
        else
        {
            return $"<|system|>\n{instructions}\n\nExamples:\n{fewShots}<|end|>\n<|user|>\n{currentInput}<|end|>\n<|assistant|>\n";
        }
    }

    /// <summary>
    /// 입력 전처리: 아이템 이름에 붙어있는 이모지나 불필요한 특수문자 제거
    /// </summary>
    private string CleanIngredientInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 이모지 및 특수 기호 제거 (한글, 영문, 숫자, 공백만 보존)
        string cleaned = Regex.Replace(input, @"[^\w\s가-힣a-zA-Z0-9]", "").Trim();
        return cleaned;
    }

    /// <summary>
    /// 후처리(Post-Processing) 정규화 필터
    /// </summary>
    private CraftResult ParseAndNormalizeResult(string rawText, List<string> ingredients, bool isKorean)
    {
        string extractedName = null;
        string extractedEmoji = null;

        // 1단계: JSON 블록 { ... } 추출
        Match jsonMatch = Regex.Match(rawText, @"\{[\s\S]*?\}");
        string jsonCandidate = jsonMatch.Success ? jsonMatch.Value : (rawText.StartsWith("{") ? rawText + "}" : null);

        if (!string.IsNullOrEmpty(jsonCandidate))
        {
            try
            {
                CraftResultJson parsed = JsonUtility.FromJson<CraftResultJson>(jsonCandidate);
                if (parsed != null)
                {
                    extractedName = parsed.result;
                    extractedEmoji = parsed.emoji;
                }
            }
            catch { }
        }

        // 2단계: Regex Fallback (JSON 키-값 직접 추출)
        if (string.IsNullOrEmpty(extractedName))
        {
            Match nameMatch = Regex.Match(rawText, @"""result""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
            if (nameMatch.Success) extractedName = nameMatch.Groups[1].Value;

            Match emojiMatch = Regex.Match(rawText, @"""emoji""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
            if (emojiMatch.Success) extractedEmoji = emojiMatch.Groups[1].Value;
        }

        // 3단계: JSON이 완전히 깨졌을 경우 텍스트 분리
        if (string.IsNullOrEmpty(extractedName))
        {
            string fallback = rawText.Split(new[] { '\n', '}', '{', ':', '"', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .FirstOrDefault() ?? string.Concat(ingredients);
            extractedName = fallback;
        }

        // 4단계: 단어 이름 정규화
        string cleanedName = NormalizeCraftName(extractedName, ingredients, isKorean);

        // 5단계: 이모지 정규화 (단일 글리프 추출)
        string cleanedEmoji = NormalizeSingleEmoji(extractedEmoji);

        return new CraftResult
        {
            ResultName = cleanedName,
            Emoji = cleanedEmoji
        };
    }

    /// <summary>
    /// Infinite Craft 단어 정규화 필터
    /// </summary>
    private string NormalizeCraftName(string name, List<string> ingredients, bool isKorean)
    {
        if (string.IsNullOrWhiteSpace(name)) return isKorean ? "새로운 물질" : "Unknown";

        // 1. 불필요한 따옴표, 구두점, 괄호, 백틱 제거
        string cleaned = name.Trim().Trim('"', '\'', '`', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']');

        // 2. 영어 관사 접두사 (A, An, The) 제거
        cleaned = Regex.Replace(cleaned, @"^(a|an|the)\s+", "", RegexOptions.IgnoreCase).Trim();

        // 3. 단순 이어붙이기 방지 (예: 재료들을 단순히 다 붙인 경우)
        string concatenated = string.Concat(ingredients);
        string joinedSpace = string.Join(" ", ingredients);
        if (cleaned.Equals(concatenated, StringComparison.OrdinalIgnoreCase) ||
            cleaned.Equals(joinedSpace, StringComparison.OrdinalIgnoreCase))
        {
            string first = ingredients.FirstOrDefault() ?? "물질";
            cleaned = isKorean ? $"{first} 원소" : $"{first} Element";
        }

        // 4. 영문일 경우 Title Case 적용
        if (!isKorean && Regex.IsMatch(cleaned, @"^[a-zA-Z\s]+$"))
        {
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            cleaned = textInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }

        // 5. 단어 수 제약 (공백 기준 최대 3단어로 제한)
        string[] words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 3)
        {
            cleaned = string.Join(" ", words.Take(3));
        }

        return string.IsNullOrEmpty(cleaned) ? (isKorean ? "새로운 발견" : "Discovery") : cleaned;
    }

    /// <summary>
    /// 단일 유니코드 이모지 글리프 추출 필터
    /// </summary>
    private string NormalizeSingleEmoji(string emojiText)
    {
        if (string.IsNullOrWhiteSpace(emojiText))
        {
            return "✨";
        }

        string trimmed = emojiText.Trim().Trim('"', '\'', '`');

        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(trimmed);
        if (enumerator.MoveNext())
        {
            string firstElement = enumerator.GetTextElement();
            if (!Regex.IsMatch(firstElement, @"^[a-zA-Z0-9가-힣\s]$"))
            {
                return firstElement;
            }
        }

        return "✨";
    }

    // 메모리 해제
    private void OnDestroy()
    {
        model?.Dispose();
    }
}
