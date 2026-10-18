# Ekip devri ve GitHub yayın raporu

Tarih: 18 Ekim 2026.

**Belgeler ve yerel denetim tamamlandı. GitHub yayını kimlik doğrulama/erişim nedeniyle engelli. Commit, push veya PR oluşturulmadı.**

## 1. Hazırlanan belgeler

Güncellendi:

- README.md: gerçek onboarding, Node/npm metadata'sı, komutlar, Docker doğrulama sınırı, model kurulumu, kontroller, localStorage ve sorun giderme.
- AGENTS.md: artık geçersiz minimal başlangıç kapsamı güncel ortak editör kurallarıyla değiştirildi; entry point/yapı, erişilebilirlik, npm/test, Türkçe rapor ve Docker sözleşmesi korundu. Storage/Unity/Git/kapsam güvenliği açık hale getirildi.
- .gitignore: additive yerel cache/AI klasörü, model kaynakları, WISP ve lisansı incelenmemiş JPEG önizleme kuralları. Dosyalar yerelde silinmedi.

Eklendi:

- docs/ARCHITECTURE.md.
- docs/CONTRIBUTING.md.
- docs/PROJECT_STATUS.md.
- docs/AI_TEAM_CONTEXT.md; her AI için kısa başlangıç prompt'u dahil.
- docs/ASSETS.md; boyut/hash/izinli teslim ve dağıtım sınırı.
- docs/PUBLISHING.md; erişim sonrası yeni sibling clone, izin listesi, Unity koruma kontrolleri ve PR adımları.
- Bu rapor.

Uygulama TS/TSX/CSS, registry, persistence, test kodu, package/lock, Vite ve Docker değiştirilmedi. Yeni UI/özellik eklenmedi. Eski raporlar ve özgün asset kaynakları silinmedi.

## 2. Gerçek proje durumu

React + TypeScript + Vite + Babylon ortak web editörü mevcut. Dashboard yerel proje CRUD, iki ortam kartı, read-only Preview ve Editor Workspace V2; zone/camera view, hotspot yerleştirme/düzenleme/kayıt ve zone-aware navigation kaynakta uygulanmış. Kamera dünya-up/pitch clamp ve Editor Spectator mevcut. Hospital Walking düz zemin prototipidir; fizik/duvar collision değildir.

Proje kartı workspace'i placeholder'dır; projeler otomatik ortama bağlanmaz. Storage yalnız origin bazlı localStorage'dır, ekip senkronizasyonu veya API yoktur. WISP entegre değil; reconstruction/AI recognition, native Unity/OpenXR VR ve web-native interoperability gelecek plandır. Önceki raporlardaki Unity WebGL demo gibi tarihî işler güncel özellik sayılmadı.

## 3. GitHub / Git sonucu

- Hedef: https://github.com/Cengaverler/interactive-vr-simulation.
- İstenen feature branch: feature/web-editor-integration — **oluşturulmadı**.
- Hedef base: main — uzaktan okunamadı; son SHA doğrulanamadı.
- Integration commit SHA: **yok**.
- PR URL: **yok**. Review bekleyen oluşturulmuş PR bulunmuyor; yayın engelli.

İlk status/branch/remotes denetiminde yerel web dosyaları zaten untracked, mevcut branch adı main ve remote listesi boştu. HEAD doğrulamasında mevcut commit bulunmadı (unborn main). Orijinal dizin stage/reset/clean/checkout yapılmadan korundu.

GitHub connector repository metadata isteği 404 Not Found verdi. Yerel git ls-remote --symref ilk denemede ağ ortamında getaddrinfo hatası verdi; ağ izinli tekrarında credential.interactive=false ile **Cannot prompt because user interactivity has been disabled / unable to get password from user** sonucuna ulaşıldı. Bu, güvenli mevcut kimlikle remote okumanın tamamlanmadığını gösterir; kullanıcıdan token istenmedi veya sır yazdırılmadı. API 404'ün depo yokluğu mu izin eksikliği mi olduğu kesinleştirilemedi.

