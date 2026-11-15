# Workplace Studio

Bu klasör projenin tarayıcı prototipidir. Güncel okul deposu
[CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation](https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation);
Unity uygulaması ve güncel proje kapsamı için [kök README](../README.md) ve
[literatür raporu](../docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) kullanılmalıdır.

27 Ekim 2026: yerel Studio arayüzü ve hiyerarşik alanlar, hastanenin 10 kaynak açıklaması ve Piko ile birleştirildi. Ortam öncesinde 10 saniyelik hazırlık ekranı açıktır.

Üniversite bitirme projesi: farklı işyerlerinde kullanılabilecek tarayıcı tabanlı eğitim ortamı editörü. React arayüzü ve Babylon.js görüntüleyicisiyle mevcut GLB ortamında eğitim hotspotları ve inceleme görünümleri hazırlanır.

Hedef pipeline: fotoğraf/video → bağımsız 3D reconstruction → GLB → eğitim editörü. Reconstruction, AI recognition, backend ve Unity/OpenXR native VR bağlantısı bugün uygulanmış değildir. Piko (önceki çalışma adı WISP), sağ altta Idle bekleme animasyonunu oynatır; üzerine tıklayınca veya klavyeyle Enter/Space basınca bir kez el sallayıp beklemeye döner. Konuşma/eğitim rehberliği yoktur. Küçültme düğmesiyle gizlenip Piko düğmesiyle yeniden gösterilebilir.

## Bugün neler var?

- Hiyerarşik eğitim alanları ve rehberli gezinme: [kullanım rehberi](docs/HIERARCHICAL_TRAINING_GUIDE.md).

- Studio Neutral Dashboard: iki eşit ortam kartı, gerçek proje listesi, create/rename/delete diyalogları ve localStorage.
- Boeing 737-800 kokpit ve Charité ameliyathane için ortak Editor Workspace V2; Editor/Preview, Explorer araması, Inspector ve responsive drawer'lar.
- Gerçek mesh üzerinde hotspot yerleştirme/düzenleme/silme ve salt okunur bilgi kartı; gerçek kayıtlı başlık/açıklama.
- Inspection Zones: kamera position/target/FOV kaydı; ilişkili zone'a geçiş tamamlanınca hotspot kartını açma.
- Kalibrasyon, dünya-up yaw/pitch, varsayılan ±85° pitch, wheel FOV zoom; history/overview/reset.
- Editor Spectator ve hastanede deneysel düz zemin Walking.

Dashboard projelerinin açtığı sayfa hâlâ **placeholder workspace**. Proje CRUD ile ortam kayıtları ayrıdır; projeye otomatik ortam bağlanmaz. Çok kullanıcılı ortak düzenleme yoktur.

Durum ve test kapsamı: [PROJECT_STATUS](docs/PROJECT_STATUS.md). Mimari: [ARCHITECTURE](docs/ARCHITECTURE.md). AI asistanları [AGENTS](AGENTS.md) ve [AI_TEAM_CONTEXT](docs/AI_TEAM_CONTEXT.md) okumalı.

## Windows kurulumu

Node.js **24** ve npm ekip standardıdır; Dockerfile da Node 24 kullanır. Denetlenen yerel sürümler: Node 24.18.0 / npm 11.16.0. package.json içinde engines/packageManager veya test script'i yoktur. Lock dosyasındaki Vite 8.3.4 ve React plugin'i Node `^20.19.0 || >=22.12.0` ister; TypeScript strip destekli test akışı için Node 24 kullanın. React 19, TypeScript 6, Babylon 9; kesin sürümler package-lock.json içindedir.

Güncel sürümün kurulumu:

```powershell
git clone https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation.git
cd ceng-407-408-2026-2027-Interactive-VR-Simulation
git fetch origin
git switch main
cd web-editor
npm ci
npm run dev
```

Mevcut branch ve PR #2 head referansları 21 Ekim 2026 tarihinde Git üzerinden doğrulandı. Son eşitlemenin sonucu [GITHUB_LATEST_SYNC_REPORT.md](GITHUB_LATEST_SYNC_REPORT.md) içindedir. Merge yapılmadı. Bağımsız yerel web klasöründe doğrudan npm ci ve npm run dev yeterlidir.

npm ci lock'a göre temiz kurulum yapar; mevcut node_modules yeniden kurulur. npm install bilinçli bağımlılık/lock güncellemesi içindir; package.json ve package-lock.json birlikte incelenir. Yeni klonda npm ci tercih edin.

Vite terminalde gerçek URL'yi gösterir; varsayılan http://localhost:5173/. Çalışan sunucu varsa yeniden kullanın; port doluysa Vite farklı porta geçebilir. Bu denetimde mevcut sunucu 5176 idi; ekip için sabit port değildir.

