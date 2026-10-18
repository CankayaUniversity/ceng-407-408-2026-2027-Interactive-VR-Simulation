# Proje durumu

Son kaynak/test denetimi: 18 Ekim 2026. Yeni işlerden sonra bu tarihi, değişen kapsamı, gerçek testleri ve engelleri güncelleyin. Bu belge ürünün tamamlandığını iddia etmez.

## Uygulanmış frontend

- React Dashboard: eşit ortam kartları, kompakt bölüm gezintisi, Projects boş/gerçek listesi, native dialog ile create/rename/delete; origin içi localStorage.
- İki SceneDefinition/registry kaydı: Boeing 737-800 Cockpit ve Charité Operating Room; ortak Babylon loader/viewport/editor.
- Editor/Preview, responsive Workspace V2, Explorer arama, contextual Inspector.
- Inspection Zones ve gerçek camera pose kaydı; hotspot placement/edit/delete/persistence; Preview gerçek bilgi kartı.
- Marker/liste için zone-aware navigation; gerçek Promise completion ve eski isteğin cancellation koruması.
- World-up yaw/pitch, pitch clamp, ters drag/hassasiyet, FOV wheel, calibration/reset/history.
- Editor Spectator: WASD/QE/Shift ve focus/blur/exit koruması.

Bu maddeler kaynakta mevcuttur; “tüm cihaz/koşullarda doğrulanmış üretim özelliği” anlamına gelmez. Proje CRUD sayfası placeholder'dır; proje→ortam ilişkisi uygulanmadı.

## Deneysel / kısmi

- Hospital Preview Walking: sabit referans zemin/göz yüksekliği, yatay WASD, kaba bounds; duvar/ekipman collision, gravity/navmesh/step yoktur.
- Başlangıç kamera/world scale provisional. Kamera yolu sınırlı ray kontrolüdür; full-body collision değildir.
- Marker occlusion her kare hesaplanmaz; örtülü yüzeylerde marker önde görünebilir.
- localStorage kalıcılığı var, server/cloud/çok kullanıcılı paylaşım ve export/import UI yoktur.
- Google Fonts harici ağ ister; fallback vardır. Büyük GLB ve lazy Babylon bundle performans sınırıdır.

## Engelli

- WISP: entegre değil. Önceki varlık incelemesinde PR #1/branch bağlantısı 404; model, skeleton, klipler/lisans ve GLB dönüşümü doğrulanamadı. [Hazırlık](WISP_ASSET_PREPARATION.md).
- Ekip deposuna web-editor/ yayını: bu denetimde GitHub API 404; ağ izinli git ls-remote kimlik doğrulama alamadı. Yeni checkout, feature branch, commit, push veya PR oluşturulmadı.
- Docker runtime: compose config başarılı, Docker Desktop Linux engine pipe bulunamadı. Container/HTTP doğrulaması yapılmadı.

## Henüz uygulanmayan hedefler

Fotoğraf/video reconstruction ve AI recognition; backend/API/auth/object storage; ekip edit/senkronizasyonu; versioned server kayıtları; Unity/OpenXR standalone VR ve browser/native veri aktarımı; WISP guide ve animasyonları. Bunlar mevcut UI'da çalışan özellik gibi sunulmamalıdır.

## Bu denetimin doğrulaması

- npm run build ve npm run lint başarılı.
- `node --experimental-strip-types --test tests/*.test.ts`: 23/23. Kamera yönü/yol, storage izolasyonu, zone-aware seçim/cancellation, Spectator/Walking input ve controller completion testleri.
- Build mevcut >500 kB lazy workspace uyarısını veriyor.
- Mevcut 5176 sunucusunda Dashboard → kokpit gerçek GLB/Preview → Editor kontrolleri → Dashboard → hastane gerçek GLB/Walking kontrolleri → Dashboard smoke testi yapıldı; console error/warn çıktısı boştu. Ayrıntılar [yayın raporunda](../GITHUB_TEAM_HANDOFF_PUBLISH_REPORT.md). Tüm CRUD/placement/keyboard/device senaryoları bu denetimde yeniden yürütülmedi.
- Eksik asset loader/fallback kaynak yolu incelendi; assetsiz temiz klon tarayıcı testi henüz yapılmadı.

## Bağımsız katkı alanları

- Onboarding ve hata tekrar örnekleri; erişilebilirlik/keyboard denetimi.
- Asset kaynak/lisans sorumluluğu ve izinli teslim prosedürü.
- Browser test planı ve düşük donanım performans ölçümü.
- WISP kaynaklarının doğru rig/animasyon/GLB teslimi; entegrasyon öncesi varlık doğrulaması.
- Collision/navmesh ve native VR veri sözleşmesi için tasarım önerileri; uygulama ayrı onay gerektirir.

Ortak kamera/storage/viewport dosyalarını paralel değiştirmeden ekipte kapsam belirleyin. Her katkıda gerçek sonuçlar ve test edilmemiş kısımlar yazılmalı.
