# İki sürümün birleştirilmesi

27 Ekim 2026. Kullanıcı yerel sitenin ve arkadaşının simülasyon sürümünün birleştirilmesini, doğrulama sonrası main'e gönderilmesini açıkça istedi.

## Seçilen kaynaklar

- Taban: `codex/hospital-source-hotspots`, `478ca999262025880d05bc1e90e88d5245f7bdd9`. Bu commit önceki `feature/web-editor-integration` ve main geçmişini içerir.
- Yerel Workplace Studio: mevcut Dashboard, 10 saniyelik hazırlık, ortak editör, hiyerarşik alanlar, rehberli Preview, süre hesabı ve testler.
- Arkadaşın sürümü: hastanenin 10 kaynak açıklaması/kamera görünümü, Piko modeli/animasyonları, kaynak atıfları, yeni kayıtta başlangıç içeriği ve mevcut kayda açık ekleme eylemi.
- Hazırlık ekranını kapatan App değişikliği alınmadı. Hiyerarşik Explorer'a kaynak ekleme ve atıf bölümü uyarlandı. v1 storage ve ilk açık hiyerarşi düzenlemesi öncesi ham kayıt yedeği korunur.

Mevcut Unity dosyalarında değişiklik yoktur. Model binary'leri ve LFS pointer'ları yeniden yazılmaz; mevcut branch içeriği korunur. Bağımlılık/lock değişikliği yoktur. Backend, migration ve deployment eklenmedi. Asıl yerel klasör ve eski ekip checkout'u üzerine kaynak yazılmadı; birleştirme ayrı çalışma kopyasında hazırlandı.

## Gerçek doğrulama

- npm ci: 30 paket; 0 audit açığı.
- npm run build: başarılı; mevcut büyük glTF chunk uyarısı sürüyor.
- npm run lint: başarılı.
- node --experimental-strip-types --test tests/*.test.ts: 38/38 başarılı.
- Yeni test: kaynak içe eklemede hiyerarşi/kamera düzenlemesi korunur, tekrar ekleme kopya üretmez, açık save eski ham kayıt yedeğini korur.
- Tarayıcıda gerçek hastane GLB ve Piko yüklendi. Hazırlık ekranı inhale/exhale sonrası editörü açtı. Ayrı localhost origin'inde 10 alan/10 nokta gösterildi; Patient bed socket seçimi tamamlanmış kamera geçişinden sonra doğru kaynak metnini açtı. Preview → Editor ve kaynak alan Inspector kontrolü yapıldı.
- Önceden boş kayıt bulunan 127.0.0.1 origin'inde kayıt otomatik değiştirilmedi. Tarayıcı kayıtları silinmedi; QA kayıt yazma işlemi yapılmadı.

Tam responsive/CRUD/authoring ve bütün hotspotların kamera rotaları yeniden sınanmadı. Kokpit gerçek yükleme kontrolü yayın öncesi ayrıca kaydedilir. İlk tarayıcı erişimi sandbox preview sunucusuna ulaşamadı; aynı tek preview Windows ortamında yeniden başlatıldı. Preview build'indeki test modelleri yerel kaynaklardan sağlandı; Git'e eklenmedi.

## Yayın

Göreve özel `codex/combined-final` branch'i kullanılır. Main güncellemesi yalnız normal fast-forward push ile yapılır; uzak main değişirse tekrar değerlendirilir. Force push yoktur. Push sonucu uzak SHA ile doğrulanır; bu kaynak yayını canlı site deployment'ı değildir.
