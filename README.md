# interactive-vr-simulation

2 Kasım 2026: `codex/combined-final` tabanından Unity masaüstü uygulamasına
geçiş başlatıldı. 14 Kasım 2026 paylaşımı `codex/unity-desktop-2026-11-14` dalında,
`Assets/_Project/Desktop` altındadır. İlk hedef Windows, WASD/fare,
yerel mesh/GLB, alan/bilgi noktası editörü ve yerel proje kaydıdır.

**İlk Windows uygulaması üretildi ve bu bilgisayarda denendi.** Unity
6000.6.7f1, 20 çekirdek/sözdizimi kontrolü ve 16 Unity testi geçti.
`Unity_Uygulama_Baslat.cmd` yerel çıktıyı açar. Build klasörü üretilmiş
çıktıdır; Git'e dahil değildir. Hastane/kokpit, Piko ve yerel kayıt açılışı
kontrol edildi; AI ve VR gözlük entegrasyonu henüz eklenmedi.

- [Masaüstü kurulum/kullanım/derleme](docs/DESKTOP_APP.md)
- [Masaüstü mimarisi](docs/DESKTOP_ARCHITECTURE.md)
- [Gerçek doğrulama ve kalan işler](docs/DESKTOP_VALIDATION_2026_11_02.md)
- [Son değişikliklerin açıklaması ve ilk çalıştırma](docs/CHANGES_UNITY_DESKTOP.md)
- [Hastane zemin düzeltmesinin doğrulaması](docs/DESKTOP_VALIDATION_2026_11_14.md)

Mevcut web sürümü `web-editor` altında korunur. Unity projesi olarak
repository kökünü açın; native sahne
`Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity` dosyasıdır.

A domain-agnostic, cross-platform VR training platform built in Unity to streamline learning on complex machinery. By digitizing physical equipment into interactive 3D assets with embedded video guides, it delivers intuitive procedural onboarding across both immersive VR and standard screens.
