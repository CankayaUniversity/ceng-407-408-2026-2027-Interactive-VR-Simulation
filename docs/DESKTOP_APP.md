# Workplace Studio — Unity masaüstü uygulamasına geçiş

15 Kasım 2026 güncellemesi. Güncel okul deposu:
[CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation](https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation).
Hazırlık dalı: `codex/nursing-room-literature-2026-11-15`; tabanı okulun
`main` dalındaki `50a5a2f` commit'idir. İlk Unity geçişi 2 Kasım'da
`codex/combined-final` (`8f1c2d6`) üzerinden başlatılmıştır.

**Durum:** İlk Unity masaüstü kaynakları ve derleme araçları hazırlandı.
Unity Editor 6000.6.7f1 kuruldu; paket çözümlemesi ve gerçek Unity C# derlemesi
tamamlandı. 17 Unity testi geçti; gerçek hastane ve hemşire odası testleri Play Mode'a girer.
Windows executable oluşturuldu;
bu bilgisayarda hastane/kokpit, Piko ve kaydet/yeniden aç akışı denendi.
Kontroller [doğrulama kaydında](DESKTOP_VALIDATION_2026_11_02.md) ayrılır.

## İlk sürümün kaynak kapsamı

- Unity UI Toolkit ile yerel ortam seçimi, Editor/Preview ve eğitim ağacı.
- Yeni **Nursing Skills Room**: Özel hazırlanmış tek yataklı hasta odası,
  etkileşimli monitör, pompa, yatak ve hemşire çağrısı. Cihaz listesi yakın
  görünüme geçer; doğrudan tıklama ve yürüyüşte E ile kontrol paneli açılır.
  Ayrıntılar: [oda ve etkileşim altyapısı](NURSING_ROOM.md).
- Hastane/kokpit GLB'lerini diskten açma; yeni, kendine yeterli yerel GLB seçme.
- WASD/fare; yürüyüş ve çarpışmasız serbest kamera. Sağ tuş girdiyi yakalar,
  Esc bırakır. Serbest modda Shift yükseltir, Ctrl alçaltır; sağ ve sol
  tuşlar desteklenir. Shift yalnız yürüyüş modunda hızlandırır. Hareket modunda UI
  klavye gezinmesi/tıklamaları engellenir; WASD arayüz odağını değiştirmez.
  Pencere odağı kaybolduğunda fare bırakılır.
- Hastane profilinde hareket hızı 0,8'den 2,4 sahne birimi/sn'ye, yürüyüş
  göz yüksekliği 1,65'ten 2,4 sahne birimine çıkarıldı. Shift ile yürüyüş
  hızı 4,8 olur. Kokpit ve diğer ortamların profili değişmedi. Kayıtlı
  inceleme kameralarının mutlak konumları korunur; yükseklik yürüyüş gövdesinindir.
- Yürüyüşe başlarken gerçek model çarpışmalarından aşağı doğru zemin aranır;
  karakterin ayakları yürünebilir yüzeye yerleştirilir. Bu işlem, eski düşük
  kamera kayıtlarının daha uzun hastane karakterini zeminin altında başlatmasını
  önler. Kayıtlı bakıştan veya Spectator'dan yürüyüşe dönünce de uygulanır;
  diskteki kamera kayıtları değiştirilmez.
- Uygulama arayüzü web sürümüyle aynı İngilizce terminolojiyi kullanır:
  Scene Explorer, Preview/Editor, Overview, Save an area, Training information.
- Kompakt sabit piksel ölçeği, 230 px Explorer ve 280 px Inspector kullanılır.
  Preview'da boş Inspector gizlidir. Explorer/Inspector düğmeleri panelleri
  ayrı ayrı açar/kapatır; Focus view ikisini gizler, Exit focus önceki düzeni getirir.
  Bir bilgi noktası veya düzenleme formu açıldığında Inspector görünür.
- Alan/alt alan, mutlak kamera bakışı, kardeş sırası ve alan kaldırmada içerik
  koruma. Preview'da üst/sonraki/önceki alan ve bilgi noktası seçimi.
- Model yüzeyine ışın seçimiyle bilgi noktası ekleme, metin ve bağlı alan düzenleme.
- Kaynak hastanenin 10 alanı ve 10 İngilizce bilgi noktası. Kaynak/atıf korunur.
- Piko için şeffaf portre, Idle döngüsü ve tıklamayla bir kez Wave kodu.
- Yerel, taşınabilir proje klasörü; model özeti doğrulaması ve önceki manifest yedeği.

