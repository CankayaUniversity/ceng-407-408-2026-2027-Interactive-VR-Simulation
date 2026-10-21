# WISP varlık teslimi ve Blender dışa aktarımı

## Güncel Piko teslimi

21 Ekim 2026: kullanıcının doğrulanmış son GLB dosyası yerel kaynaklardan
alındı ve Piko adıyla 3D köşe maskotu olarak entegre edildi. Gerçek boyut,
SHA-256, nesne hiyerarşisi ve beş klip bilgisi
`public/guides/piko/README.md` içinde bulunur. Humanoid skeleton kullanılmaz.
Web görünümü Idle oynatır; portreye tıklama/dokunma/Enter/Space ile Wave bir
kez oynar ve Idle'a döner. Konuşan eğitim rehberi henüz yoktur.
Aşağıdaki erişim engeli ve önerilen teslim adları önceki incelemeyi anlatır.

## Mevcut engel

GitHub bağlantısı `Cengaverler/interactive-vr-simulation` PR #1 ve `feat/wisp-mascot-assets` branch içeriği isteklerine 404 döndürdü. Özel depo izni, depo adı veya PR durumu bu yanıtla birbirinden ayırt edilemez. Gerçek dosya adları ve animasyonlar görülmedi. Aşağıdaki isimler **önerilen teslim adlarıdır**, depoda bulunduğu iddiası değildir.

## Devam etmek için gerekli teslim

İki seçenekten biri yeterlidir:

1. Mevcut GitHub bağlantısına ilgili depo için okuma erişimi verin; PR #1 ve branch'in mevcut olduğunu doğrulayın. Şifre veya erişim token'ını sohbete koymayın.
2. Yalnız WISP varlık paketini yerel erişilebilir bir klasöre koyun ve yolunu paylaşın. Unity projesinin tamamı gerekmez.

Paket şu bilgileri içermelidir:

- Tercihen rig ve mevcut animasyonları içeren `.blend`; yoksa rig'li ana `.fbx` ve aynı skeleton'a ait tüm ayrı animasyon `.fbx` dosyaları. Armature/bone hiyerarşisi ve skin weights gereklidir. Unity `.anim`/Animator Controller dosyaları varsa eşlik etsin; bunlar tek başına GLB animasyonu olarak kullanılamaz.
- Kullanılan tüm doku dosyaları: mevcut base color, alpha, normal, roughness/metallic veya diğer gerçek kaynaklar. Eksik kanallar uydurulmamalıdır.
- Yazar, kaynak, lisans/yeniden dağıtım izni ve gerekli atıf. Unity Asset Store veya üçüncü taraf içerik varsa ilgili lisans metni ve web dağıtım izni açıklaması.
- Varsa Unity `.meta`, materyal, prefab ve Animator Controller: model ölçeği, materyal bağlantıları ve klip aralıklarını anlamaya yardımcı olur. Unity shader'ları web'e doğrudan taşınmış kabul edilmez.
- Blender sürümü, gerçek klip adları, FPS, kare aralıkları, döngü bilgisi ve intro/idle olduğu doğrulanan kliplerin listesi. Bu rollerin yokluğu açıkça belirtilmeli.

Hazır GLB verilebiliyorsa önerilen paket: `wisp.glb`, `WISP_ASSET_LICENSE.md`, `WISP_CLIPS.md` ve özgün kaynakların ayrı arşivi. Tek GLB içine mesh, skin, animasyon ve dokuların gömülmesi tercih edilir. Hedef web yolu, doğrulamadan sonra `public/guides/wisp/wisp.glb` olabilir; henüz bu dosya oluşturulmadı.

## Blender işlemleri

Özgün dosyayı koruyup çalışma kopyasını açın. FBX varsa File → Import → FBX ile alın. Mesh ve armature ilişkisini, dokuları ve mevcut Action'ları kontrol edin. Idle ve varsa intro'yu timeline'da ayrı ayrı oynatın; sürelerini ve gerçek adlarını kaydedin.

Mesh ile armature'ı birlikte seçin. Rig'e körlemesine transform uygulamayın; rest pose ve deformasyonun korunmasını karşılaştırın. File → Export → glTF 2.0 üzerinden GLB seçin. Yalnız seçili mesh/armature'ı, skinning ve animasyonları dahil edin; gereksiz kamera/ışıkları dışarıda bırakın. Dokuları gömün; desteklenmeyen özel materyalleri görsel karşılaştırmayla kontrol edin.

Animation → Mode seçimi Blender sürümüne bağlıdır. Mevcut Action'ları ayrı klipler olarak dışa aktarın; NLA düzeni kullanılıyorsa ilgili NLA Tracks modunu seçin. Klipleri tek uzun animasyonda yanlışlıkla birleştirmeyin. Olmayan intro/success klibi üretmeyin.

GLB'yi boş Blender dosyasına yeniden import edin. Rig, görünüm, ölçek, klip ayrımı ve döngüleri tekrar kontrol edin. Ardından Babylon'da gerçek AnimationGroup adlarını ve oynatmayı doğrulamak gerekir; Blender doğrulaması tek başına tarayıcı kabul testi değildir.

Resmî başvuru: [Blender glTF dışa aktarım belgesi](https://docs.blender.org/manual/en/5.1/addons/import_export/scene_gltf2.html), [Khronos Blender exporter kaynak belgesi](https://github.com/KhronosGroup/glTF-Blender-IO/blob/main/docs/blender_docs/scene_gltf2.rst). Arayüz etiketleri sürüme göre değişebilir.

## Web kabul kapısı

GLB JSON metaverisi ve gerçek Babylon yüklemesinden mesh/skin/material/texture/AnimationGroup sayıları, boyut ve gerçek klip adları raporlanmalı. Idle ve varsa intro oynatılmalı; root-motion, orantı ve materyaller gözle incelenmeli. Intro yoksa mevcut idle veya sabit poz kullanılabilir; yeni animasyon oluşturulduğu söylenmez. Idle da yoksa bu açık sınırlama olarak sunulur. Lisans ve yeniden dağıtım koşulları doğrulanmadan asset Git'e eklenmez.
