# Interactive VR Simulation

Çankaya Üniversitesi CENG 407/408, 2026-2027 bitirme projesi. Uygulamanın
çalışma adı **Workplace Studio**; ilk eğitim odağı yeni başlayan hemşirelerdir.
Güncel depo: [CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation](https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation).

**CENG 407 academic deliverable:** Literature and Technology Review — 15 November 2026 · [PDF](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) · [Markdown](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.md) · [Documentation index](docs/README.md)

Unity masaüstü uygulaması `Assets/_Project/Desktop` altındadır. Güncel hazırlık
dalı `codex/nursing-room-literature-2026-11-15`; okul deposunun `main` dalı
üzerine hemşire odası ve literatür raporu eklenmiştir. İlk hedef Windows,
WASD/fare, mesh/GLB, alan/bilgi noktası editörü ve yerel proje kaydıdır.

**İlk Windows uygulaması üretildi ve bu bilgisayarda denendi.** Unity
6000.6.7f1, 25 çekirdek/sözdizimi kontrolü ve 17 Unity testi geçti.
`Unity_Uygulama_Baslat.cmd` yerel çıktıyı açar. Build klasörü üretilmiş
çıktıdır; Git'e dahil değildir. Hastane/kokpit, Piko ve yerel kayıt açılışı
kontrol edildi; AI ve VR gözlük entegrasyonu henüz eklenmedi.

- [Masaüstü kurulum/kullanım/derleme](docs/DESKTOP_APP.md)
- [Masaüstü mimarisi](docs/DESKTOP_ARCHITECTURE.md)
- [Gerçek doğrulama ve kalan işler](docs/DESKTOP_VALIDATION_2026_11_02.md)
- [Son değişikliklerin açıklaması ve ilk çalıştırma](docs/CHANGES_UNITY_DESKTOP.md)
- [Hastane zemin düzeltmesinin doğrulaması](docs/DESKTOP_VALIDATION_2026_11_14.md)
- [Yeni Nursing Skills Room ve cihaz etkileşim altyapısı](docs/NURSING_ROOM.md)
- [Nursing Skills Room son kontrolleri](docs/NURSING_ROOM_VALIDATION_2026_11_14.md)

**Nursing Skills Room:** Özel hazırlanmış tek yataklı hasta odası; canlı ekranlı
monitör, kapak animasyonlu pompa, ayarlanabilir yatak ve ışıklı hemşire çağrısı.
Cihaz listesi yakın görünümleri açar; cihaz eylemleri gelecekteki eğitim akışları
için bağımsız Core olayları üretir. Blender kaynağı ve yerel GLB dosyaları
`desktop-content/nursing-room` içindedir.

Mevcut web sürümü `web-editor` altında korunur. Unity projesi olarak
repository kökünü açın; native sahne
`Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity` dosyasıdır.

The complete project is planned to combine photograph/video-based AI reconstruction,
instructor authoring, and desktop/VR training. AI execution will support local
processing for privacy-sensitive use and explicitly selected online services.
The current desktop prototype implements local editing and basic equipment
practice; reconstruction, selectable online processing and headset integration
remain planned project deliverables. See the literature review for the full scope.

## WISP maskotu

Animasyonlu robotun Unity FBX dosyası, beş hareketi, GLB kopyası ve Blender
kaynakları `Assets/_Project/Art/Models/Wisp` klasöründedir.
[Önizleme ve kullanım rehberi](Assets/_Project/Art/Models/Wisp/README.md).
