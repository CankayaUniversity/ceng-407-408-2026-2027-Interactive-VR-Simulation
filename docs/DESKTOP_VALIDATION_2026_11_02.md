# Unity masaüstü geçişi — doğrulama kaydı

## Kaynak

- 2 Kasım 2026, Europe/Istanbul.
- Kullanıcının istediği `origin/codex/combined-final` fetch edildi.
- Uzak/taban commit: `8f1c2d68193d802edfafd7663a6f105f1ea33731`.
- Yerel ağaç başlangıçta temizdi. `codex/combined-final` tracking dalı ve
  bunun üzerinden `codex/unity-desktop` geliştirme dalı oluşturuldu.
- Web kaynakları veya kayıtları değiştirilmedi; yeni masaüstü sahnesi eklendi.

## Geçen kontroller

- Gerçek C# çekirdeği .NET SDK 10.0.204 ile derlendi.
- 19 çekirdek/veri testi: proje JSON round-trip, hiyerarşi döngü/üst alan
  kontrolü, alan kaldırmada içerik koruma, yol traversal, GLB header/binary,
  harici URI reddi, dosya hash'i, kayıt/yeniden açma, önceki manifest yedeği,
  bozuk veride eski kaydı koruma, gerçek üç GLB'nin kabulü ve alan düzenleme/
  taşıma sırasında mevcut kardeş sırasını koruma.
- C# 9 sözdizimi kontrolü Roslyn ile yapıldı. Bu, Unity engine API'lerinin
  derlendiği veya çalıştığı anlamına gelmez.
- `Prepare-DesktopContent.ps1`: iki ortam + Piko gerçek modellerini kaynak
  hash'leriyle doğrulayıp üretilmiş StreamingAssets klasörüne kopyaladı.
- `verify-desktop.mjs`: sahne/script ve pipeline/renderer GUID eşleşmeleri,
  metadata tekilliği, model hash'leri ve paket sürümü kayıtları kontrol edildi.
- PowerShell betikleri parser ile incelendi; sözdizimi hatası yok.
- glTFast 6.19.0 ve Newtonsoft 3.2.2 resmî Unity paket kaynağından indirildi;
  tarball SHA-1 değerleri registry metadata'sıyla eşleşti. API'ler incelendi.
- Unity Hub Windows kurulum dosyası resmî kaynaktan indirildi; Authenticode
  imzası Valid / Unity Technologies SF olarak doğrulandı. Installer kaynak
  deposuna eklenmedi ve lisans onayı otomatik yapılmadı.

## Unity kurulumu sonrasında geçen gerçek kontroller

Kullanıcı kurulumu tamamladı. Kurulan sürüm 6000.6.7f1
(`b22767f04f04`), Windows build modülü mevcut. Masaüstü geliştirme dalı bu
sürümle açıldı; Unity ProjectVersion, URP ve ilgili paket/ayarları yükseltti.
Kaynak web dalının sürümü 6000.5.8f1 idi.

- Gerçek UPM çözümlemesi ve Unity Editor C# derlemesi başarılı.
- `Test-Unity.ps1`: 13 EditMode testi geçti; 0 başarısız, 0 atlanan.
  Kaynak/Unity koordinat dönüşümü, kamera/world-up, geçiş iptali ve
  gerçek BoxCollider ile engellenen kamera yolu doğrulandı.
  Yeni 9 UI regresyon testi gerçek Editor UI panelinde WASD'nin kontrollere
  ulaşmamasını, yön olayının odağı değiştirmemesini, submit'in düğmeye
  basmamasını ve hareket bırakıldıktan sonra düğmenin tekrar çalışmasını kapsar.
  Bu UI testleri grafik aygıtı gerektirir; test betiği bu nedenle -nographics kullanmaz.
  Son iki test, önceden odaklanmış düğmenin odağını temizlemeyi, kontrollerin
  hareket sırasında odak alamamasını ve bırakıldığında odaklanabilirliğin
  seçim çerçevesi bırakmadan geri yüklenmesini doğrular.
- `Build-Windows.ps1`: Windows x64 player başarıyla üretildi; çıkış kodu 0.
  Son paket `Builds/WorkplaceStudio-Windows/WorkplaceStudio.exe` altında.
  Shader'lar, iki ortam GLB'si ve Piko çıktı klasöründe bulunuyor.
- Son C# çekirdek/sözdizimi turu: **20 geçti, 0 başarısız**.
- Üretilmiş Windows uygulaması gerçek GPU ile açıldı. Native arayüz,
  hastane ve kokpit geometrisi/dokuları görüntülendi; browser kullanılmadı.
- Hastanenin ilk bilgi noktası seçildi; kamera geçişi, yerinde numaralı işaret
  ve açıklama kartı görüntülendi.
