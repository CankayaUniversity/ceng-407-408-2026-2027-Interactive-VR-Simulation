# Mimari

18 Ekim 2026 çekirdek kaynak denetimi; 20 Ekim 2026 UI kompozisyonu güncellemesi. Ayrıntılar [ENVIRONMENT_CONTRACT](ENVIRONMENT_CONTRACT.md) içinde; raporlar yerine güncel kod/testler esas alınır.

## Yapı ve sorumluluklar

- `src/main.tsx`: StrictMode başlangıcı ve ortak CSS.
- `src/app/App.tsx`: Dashboard/proje placeholder/ortam seçimi, native ProjectDialog; lazy EnvironmentWorkspace + Suspense ve EnvironmentBoundary.
- `src/features/projects/`: Dashboard, registry'den EnvironmentGallery, Project tipi, useProjects ve diyaloglar.
- `src/features/environments/types.ts` Babylon'dan bağımsız JSON tiplerini, `registry.ts` gerçek id/modelUrl/transform/kamera/interaction/attribution verisini tutar.
- `EnvironmentWorkspace` → `BabylonViewport`: tüm ortamlar için ortak V2 UI ve runtime.
- `useEnvironmentEditor`: mode/draft/selection/history, kayıt ve eylemler. `editorStorage`: doğrulama ve yerel kalıcılık.
- `viewerInteraction`: controller, move Promise, picking, marker projeksiyonu, zoom ve hareket araçları.
- `cameraOrientation`, `cameraPath`, `inspectorNavigation`, `hotspotNavigation`: yönelim, sınırlı yol kontrolü, odaklı hareket ve seçim sıralaması.
- `WorkspacePanels` (AuthoringHelp dahil), `EnvironmentEditorPanels`, `HotspotInfoCard`, `WorkspaceIcon`: Explorer, eyleme bağlı Inspector/form, ortak SVG araç ikonları ve read-only sunum. Görünür görev toolbar'ı BabylonViewport'un UI bölümündedir; sağ panel ilk açılışta kapalı, 1200px altında tek drawer vardır.
- `src/styles/`: ortak, Dashboard kapsamlı ve editor-v2 kapsamlı CSS.
- `scripts/inspect-glb.mjs`: yerel GLB metadata/bounds/hash; reconstruction değildir.
- `tests/`: Node runner, bazı testlerde gerçek Babylon NullEngine; browser otomasyon paketi yoktur.

App gezinmesi bellek state'idir; URL router, deep-link yükleme veya browser history route sistemi yoktur. Dashboard anchor'ları bölüm bağlantısıdır.

## Ortam, modlar ve kamera

SceneDefinition id/displayName/modelUrl/modelTransform/navigationMode/camera ve isteğe bağlı interaction/attribution taşır. NavigationMode yalnız seated | walking'dir. Kaynak node dönüşümleri korunur; sahne sağ el/Y-up. Ölçeğin metre karşılığı doğrulanmamıştır.

Preview read-only; Editor açık placement/draft/Save ile yazar. Spectator Editor içi oturumluk serbest kamera aracıdır, yeni NavigationMode/storage alanı değildir: W/S tam bakış yönü, A/D yatay, Q/E dünya Y. Hospital Preview Walking düz referans zemine sabit yatay WASD'dir. Ortak hareket çekirdeği kullanılır; aynı anda aktif olmazlar. UI odağı/blur/mode/lock exit/unmount girdiyi temizler. Tam fizik/collision yoktur.

InspectionZone environmentId + sahne koordinatında position/target/FOV kaydeder. Hotspot position model kökü yerelidir; targetNode yardımcı isim, zoneId isteğe bağlı. Marker/liste → navigateAndOpenHotspot → gerçek controller completion → yalnız istenen kart. Revision eski istekleri geçersiz kılar; timeout tahminiyle kart açılmaz. Eksik zone uyarılı fallback; direct zone hotspot açmaz.

Move sonucu completed | cancelled | blocked Promise'ıdır. Capture gerçek pozu alır. Dünya-up yaw/pitch, sıfır roll, varsayılan ±85° pitch; yaw serbest. Mouse yönü ve 2200 hassasiyet korunur. setTarget'ın dikey singularity/konum kaydırması kullanılmaz. Wheel FOV zoom'dur, dolly değildir.

## React / Babylon ayrımı

React mode, draft, UI ve seri veriyi yönetir. Engine/Scene/camera/container/controller ref/effect kapsamındadır. Viewport effect'i definition/attempt ile yenilenir; panel/arama güncellemesi engine oluşturmaz. Workspace ortam key'i değişince cleanup çalışır.

LoadAssetContainerAsync ortamı yükler; tek engine render loop, ResizeObserver → engine.resize. Geç asset sonucunda disposed kontrol edilir. Çıkışta input/listener/controller/observer, pointer lock, loop, scene/engine temizlenir. Marker DOM projeksiyonu her kare React state yazmaz. Her marker için her kare occlusion ray yoktur. Kamera yolu beş ray ile sınırlı kontroldür; hacimsel collision değildir.

