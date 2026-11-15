# Okul deposu için hemşire odası ve literatür raporu

Tarih: 15 Kasım 2026. Hazırlık dalı: `nursing-room-literature-2026-11-15`.
Taban: okul deposunun `main` dalı, `50a5a2fbbb0648ab709c8f07e4193c684d97cbf0`.

Güncel hedef: [CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation](https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation).
Kullanıcı incelemesinden sonra 15 Kasım 2026'da bu yeni dalın okul deposuna
yayınlanması onaylandı. Uygulama dalı `main` ile birleştirilmedi. Kullanıcının
ayrı isteğiyle yalnız `docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf`, `0864171`
commit'i ile `main` dalına eklendi; başka uygulama/belge değişikliği taşınmadı.

Okul reposundaki dört dalın `codex/` ön eki GitHub'ın yeniden adlandırma
işlemiyle kaldırıldı; yeniden adlandırma sırasında tüm commit özetleri korundu.
Yerel dal adları ve güncel kullanım belgeleri de yeni isimlerle eşleştirildi.

## Uygulama ve içerik

- Nursing Skills Room katalog girdisi; düzenlenebilir Blender kaynağı ve
  kendine yeterli GLB. CC0 kaynak cihazlar ve atıf bilgileri birlikte korunur.
- Monitör açma/kapama ve demo dalga ekranı; pompa güç/çalışma/kapak
  durumları; yatak başlığı ve korkuluk hareketi; ışıklı hemşire çağrısı.
- Cihaz durumları ve komut kuralları bağımsız Core katmanında; gelecekteki
  eğitim akışları için başarılı eylem olayları ve önce/sonra durumları.
- Cihaz listesinde yakın kamera görünümü; seçili cihaz pininin ekranı
  kapatmaması; hareketli parçaya bağlı hotspot konumları ve taşınabilir kayıt.
- Hemşire odası ışık/zemin görünümü ve MSAA iyileştirmeleri. Yatak hareket
  yönü ve çağrı ışığı için doğrulanan düzeltmeler.
- Önceki hastane, kokpit, Piko ve okul main'indeki WISP kaynakları korunur.

## Akademik teslim ve tutarlılık

- [İngilizce rapor PDF](LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) ve
  [düzenlenebilir Markdown](LITERATURE_AND_TECHNOLOGY_REVIEW.md): 31 kaynak,
  karşılaştırmalar, fotoğraf/video rekonstrüksiyonu ve yerel/çevrimiçi AI kapsamı.
- Çankaya Üniversitesi logolu kapak, 2026-2027 dönemi, altı öğrenci/ad/numara
  ve danışman. PDF içindeki bağlantı güncel okul deposuna gider.
- [Kapak verisi](report-metadata.json), [PDF üretim aracı](tools/build_literature_review.py)
  ve [logo kaynak notu](assets/SOURCES.md) repoya dahil edilir.
- Kök README, docs dizini, web klonlama adresi ve güncel masaüstü kurulum
  belgesi okul reposuyla eşleştirilir. Tarihsel PR ve teslim kayıtlarının
  eski repo bağlantıları geçmişi doğru tutmak için korunur.
- `Cengaverler` kaynak atıfları ve mevcut yerel kayıt klasörü bir depo URL'si
  değildir; değiştirilmeleri geçmiş kayıtları/atıfları bozabileceği için korunur.
- Ana hedef depo yerelde `origin`, eski ekip deposu `legacy` olarak tutulur.
  Hazırlık dalı okul main'inin üzerindedir; geçmiş yeniden yazılmaz.

## Dosya dağıtımı

Hastane/kokpit okul main'inde zaten normal Git dosyalarıdır; yeni hemşire
odası da normal Git dosyalarıyla hazırlanır. Eski dallarda LFS pointer'ları
bulunabilir. Güncel dalı kullanın. Lisans/kaynak koşulları değişmez.

Builds, Library, Logs, kullanıcı eğitim kayıtları, Blender `.blend1`
yedekleri ve yerel araç/önizleme çıktıları gönderim kapsamına alınmaz.
AI rekonstrüksiyonu, çevrimiçi servis seçimi ve VR gözlük entegrasyonu bu
hazırlıkta uygulanmış özellik olarak sunulmaz; raporda proje hedefi olarak ayrılır.

## Kontroller

- Okul main'ine taşıma sonrasında 25 çekirdek testi ve masaüstü asset/hash
  kontrolleri başarılı.
- Aynı dalda Unity testleri yeniden çalıştırıldı: 17 başarılı, 0 başarısız.
  Bu turda Windows çıktısı yeniden derlenmedi; önceki doğrulanmış build ve
  GUI kontrolünün kapsamı aşağıdaki tarihli raporda korunur.
- PDF: 19 sayfa, 31 kaynak, 34 bölüm yer imi; öğrenci/ad/numara ve bağlantı
  kontrolleri başarılı. Yeni kapak görsel olarak incelendi; 2-19. sayfalar
  önceki incelenmiş PDF ile piksel düzeyinde aynı.
- Önceki runtime/arayüz kontrolleri ve fiziksel klavye sınırı:
  [hemşire odası doğrulaması](NURSING_ROOM_VALIDATION_2026_11_14.md).

## Yayın öncesi son içerik incelemesi

- İnceleme türü odaklı anlatısal literatür/teknoloji incelemesi olarak açık;
  yapılmamış sistematik tarama, kullanıcı deneyi veya klinik başarı iddiası yok.
- Yerel ve çevrimiçi AI seçenekleri, fotoğraf/video rekonstrüksiyonu ve VR,
  iki dönemlik proje hedefleri olarak korunuyor; mevcut prototiple ayrılıyor.
- Padilha, Liaw ve Li çalışmalarının örneklem/bulgu ifadeleri özgün yayınlarla
  yeniden karşılaştırıldı. Hemşirelik öğrencilerinden yeni çalışan hemşirelere
  genellemenin sınırı ve bilgi kazanımı/beceri farkı raporda açıklanıyor.
- 31 kaynak numarası, atıf hedefleri, kapaktaki altı öğrenci/ad/numara,
  danışman bilgisi, güncel depo adresi ve README gezinmesi yeniden kontrol edildi.
- Raporun bilimsel içeriğinde bu son incelemede ek düzeltme gerektiren bir
  uyumsuzluk bulunmadı; incelenmiş PDF korunuyor. Bu kontrol, paylaşılmamış
  bir ders rubriğine veya notlandırma ölçütüne uygunluk garantisi değildir.
