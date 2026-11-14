# Unity masaüstü mimarisi

## Sınırlar

`Assets/_Project/Desktop` yeni uygulamadır. Eski Unity denemeleri ve
`web-editor` mevcut sürüm/reference olarak korunur. Yeni runtime bir browser
penceresi veya yerel web sunucusu içermez.

- **Core:** UnityEngine'den bağımsız C# veri tipleri, v1 doğrulama, alan
  hiyerarşisi, yerel yol/GLB kontrolü ve atomik proje kaydı.
- **Runtime:** Unity UI Toolkit, glTFast yerel dosya yükleme, DesktopNavigation,
  kaynak koordinat adaptörü, Piko portresi ve eğitim eylemleri.
- **Editor:** Ayrı sahne açma, içerik hazırlama ve Windows player derlemesi.
- **Tests:** Core gerçek .NET derlemesiyle; Unity adaptörü ayrı EditMode testleriyle.

`ITrainingActions` alan açma, bilgi noktası etkinleştirme ve genel görünüme
dönüşün cihazdan bağımsız girişidir. İlk adaptör klavye/faredir. İkinci dönemde
XR adaptörü bu eylemleri kullanabilir; XR rig/kontrolcü/konfor uygulaması
henüz yoktur. UI Toolkit ekran arayüzünün VR sunumu ayrıca tasarlanmalıdır.

## Veri ve koordinat

Eğitim verisi Unity GameObject referansları içermez. `TrainingProject`
formatVersion=1; içindeki EditorDocument mevcut web schemaVersion=1 ve
isteğe bağlı hierarchyVersion=1 alanlarını taşır. Nokta pozisyonları model
köküne göre, alan kamera bakışları mutlak sağ el/Y-up uzayında kalır.

glTFast, sağ el glTF'yi Unity'ye aktarırken X'i yansıtır. Runtime'ın
Coordinates adaptörü kamera/hedef/noktalara aynı dönüşümü uygular. Kaydetme
ters dönüşümle kaynak uzayına döner; GLB'ye ikinci ölçek/eksen dönüşümü yazılmaz.
Katalog yalnız tanımlı 10 kaynak alan/noktayı içerir; kullanıcının tarayıcıya
kaydettiği diğer içeriklere erişildiği varsayılmaz.

## Dosya akışı

Kurulum içindeki StreamingAssets salt okunur kaynak kabul edilir. Düzenleme
klonlanmış belge üzerindedir. Açık Kaydet eylemi kullanıcı veri klasörüne
content-addressed GLB ve manifest yazar. Önce model doğrulanıp kopyalanır;
manifest en son atomik değiştirilir. Eski manifest ayrı dosyada tutulur.
Bozuk/uyumsuz kayıt kendiliğinden sıfırlanmaz.

GLB'ler boyut/başlık/JSON/binary bölümleri ve harici URI bakımından kontrol
edilir. Model yükleyicisi yalnız yerel FileStream API'sini kullanır;
IDownloadProvider tüm harici istekleri reddeder. Proje yolları klasör
sınırında çözülür, traversal ve reparse-point başvuruları reddedilir.
Yeni kaynakta sahneye yalnız mesh, Piko'da mesh/animasyon alınır; ithal
kamera ve ışıkların uygulama kameralarını ele geçirmesine izin verilmez.

Bu kontroller cihaz güvenliği veya ağsızlık sertifikasyonu değildir. Unity
player ağ davranışı, dosya yetkileri ve kurumsal dağıtım ayrıca doğrulanmalıdır.

## Yükleme ve giriş yaşam döngüsü

Model değişiminde önceki yükleme CancellationToken ile iptal edilir. Yeni
model tamamen hazır olmadan etkin proje değişmez; hata önceki ortamı korur.
Scene/importer/token/RenderTexture kaynakları çıkışta temizlenir.

Fare yakalama yalnız sahnedeki sağ tıklamayla başlar. Esc, odak kaybı ve UI
odağı hareketi bırakır. Kamera taşıma completed/blocked/cancelled olarak
ayrılır; revizyon eski seçimlerin sonradan bilgi kartı açmasını önler.
Markerlar kamera projeksiyonuyla güncellenir; her marker için her kare
geometri picking yapılmaz.

## Gelecek modüller

Yerel AI üretimi bu dilimde yoktur. Gelecekteki modülün ilk sınırı yerel
mesh/GLB + doku + ölçek/kaynak metadata'sı taşıyan ortam paketidir. Eğitim
belgesi AI yöntemine bağlı tutulmaz. Çok kullanıcı, şifreleme ve XR için
bugün çalışıyormuş gibi API/ekran sunulmaz.