AI üretimi, VR gözlük sürücüsü/kontrolcü adaptörü, hesap/yetki yönetimi,
şifreleme, kurumsal güvenlik sertifikasyonu ve installer bu ilk dilimde yoktur.
Mesh/GLB gelecekteki ortam üretim modülünün giriş sınırıdır; AI servisi eklenmedi.

## Geliştirici kurulumu

1. Unity Hub kurun ve kendi hesabınız/lisansınızla giriş yapın. Resmî Hub
   kurulum dosyası yerelde `.tools/installers/UnityHubSetup-x64.exe` olarak
   çalışma alanının üst klasörüne indirildi; Unity dijital imzası doğrulandı.
   Kurulum/lisans onayı otomatik yapılmadı.
2. **Unity Editor 6000.6.7f1** sürümünü kurun.
   Revizyon `b22767f04f04`. Kaynak dal 6000.5.8f1 kullanıyordu; masaüstü dalı,
   bu bilgisayarda kurulan sürüme Unity tarafından yükseltildi.
   Windows build desteği bulunmalı. Şimdilik Android,
   VR veya WebGL modülünü kurmak bu masaüstü diliminin şartı değildir.
3. Unity Hub → Projects → Add project from disk ile repository kökünü seçin.
   `web-editor` klasörünü Unity projesi olarak açmayın.
4. İlk açılışta Package Manager'ın paketleri çözmesini ve C# derlemesini bekleyin.
   Sabit bağımlılıklar: glTFast `6.19.0`, Newtonsoft JSON `3.2.2`.
5. Repository kökünde gerekirse `git lfs pull` çalıştırın. Büyük modeller
   gerçek GLB olmalı; 133 baytlık LFS pointer dosyası kabul edilmez.
6. Unity menüsünden **Workplace Studio → Open Desktop Application Scene**
   seçin. Kaynak GLB'ler hash kontrolüyle StreamingAssets'e hazırlanır.
7. `Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity` sahnesinde Play'e basın.

Mevcut `02_TrainingLab.unity`, web editörü ve tarayıcı kayıtları korunur.
Masaüstü yeni bir başlangıç sahnesidir; eski sahneleri otomatik değiştirmez.

## Windows uygulaması üretme

Unity menüsü: **Workplace Studio → Build Windows Application**.

Alternatif PowerShell:

```powershell
.\desktop-tools\Build-Windows.ps1
# Farklı Unity konumu:
.\desktop-tools\Build-Windows.ps1 -UnityEditor "D:\Unity\6000.6.7f1\Editor\Unity.exe"
```

Çıktı: `Builds/WorkplaceStudio-Windows/WorkplaceStudio.exe`.
Build sonrasında `Unity_Uygulama_Baslat.cmd` bu executable'ı açar. Tam çıktı
klasörünü birlikte taşıyın; yalnız `.exe` kopyalamak yeterli değildir.
Son kullanıcının Node.js, Git, Unity Editor veya geliştirme sunucusu kurması
hedeflenmez. İlk paket gerçek Windows bilgisayarda ayrıca doğrulanmalıdır.

Derleme glTF shader'larını dahil eder; geçici ürün/pipeline/shader ayarlarını
sonrasında eski değerlerine döndürür. Eski build scene listesini değiştirmez.
Standalone'da `GLTFAST_KEEP_MESH_DATA`, `GLTFAST_SAFE` ve
`GLTFAST_EDITOR_IMPORT_OFF` kullanılır; colliders için CPU mesh verisi korunur.

## Kullanım ve kayıt

- Dashboard'dan hastaneyi/kokpiti veya yerel GLB'yi açın.
- Sağ tuşla model alanında WASD/fareyi etkinleştirin. Esc ile UI'ya dönün.
- Hareket sırasında mevcut UI odağı temizlenir, kontroller geçici olarak odak
  alamaz ve hover/focus çerçeveleri bastırılır. Esc'den sonra odaklanabilirlik
  geri gelir; klavye ile UI kullanımı korunur.
- Editor'de alan seçmek Inspector'ı açar; kayıtlı bakışa gitmek ayrı eylemdir.
- Alan başlığı, üst alanı ve sırası düzenlenebilir. Alan kaldırma onay ister;
  doğrudan alt alanlar/bilgi noktaları üst alana taşınır.