Bu nedenle yeni sibling checkout, fetch main, kopyalama, commit/push ve PR adımlarına geçilmedi. Yerel projeye remote eklenmedi, ilişkisi olmayan repository history'leri birleştirilmedi. Main/force push veya otomatik merge yapılmadı.

## 4. Unity koruma sonucu

Uzak Unity ağacı erişilemediği için root dosya envanteri/hash karşılaştırması veya web-editor/ mevcut mu kontrolü yapılamadı. **Unity dosyalarına hiçbir yazma/kopyalama/silme yapılmadı.** Böylece bu oturumda uzak Unity dosyaları değiştirilmedi; başarılı integration diff kontrolü yapılmış gibi sunulmuyor.

Erişim sonrası yeni checkout'ta root talimatları okunmalı; web-editor varsa durulmalı. Staged tüm yolların yalnız web-editor/ altında yeni dosyalar olduğu doğrulanmalı. Unity root README/ignore değişikliği bu plan için gerekli değil. Kesin komutlar docs/PUBLISHING.md içinde.

## 5. Asset ve yayın adayları

>20 MiB denetimi node_modules, dist ve .git gibi generated/metadata dizinleri dışındaki yerel proje içeriğinde yapıldı:

- public/environments/cockpit/boeing_737-800_cockpit.glb: **86.081.540 bayt**, yaklaşık 82,09 MiB; SHA-256 `a950013ef6c7570c64810b4d49486449f17220fae489e2c989385c69c22f2e0c`.
- public/environments/charite_university_hospital_-_operating_room.glb: **46.555.672 bayt**, yaklaşık 44,40 MiB; SHA-256 `951401d851d45c1390a585bdba7ad14b020417e119a7fcf959f3133f8a0168b0`.

Hash'ler bu oturumda yeniden hesaplandı; asset notlarıyla eşleşti. İki GLB ile JPEG önizlemelerin ignore kapsamında olduğu git check-ignore ile doğrulandı. Otomatik commit/upload yok.

Yayın izin listesi: src/tests/scripts, gerekli package/lock/TS/Vite/lint/Docker dosyaları, güncel README/AGENTS/docs, bu rapor ve plain-text asset/preview notları. **Henüz hiçbir dosya yayınlanmadı.**

Hariçler: nested .git, bağımlılık/build/cache/temporary çıktıları, .env/sırlar, GLB/FBX/Blend/bin/animasyon/doku kaynakları, WISP, kaynak model haklarına bağlı dört JPEG ve makineye özel yollar içeren geçmiş CODEX/GPT raporları. Geçmiş raporlar yerelde korunur; gerekli bilgiler yeni docs içinde özetlenmiştir.

Hospital metadata'sının CC-BY-NC-4.0 beyanı bağımsız incelenmelidir; ticari kullanım/yeniden dağıtım izni varsayılmadı. Kokpit metadata'sı CC-BY-4.0 beyan eder, bu da bağımsız kaynak/atıf doğrulamasına tabidir. Git LFS izin sorununu çözmez. Yerel public asset'leri build sırasında dist'e kopyalanır; dist yayınlama izni verilmiş sayılmaz.

## 6. Sır ve kişisel bilgi denetimi

Kaynak/belge/config içeriklerinde yaygın GitHub token, AWS access key, özel anahtar bloğu ve API key biçimleri için değerleri yazdırmayan dosya tabanlı regex taraması yapıldı; bu imzalara eşleşme bulunmadı. Bu, eksiksiz güvenlik denetimi veya sır yokluğu garantisi değildir.

Bazı eski raporlarda kişisel Windows kullanıcı yolları bulundu; bunları değiştirmek/silmek yerine ilk yayın izin listesinin dışında bıraktık. Yeni ekip belgeleri taşınabilir göreli yollar kullanır. Kaynak attribution yazar/linkleri beklenen asset köken bilgileridir. Staging olmadığı için staged diff secret review tamamlandı iddiası yok; erişim sonrası gerçek aday liste tekrar taranıp insan tarafından incelenmelidir.