- Piko'nun Idle ve tıklamayla Wave animasyonları gözle kontrol edildi.
- Sağ tuş, W girdisi ve Esc ile fare bırakma akışı denendi.
- Hastane uygulama arayüzünden yerel diske kaydedildi; uygulama kapatılıp
  son build yeniden açıldı ve dashboard'daki kayıttan geri yüklendi.
  10 alan/10 bilgi noktası ve kopyalanan GLB'nin SHA-256 eşleşmesi kontrol edildi.
- Alan formunu aynı üst alanla kaydetmenin kardeş sırasını değiştirdiği
  görsel denemede bulundu. Core düzeltmesi ve iki regresyon testi eklendi;
  son Windows paketinde aynı form kaydından sonra sıra korundu.
- Son paket pencereyi boyutlandırmayı ve kaydedilmemiş değişiklik kontrolünü
  kullanan Çıkış düğmesini içerir. Yükleme sırasında eski ekranın iz bırakması
  düzeltildi. Kaynak sahneler veya web kaynakları değiştirilmedi.
- Kullanıcının bildirdiği WASD/UI odak çakışması düzeltildi: hareket sırasında
  UI Toolkit olayları kökte trickle-down aşamasında durdurulur. Odak değişince
  hareketi kapatan FocusIn işleyicisi kaldırıldı. Esc ve pencere odağı kaybı
  fareyi bırakır. Hareket durumuna göre yardım metni güncellenir.
- Kullanıcının istediği gibi menüler, alan/hotspot formları, yardım metinleri,
  Piko ipuçları, dosya seçme başlıkları ve uygulama hata mesajları İngilizceye
  döndürüldü. Web kaynaklarındaki terminoloji esas alındı; kayıtlı eğitim
  içerikleri ve model dosyaları değiştirilmedi.
- Kullanıcının 5,55 saniyelik kaydından kareler okunarak hareket sırasında
  Environments ve bırakma sonrasında Exit odak çerçeveleri incelendi.
  Input gate artık panel düzeyindeki odak geçişlerine karşı kontrollerin
  focusable durumunu hareket boyunca kapatır; kontroller görsel olarak disabled olmaz.
- UI büyümesine yol açan 1280x800'e göre ölçekleme sabit piksel boyutuna alındı.
  Yan paneller daraltıldı, Preview'da boş Inspector gizlendi; Explorer/Inspector
  ve Focus view / Exit focus kontrolleri eklendi. Kamera viewport'u ve hotspot
  konumları mevcut GeometryChanged/LateUpdate akışıyla yeni boyuta uyarlanır.
- Son Windows çıktısında kullanıcının kayıtlı kokpiti açıldı. Boş Inspector'ın
  varsayılan olarak gizlenmesi, Inspector düğmesi, Focus view ile iki panelin
  gizlenmesi ve Exit focus ile önceki düzenin geri gelmesi gözle doğrulandı.
  Önceden odaklanmış alan düğmesinin çerçevesi sağ tıkla harekete girildiğinde
  kalktı; Mouse captured durumunda W girdisi sırasında yeni UI çerçevesi görülmedi.
- Hastane hareket profili 2,4 sahne birimi/sn, Shift ile 4,8 ve yürüyüş göz
  yüksekliği 2,4 sahne birimi olacak şekilde ayrıldı. Kokpit/diğer ortamlar
  0,8 hız ve 1,65 göz yüksekliğinde kaldı. Ortam değişiminde CharacterController
  ve kamera yerel yüksekliği birlikte güncellenir; kayıtlı mutlak bakışlar
  değiştirilmez. Bu değişiklikten sonra mevcut 13 Unity testi tekrar geçti.

## Kalan kapsam ve bilinen sınırlamalar

- Denemeler bu Windows bilgisayarda yapıldı. Temiz başka bilgisayarda ve
  ağ fiziksel olarak kapalıyken çalıştırma henüz yapılmadı. Diskten GLB yükleme
  harici URI'leri reddeder; çalışma akışına uzak AI/web servisi eklenmedi.
- Editor Play Mode ayrıca denenmedi; görsel kontrol üretilmiş Windows player'da yapıldı.
- Kokpit kaynak modelinde 500 birimden büyük üçgenler için PhysX uyarısı var.
  Kokpit serbest kamera modunda açılır; kaynak model çarpışma/VR optimizasyonu bekler.
- Runtime PanelSettings için ICU fallback uyarısı var. Denenen Türkçe ve
  İngilizce metinler görüntülendi; karmaşık dil/emoji kapsamı doğrulanmadı.
- Farklı cihazlarda performans, tüm düzenleme kombinasyonları, güvenlik
  denetimi, VR gözlük ve AI entegrasyonu bu ilk masaüstü diliminin dışında.

Paket runtime'ı için Unity hesabı/Editor kullanıcı makinesine dağıtılmayacak;
bu gereksinimler geliştirme/derleme aşamasınındır. Çıktı klasörü bütün olarak taşınmalıdır.

Bu aşamada yeni dal GitHub'a yüklenmedi; main/combined-final üzerine
yazılmadı. AI veya VR gözlük entegrasyonu eklenmedi.
