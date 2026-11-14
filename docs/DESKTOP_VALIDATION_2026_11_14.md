# Hastanede yürüyüş zemini — 14 Kasım 2026

## Sorun ve düzeltme

Hastanenin kayıtlı Overview göz yüksekliği 1,65; yeni yürüyüş profilinin
göz yüksekliği 2,4 sahne birimiydi. Kamera kaydı uygulanınca kapsülün ayak
konumu Y=-0,75 oluyordu. Gerçek GLB'de bu noktadaki zemin yaklaşık Y=-0,067
olduğu için kapsül tek yüzlü tarama zemininin altında başlıyor ve düşüyordu.

Yürüyüş başlangıcında gözden aşağı doğru gerçek çarpışma yüzeyleri sorgulanır.
Kapsül/Piko katmanları ve trigger'lar hariç tutulur; eğim sınırındaki en yakın
yürünebilir yüzeye ayaklar yerleştirilir, dikey hız sıfırlanır. Yerçekimi ve
normal CharacterController çarpışmaları daha sonra çalışmaya devam eder.
Kayıtlı bakışa geçiş, iptal ve Spectator'dan dönüş yeni bir zemin hazırlığı
gerektirir. Bakış kayıtları diskte değiştirilmez; görünmez düzlem eklenmedi.
Hastanenin 2,4 yürüyüş yüksekliği ve hareket hızı korundu.

## Otomatik doğrulama

`desktop-tools/Test-Unity.ps1`: 16 başarılı, 0 başarısız test.

- Eski düşük göz kaydının uzun kapsülü zeminin altında başlatması yeniden
  oluşturuldu; 600 yürüyüş adımından sonra göz yüksekliği zeminden 2,4 olarak
  kaldı ve yatay hareket sürdü.
- Spectator sırasında bakış yüksekliği korundu; yürüyüşe dönünce ayakta durma
  yüksekliği geri kuruldu.
- Gerçek hastane GLB'si aynı LocalSceneLoader ile Play Mode'da açıldı.
  Orijinal Overview kapsülünün zeminin altında başladığı doğrulandı.
  600 sabit adım ve ardından tarama zemini üzerinde 60 ileri hareket adımı
  sonunda yükseklik korundu; hareket engellenmedi.
- Önceki kamera yolu/koordinat ve arayüz odağı regresyon testleri geçti.

Test raporu: `Builds/DesktopTests/editmode-results.xml`.
GLB yükleme testi, glTFast çalışma zamanı defer agent'ının gerektirdiği
Play Mode'a `EnterPlayMode` ile girer ve test sonunda çıkar.

## Windows çıktısı

`desktop-tools/Build-Windows.ps1` başarıyla tamamlandı. Güncel player açılıp
kullanıcının kayıtlı hastanesi yüklendi. Sağ tuşla yürüyüşe girildiğinde bakış
ayakta durma yüksekliğine geldi ve sonraki ekran gözlemlerinde zeminin altına
düşmedi. Yatay hareketin ölçümlü doğrulaması yukarıdaki gerçek GLB testindedir;
UI denemesi kısa klavye girdileriyle sınırlıydı. Kontrol sonunda uygulama
kapatıldı; kullanıcının kayıtları değiştirilmedi.