Son izin listesi denetimi 58 dosyayı kapsadı: yaygın sır biçimleri/literal credential ve kişisel Windows yol eşleşmesi 0, kırık yerel Markdown bağlantısı 0, 20 MiB üzeri aday dosya 0. Bu kontrol yayın paketinin kopyalanması veya staged diff incelemesinin yerini tutmaz.

## 7. Gerçek test sonuçları

- npm run build: **başarılı**, çıkış 0. TypeScript + Vite; lazy EnvironmentWorkspace yaklaşık 1.401,24 kB ve mevcut >500 kB uyarısı sürüyor.
- npm run lint: **başarılı**, çıkış 0.
- node --experimental-strip-types --test tests/*.test.ts: **23/23 başarılı**, başarısız/atlanan yok. Kamera yönü/yol, storage/missing-zone, ortam izolasyonu, navigation completion/cancellation ve Spectator/Walking input çekirdeği.
- docker compose config: **başarılı**. Tek frontend, 127.0.0.1:5175→5173, /app bind ve ayrı node_modules volume doğrulandı.
- docker compose ps ve docker version server kontrolü: **başarısız**, Docker Desktop Linux engine pipe yok. Başlatma/log/HTTP turu yapılmadı; mevcut yapı bu görevde değiştirilmedi.
- Mevcut uygulama içi Chromium/5176 üzerinde Dashboard → Cockpit gerçek GLB Preview → Editor araçlarının görünmesi → Dashboard → Hospital gerçek GLB Preview/Walking kontrolleri → Dashboard smoke testi yapıldı. Gerçek geometri görsel olarak kontrol edildi; yalnız boş canvas veya durum metniyle yetinilmedi. Son error/warn console çıktısı boştu.
- Kullanıcı eğitim kaydı eklenmedi/silinmedi. Bu turda tam CRUD, placement, zone save/delete, WASD, responsive/reduced-motion ve tüm Escape/history akışları tekrar manuel yürütülmedi; kod/test mevcutluğu bunların tüm cihazlarda çalıştığının kanıtı değildir.
- Eksik asset ve önizleme fallback kaynak yolları incelendi; orijinal dosyalar kaldırılmadan assetsiz temiz clone browser testi yapılmadı.
- Yeni sibling web-editor içinde npm ci/build/lint/test/relative path doğrulaması **yapılmadı**, çünkü checkout yok. Yerel test başarısı kopyanın doğrulanması olarak sunulmadı.

Yerel Dashboard denetim görüntüsü: çalışma oturumunun ekran kanıtı klasöründe `team-handoff-dashboard-audit.png`. Modelden türeyen görseller ilk kaynak yayınına dahil değildir; ekran kanıtının yeni clone'da bulunacağı varsayılmaz.

## 8. Ekibin devam adımları

1. Güvenli Git Credential Manager veya ekibin onaylı yöntemiyle GitHub hesabına/organization SSO'ya erişimi tamamlayın; repository read/write iznini doğrulayın. Şifre/token sohbete konmaz.
2. docs/PUBLISHING.md üzerinden main'i okuyun, yeni sibling clone oluşturun, root talimatları ve web-editor çakışmasını kontrol edin. Feature branch'i son origin/main'den oluşturun; orijinal web klasörüne dokunmayın.
3. Yalnız izin listesini web-editor/ altına kopyalayın; modeller/önizlemeler hariç. Orada npm ci/build/lint/test ve gerçek assetsiz hata testi yapın.
4. Sır/kişisel yol/büyük dosya/staged Unity diff kontrollerini geçince yalnız feature/web-editor-integration'a normal push ve main'e PR açın. SHA/URL'yi bu raporda gerçek sonuçlarla güncelleyin; merge etmeyin.
5. Yayın doğrulandıktan sonra takım README'deki git clone → feature branch checkout → cd web-editor → npm ci → npm run dev adımlarını izler. Lisanslı GLB'leri docs/ASSETS.md yollarına ayrıca kurar. Main'e merge olmadan main'de web-editor beklenmez.

Belgeler incelemeye hazırdır; yayın engeli giderilene kadar GitHub entegrasyonu tamamlanmış değildir. Bu kapsamda duruldu; kullanıcı/ekip incelemesi bekleniyor.
