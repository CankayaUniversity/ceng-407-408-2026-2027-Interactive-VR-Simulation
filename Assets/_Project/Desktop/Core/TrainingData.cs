using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorkplaceStudio.Core
{
    // File coordinates stay right-handed/Y-up, as in the existing web editor.
    // Only Unity's presentation adapter mirrors X; saved data is engine-independent.
    [Serializable] public sealed class Pose
    {
        public float[] position = { 0, 1.65f, 4.5f };
        public float[] target = { 0, 1.65f, 0 };
        public float fovDegrees = 70;
    }
    [Serializable] public sealed class ModelTransform
    {
        public float[] position = { 0, 0, 0 };
        public float[] rotation = { 0, 0, 0 };
        public float[] scale = { 1, 1, 1 };
    }
    [Serializable] public sealed class Area
    {
        public string id, title, environmentId, parentZoneId;
        public int? order;
        public string calibration = "reviewed";
        public Pose camera = new Pose();
    }
    [Serializable] public sealed class Hotspot
    {
        public string id, title, description = "", zoneId, targetNode;
        public float[] position = { 0, 0, 0 };
        public float[] targetPosition;
    }
    [Serializable] public sealed class EditorDocument
    {
        public int schemaVersion = 1;
        public int? hierarchyVersion;
        public string environmentId, modelUrl;
        public ModelTransform modelTransform = new ModelTransform();
        public List<Area> zones = new List<Area>();
        public List<Hotspot> hotspots = new List<Hotspot>();
    }
    [Serializable] public sealed class Attribution
    {
        public string author, sourceUrl, license, licenseUrl;
    }
    [Serializable] public sealed class BedTraining
    {
        public string title, patientRequest, equipmentId;
        public int targetDegrees;
        public BedTraining Copy() => (BedTraining)MemberwiseClone();
    }
    [Serializable] public sealed class TrainingProject
    {
        public int formatVersion = 1;
        public string id, title, environmentId, modelFile, modelSha256;
        public string navigationMode = "walking";
        public Pose overview = new Pose();
        public EditorDocument editor = new EditorDocument();
        public Attribution attribution;
        public List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
        public List<BedTraining> bedTrainings = new List<BedTraining>();
        public List<BedTraining> bedExams = new List<BedTraining>();

        // Older projects used their training list for both menus. An explicit
        // empty exam list in a newer save means the author deleted every exam.
        [OnDeserializing] void BeforeRead(StreamingContext _) => bedExams = null;
        [OnDeserialized] void AfterRead(StreamingContext _) =>
            bedExams ??= bedTrainings.Select(lesson => lesson?.Copy()).ToList();
    }
    [Serializable] public sealed class Catalogue
    {
        public int formatVersion = 1;
        public string sourceCommit;
        public List<TrainingProject> environments = new List<TrainingProject>();
    }

    // Device adapters call the same operations; a future XR adapter does not own training state.
    public interface ITrainingActions
    {
        void OpenArea(string id);
        void ActivateHotspot(string id);
        void ReturnToOverview();
    }

    [Serializable] public sealed class PikoAiConfig
    {
        public const string DefaultModel = "gemini-3.5-flash";
        public const string FallbackModel = "gemini-3.5-flash-lite";
        public static readonly string[] SupportedModels = { DefaultModel, FallbackModel };

        public string apiKey = "";
        public string model = DefaultModel;

        public static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cengaverler", "WorkplaceStudio", "piko-ai.json");

        public static string NormalizeModel(string value, out bool migrated)
        {
            migrated = false;
            if (value == "gemini-2.5-flash") { migrated = true; return DefaultModel; }
            if (value == "gemini-2.5-flash-lite") { migrated = true; return FallbackModel; }
            if (string.IsNullOrWhiteSpace(value) || !SupportedModels.Contains(value)) return DefaultModel;
            return value;
        }

        public static PikoAiConfig Load()
        {
            var config = new PikoAiConfig();
            bool hadFile = false;
            try
            {
                var path = ConfigFilePath;
                if (File.Exists(path) && new FileInfo(path).Length <= 64 * 1024)
                {
                    var loaded = JsonConvert.DeserializeObject<PikoAiConfig>(File.ReadAllText(path, Encoding.UTF8));
                    if (loaded != null) { config = loaded; hadFile = true; }
                }
            }
            catch { }
            if (string.IsNullOrWhiteSpace(config.apiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrWhiteSpace(envKey)) envKey = Environment.GetEnvironmentVariable("PIKO_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey)) config.apiKey = envKey.Trim();
            }
            config.model = NormalizeModel(config.model, out bool migrated);
            if (hadFile && migrated)
            {
                try { config.Save(); } catch { }
            }
            return config;
        }

        public void Save()
        {
            var path = ConfigFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            model = NormalizeModel(model, out _);
            apiKey = apiKey?.Trim() ?? "";
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented), new UTF8Encoding(false));
        }
    }

    [Serializable] public sealed class PikoChatMessage
    {
        public string role;
        public string text;
    }

    public sealed class PikoLearningState
    {
        public const int DefaultRequiredHotspots = 5;
        public TrainingProject Project { get; private set; }
        public string ActiveAreaId { get; private set; }
        public string SelectedHotspotId { get; private set; }
        readonly HashSet<string> visitedHotspotIds = new HashSet<string>(StringComparer.Ordinal);

        public int TotalHotspots => Project?.editor?.hotspots?.Count ?? 0;
        public int RequiredHotspotsForQuiz => TotalHotspots <= 0
            ? DefaultRequiredHotspots
            : Math.Min(DefaultRequiredHotspots, TotalHotspots);
        public int VisitedHotspotCount => Project?.editor?.hotspots == null
            ? 0
            : Project.editor.hotspots.Count(h => visitedHotspotIds.Contains(h.id));
        public bool IsQuizUnlocked => TotalHotspots > 0 && VisitedHotspotCount >= RequiredHotspotsForQuiz;

        public void SetProject(TrainingProject project)
        {
            if (Project?.id != project?.id || project == null) visitedHotspotIds.Clear();
            Project = project;
            ActiveAreaId = null;
            SelectedHotspotId = null;
        }

        public void SetActiveArea(string areaId)
        {
            ActiveAreaId = areaId;
        }

        public bool RecordHotspotVisit(string hotspotId)
        {
            SelectedHotspotId = hotspotId;
            if (string.IsNullOrWhiteSpace(hotspotId) || Project?.editor?.hotspots == null) return false;
            if (!Project.editor.hotspots.Any(h => h.id == hotspotId)) return false;
            return visitedHotspotIds.Add(hotspotId);
        }

        public List<Hotspot> VisitedHotspots() => Project?.editor?.hotspots == null
            ? new List<Hotspot>()
            : Project.editor.hotspots.Where(h => visitedHotspotIds.Contains(h.id)).ToList();

        public List<Hotspot> UnvisitedHotspots() => Project?.editor?.hotspots == null
            ? new List<Hotspot>()
            : Project.editor.hotspots.Where(h => !visitedHotspotIds.Contains(h.id)).ToList();

        static readonly Random RandomGenerator = new Random();

        static readonly Dictionary<string, string[]> HotspotTurkishAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Patient bed socket"] = new[] { "hasta yatagi soketi", "yatak soketi", "hasta yatagi", "yatak prizi", "bed socket", "patient bed" },
            ["Anesthesiological tower"] = new[] { "anestezi kulesi", "anestezi cihazi", "anestezi", "anesthesiological tower", "anesthesia tower" },
            ["Main lock"] = new[] { "ana kilit", "giris kilidi", "hazirlik odasi", "ana giris", "main lock" },
            ["Room controls"] = new[] { "oda kontrolleri", "oda kontrol", "kontrol paneli", "isik kontrol", "room controls", "room control" },
            ["Disinfactant supplies"] = new[] { "dezenfektan malzemeleri", "dezenfektan", "sterilizasyon", "disinfactant", "disinfectant" },
            ["Main PC"] = new[] { "ana bilgisayar", "bilgisayar", "main pc" },
            ["Main display"] = new[] { "ana ekran", "buyuk ekran", "monitor", "main display", "ekran" },
            ["Patient air conditioner"] = new[] { "hasta klimasi", "hasta isiticisi", "klima", "air conditioner" },
            ["Anesthesiological supplies"] = new[] { "anestezi malzemeleri", "anestezi dolabi", "ilac dolabi", "malzeme dolabi", "anesthesiological supplies" },
            ["Cauter & Suction tower"] = new[] { "koter ve aspirasyon", "koter", "aspirasyon", "vakum kulesi", "cauter", "suction" }
        };

        static readonly string[] NavigationActionKeywords =
        {
            "gotur", "git", "gidelim", "odakla", "odaklan", "yaklas", "kamerayi", "go to", "take me", "focus"
        };

        static readonly string[] RandomKeywords =
        {
            "rastgele", "random", "herhangi bir", "baska bir hotspot", "baska bir nokta", "incelemedigim bir", "bakmadigim bir"
        };

        static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "beni", "bir", "hotspota", "hotspotuna", "hotspot", "noktasina", "noktaya", "nokta",
            "gotur", "git", "gidelim", "odakla", "odaklan", "goster", "bakalim", "lutfen", "piko", "alanina", "bolgesine"
        };

        public static string NormalizeForMatch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new StringBuilder(text.Length);
            foreach (char raw in text.ToLowerInvariant())
            {
                char c = raw switch
                {
                    'ı' or 'İ' => 'i',
                    'ğ' or 'Ğ' => 'g',
                    'ü' or 'Ü' => 'u',
                    'ş' or 'Ş' => 's',
                    'ö' or 'Ö' => 'o',
                    'ç' or 'Ç' => 'c',
                    _ => raw
                };
                if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
                else sb.Append(' ');
            }
            return string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public Hotspot PickRandomHotspot()
        {
            var spots = Project?.editor?.hotspots;
            if (spots == null || spots.Count == 0) return null;
            var pool = UnvisitedHotspots();
            if (pool.Count == 0)
            {
                pool = spots.Where(h => h.id != SelectedHotspotId).ToList();
                if (pool.Count == 0) pool = spots;
            }
            lock (RandomGenerator)
            {
                return pool[RandomGenerator.Next(pool.Count)];
            }
        }

        public bool TryMatchNavigationCommand(string userMessage, out Hotspot targetSpot, out bool isRandom)
        {
            targetSpot = null;
            isRandom = false;
            var spots = Project?.editor?.hotspots;
            if (spots == null || spots.Count == 0 || string.IsNullOrWhiteSpace(userMessage)) return false;

            string norm = NormalizeForMatch(userMessage);
            if (norm.Length == 0) return false;

            bool hasNavVerb = NavigationActionKeywords.Any(k => norm.Contains(k));
            bool hasRandomWord = RandomKeywords.Any(k => norm.Contains(k));

            if (hasRandomWord && (hasNavVerb || norm.Contains("hotspot") || norm.Contains("nokta")))
            {
                isRandom = true;
                targetSpot = PickRandomHotspot();
                return targetSpot != null;
            }

            if (!hasNavVerb) return false;

            targetSpot = MatchHotspotByText(norm, spots);
            return targetSpot != null;
        }

        static Hotspot MatchHotspotByText(string normalizedText, List<Hotspot> spots)
        {
            // 1. Direct normalized title or exact ID match
            Hotspot best = null;
            int bestLen = 0;
            foreach (var spot in spots)
            {
                string normTitle = NormalizeForMatch(spot.title);
                if (normTitle.Length > 0 && normalizedText.Contains(normTitle) && normTitle.Length > bestLen)
                {
                    best = spot;
                    bestLen = normTitle.Length;
                }
                if (!string.IsNullOrWhiteSpace(spot.id) && normalizedText.IndexOf(spot.id, StringComparison.OrdinalIgnoreCase) >= 0)
                    return spot;
            }
            if (best != null) return best;

            // 2. Turkish & English alias dictionary match (longest phrase wins)
            foreach (var spot in spots)
            {
                if (spot.title != null && HotspotTurkishAliases.TryGetValue(spot.title, out var aliases))
                {
                    foreach (var alias in aliases)
                    {
                        string normAlias = NormalizeForMatch(alias);
                        if (normAlias.Length > 0 && normalizedText.Contains(normAlias) && normAlias.Length > bestLen)
                        {
                            best = spot;
                            bestLen = normAlias.Length;
                        }
                    }
                }
            }
            if (best != null) return best;

            // 3. Token / prefix match against hotspot titles and aliases
            var queryTokens = normalizedText.Split(' ').Where(t => t.Length >= 2 && !StopWords.Contains(t)).ToList();
            if (queryTokens.Count == 0) return null;

            int bestScore = 0;
            foreach (var spot in spots)
            {
                int score = 0;
                var candidateWords = new List<string>(NormalizeForMatch(spot.title).Split(' '));
                if (spot.title != null && HotspotTurkishAliases.TryGetValue(spot.title, out var aliases))
                {
                    foreach (var a in aliases) candidateWords.AddRange(NormalizeForMatch(a).Split(' '));
                }
                foreach (var q in queryTokens)
                {
                    foreach (var cw in candidateWords)
                    {
                        if (cw.Length < 2) continue;
                        if (q == cw) score += 4;
                        else if ((q.Length >= 4 && cw.StartsWith(q, StringComparison.Ordinal)) ||
                                 (cw.Length >= 4 && q.StartsWith(cw, StringComparison.Ordinal))) score += 2;
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = spot;
                }
            }
            return bestScore >= 2 ? best : null;
        }

        public string ResolveHotspotId(string idOrTitle)
        {
            var spots = Project?.editor?.hotspots;
            if (spots == null || spots.Count == 0 || string.IsNullOrWhiteSpace(idOrTitle)) return null;
            string query = idOrTitle.Trim().Trim('\'', '"', '`');
            if (query.Equals("random", StringComparison.OrdinalIgnoreCase) || query.Equals("rastgele", StringComparison.OrdinalIgnoreCase))
                return PickRandomHotspot()?.id;

            var exactId = spots.Find(h => string.Equals(h.id, query, StringComparison.OrdinalIgnoreCase));
            if (exactId != null) return exactId.id;
            var exactTitle = spots.Find(h => string.Equals(h.title, query, StringComparison.OrdinalIgnoreCase));
            if (exactTitle != null) return exactTitle.id;
            var matched = MatchHotspotByText(NormalizeForMatch(query), spots);
            if (matched != null) return matched.id;
            var partial = spots.Find(h =>
                (h.id != null && h.id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (h.title != null && (h.title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     query.IndexOf(h.title, StringComparison.OrdinalIgnoreCase) >= 0)));
            return partial?.id;
        }
    }

    public static class PikoGeminiPayload
    {
        const string GotoTagPrefix = "[GOTO_HOTSPOT:";
        static readonly string[] QuizKeywords =
        {
            "quiz", "kuiz", "test et", "beni test", "soru sor", "sınav", "sinav", "beni sına", "beni sina", "test yap"
        };

        public static bool IsQuizIntent(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var lower = message.ToLowerInvariant();
            return QuizKeywords.Any(k => lower.Contains(k));
        }

        public static string ExtractGotoHotspot(string replyText, out string hotspotId)
        {
            hotspotId = null;
            if (string.IsNullOrWhiteSpace(replyText)) return "";
            string cleaned = replyText;
            int searchFrom = 0;
            while (searchFrom < cleaned.Length)
            {
                int start = cleaned.IndexOf(GotoTagPrefix, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;
                int end = cleaned.IndexOf(']', start + GotoTagPrefix.Length);
                if (end < 0) break;
                string value = cleaned.Substring(start + GotoTagPrefix.Length, end - (start + GotoTagPrefix.Length)).Trim();
                if (value.Length > 0) hotspotId = value;
                cleaned = cleaned.Remove(start, end - start + 1);
                searchFrom = start;
            }
            return cleaned.Trim();
        }

        public static string BuildSystemInstruction(PikoLearningState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sen 'Piko' adında, Workplace Studio 3B VR/Masaüstü eğitim simülasyonunun yapay zeka eğitim asistanısın.");
            sb.AppendLine("Görevin: Kullanıcının çalışma ortamındaki bölgeler ve ekipman bilgi noktaları (hotspot) hakkındaki sorularını yanıtlamak, istediğinde onu ilgili bilgi noktasına götürmek ve yeterli bilgi noktasını incelediğinde doğal sohbet tabanlı quiz yapmaktır.");
            sb.AppendLine("Dil ve Üslup: Kullanıcı Türkçe yazıyorsa akıcı ve samimi bir Türkçe ile, başka dilde yazıyorsa o dilde yanıt ver.");
            sb.AppendLine("Yanıt Uzunluğu: Cevaplarını gereksiz uzatma; en fazla 2-3 kısa ve net paragrafta tut ve cümleleri yarım bırakmadan tamamla.");
            sb.AppendLine();

            if (state == null || state.Project == null)
            {
                sb.AppendLine("MEVCUT DURUM: Kullanıcı şu anda ana menüde (Dashboard). Henüz bir 3B eğitim ortamı açmadı.");
                sb.AppendLine("Kullanıcıya 'Charité Hospital · Operating Room' (Ameliyathane) veya 'Boeing 737-800 Cockpit' (Kokpit) ortamlarından birini açarak ekipmanları inceleyebileceğini söyleyebilirsin.");
                sb.AppendLine("QUIZ DURUMU: KİLİTLİ (Henüz bir ortam açılmadı ve hiçbir bilgi noktası incelenmedi). Kullanıcı quiz isterse önce bir ortam açıp en az 5 bilgi noktasını incelemesi gerektiğini söyle.");
                return sb.ToString();
            }

            var project = state.Project;
            sb.AppendLine("AÇIK EĞİTİM ORTAMI: " + project.title + " (" + project.environmentId + ")");
            if (!string.IsNullOrWhiteSpace(state.ActiveAreaId))
            {
                var areaPath = string.Join(" / ", AreaHierarchy.Path(project.editor, state.ActiveAreaId).Select(a => a.title));
                if (!string.IsNullOrWhiteSpace(areaPath)) sb.AppendLine("AKTİF BÖLGE (Kamera Bakışı): " + areaPath);
            }
            if (!string.IsNullOrWhiteSpace(state.SelectedHotspotId))
            {
                var currentSpot = project.editor.hotspots.Find(h => h.id == state.SelectedHotspotId);
                if (currentSpot != null) sb.AppendLine($"ŞU AN SEÇİLİ BİLGİ NOKTASI: ID={currentSpot.id} | Başlık={currentSpot.title} — {currentSpot.description}");
            }

            var visited = state.VisitedHotspots();
            var unvisited = state.UnvisitedHotspots();
            int required = state.RequiredHotspotsForQuiz;

            sb.AppendLine($"KEŞİF İLERLEMESİ: Kullanıcı bu ortamda toplam {state.TotalHotspots} bilgi noktasından {visited.Count} tanesini inceledi (Quiz için gereken minimum: {required}).");
            sb.AppendLine();

            if (visited.Count > 0)
            {
                sb.AppendLine("KULLANICININ ŞU ANA KADAR İNCELEDİĞİ BİLGİ NOKTALARI (ID, Başlık ve Açıklama — Quiz soruları için kaynak):");
                foreach (var spot in visited)
                {
                    var areaName = project.editor.zones.Find(a => a.id == spot.zoneId)?.title ?? "Genel";
                    sb.AppendLine($"- ID: {spot.id} | Başlık: {spot.title} | Bölge: {areaName} | Açıklama: {spot.description}");
                }
                sb.AppendLine();
            }

            if (unvisited.Count > 0)
            {
                sb.AppendLine("KULLANICININ HENÜZ İNCELEMEDİĞİ BİLGİ NOKTALARI (ID, Başlık ve Açıklama):");
                foreach (var spot in unvisited)
                {
                    var areaName = project.editor.zones.Find(a => a.id == spot.zoneId)?.title ?? "Genel";
                    sb.AppendLine($"- ID: {spot.id} | Başlık: {spot.title} | Bölge: {areaName} | Açıklama: {spot.description}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("KESİN KURALLAR:");
            sb.AppendLine("1. GENEL SORU-CEVAP: Kullanıcı ortamdaki herhangi bir ekipman, bölge veya prosedür hakkında soru sorarsa yukarıdaki ortam bilgilerine dayanarak 2-3 kısa paragrafta doğru ve net şekilde yanıtla.");
            sb.AppendLine("2. HOTSPOT'A GÖTÜRME (KAMERA YÖNLENDİRME): Kullanıcı 'beni rastgele bir hotspota götür', 'beni başka bir noktaya götür' veya 'beni [hotspot adı / Türkçe karşılığı / kısmi adı] noktasına götür' derse, yukarıdaki listeden en uygun hotspot'u eşleştir (rastgele istendiyse öncelikle henüz incelenmemiş bir hotspot seç), kullanıcıya o noktaya götürdüğünü kısaca söyle ve cevabının EN SONUNA tam olarak [GOTO_HOTSPOT:ilgili_hotspot_id] etiketini ekle.");
            if (!state.IsQuizUnlocked)
            {
                sb.AppendLine($"3. QUIZ KİLİTLİ (visited={visited.Count} < required={required}): Kullanıcı senden quiz yapmanı, soru sormanı veya kendisini test etmeni isterse KESİNLİKLE quiz başlatma ve soru sorma! Şu ana kadar {visited.Count} bilgi noktasına baktığını, quiz yapabilmen için en az {required} bilgi noktasını inceleyip biraz daha öğrenmesi gerektiğini söyle ve henüz bakmadığı bilgi noktalarından 2-3 tanesini öner.");
            }
            else
            {
                sb.AppendLine($"3. QUIZ AÇIK (visited={visited.Count} >= required={required}): Kullanıcı yazarak quiz/test/soru isterse doğal sohbet akışında 3 soruluk (kullanıcı farklı bir soru sayısı istemediyse 3 soruluk) bir quiz başlat.");
                sb.AppendLine("4. QUIZ İÇERİĞİ: Quiz sorularını YALNIZCA 'KULLANICININ ŞU ANA KADAR İNCELEDİĞİ BİLGİ NOKTALARI' listesindeki konulardan ve önceki sohbet geçmişinizde konuştuğunuz bilgilerden üret. Henüz incelemediği noktalardan soru sorma.");
                sb.AppendLine("5. TEK TEK SORU AKIŞI: Tüm soruları tek mesajda alt alta yazma! Her mesajda yalnızca 1 soru sor (örneğin 'Soru 1/3:' şeklinde, A/B/C/D şıklı veya kısa cevaplı) ve kullanıcının cevabını bekle. Kullanıcı cevap verdikçe doğru/yanlış olduğunu kısaca açıkla ve sıradaki soruyu ('Soru 2/3:', 'Soru 3/3:') sor.");
                sb.AppendLine("6. QUIZ SONU SKOR VE EKSİK ÖZETİ: Son soru cevaplandığında kullanıcıya kısa bir skor özeti ver (örneğin 'Skorun: 3/3' veya 'Skorun: 2/3'). Yanlış yaptığı veya eksik kaldığı noktaları kısaca hatırlatıp doğrusunu özetle; ayrıca sahnede henüz bakmadığı bilgi noktaları varsa onları da keşfetmesini öner.");
            }
            return sb.ToString();
        }

        public static string BuildRequestJson(PikoLearningState state, IReadOnlyList<PikoChatMessage> history)
        {
            var contents = new JArray();
            if (history != null)
            {
                for (int i = 0; i < history.Count; i++)
                {
                    var item = history[i];
                    if (item == null || string.IsNullOrWhiteSpace(item.text)) continue;
                    string role = item.role == "model" ? "model" : "user";
                    if (contents.Count == 0 && role != "user") continue;
                    string text = item.text;
                    if (i == history.Count - 1 && role == "user" && IsQuizIntent(text) && (state == null || !state.IsQuizUnlocked))
                    {
                        int visitedCount = state?.VisitedHotspotCount ?? 0;
                        int requiredCount = state?.RequiredHotspotsForQuiz ?? PikoLearningState.DefaultRequiredHotspots;
                        text += $"\n\n[Sistem Notu: Kullanıcı quiz istedi ancak şu ana kadar yalnızca {visitedCount}/{requiredCount} bilgi noktasını inceledi. Quiz kilidi AÇIK DEĞİL. Kesinlikle quiz sorusu sorma; biraz daha bilgi noktası incelemesi gerektiğini belirt ve henüz bakmadığı noktalardan öner.]";
                    }
                    contents.Add(new JObject
                    {
                        ["role"] = role,
                        ["parts"] = new JArray { new JObject { ["text"] = text } }
                    });
                }
            }

            var payload = new JObject
            {
                ["system_instruction"] = new JObject
                {
                    ["parts"] = new JArray { new JObject { ["text"] = BuildSystemInstruction(state) } }
                },
                ["contents"] = contents,
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = 0.4,
                    ["maxOutputTokens"] = 2048
                }
            };
            return payload.ToString(Formatting.None);
        }

        public static string ReadErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try { return JObject.Parse(body)["error"]?["message"]?.Value<string>(); }
            catch (JsonException) { return null; }
        }

        public static string ParseResponseText(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson)) throw new InvalidDataException("Gemini API boş yanıt döndürdü.");
            var root = JObject.Parse(responseJson);
            var errorMessage = root["error"]?["message"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(errorMessage)) throw new InvalidDataException(errorMessage);

            var candidates = root["candidates"] as JArray;
            if (candidates == null || candidates.Count == 0)
                throw new InvalidDataException("Gemini yanıtında aday metin bulunamadı.");

            var parts = candidates[0]?["content"]?["parts"] as JArray;
            if (parts == null || parts.Count == 0)
                throw new InvalidDataException("Gemini yanıt içeriği boş.");

            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part?["thought"]?.Value<bool>() == true) continue;
                var text = part?["text"]?.Value<string>();
                if (!string.IsNullOrEmpty(text)) sb.Append(text);
            }
            var result = sb.ToString().Trim();
            if (result.Length == 0) throw new InvalidDataException("Gemini metin üretmedi.");
            return result;
        }
    }
}

