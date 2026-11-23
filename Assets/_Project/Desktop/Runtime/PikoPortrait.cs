using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public sealed class PikoPortrait : MonoBehaviour
    {
        const string ExpectedHash = "fd2338c12af77d767f769fdf0c06beba1b3279df78ceb6c36dbda2c5f82383ac";
        VisualElement container, portrait, chatPanel;
        ScrollView messagesScroll;
        TextField inputField;
        Button chatToggle, sendButton;
        Label progressBadge;
        Camera portraitCamera;
        GameObject modelRoot, cameraRoot;
        GltfImport importer;
        RenderTexture texture;
        Animation animationPlayer;
        CancellationTokenSource cancellation, requestCancellation;
        Action<string> statusMessage;
        Action releasePointer;
        Action<string> navigateHotspot;
        PikoAiConfig config;
        readonly PikoLearningState learningState = new PikoLearningState();
        readonly List<PikoChatMessage> history = new List<PikoChatMessage>();
        bool waving, minimized, chatOpen, sending;

        public void Initialize(VisualElement anchor, Action<string> message, Action onReleasePointer = null, Action<string> onNavigateHotspot = null)
        {
            statusMessage = message;
            releasePointer = onReleasePointer;
            navigateHotspot = onNavigateHotspot;
            config = PikoAiConfig.Load();

            container = new VisualElement(); container.AddToClassList("piko");
            container.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            BuildChatPanel();
            container.Add(chatPanel);

            var avatarColumn = new VisualElement(); avatarColumn.AddToClassList("piko-avatar");
            var button = new Button(() =>
            {
                Wave();
                if (!chatOpen) ToggleChat(true);
            }) { tooltip = "Piko ile konuş / El salla" };
            button.AddToClassList("piko-button");
            portrait = button; avatarColumn.Add(button);

            var controls = new VisualElement(); controls.AddToClassList("piko-controls");
            controls.Add(new Label("Piko"));
            chatToggle = new Button(() => ToggleChat(!chatOpen)) { text = "Chat", tooltip = "Piko AI sohbetini aç/kapat" };
            controls.Add(chatToggle);
            var minimize = new Button(() =>
            {
                minimized = !minimized;
                portrait.style.display = minimized ? DisplayStyle.None : DisplayStyle.Flex;
                if (portraitCamera != null) portraitCamera.enabled = !minimized;
                if (animationPlayer != null) { if (minimized) animationPlayer.Stop(); else Idle(); }
            }) { text = "− / +", tooltip = "Piko portresini küçült / göster" };
            controls.Add(minimize);
            avatarColumn.Add(controls);
            container.Add(avatarColumn);
            anchor.Add(container);

            UpdateProgressBadge();
            cancellation = new CancellationTokenSource();
            _ = Load(button, message, cancellation.Token);
        }

        void BuildChatPanel()
        {
            chatPanel = new VisualElement();
            chatPanel.AddToClassList("piko-chat-panel");
            chatPanel.style.display = DisplayStyle.None;

            var header = new VisualElement(); header.AddToClassList("piko-chat-header");
            var titleGroup = new VisualElement(); titleGroup.AddToClassList("piko-chat-title-group");
            var title = new Label("Piko AI Asistan"); title.AddToClassList("piko-chat-title");
            progressBadge = new Label("Dashboard"); progressBadge.AddToClassList("piko-progress-badge");
            titleGroup.Add(title); titleGroup.Add(progressBadge);
            header.Add(titleGroup);

            var clearBtn = new Button(ResetConversation) { text = "Temizle", tooltip = "Konuşma geçmişini sıfırla" };
            clearBtn.AddToClassList("piko-header-btn");
            var closeBtn = new Button(() => ToggleChat(false)) { text = "×", tooltip = "Sohbeti kapat" };
            closeBtn.AddToClassList("piko-header-btn");
            header.Add(clearBtn); header.Add(closeBtn);
            chatPanel.Add(header);

            messagesScroll = new ScrollView(); messagesScroll.AddToClassList("piko-messages");
            chatPanel.Add(messagesScroll);

            var inputRow = new VisualElement(); inputRow.AddToClassList("piko-input-row");
            inputField = new TextField { value = "" };
            inputField.AddToClassList("piko-input");
            inputField.tooltip = "Sorunuzu yazın veya 'bana quiz yap' diyerek kendinizi test edin";
            inputField.RegisterCallback<FocusInEvent>(_ => releasePointer?.Invoke());
            inputField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !evt.shiftKey)
                {
                    evt.StopPropagation();
                    SendFromInput();
                }
            }, TrickleDown.TrickleDown);
            sendButton = new Button(SendFromInput) { text = "Gönder" };
            sendButton.AddToClassList("primary"); sendButton.AddToClassList("piko-send");
            inputRow.Add(inputField); inputRow.Add(sendButton);
            chatPanel.Add(inputRow);

            AddWelcomeMessage();
        }

        void AddWelcomeMessage()
        {
            AddBubble("assistant", "Merhaba! Ben Piko. Ortamdaki bölgeler ve ekipmanlar hakkında bana soru sorabilirsin.");
        }

        void ResetConversation()
        {
            requestCancellation?.Cancel();
            sending = false;
            sendButton?.SetEnabled(true);
            history.Clear();
            messagesScroll.Clear();
            AddWelcomeMessage();
            statusMessage?.Invoke("Piko sohbet geçmişi temizlendi.");
        }

        void ToggleChat(bool open)
        {
            chatOpen = open;
            if (chatPanel != null) chatPanel.style.display = chatOpen ? DisplayStyle.Flex : DisplayStyle.None;
            chatToggle?.EnableInClassList("selected", chatOpen);
            if (chatOpen)
            {
                releasePointer?.Invoke();
                inputField?.Focus();
            }
        }

        public void SetProject(TrainingProject project)
        {
            learningState.SetProject(project);
            UpdateProgressBadge();
        }

        public void SetActiveArea(string areaId)
        {
            learningState.SetActiveArea(areaId);
            UpdateProgressBadge();
        }

        public void NotifyHotspotVisited(string hotspotId)
        {
            bool newlyVisited = learningState.RecordHotspotVisit(hotspotId);
            UpdateProgressBadge();
            if (newlyVisited && learningState.VisitedHotspotCount == learningState.RequiredHotspotsForQuiz)
            {
                AddBubble("assistant", "Tebrikler! 5 hotspotu inceledin, istersen bu hotspotlarla ilgili sana quiz yapabilirim.");
                Wave();
            }
        }

        void UpdateProgressBadge()
        {
            if (progressBadge == null) return;
            if (learningState.Project == null)
            {
                progressBadge.text = "Ana Menü (Dashboard)";
                progressBadge.EnableInClassList("ready", false);
                return;
            }
            int visited = learningState.VisitedHotspotCount;
            int required = learningState.RequiredHotspotsForQuiz;
            int total = learningState.TotalHotspots;
            bool unlocked = learningState.IsQuizUnlocked;
            progressBadge.text = unlocked
                ? $"Keşif: {visited}/{total} nokta · Quiz Hazır ✓"
                : $"Keşif: {visited}/{required} nokta (Quiz için {Math.Max(0, required - visited)} kaldı)";
            progressBadge.EnableInClassList("ready", unlocked);
        }

        Label AddBubble(string type, string text)
        {
            var bubble = new Label(text);
            bubble.AddToClassList("piko-msg");
            bubble.AddToClassList("piko-msg-" + type);
            messagesScroll.Add(bubble);
            messagesScroll.schedule.Execute(() =>
            {
                if (messagesScroll.verticalScroller != null)
                    messagesScroll.verticalScroller.value = messagesScroll.verticalScroller.highValue;
            }).ExecuteLater(25);
            return bubble;
        }

        void SendFromInput()
        {
            if (sending || inputField == null) return;
            string userText = inputField.value?.Trim() ?? "";
            if (userText.Length == 0) return;
            inputField.value = "";
            _ = ProcessUserMessageAsync(userText);
        }

        async Task ProcessUserMessageAsync(string userText)
        {
            AddBubble("user", userText);
            history.Add(new PikoChatMessage { role = "user", text = userText });

            // Deterministic 5-hotspot check when the user asks for a quiz before exploring enough points.
            if (PikoGeminiPayload.IsQuizIntent(userText) && !learningState.IsQuizUnlocked)
            {
                string lockReply = BuildQuizLockedReply();
                history.Add(new PikoChatMessage { role = "model", text = lockReply });
                AddBubble("assistant", lockReply);
                Wave();
                return;
            }

            // Direct C# hotspot navigation matching (random or title/alias match without calling Gemini).
            if (learningState.TryMatchNavigationCommand(userText, out var matchedSpot, out bool isRandomSpot) && matchedSpot != null)
            {
                string navReply = isRandomSpot
                    ? $"Seni rastgele seçtiğim '{matchedSpot.title}' bilgi noktasına götürdüm. {matchedSpot.description}"
                    : $"Seni '{matchedSpot.title}' bilgi noktasına götürdüm. {matchedSpot.description}";
                history.Add(new PikoChatMessage { role = "model", text = navReply });
                AddBubble("assistant", navReply);
                NotifyHotspotVisited(matchedSpot.id);
                navigateHotspot?.Invoke(matchedSpot.id);
                Wave();
                return;
            }

            if (string.IsNullOrWhiteSpace(config.apiKey))
            {
                config = PikoAiConfig.Load();
                if (string.IsNullOrWhiteSpace(config.apiKey))
                {
                    AddBubble("system", "Piko AI servisi için API anahtarı bulunamadı (piko-ai.json veya GEMINI_API_KEY tanımlı değil).");
                    return;
                }
            }

            sending = true;
            sendButton?.SetEnabled(false);
            var thinkingBubble = AddBubble("assistant", "Piko düşünüyor…");

            requestCancellation?.Cancel();
            requestCancellation?.Dispose();
            requestCancellation = new CancellationTokenSource();
            var token = requestCancellation.Token;

            try
            {
                string payloadJson = PikoGeminiPayload.BuildRequestJson(learningState, history);
                string rawReply = await SendGeminiWithFallbackAsync(payloadJson, token);
                if (token.IsCancellationRequested) return;
                string cleanReply = PikoGeminiPayload.ExtractGotoHotspot(rawReply, out string gotoTarget);
                if (string.IsNullOrWhiteSpace(cleanReply)) cleanReply = "Seni ilgili bilgi noktasına yönlendiriyorum.";
                thinkingBubble.text = cleanReply;
                history.Add(new PikoChatMessage { role = "model", text = cleanReply });
                Wave();

                if (!string.IsNullOrWhiteSpace(gotoTarget))
                {
                    string resolvedHotspotId = learningState.ResolveHotspotId(gotoTarget);
                    if (!string.IsNullOrWhiteSpace(resolvedHotspotId))
                    {
                        NotifyHotspotVisited(resolvedHotspotId);
                        navigateHotspot?.Invoke(resolvedHotspotId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (thinkingBubble.parent == messagesScroll) messagesScroll.Remove(thinkingBubble);
            }
            catch (Exception error)
            {
                if (history.Count > 0 && history[history.Count - 1].role == "user")
                    history.RemoveAt(history.Count - 1);
                thinkingBubble.text = "Yanıt alınamadı: " + error.Message;
                thinkingBubble.RemoveFromClassList("piko-msg-assistant");
                thinkingBubble.AddToClassList("piko-msg-system");
            }
            finally
            {
                sending = false;
                sendButton?.SetEnabled(true);
                messagesScroll?.schedule.Execute(() =>
                {
                    if (messagesScroll.verticalScroller != null)
                        messagesScroll.verticalScroller.value = messagesScroll.verticalScroller.highValue;
                }).ExecuteLater(25);
            }
        }

        string BuildQuizLockedReply()
        {
            if (learningState.Project == null)
                return "Henüz bir eğitim ortamı açmadın! Quiz yapabilmem için önce Dashboard'dan bir ortam (örneğin Charité Operating Room) açıp en az 5 bilgi noktasını incelemen gerekiyor.";

            int visited = learningState.VisitedHotspotCount;
            int required = learningState.RequiredHotspotsForQuiz;
            int remaining = Math.Max(1, required - visited);
            var unvisited = learningState.UnvisitedHotspots().Take(3).Select(h => "'" + h.title + "'").ToList();
            string suggestions = unvisited.Count > 0
                ? " Örneğin henüz bakmadığın şu noktalara göz atabilirsin: " + string.Join(", ", unvisited) + "."
                : "";
            return $"Şu ana kadar {visited}/{required} bilgi noktasını inceledin. Quiz yapabilmem için biraz daha öğrenmen ve en az {remaining} bilgi noktasını daha incelemen gerekiyor!{suggestions}";
        }

        async Task<string> SendGeminiWithFallbackAsync(string payloadJson, CancellationToken token)
        {
            string primaryModel = string.IsNullOrWhiteSpace(config.model) ? PikoAiConfig.DefaultModel : config.model;
            try
            {
                return await PostGeminiWithRetryAsync(primaryModel, payloadJson, token);
            }
            catch (GeminiHttpException primaryError) when (
                primaryModel != PikoAiConfig.FallbackModel &&
                IsTransientOrQuotaError(primaryError.StatusCode))
            {
                statusMessage?.Invoke($"Piko: {primaryModel} yoğun ({primaryError.StatusCode}), {PikoAiConfig.FallbackModel} yedek modeline geçiliyor…");
                await Task.Delay(1200, token);
                return await PostGeminiWithRetryAsync(PikoAiConfig.FallbackModel, payloadJson, token);
            }
        }

        static bool IsTransientOrQuotaError(long statusCode) =>
            statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504 || statusCode == 404;

        async Task<string> PostGeminiWithRetryAsync(string modelName, string payloadJson, CancellationToken token)
        {
            try
            {
                return await PostGeminiRequestAsync(modelName, payloadJson, token);
            }
            catch (GeminiHttpException firstError) when (
                firstError.StatusCode == 503 || firstError.StatusCode == 429 || firstError.StatusCode == 500)
            {
                statusMessage?.Invoke($"Piko: Sunucu yoğun ({firstError.StatusCode}), yeniden deneniyor…");
                await Task.Delay(1300, token);
                return await PostGeminiRequestAsync(modelName, payloadJson, token);
            }
        }

        async Task<string> PostGeminiRequestAsync(string modelName, string payloadJson, CancellationToken token)
        {
            string url = "https://generativelanguage.googleapis.com/v1beta/models/" + modelName + ":generateContent";
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payloadJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", config.apiKey.Trim());
                request.timeout = 45;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (token.IsCancellationRequested)
                    {
                        request.Abort();
                        token.ThrowIfCancellationRequested();
                    }
                    await Task.Yield();
                }

                string responseText = request.downloadHandler?.text ?? "";
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string detail = request.error;
                    try { PikoGeminiPayload.ParseResponseText(responseText); }
                    catch (Exception parsedError) { if (!string.IsNullOrWhiteSpace(parsedError.Message)) detail = parsedError.Message; }
                    throw new GeminiHttpException(request.responseCode, detail);
                }

                return PikoGeminiPayload.ParseResponseText(responseText);
            }
        }

        sealed class GeminiHttpException : Exception
        {
            public long StatusCode { get; }
            public GeminiHttpException(long statusCode, string message) : base(message) => StatusCode = statusCode;
        }

        public void Attach(VisualElement anchor)
        {
            if (container != null && container.parent != anchor)
            {
                container.RemoveFromHierarchy();
                anchor.Add(container);
            }
        }

        async Task Load(Button button, Action<string> message, CancellationToken token)
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "WorkplaceStudio", "guides", "piko.glb");
                if (!File.Exists(path)) { container.style.display = DisplayStyle.None; return; }
                GlbFiles.Verify(path, ExpectedHash);
                modelRoot = new GameObject("Piko portrait"); modelRoot.transform.SetParent(transform); modelRoot.transform.position = new Vector3(1000, 0, 0);
                importer = new GltfImport(downloadProvider: new NoExternalDownloads());
                if (!await importer.LoadFile(path, importSettings: new ImportSettings { AnimationMethod = AnimationMethod.Legacy }, cancellationToken: token) ||
                    !await importer.InstantiateMainSceneAsync(new GameObjectInstantiator(importer, modelRoot.transform, settings: new InstantiationSettings { Mask = ComponentType.Mesh | ComponentType.Animation, Layer = 30 }), token)) throw new InvalidDataException("Piko could not be loaded.");
                token.ThrowIfCancellationRequested();
                foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                animationPlayer = modelRoot.GetComponentInChildren<Animation>(true);
                if (animationPlayer == null || animationPlayer["Idle"] == null || animationPlayer["Wave"] == null) throw new InvalidDataException("Piko Idle/Wave animation clips were not found.");
                animationPlayer.Play("Idle"); animationPlayer["Idle"].time = 0; animationPlayer.Sample();
                Bounds bounds = BoundsOfModel();
                animationPlayer.Play("Wave");
                for (int i = 0; i <= 10; i++) { animationPlayer["Wave"].time = animationPlayer["Wave"].length * i / 10; animationPlayer.Sample(); bounds.Encapsulate(BoundsOfModel()); }
                Idle();
                texture = new RenderTexture(256, 320, 24, RenderTextureFormat.ARGB32); texture.Create();
                var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; image.AddToClassList("piko-image"); button.Add(image);
                cameraRoot = new GameObject("Piko portrait camera"); cameraRoot.transform.SetParent(transform);
                portraitCamera = cameraRoot.AddComponent<Camera>(); portraitCamera.cullingMask = 1 << 30; portraitCamera.clearFlags = CameraClearFlags.SolidColor;
                portraitCamera.backgroundColor = Color.clear; portraitCamera.targetTexture = texture; portraitCamera.nearClipPlane = .01f; portraitCamera.farClipPlane = 100;
                portraitCamera.fieldOfView = 38;
                float distance = Mathf.Max(bounds.size.y, bounds.size.x / .8f) / (2 * Mathf.Tan(portraitCamera.fieldOfView * Mathf.Deg2Rad / 2)) * 1.12f + bounds.size.z / 2;
                cameraRoot.transform.position = bounds.center + new Vector3(.08f, .03f, 1).normalized * distance;
                cameraRoot.transform.LookAt(bounds.center);
                var lightRoot = new GameObject("Piko key light"); lightRoot.transform.SetParent(modelRoot.transform);
                var light = lightRoot.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 30; lightRoot.transform.rotation = Quaternion.Euler(30, 150, 0);
                portraitCamera.enabled = !minimized;
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (!token.IsCancellationRequested) { container.style.display = DisplayStyle.None; message("Could not open Piko: " + error.Message); } }
        }
        Bounds BoundsOfModel()
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidDataException("Piko geometry is missing.");
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        void Idle()
        {
            waving = false; if (animationPlayer == null) return;
            animationPlayer["Idle"].wrapMode = WrapMode.Loop; animationPlayer.Play("Idle");
        }
        void Wave()
        {
            if (animationPlayer == null || waving || minimized) return;
            waving = true; animationPlayer["Wave"].wrapMode = WrapMode.Once; animationPlayer.Play("Wave");
        }
        void Update() { if (waving && animationPlayer != null && !animationPlayer.IsPlaying("Wave")) Idle(); }
        void OnDestroy()
        {
            requestCancellation?.Cancel(); requestCancellation?.Dispose();
            cancellation?.Cancel(); cancellation?.Dispose(); importer?.Dispose();
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (modelRoot != null) Destroy(modelRoot); if (cameraRoot != null) Destroy(cameraRoot);
        }
    }
}

