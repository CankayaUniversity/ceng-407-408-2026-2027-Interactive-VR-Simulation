# Proje durumu

21 Ekim 2026 ekip eşitlemesi: hedef web-editor içinde npm ci/build/lint ve 23 test başarılı. Kaynak commit 7c3bdb2 uzak feature branch ve PR #2 head üzerinde doğrulandı. GLB dağıtımı lisans/LFS kota doğrulaması bekliyor; hedef browser kontrolü güvenlik politikasıyla engellendi. Ayrıntılar GITHUB_LATEST_SYNC_REPORT.md ve docs/TEAM_HANDOFF.md içinde.

Son UI kaynak/test denetimi: 20 Ekim 2026. Çekirdek özelliklerin önceki kapsamlı denetimi: 18 Ekim 2026. Bu belge ürünün tamamlandığını iddia etmez.

20 Ekim 2026 Studio Neutral kalite turu: kullanıcı önceki zeytin/küçük kontrol görünümünü reddetti. Çalışan Dashboard ve ortak editör CSS'i nötr palet, 15px gövde/Explorer, 14px araçlar, 40–44px kontroller, 260px Explorer/352px Inspector ve 56px üst bar ile düzeltildi. İki görsel iyileştirme turu, gerçek iki GLB ve Editor/Preview kontrolleri yapıldı. DESIGN.md ve sidecar güncel; ayrıntılı ölçüler, kayıtlar ve doğrulanmayan durumlar VISUAL_QUALITY_REPORT.md içinde. Kalıcı QA verisi oluşturulmadı. 25 CSS dışı kaynak/manifest hash'i aynı; kamera/storage/navigation/lifecycle ve Unity değiştirilmedi. Build/lint ve 23/23 test geçti; son kullanıcı görsel kabulü bekleniyor.

20 Ekim 2026 hazırlık akışı entegrasyonu: Dashboard ortam kartları ve Cockpit Explorer girişi artık ortak SimulationPreparation ekranını açar. Seçilen ortamın mevcut gerçek önizlemesi üzerinde 4sn Breathe in + 6sn Slowly exhale sonrası aynı EnvironmentWorkspace mount edilir. Escape Dashboard'a döner; unmount timer/listener/RAF temizler. Kamera/Babylon/storage kodu değiştirilmedi. Build/lint ve mevcut 23 test geçti. Gerçek browser'da iki ortam hazırlığı, cockpit exhale aşaması, otomatik model açılışı ve Escape dönüşü kontrol edildi. Ayrı HTML denemeleri artık uygulama entegrasyonu yerine gösterilmiyor.

## Uygulanmış frontend

20 Ekim 2026 kart tıklama düzeltmesi: genel button :active transform'u stretched ortam düğmesinin hit alanını mouse-down sırasında daraltıyordu; kart görseline tıklama ortamı açmıyor, başlığa doğrudan tıklama çalışıyordu. `.home-environment-open` basma transform'undan çıkarıldı. Gerçek koordinatlı fare tıklamasıyla cockpit ve hospital görsellerinden workspace açılışı yeniden doğrulandı. Kamera/model/storage kodu değiştirilmedi. Önceki yalnız başlık odaklı smoke testi bu regresyonu kapsamıyordu.

20 Ekim 2026 uygulama güncellemesi: kullanıcı yapısal üretim UI değişikliğini yetkilendirdi. Gerçek Dashboard ve ortak editör yeniden düzenlendi: tek görev toolbar'ı, metin etiketli araçlar, varsayılan kapalı/contextual Inspector, eylemli Explorer, ayrı proje kayıtları ve 1200px altında drawer. DESIGN.md çalışan sonuçtan üretildi. Eski üç prototip onaylanmış sayılmadı. Ayrıntılar kök REDESIGN_IMPLEMENTATION_REPORT.md içinde; 1280/1440/1920 ve 1024/390 browser kontrolleri, gerçek QA authoring/CRUD/hareket senaryoları yapıldı. Geçici veriler temizlendi; üç özgün storage kaydı byte-byte aynı. Build/lint başarılı ve 23/23 test geçti. Kamera/storage/navigation çekirdeği değiştirilmedi; eski performans/fizik sınırları sürer.

20 Ekim 2026 tasarım keşfi: docs/DESIGN_AUDIT.md ve docs/design-exploration/ altında üç izole statik alternatif hazırlandı. Bunlar üretim özelliği veya onaylı tasarım değildir; gerçek model önizlemelerini statik görüntü olarak kullanır, storage/Babylon çalıştırmaz. DESIGN.md oluşturulmadı; yön seçimi insan incelemesini bekliyor. Ayrıntılı doğrulama ve sınırlar audit belgesindedir.

20 Ekim 2026 belge güncellemesi: Impeccable init ile kök PRODUCT.md oluşturuldu. Birincil kullanıcı eğitim içeriği hazırlayan eğitmen / uzman olarak doğrulandı; başarı ölçütü ve gelecek tanıtım/vitrin çalışmaları için yöntem tercihi açık bırakıldı. Bu işlem uygulama kodunu değiştirmedi; önceki test sonuçları yeniden çalıştırılmış sayılmaz.

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
- Arayüz sistem UI fontlarını kullanır, Google Fonts isteği yoktur. Büyük GLB ve lazy Babylon bundle performans sınırıdır.

## Engelli

- WISP: entegre değil. Önceki varlık incelemesinde PR #1/branch bağlantısı 404; model, skeleton, klipler/lisans ve GLB dönüşümü doğrulanamadı. [Hazırlık](WISP_ASSET_PREPARATION.md).
- İlk yayın erişim engeli tarihsel kayıttır. 21 Ekim 2026 mevcut feature branch ve PR #2 Git referansları doğrulandı. Güncel eşitleme sonucu GITHUB_LATEST_SYNC_REPORT.md içindedir; connector API erişimi ayrıca 404 döndü.
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

21 Ekim 2026 son GLB teslimi: kullanici talebiyle iki model Git LFS uzerinden GitHub origin sunucusuna yuklendi (2/2, 133 MB); fsck OK. Yalniz iki model icin ignore istisnasi var. Onceki asset engeli kaydi tarihseldir; kaynak/lisans/atif notlari korunur. Kurulum icin git lfs pull gerekir.