- Nokta eklemek için Editor → Bilgi noktası → gerçek yüzeye sol tıklama → metin → kaydet.
- Üst bardaki **Kaydet**, bütün projeyi yerel diske yazar. Düzenleme açıkken
  yalnız nokta formunu kaydetmek henüz diske yazıldığı anlamına gelmez.
- Proje klasörleri `%LOCALAPPDATA%\Cengaverler\WorkplaceStudio\Projects` altındadır.
  Editor ve standalone aynı klasörü kullanır.
- Başka bilgisayara eğitim taşımak için ilgili proje klasörünün tamamını
  kopyalayın; uygulamadaki Eğitim projesi aç ile `project.json` seçin.

Klasör `project.json`, `project.previous.json` (ikinci kayıttan sonra) ve
`assets/<sha256>.glb` içerir. Modeller içerik adreslidir; önceki manifestin
modeli üzerine yazılmaz. Bozuk kayıtta eski dosyalar korunur. Silinen içerik
başlangıç kataloğundan sessizce geri yüklenmez.

Web'in localStorage kayıtları otomatik okunmaz veya göç ettirilmez. Veri
biçiminin eğitim bölümü mevcut v1 ve isteğe bağlı hiyerarşi alanlarıyla
uyumludur; bir browser export/import UI'si bu görevde eklenmedi.

## Sınırlar

- İlk hedef Windows ve statik, kendine yeterli GLB 2.0'dır; dosya sınırı 512 MB.
- Harici URI/dosya bağımlılığı olan GLB reddedilir. Draco/KTX gibi ek codec'ler
  yüklenmiş sayılmaz; desteklenmeyen dosya açık hata vermelidir.
- GLB doğrulaması ve dış indirmeyi reddeden provider temel giriş korumasıdır;
  tüm kötü niyetli içeriklere karşı güvenlik denetimi yapılmış sayılmaz.
- Kaynak koordinatları sağ el/Y-up kalır; glTFast'ın Unity dönüşümüne uygun
  olarak yalnız sunumda X yansıtılır. Alan kameraları mutlak kalır.
- Doğrudan kamera yolu ekipmanla kesişirse geçiş engellenir. Bilgi kartı mevcut
  bakışta açılabilir; iptal edilen eski seçim sonradan kart açmaz.
- Model ölçeği ve tarama geometrisinin yürüyüş için yeterliliği gözden geçirilmeli.
  Serbest kamera geometri içinden geçer. VR konforu henüz uygulanmadı.
- İlk Windows çıktısında iki ortamın görünümü ve Piko animasyonları denendi.
  Kokpit kaynak modelinde çok büyük üçgenler için PhysX uyarısı var;
  kokpit serbest kamera modunda açılır. Modellerin VR/çarpışma optimizasyonu
  ve farklı donanımda performans ölçümü ayrıca yapılmalıdır.
- Runtime oluşturulan PanelSettings, ICU verisi için fallback uyarısı verir.
  Denenen Türkçe/İngilizce metinler görüntülendi; karmaşık dil/emoji kapsamı
  henüz doğrulanmadı.

## Doğrulama araçları

```powershell
.\desktop-tools\Test-Core.ps1
.\desktop-tools\Prepare-DesktopContent.ps1
# Node.js ile, geliştirici kaynak kontrolü:
node desktop-tools/verify-desktop.mjs
# Unity kurulduktan sonra:
.\desktop-tools\Test-Unity.ps1
```

Katalog, `combined-final` registry ve kaynak annotation modülünden
`desktop-tools/export-content.mjs` ile üretilmiştir. Bu geliştirici aracı
mevcut web TypeScript bağımlılığını kullanır; son kullanıcıya Node gerekmez.
Okul deposunun güncel tabanında hastane/kokpit GLB'leri `web-editor/public`
altında normal Git dosyalarıdır; bu dal için LFS ile ayrıca indirmek gerekmez.
Hemşire odasının Blender/GLB kaynakları `desktop-content/nursing-room` içindedir.
StreamingAssets kopyaları üretilmiş dosyadır ve tekrar Git'e eklenmez.

## Kaynak ve lisans

Hastane Digital Surgery / CC-BY-NC-4.0, kokpit hakai315 / CC-BY-4.0 kaynak
notlarını korur. [Asset envanteri](../web-editor/docs/ASSETS.md) ve
[hastane atıfları](../web-editor/docs/HOSPITAL_ANNOTATIONS.md) geçerlidir.
Kurumlara ticari dağıtım, örnek hastane modelinin NC koşulundan ayrı
değerlendirilmelidir; framework değiştirmek asset lisansını değiştirmez.
