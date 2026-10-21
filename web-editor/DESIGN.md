---
name: Workplace Studio
description: Eğitmenler için okunabilir, sahne odaklı Studio Neutral çalışma alanı
colors:
  canvas: "#111318"
  surface: "#1c2028"
  hover: "#282d37"
  text: "#f3f5f8"
  muted: "#aeb6c3"
  line: "#353b47"
  primary: "#6699ff"
  primary-ink: "#0b1425"
  primary-hover: "#85adff"
  selected: "#20304d"
  selected-line: "#527fc8"
  danger: "#efaba3"
typography:
  headline:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "clamp(26px, 2.5vw, 34px)"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "-0.025em"
  title:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "22px"
    fontWeight: 600
    lineHeight: 1.3
  section:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "17px"
    fontWeight: 600
    lineHeight: 1.5
  body:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "15px"
    lineHeight: 1.5
  reading:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "15px"
    lineHeight: 1.75
  label:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 500
    lineHeight: 1.3
  caption:
    fontFamily: "Segoe UI Variable, Segoe UI, system-ui, sans-serif"
    fontSize: "13px"
    lineHeight: 1.5
rounded:
  control: "6px"
  project: "8px"
  card: "12px"
spacing:
  tight: "4px"
  control: "8px"
  compact: "12px"
  group: "16px"
  panel: "20px"
  section: "24px"
  page: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.primary-ink}"
    rounded: "{rounded.control}"
    padding: "9px 12px"
    height: "44px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
    textColor: "{colors.primary-ink}"
  input:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "9px 10px"
    height: "44px"
  environment-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.card}"
---

# Design System: Workplace Studio

## Overview

**Creative North Star: "Studio Neutral"**

Kullanıcının 20 Ekim 2026 tarihli açık talebi doğrultusunda çalışan React uygulamasının nötr görsel sistemi belgelenmiştir. Kullanıcı eski koyu zeytin/yeşil yüzeyleri ve asit yeşili vurguyu reddetmiştir; bu belge eski prototipleri veya teslim edilen her ayrıntıyı kullanıcı tarafından onaylanmış saymaz. Buradaki ölçüler kaynak CSS ve gerçek tarayıcı ölçümlerinden gelir; görsel değerlendirme ve iki iyileştirme turu uygulama ekibinin kararıdır.

Birincil kullanıcı, etkileşimli işyeri eğitimi hazırlayan eğitmen / uzmandır. Sahne geniş kalır; okunabilir araçlar, kayıtlı görünümler ve bağlamsal içerik sahnenin çevresinde yer alır. Tek sistem fontu, nötr katmanlar ve seyrek mavi vurgu kullanılır. Tasarım sistemi yeni özellik veya Babylon davranışı tanımlamaz.

**Key Characteristics:**

- Büyük merkez viewport ve ihtiyaç olduğunda açılan Inspector.
- Okunabilir metin etiketli araçlar ve tutarlı tıklama alanları.
- Preview ile Editor arasında açık mod ayrımı.
- Ortam kartları ile bağımsız proje kayıtlarının ayrı sunumu.

## Colors

Normatif palet üstteki frontmatter ve `src/styles/index.css` tokenlarıdır. Canvas, panel ve yükseltilmiş/hover yüzeyleri nötr koyu katmanlardır; zeytin veya yeşil tonlu yüzey kullanılmaz. Primary yalnız ana eylem, aktif mod, odak ve seçim için kullanılır. Seçim ayrıca metin ve ARIA durumuyla ifade edilir; renk tek başına anlam taşımaz.

Muted yardımcı bilgiyi belirtir; okunması gereken açıklamalar disabled görünümüne düşürülmez. Editor disabled kontrolleri %45 opacity kullanır. Danger silme ve uyarı içindir. Orijinal GLB materyalleri UI paletine dönüştürülmez. Önizleme görsellerindeki doğal yeşil ekipman/model renkleri arayüz yüzeyi değildir.

## Typography

Tek aile: Segoe UI Variable → Segoe UI → sistem sans. Sistem fontu gecikmesiz yükleme ve profesyonel araç okunabilirliği için korunmuştur; harici font bağımlılığı yoktur.

Masaüstünde Editor gövdesi ve Explorer satırları 15px; normal araçlar 14px; form alanları 15px ve etiketleri 14px; panel başlıkları 17px; çalışma alanı ortam adı ve Inspector içerik başlığı 18px'tir. Dashboard ortam başlıkları 22px, ana sayfa başlığı 26–34px'tir. Preview eğitim başlığı 22px/1.25 ve içerik 15px/1.75'tir. 13px durum, sayım ve yardımcı metadata içindir; Dashboard görsel atıfları/tarih gibi ikincil bilgiler 12px olabilir.

Uzun liste başlıkları sarılır. Workspace ortam adı tek satırda ellipsis kullanır. Sayısal kamera verileri tabular numerals kullanır. Mobilde workspace adı 16px olur; normal masaüstü ölçüsü küçültülmez. Teknik kalibrasyon alanlarının 13px etiketleri ve kompakt 40px girdileri ikincil, açılır ayrıntı istisnasıdır.

## Layout

Dashboard başlığı masaüstünde en az 56px; iki eşit ortam kolonu arasında 24px aralık vardır. Ortamlar üstte, bağımsız kullanıcı projeleri alttadır. İçerik maksimum 1800px; gutter `clamp(24px,4vw,72px)` ve üst boşluk 32px'tir. Kart gövdesi 24px padding kullanır. 1280px altında kart eylemi alt satıra geçer; 620px altında ortamlar tek kolondur. Dashboard gerektiğinde doğal dikey kayar.

