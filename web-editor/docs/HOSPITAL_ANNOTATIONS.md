# Charité kaynak hotspot aktarımı

22 Ekim 2026, kullanıcının isteğiyle [özgün Sketchfab modelindeki](https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3)
10 açıklama, sırası, UID'leri, yüzey konumları ve kamera bakışları alındı.
Kaynak: Digital Surgery; sayfada CC-BY-NC-4.0 beyanı bulunur.

Veri `src/features/environments/hospitalAnnotations.ts` içindedir. Özgün
İngilizce metinler, yazım hataları ve içerideki numara referansları dahil
korundu. Metinler HTML olarak çalıştırılmaz; React düz metin gösterir.
Yeni klinik bilgi veya işlem talimatı üretilmedi. İçerik kaynak açıklamasıdır;
klinik doğruluk incelemesi yapılmış sayılmaz.

## Aktarılan noktalar

1. Patient bed socket
2. Anesthesiological tower
3. Main lock
4. Room controls
5. Disinfactant supplies
6. Main PC
7. Main display
8. Patient air conditioner
9. Anesthesiological supplies
10. Cauter & Suction tower

## Koordinat ve kamera

Kaynak görüntüleyici Z-up kullanır. Yerel GLB'nin `Sketchfab_model` kökü
-90° X dönüşü içerir; kaynak dünya konumu `[x,y,z]`, yerel sağ el/Y-up
sahnede `[x,z,-y]` olur. Aynı dönüşüm kaynak kamera eye/target değerlerine
uygulanır. GLB içindeki ölçek ve alt düğüm dönüşümleri yeniden uygulanmaz.
Kaynak FOV 45° korunur. Model, environment id, modelTransform ve GLB hash'i
değiştirilmedi.

Kamera bakışları normal inspection zone'larıdır. Mevcut beş ışınlı yol
kontrolü sürer; ekipmanın içinden geçen düz geçiş engellenebilir. Böyle bir
geçişte açıklama mevcut bakışta açılır; kameraya zorla teleport uygulanmaz.
Noktaların arkasında kalma/occlusion sınırı mevcut editörle aynıdır.

## Kayıt davranışı

`SceneDefinition.starterContent` yalnız çalışma zamanı yapılandırmasıdır.
Yeni tarayıcı kaydı başlangıçta 10 hotspot / 10 view gösterir; açılışta
localStorage'a otomatik yazılmaz. İlk düzenleme mevcut şema v1 ile kaydedilir.

Önceden kaydedilmiş boş veya dolu kayıtlar aynen yüklenir; silinen noktalar
kendiliğinden geri gelmez. Editor → Scene Explorer → **Add source annotations**
yalnız eksik kaynak ID'lerini ekler. Kullanıcı metinleri, konumları, sırası ve
zone bağlantıları korunur. İşlem tekrarlandığında kopya oluşmaz. 32 view / 100
hotspot sınırında hata verilir; kayıt ve bellek verisi korunur. Bozuk kayıt
üzerine yazılmaz. Başka ortamların başlangıç verisi değişmez.

## Kaynak ve atıf

- [Model ve açıklamalar — Digital Surgery](https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3)
- [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/)
- Kaynak sayfasının istediği yayın atfı: Queisner M, Pogorzhelskiy M, Remde C,
  Pratschke J, Sauer IM. *VolumetricOR: A New Approach to Simulate Surgical
  Interventions in Virtual Reality for Training and Education.* Surg Innov.
  2022;29(3):406–415. doi:10.1177/15533506211054240.
  [Yayın](https://pubmed.ncbi.nlm.nih.gov/35137646/).

Uygulama Scene Explorer içinde açıklama yazarını, kaynak ve lisans
bağlantılarını gösterir. Aktarımda yalnız metin ve konum/kamera metaverisi
kullanıldı; kaynak küçük resimleri ve Sketchfab görüntüleyici kodu kopyalanmadı.
Çalışma zamanı Sketchfab bağlantısı veya hesap girişi gerekmez.