`features/mascot/PikoMascot` kullanıcının doğrulanmış GLB'sini ayrı küçük şeffaf sahnede gösterir. App içinde bağımsız lazy/Suspense sınırındadır; Dashboard veya ortam açılışını bekletmez ve mod/ortam değişiminde yeniden mount edilmez. Idle döngüdedir; semantik portre düğmesi tıklama/dokunma/Enter/Space ile Wave'i bir kez başlatır. Wave tamamlanma olayı Idle'a döndürür; aynı anda tek klip çalışır, art arda tıklama hareketi kesmez. Kamera Idle başlangıcı ve örneklenmiş Wave sınırlarını kapsar. Portre döngüsü en fazla 30 fps'dir; gizlenen belge duraklatılır. Reduced motion altında Idle başlangıcı sabittir, yalnız kullanıcı eylemiyle Wave oynar. ResizeObserver yalnız boyut/kadrajı günceller; her kare React state yazılmaz. Model sahnesi ortamı çoğaltmaz. Küçültme/unmount geç yüklemeyi geçersiz kılar; observer/listener/loop/scene/engine temizlenir. Konuşan eğitim rehberi yoktur. Kaynak teslimi için `public/guides/piko/README.md` esas alınır; önceki WISP erişim engeli tarihsel kayıttır.

## Kalıcılık

`SceneDefinition.starterContent` isteğe bağlı, paketle gelen başlangıç
hotspot/view içeriğidir. `loadEditor` yalnız kayıt yoksa doğrulayıp kopyalar;
var olan kayıtları birleştirmez veya değiştirmez. Editor'deki açık kullanıcı
eylemi `addStarterContent` ile yalnız eksik ID'leri ekler; mevcut içerik
önceliklidir ve kaydetme mevcut şema v1 sınırlarını kullanır. Hastanenin
10 kaynak açıklaması ve koordinat dönüşümü `hospitalAnnotations.ts` içindedir.

- Proje anahtarı `workplace-studio.projects.v1`: id/name/description/createdAt/updatedAt doğrulanan dizi. Sürüm anahtar adındadır, nesnede schemaVersion yoktur. Ortam/proje ilişkisi yoktur; proje workspace'i placeholder.
- Ortam anahtarı `workplace-studio.environment-editor.v1.${encodeURIComponent(environmentId)}`. SerializedEditorV1: schemaVersion=1, environmentId, modelUrl, modelTransform, zones, hotspots. En fazla 32 zone / 100 hotspot; id/koordinat/metin/model eşleşmesi kontrol edilir.
- SerializedSceneV1 geleceğe yönelik tiptir; mevcut editor kayıt formatı değildir.
- Bozuk/bilinmeyen sürüm/model uyuşmazlığı yazmayı kapatır, ham veriyi silmez. Eksik zone uyarılı fallback'tir; migration yoktur.
- Origin/tarayıcı bağımsızdır. Cloud, export/import UI, storage-event birleştirme, ortak edit ve conflict resolution yoktur.

## Yeni ortam / gelecek pipeline

1. Kaynak/dağıtım iznini [ASSETS](ASSETS.md) içinde kaydedin; GLB'yi doğrulayın.
2. Koordinat, ölçek, transform, materyal ve gerçek başlangıç pozunu inceleyin.
3. Registry'ye benzersiz/kararlı id ile SceneDefinition ekleyin. Walking explicit ground/eye-height/bounds ister; seated için fizik uydurmayın.
4. Galeri registry'den oluşur; isteğe bağlı önizlemeler `<id>.jpg` / `<id>-640.jpg`. Loader/editör kopyalamayın.
5. Gerçek yükleme, storage izolasyonu, zone/placement/kamera ve Dashboard dönüşünü test edin. Eski id/modelUrl/transform değişimi migration tasarımı gerektirebilir.

Gelecek bağımsız reconstruction çıktısı doğrulanmış GLB + kaynak/ölçek/lisans/kalite metadata'sı olmalıdır. Bugün fotoğraf/video upload, reconstruction/recognition ve model import API/UI'si yoktur.

Gelecek backend kimlik/yetki, model/JSON sürümleri, doğrulama, revision/concurrency ve paylaşımı yönetebilir. Babylon runtime nesneleri JSON'a yazılmamalı; localStorage geçişi onaysız yapılmaz. Fotoğraf gizlilik/izinleri ayrıca tasarlanmalıdır.

Unity/OpenXR ayrı standalone VR hedefidir. Ortak repository çalışan interoperability demek değildir. Gelecekte versioned eğitim JSON'u ve koordinat/ölçek/asset sözleşmesi tasarlanabilir; bugün browser→Unity transferi veya native VR akışı yoktur. Unity kökü korunur; web yalnız web-editor/ altında.

## Teknik borç

Büyük lazy bundle/modeller, provisional ölçü/kameralar, sade yol kontrolü, eksik marker occlusion ve yalnız localStorage başlıca sınırlardır. Tam fizik, çok kullanıcı, server persistence, browser E2E ve tüm cihazlarda profiling yoktur. Mutlak asset URL'leri alt yol hosting için ayrı çalışma gerektirir.

## Simülasyon öncesi hazırlık
21 Ekim 2026 itibarıyla kullanıcının isteğiyle `App.tsx` içindeki `SIMULATION_PREPARATION_ENABLED` kapalıdır. Ortam seçimi doğrudan exploring state'ine geçer; preparing boş kalır ve yapay bekleme uygulanmaz. Gerçek model yükleme göstergesi BabylonViewport içinde korunur.

Hazırlık ileride yeniden açılırsa App ortam seçimini önce preparing state'ine alır. Ortak SimulationPreparation seçilen definition.id ile mevcut önizlemeyi kullanır; 4sn inhale / 6sn exhale sonrası exploring'e geçilir. Hazırlık sırasında Babylon mount edilmez. Escape iptalinde Dashboard odağı geri gelir. Timer ve pointer RAF/listener'ları unmount sırasında temizlenir; reduced-motion altında küre/parallax hareket etmez. Yeni kayıt/storage alanı yoktur.
