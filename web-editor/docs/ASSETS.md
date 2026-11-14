# Asset kurulumu ve yayın sınırı

22 Ekim 2026: kullanıcının kaynak hotspot aktarımı isteğiyle Charité sayfası
incelendi. Digital Surgery / CC-BY-NC-4.0 ve yayın atfı sayfada doğrulandı.
10 açıklama ile konum/kamera metaverisi `hospitalAnnotations.ts` içindedir;
GLB değişmedi. Kaynak metinler korunur, koordinatlar dönüştürülür. Tam atıf
ve kapsam: [HOSPITAL_ANNOTATIONS](HOSPITAL_ANNOTATIONS.md). Aşağıdaki önceki
web doğrulaması yapılmadı ifadeleri bu yeni kaynak incelemesinden öncedir.

## Kullanıcının Piko maskotu

21 Ekim 2026 kullanıcının açık entegrasyon isteğiyle son maskotun değişmemiş
GLB kopyası `public/guides/piko/piko.glb` altına alındı. Yalnız bu dosya için
Git ignore istisnası eklendi; 2,1 MB dosya LFS ortam modeli kurallarına dahil
değildir. Gerçek kaynak/hash/klip bilgisi yanındaki README'dedir. Eski WISP
erişim engeli ve aşağıdaki gelecekteki teslim notları bu dosya için geçerli
bir engel olarak yorumlanmamalıdır. Diğer modellerin teslim kuralları korunur.

Modeller uygulamanın kaynak kodundan ayrı teslim edilir. Yerel public/ içeriği Vite build ile dist/ içine kopyalanır; **Git ignore, deployment paketinden dışlama değildir**. Yerel dist/ klasörünü yetkisiz olarak dağıtmayın.

## Beklenen yerel dosyalar

- `public/environments/cockpit/boeing_737-800_cockpit.glb`: 86.081.540 bayt / yaklaşık 82,09 MiB; SHA-256 `a950013ef6c7570c64810b4d49486449f17220fae489e2c989385c69c22f2e0c`.
- `public/environments/charite_university_hospital_-_operating_room.glb`: 46.555.672 bayt / yaklaşık 44,40 MiB; SHA-256 `951401d851d45c1390a585bdba7ad14b020417e119a7fcf959f3133f8a0168b0`.

Bu görevde kaynak/generated dependency klasörleri dışındaki >20 MiB taramasında bu iki dosya bulundu. Hash'ler yeniden kontrol edilir; kaynak modeller değiştirilmez.

## Kaynak ve lisans

Kokpit metadata'sı hakai315 / CC-BY-4.0 beyan eder. [Kaynak modeli](https://sketchfab.com/3d-models/boeing-737-800-cockpit-d463256f98654b36af019d2d88bd5bbe), [yerel asset notu](../public/environments/cockpit/ASSET_NOTES.md).

Hastane metadata'sı Digital Surgery / CC-BY-NC-4.0 beyan eder. [Kaynak modeli](https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3), [yerel asset notu](../public/environments/HOSPITAL_ASSET_NOTES.md). NC beyanı ticari kullanım açısından sınırlayıcı olabilir; lisansın kapsamı ve kullanım/dağıtım izinleri ekip tarafından bağımsız doğrulanmalıdır. Bu belge hukuki hak doğrulaması yapmaz; source sayfalarının güncel lisansı bu görevde bağımsız doğrulanmadı.

Dosyaları yalnız yetkili ekip sorumlusundan veya kaynağın izin verdiği kanaldan edinin. Hak sahibini, lisans sürümünü, izin/atıf metnini, değişiklikleri ve elde etme tarihini kaydedin. Üyelik/indirme izni web'e yeniden dağıtım izni varsayılmaz. Gerekirse ekip güvenli ayrı asset deposu/LFS politikasını belirler; LFS iznin yerini tutmaz.

## Kurulum ve doğrulama

1. İzinli kaynaktan alınan dosyaları yukarıdaki kesin yollara yerleştirin; GLB adlarını değiştirmeyin.
2. PowerShell `Get-FileHash -Algorithm SHA256 <dosya>` ile teslim sürümünü karşılaştırın.
3. `node scripts/inspect-glb.mjs` kokpiti; `node scripts/inspect-glb.mjs public/environments/charite_university_hospital_-_operating_room.glb` hastaneyi inceler.
4. Dashboard kartından açın; gerçek geometri/materyal ve Environment loaded durumu doğrulansın. HTTP 200 tek başına yeterli değildir.

Eksik GLB, loader catch yolunda “The environment could not load…” ve Retry loading üretir. Modelin yerine sahte eğitim sahnesi konmaz; Dashboard/CRUD kullanılabilir. Eksik önizleme görseli için mevcut Preview unavailable fallback'i vardır.

## Yayına otomatik dahil edilmeyenler

- Yukarıdaki GLB'ler ve gelecekteki FBX/Blend/glTF/bin kaynakları.
- Diğer WISP/mascot kaynakları; yalnız yukarıda belirtilen doğrulanmış Piko GLB'si istisnadır.
- `public/previews/environments/*.jpg`: gerçek GLB ekran görüntüleri de kaynak hakları/atıf yönünden incelenmelidir. Yerelde kalır; ilk kaynak PR listesi bunları dışarıda bırakır. Kartlar fallback ile kullanılabilir.
- Üçüncü taraf Unity asset'leri veya yeni kaynak arşivleri; Unity kökünden web'e kopyalanmaz.

Plain-text asset notları, registry'deki atıf ve önizleme README'si yayın adayına dahildir. .gitignore ek korumadır; son staged liste yine manuel incelenir. Onay gelirse ilgili ignore kuralı review ile değiştirilir; körlemesine force-add yapılmaz.

## 21 Ekim 2026 teslim guncellemesi

Kullanici iki GLB modelinin mevcut private ekip deposuna yuklenmesini acikca istedi. Yalniz bu iki kesin yol icin Git LFS dagitimi hazirlaniyor; onceki yayindan haric ifadeleri bu iki dosya icin tarihseldir. Lisans metadata ve atif notlari aynen korunur; bagimsiz yeniden dagitim hak dogrulamasi yapildigi iddia edilmez. JPEG onizlemeler ve diger assetler haric kalir. Nihai upload sonucu GITHUB_LATEST_SYNC_REPORT.md icindedir.

## 14 Kasım 2026: üniversite deposu model dağıtımı

Üniversite deposunun main dalında kokpit (86.081.540 bayt) ve hastane
(46.555.672 bayt) GLB dosyaları normal Git bloblarıdır; temiz klon için
Git LFS kurulumu gerekmez. Önceki LFS kurulum notları tarihsel dallar içindir.
Altı özgün kaynak dalın uçları ve commit SHA değerleri değiştirilmedi.
Bu tarihsel dallarda LFS pointer dosyaları bulunabilir; hedefte LFS kapalı
olduğundan bu dallar tek başına modelleri geri yükleyemeyebilir.
Main üzerindeki gerçek GLB dosyalarını kullanın. Mevcut atıflar ve kokpitin
CC BY 4.0 / hastanenin CC BY-NC 4.0 koşulları aynen geçerlidir.
