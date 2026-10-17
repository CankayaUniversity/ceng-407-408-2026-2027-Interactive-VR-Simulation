# İnteraktif İş Yeri Eğitim Simülasyonu - Mimari ve Tasarım Kılavuzu

Bu doküman, 9 aylık Bilgisayar Mühendisliği bitirme projesinin temel mimari kurallarını, WebGL ve VR ayrıştırma (decoupling) prensiplerini ve WebGL <-> Web iletişim sözleşmesini tanımlar.

---

## 1. PROJE VİZYONU VE TEMEL MİMARİ KURALLAR (SYSTEM RULES)

### 1.1. Öncelik WebGL, Sonra VR (Decoupled Architecture)
- **1. Faz Odak Noktası:** Projenin ilk aşaması **%100 WebGL (WASD + Mouse)** desteğiyle standart web tarayıcılarında çalışacak şekilde geliştirilmektedir. Bu aşamada projeye gereksiz XR/VR paketleri eklenmez.
- **VR-Ready Etkileşim Modeli:** Sahnedeki tüm etkileşimli nesneler (butonlar, vanalar, yangın tüpleri, kişisel koruyucu donanımlar vb.) kendilerini tetikleyen istemcinin masaüstü (klavye/fare) mi yoksa VR (OpenXR kontrolcüleri/el takibi) mi olduğunu **bilmez**.
- Tüm etkileşimler `Simulation.Interaction` altındaki `IInteractable`, `IGrabbable` ve `InteractorContext` arayüz ve yapıları üzerinden soyutlanır.

### 1.2. Unity Sürümü ve Render Pipeline
- **Unity Sürümü:** Unity 6 (6000.0.x LTS).
- **Grafik Hattı:** Universal Render Pipeline (URP).
- **Girdi Sistemi:** Yalnızca **Yeni Input System** (`UnityEngine.InputSystem`) kullanılır. Eski `UnityEngine.Input` kesinlikle kullanılmaz.

### 1.3. Kritik WebGL ve Tarayıcı Kısıtlamaları
- **Single-Thread Kuralı (Multi-threading Yasağı):** WebAssembly tek iş parçacığında çalıştığından ötürü kod tabanında `Task.Run()` veya `System.Threading.Thread` **kesinlikle kullanılamaz**. Asenkron akışlar Unity `Coroutine (IEnumerator)` veya Unity 6 `Awaitable` mekanizmaları ile yürütülür.
- **Cursor Lock Güvenliği:** Web tarayıcıları güvenlik nedeniyle `Start()` veya `Awake()` anında fare imlecini kilitlemeye (`CursorLockMode.Locked`) izin vermez. Fare kilitleme yalnızca kullanıcının Canvas üzerine doğrudan tıklaması ile tetiklenir (`Mouse.current.leftButton.wasPressedThisFrame`). Escape tuşuna basıldığında kilit serbest bırakılır.
- **Klavye Odak Yönetimi:** Web sayfasında form girdileri veya UI elemanları ile Unity WebGL tuş yakalamasının çakışmaması adına `WebGLInput.captureAllKeyboardInput` bayrağı `WebBridgeService` üzerinden dinamik olarak kontrol edilir.

### 1.4. Kod Standartları ve Namespace Hiyerarşisi
Tüm proje kaynakları `Assets/_Project/` klasöründe izole edilir. Temel namespace hiyerarşisi:
- `Simulation.Core`: Çekirdek durum yöneticileri, singleton'lar, genel yardımcı sınıflar.
- `Simulation.Interaction`: Etkileşim arayüzleri (`IInteractable`, `IGrabbable`, `InteractorContext`).
- `Simulation.Interactables`: Sahnedeki somut etkileşimli nesneler (acil durum butonu, vana vb.).
- `Simulation.Player`: Oyuncu hareket ve ışın (raycast) etkileşim denetleyicileri.
- `Simulation.ScenarioSystem`: Görev akışları, senaryo adımları, skorlama mekanizması.
- `Simulation.UI`: Canvas arayüzleri, HUD, yönlendirme metinleri.
- `Simulation.WebBridge`: WebGL JSLib ve JavaScript çift yönlü iletişim köprüsü.

---

## 2. WEBGL <-> WEB İLETİŞİM SÖZLEŞMESİ (WEB BRIDGE CONTRACT)

Simülasyon, bir web portalı veya Öğrenim Yönetim Sistemi (LMS) içerisine `iframe` veya doğrudan `<canvas>` olarak gömülür. İki yönlü iletişim `WebBridge.jslib` ve `WebBridgeService.cs` ile sağlanır.

### 2.1. Unity -> Web (JSLib Event Callbacks)

| JSLib Fonksiyonu | Web Tarafındaki JS Fonksiyonu | Veri Formatı | Açıklama |
|---|---|---|---|
| `NotifyWebUnityReady()` | `window.onUnitySimulationReady()` | `void` | Unity WebGL motoru ve sahnesi yüklendiğinde tarayıcıya hazır sinyali gönderir. |
| `SendStepProgressToWeb(jsonStringPtr)` | `window.onUnityStepCompleted(jsonPayload)` | JSON String | Kullanıcı bir senaryo adımını tamamladığında (başarılı/başarısız) anlık veri gönderir. |
| `SendSimulationResultToWeb(jsonStringPtr)` | `window.onUnitySimulationFinished(jsonPayload)` | JSON String | Simülasyon bittiğinde toplam skor, hata sayısı ve süreyi iletir. |

#### JSON Yükü Örnekleri (Unity -> Web):
**Adım Tamamlama (`onUnityStepCompleted`):**
```json
{
  "stepId": "emergency_btn_01",
  "isSuccess": true,
  "timeSpent": 14.25
}
```

**Simülasyon Bitişi (`onUnitySimulationFinished`):**
```json
{
  "totalScore": 95,
  "mistakeCount": 1,
  "totalDuration": 128.50
}
```

---

### 2.2. Web -> Unity (`unityInstance.SendMessage`)

Web sayfası, Unity örneğine komut göndermek için Unity'nin `SendMessage` API'sini kullanır. Alıcı nesne sahnede **`WebBridgeReceiver`** isimli GameObject olmalıdır.

| Çağrı Formatı | Parametre Tipi | Açıklama |
|---|---|---|
| `unityInstance.SendMessage("WebBridgeReceiver", "InitializeSimulationFromWeb", jsonConfig)` | `string` (JSON) | Kullanıcı kimliği, senaryo ID'si veya konfigürasyonu Web'den Unity'e aktarır. |
| `unityInstance.SendMessage("WebBridgeReceiver", "SetWebGLKeyboardCapture", 0 veya 1)` | `int` (`0` veya `1`) | Tarayıcıda bir input aktifken Unity'nin klavye yakalamasını kapatmak (`0`) veya geri açmak (`1`) için kullanılır. |

---

## 3. KLASÖR YAPISI ŞEMASI

```text
Assets/
├── _Project/
│   ├── Art/
│   │   ├── Materials/
│   │   ├── Models/
│   │   └── Textures/
│   ├── Audio/
│   ├── Prefabs/
│   │   ├── Environment/
│   │   ├── Interactables/
│   │   └── Player/
│   ├── Scenes/
│   ├── ScriptableObjects/
│   │   └── Scenarios/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Interaction/
│   │   ├── Interactables/
│   │   ├── Player/
│   │   ├── ScenarioSystem/
│   │   ├── UI/
│   │   └── WebBridge/
│   └── Settings/
├── Plugins/
│   └── WebGL/
└── ThirdParty/
```