```powershell
npm run build
npm run lint
node --experimental-strip-types --test tests/*.test.ts
npm run preview
```

Build tsc -b + Vite ile dist/ üretir. Preview hazır build'i yerelde sunar; deployment değildir. 27 Node testi modeller olmadan çekirdek davranışları ve başlangıç hotspotlarının kayıt korumasını doğrular; tam browser/E2E kapsamı değildir.

## Ortam dosyaları

Üçüncü taraf modeller otomatik Git dağıtımına dahil edilmez. Yetkili ekip kaynağından, lisansı bağımsız doğrulanarak kurulmalı:

- `public/environments/cockpit/boeing_737-800_cockpit.glb` — 86.081.540 bayt.
- `public/environments/charite_university_hospital_-_operating_room.glb` — 46.555.672 bayt.

Kaynak/atıf, SHA-256 ve teslim kontrolü: [ASSETS](docs/ASSETS.md). Hastane metadata'sı CC-BY-NC-4.0 beyan eder; ticari kullanım ve yeniden dağıtım koşulları/izinleri bağımsız incelenmelidir. Yerel dosyaya sahip olmak veya LFS kullanmak yayın izni değildir.

Dashboard → Your Environments → **Boeing 737-800 Cockpit** veya **Charité Hospital · Operating Room** kartını açın. Kart görseli model değildir. GLB yoksa yükleme hatası ve Retry loading gösterilir; Dashboard/CRUD kullanılabilir. Dosya adı/büyük-küçük harf ve origin kökünden /environments/... URL'sini kontrol edin.

Gerçek modellerden alınmış yerel ekran görüntüleri `public/previews/environments/` içindedir; [önizleme notları](public/previews/environments/README.md) adları açıklar. Lisans incelemesi tamamlanmadan önizlemeler de yayın paketine eklenmez. Eksik önizlemede yer tutucu kullanılır; açma eylemi korunur. Eski makineye özel ekran kanıtlarının yeni klonda bulunduğu varsayılmaz.

## Editör kullanımı

Hastanede kaynak Sketchfab modelinden alınan 10 ekipman hotspotu ve kamera
bakışı hazır gelir. İngilizce özgün metinler korunur. Önceden kaydedilmiş
hastane içeriğiniz varsa Editor → Scene Explorer → **Add source annotations**
ile yalnız eksik kaynak noktalarını ekleyebilirsiniz; mevcut notlar korunur.
Kaynak/atıf ve aktarım ayrıntıları: [HOSPITAL_ANNOTATIONS](docs/HOSPITAL_ANNOTATIONS.md).

Preview başlangıç modudur. Drag mevcut ters yönle bakar; wheel FOV değiştirir. Kokpit Preview'da WASD yoktur. Editor → Add hotspot → gerçek yüzeye tıklama → başlık/bilgi/isteğe bağlı zone → Save hotspot. Cancel taslağı bırakır. Save Current View gerçek kamera pozunu zone olarak kaydeder. Inspector → Advanced Camera Calibration sayısal ayar içindir; başlangıç kamera/ölçekleri provisional'dır.

Marker ve Saved hotspots aynı navigation eylemini kullanır. Geçerli zone varsa gerçek transition completion beklenir, ardından kart açılır. Eksik zone'da uyarı ve mevcut bakışta bilgi fallback'i vardır; kayıt sessizce değiştirilmez. Direct zone rastgele hotspot açmaz. Escape önce aktif hareket modunu, sonra panel/taslağı, sonra aktif zone'u kapatır; native pointer lock davranışı geçerlidir. Back to Previous View / Back to Overview ayrıdır.

Editor Spectator: W/S bakış yönünde, A/D yatay strafe, Q/E dünya Y ekseninde; Shift boost. Hospital Preview → Start walking: yatay WASD ve sabit referans zemin/göz yüksekliği, Q/E yok. Canvas odağı gerekir; UI odağı hareketi keser. Walking duvar/ekipman collider'ı, gravity veya fiziksel güvenlik garantisi içermez. Spectator serbest uçuş aracıdır.

## Kayıt ve yedek

Projeler `workplace-studio.projects.v1`; ortamlar `workplace-studio.environment-editor.v1.<encodedEnvironmentId>` anahtarındadır. Başka tarayıcı/host/port ayrı kayıttır. Git clone eğitim notlarını taşımaz. Cloud backup, export/import ekranı ve ekip senkronizasyonu yoktur. Veri silmeden önce ilgili anahtarları güvenli yere yedekleyin; kurtarma için localStorage.clear kullanmayın. Bozuk/uyumsuz kayıt korunur, yazma kapatılır.

## Docker geliştirme

Docker Desktop Linux engine açıkken **web dizininde**, Unity kökünde değil:

