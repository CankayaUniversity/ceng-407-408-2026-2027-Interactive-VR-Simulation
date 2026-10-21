# Piko

Kullanıcının bu projede hazırlattığı robotun son poz ve animasyon sürümünün
değiştirilmeden kopyalanmış GLB dosyasıdır. Önceki çalışma adı WISP'tir;
karakterin kullanıcı tarafından seçilen adı **Piko**'dur. GLB içindeki özgün
düğüm ve klip adları korunur.

- Kaynak dosya: `wisp_son_animated.glb` (yerel `wisp/final` teslimi).
- Önceki ekip teslimi: [maskot PR #1](https://github.com/Cengaverler/interactive-vr-simulation/pull/1).
- Boyut: **2.133.584 bayt**.
- SHA-256: `fd2338c12af77d767f769fdf0c06beba1b3279df78ceb6c36dbda2c5f82383ac`.
- GLB 2.0; 113 mesh, 149 node, 9 malzeme, 98.196 üçgen.
- Mevcut klipler: Idle, Wave, Point, Talk, Success. Nesne hiyerarşisi üzerinden
  eklem hareketleri kullanılır; Humanoid skeleton varsayılmaz.

Web arayüzü **Idle'ı döngüde oynatır**. Piko'ya tıklama/dokunma veya odaktayken
Enter/Space, **Wave'i bir kez oynatır**, sonra Idle'a döner. Tekrarlanan
tıklamalar devam eden el sallamayı kesmez. Diğer klipler tetiklenmez. Modelin
dinlenme dönüşümleri bu karede bulunduğu için klibi hiç uygulamadan göstermek
parçaların üst üste gelmesine neden olur. Kamera önden sabittir; el sallama
sınırları da kadraja dahil edilir. Yeni klip üretilmedi; konuşma/eğitim
rehberliği yoktur.

Piko portresi kendi küçük, şeffaf Babylon sahnesinde en fazla 30 fps çizilir.
Sekme gizlenince oynatma duraklar. Reduced motion tercihinde Idle ilk
karede kalır; kullanıcının açıkça başlattığı Wave yine oynatılır. Ortam GLB'si bu
sahneye kopyalanmaz. Ek HDR/doku/CDN isteği yoktur. Küçültme portreyi unmount
ederek engine/scene kaynaklarını serbest bırakır; sayfadan çıkış da temizlenir.