Editor masaüstü başlığı 56px; görev araç satırı en az 58px'tir. Explorer 260px, Inspector 352px, merkez kalan esnek alandır. Panel iç boşluğu 20px ve başlık bölgesi en az 56px'tir. Inspector varsayılan kapalıdır. Authoring grubu Select / Spectator / Add hotspot / Save view içerir; kamera araçları ayrı gruptur. Preview authoring araçlarını gizler.

1200px ve altında paneller viewport'u daraltmayan, aynı anda yalnız biri açık drawer olur; genişlik `min(340px,100% - 32px)` değerindedir. 620px altında başlık çok satırlıdır ve authoring araçları iki kolona geçer. Panel kendi içinde kayar. Viewport aynı JSX konumunu korur; panel açmak scene/engine oluşturmaz.

## Elevation & Depth

Katmanlar renk tonu ve ince border ile ayrılır; dekoratif glass/blur yoktur. Ortam kartı hover sırasında yer değiştirmez, görsel zoom yapmaz. Mobil drawer gölgesi `0 12px 28px #0005` yalnız örtüşmeyi anlatır. Preview bilgi kartı opak paneldir ve gölgesizdir. Model ışığı arayüz dekorasyonu için değiştirilmez.

## Shapes

Kontroller ve mode switch 6px, proje kartları 8px, ortam ve Preview kartları 12px yarıçap kullanır. Explorer ve Inspector sabit kenarlı çalışma bölgeleridir. Hotspot marker'ı 36px numaralı dairedir; sembol anlamı liste metniyle desteklenir. SVG araç ikonları 18px ve ortak çizgi dilindedir.

## Components

**Düğmeler:** normal kontroller en az 40px, sık authoring araçları en az 42px, form eylemleri en az 44px yüksekliğindedir. Mod düğmeleri masaüstünde 36px, mobilde 40px'tir. Add hotspot mavi ana eylemdir; Save view yükseltilmiş nötr yüzeydir. Hover, focus, pressed, disabled ve placing durumları ayrıdır. Yerleştirme sırasında gerçek araç metni “Placing hotspot…” olur. 44px her yardımcı kontrolde evrensel değildir; fiziksel cihaz erişilebilirliği bütünüyle doğrulanmış sayılmaz.

**Explorer:** arama, ayrı Inspection views / Equipment hotspots disclosure listeleri, sayımlar ve boş durum eylemleri korunur. Satırlar en az 44px; tek satır gerçek ölçüm yaklaşık 44.5px'tir. Liste seçim metni yüksek kontrastlıdır, seçim ARIA ile de belirtilir. Mevcut 32 view / 100 hotspot sınırlarında sanallaştırma yoktur.

**Inspector ve formlar:** bağlamsal seçim veya Save view ile açılır. Girdiler en az 44px, açıklama textarea'sı en az 144px'tir. Save/Cancel belirgindir; Delete açıklamalı ayrı disclosure'dadır. Environment information ve Advanced Camera Calibration ikincil ayrıntılardır. Mevcut kayıt ve kamera controller sözleşmesi korunur.

**Preview:** salt okunur eğitim bilgisi; masaüstünde en fazla 400px köşe kartı, mobilde sahne yüksekliğinin en fazla %70'i olan alt panel. İçerik kayar; başlık/kapatma alanı erişilebilir kalır. Mevcut odak ve Escape davranışı korunur. WISP uygulanmış özellik değildir.

**Ortam ve proje kartları:** gerçek model önizlemeleri korunur; iki ortam eşit keşfedilir. Projeler ayrı gerçek create/rename/delete dialoglarını kullanır; proje açılışı placeholder olmaya devam eder. Ortam başlığındaki stretched düğmeye transform uygulanmaz: pseudo-element'in containing block'unu değiştirip pointer basılıyken tıklama alanını daraltabilir. Kartın görsel alanı gerçek pointer ile ayrıca doğrulanmalıdır.

**Odak ve hareket:** görünür mavi outline; Editor'da 2px ve 2px offset. Kart odağı içe alınır. Editor hover durumları 120ms ease-out renk/border geçişi, ortam kartları 140ms border geçişi kullanır. Sık görevler animasyonla geciktirilmez. Reduced motion yeni UI geçişlerini kapatır. Bu UI kuralları Babylon kamera hareket süresini, yönünü veya cancellation davranışını değiştirmez.

## Do's and Don'ts

- **Do** normal etiketleri okunabilir ölçekte ve araçları tutarlı tıklama alanlarıyla sun.
- **Do** merkezi sahneyi, gerçek authoring akışını ve Preview ayrımını koru.
- **Do** loading, disabled, hata, odak ve seçili durumları görünür tut.
- **Do** gerçek modelleri, kullanıcı verisini ve doğru asset atıflarını koru.
- **Don't** zeytin/yeşil yüzeylere veya asit yeşili vurguya dön.
- **Don't** normal kontrol metnini 9–11px yapma veya okunabilirlik için tarayıcı zoom'una güvenme.
- **Don't** kamera, storage, Babylon lifecycle veya Unity kapsamını görsel iş gerekçesiyle değiştir.
- **Don't** WISP/AI/VR hedeflerini ya da proje placeholder'ını çalışan özellik gibi göster.

Simülasyon öncesi hazırlık istisnası: kullanıcı tarafından seçilen koyu lacivert #090f1c ve sıcak amber #efdfc6, yalnız 10 saniyelik giriş ekranındadır. Ortamın gerçek önizlemesi %27 opacity/8px blur ile fonda; ince Segoe UI Light, 26–36px ve .18em harf aralığı kullanılır. Tek küre 4sn büyür/aydınlanır, 6sn küçülür; metin Breathe in / Slowly exhale. Menü/düğme yok; Escape iptal eder. Dashboard/Editor Studio Neutral tokenları korunur.