```powershell
docker compose config
docker compose up -d --build
docker compose ps
docker compose logs --tail 100 frontend
docker compose exec frontend npm run build
docker compose exec frontend npm run lint
docker compose exec frontend node --experimental-strip-types --test tests/*.test.ts
docker compose down
```

Host http://localhost:5175/, container 0.0.0.0:5173. Yalnız frontend; /app bind mount ve /app/node_modules named volume. Başlangıç npm ci ile lock eşleşir. Compose Windows polling kullanır; yerel akış değişmez. down volume'u korur; down -v bağımlılık volume'unu siler.

Bu denetimde **compose config geçti; Linux engine pipe bulunamadığından container/HTTP doğrulanamadı**. Docker config değiştirilmedi; çalışan Docker ortamı iddiası yoktur.

## Sorun giderme / sınırlar

- GLB yok veya HTML yanıtı: dosyayı doğru yere kurun; SPA fallback'i 200 HTML döndürebilir. HTTP 200 yerine gerçek model/Environment loaded durumu doğrulanır.
- Boş görünüm: model yolu, WebGL/GPU ve console kontrol edin; Retry loading/Reset Camera kullanın. Dünya ölçeği kalibre edilmiş değildir.
- Hareket yok: doğru mode, Spectator/Start walking ve canvas odağını kontrol edin. Text field odağı hareketi engellemelidir.
- Kayıt görünmüyor: origin/port/id kontrol edin. Model URL/transform değişimi uyumsuzluk yaratabilir; migration yoktur.
- Docker pipe: Docker Desktop Linux engine'i açın; config doğrulaması çalışan servis kanıtı değildir.
- Büyük lazy workspace >500 kB; 82/44 MiB modeller düşük donanımda maliyetlidir. Tam collision ve marker occlusion yoktur.
- Arayüz Segoe UI Variable / Segoe UI / sistem sans ailesini kullanır; harici Google Fonts isteği yoktur.
- Mutlak asset URL'leri origin kökünü varsayar. Kaynakların web-editor/ altında olması URL'nin otomatik /web-editor/ altında çalıştığı anlamına gelmez; alt yol deployment ayrı iştir.

Ekip akışı: [CONTRIBUTING](docs/CONTRIBUTING.md). Yayın planı: [PUBLISHING](docs/PUBLISHING.md).


## Ekip ve AI devri

Yapılanlar, son tasarım kararları, sınırlar ve sonraki öneriler: [TEAM_HANDOFF](docs/TEAM_HANDOFF.md). 4sn Breathe in / 6sn Slowly exhale hazırlığı şimdilik kapalıdır; iki ortam doğrudan editörde açılır. Bağımsız Resq landing denemesi ürünün parçası değildir.

## GLB modellerini indirme — son guncelleme

Iki ortam modeli artik bu feature branch uzerinden Git LFS ile paylasilir. Onceki haric/ayri teslim ifadeleri bu iki GLB icin tarihseldir; diger asset ve JPEG onizlemeleri halen harictir. Git LFS kurulu olmali. Repository kokunde git lfs install, git pull --ff-only ve git lfs pull komutlarini calistirin. Iki GLB icin beklenen boyut/hash docs/ASSETS.md icindedir. 130 bayt civari dosya LFS pointeridir; model degildir. Lisans/kaynak/atif notlari korunur; kullanici ekip paylasimini istedi, bagimsiz hukuki dogrulama yapilmadi.


27 Ekim 2026 birleştirme: yerel Studio arayüzü, hazırlık ekranı ve hiyerarşik eğitim alanları; codex/hospital-source-hotspots üzerindeki 10 kaynak hastane açıklaması ve Piko ile birleştirildi. Ortak Babylon editörü, v1 anahtarları ve hiyerarşi yedeği korunur. Kaynak noktalar mevcut kaydı değiştirmeden yüklenir; eksikleri ekleme açık kullanıcı eylemidir.

## 14 Kasım 2026: üniversite deposu model dağıtımı

Üniversite deposunun main dalında kokpit (86.081.540 bayt) ve hastane
(46.555.672 bayt) GLB dosyaları normal Git bloblarıdır; temiz klon için
Git LFS kurulumu gerekmez. Önceki LFS kurulum notları tarihsel dallar içindir.
Altı özgün kaynak dalın uçları ve commit SHA değerleri değiştirilmedi.
Bu tarihsel dallarda LFS pointer dosyaları bulunabilir; hedefte LFS kapalı
olduğundan bu dallar tek başına modelleri geri yükleyemeyebilir.
Main üzerindeki gerçek GLB dosyalarını kullanın. Mevcut atıflar ve kokpitin
CC BY 4.0 / hastanenin CC BY-NC 4.0 koşulları aynen geçerlidir.
