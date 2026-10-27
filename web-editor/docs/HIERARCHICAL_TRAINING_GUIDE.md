# Alanlar ve bilgi noktalarıyla eğitim hazırlama

1. Dashboard üzerinden ortamı açın. Hazırlık ekranından sonra Preview gelir. İçerik hazırlamak için **Editor** seçin.
2. Kamera açısını drag/zoom veya Spectator ile ayarlayın. **Save view / Save an area** ile başlık verin, Parent area = Environment seçin, görüntüyü kontrol edip **Save this view** kullanın.
3. Scene Explorer ağacında ana alanı seçin. Bu seçim Editor içinde kamerayı oynatmaz; Inspector o alanı düzenler. **Open saved view** kamerayı açıkça kayıtlı görünüme taşır.
4. Daha yakın bir görünüm ayarlayın; ana alan Inspector içinde **Add sub-area** seçin. Üst alan hazır gelir, gerekirse Parent area listesinden değiştirin. Alt alan kendi kamera konum/yön/FOV değerlerini mutlak sahne koordinatlarında kaydeder. Üst alanın kamerasından türetilmez.
5. İlgili alanı seçip **Add hotspot / Add information point** kullanın; gerçek model yüzeyine tıklayın. Form alan yolunu gösterir. **Move to area** listesinden başka alanı veya Unassigned seçebilirsiniz. Başlık/bilgiyi doldurup Save hotspot ile kaydedin.

## Düzenleme

Alan seçilince Inspector içindeki Area name ve Move to area ile adını/üst alanını değiştirin; Save area details kullanın. Kendi alt alanları üst alan seçicisinde sunulmaz. Move earlier/later yalnız aynı üst alanın çocuklarının sırasını değiştirir. Kimlikler ve kamera pozları değişmez.

Yeni kamera açısını kalıcı yapmak için ilgili alanı seçip **Update saved view** kullanın: o anda ekrandaki kamerayı kaydeder. Önce görüntüyü kontrol edin. XYZ/FOV teknik ayarları yalnız Advanced Camera Calibration açıldığında görünür; teknik değer yazmak normal akışın şartı değildir.

**Remove area…** önce onay ve sayıları gösterir. Kullanıcının onayladığı strateji: doğrudan alt alanlar ve o alana bağlı bilgi noktaları bir üst alana taşınır. Üst alan yoksa alt alanlar kök olur, bilgi noktaları Unassigned olur. Daha derin içerik ve hotspot kimlikleri korunur. Yalnız kaldırılan alanın kayıtlı kamera görünümü silinir. Otomatik geri alma ekranı yoktur.

## Preview

İlk açılış ortam genel görünümünü ve kök alanları gösterir. Alan açılınca kayıtlı kamera görünümüne geçilir; alt alanlar ve yalnız o alanın bilgi noktaları gezinme şeridinde görünür. Breadcrumb gerçek tamamlanmış kamera bağlamını gösterir. Back to parent, Overview, Previous view ve kardeş alan Previous/Next eylemleri ayrı anlam taşır. Sıralı tur zorunlu değildir; ağaçtan doğrudan seçim yapılabilir.

Bilgi noktası seçilince bağlı alana geçişin gerçek tamamlanması beklenir, ardından yalnız o bilgi kartı açılır. Yalnız alan seçmek hiçbir rastgele kart açmaz. Hızlı yeni seçim önceki isteği geçersiz kılar. Engelli geometri yolunda alan değişmez; mevcut eski sözleşme gereği bilgi kartı açık uyarıyla mevcut bakışta gösterilebilir.

Unassigned veya eski kayıtta eksik alana bağlı noktalar ağaçta bulunur. Genel görünümde ayrıca “stays in this view” etiketiyle listelenir; kamera hareketi başlatmadan bilgi gösterir. Başka alanda gezinirken sahne markerları yalnız o alana ait noktaları gösterir. Editor bütün noktaları gösterir.

## Klavye ve kayıt

Ağaçta Up/Down, Home/End odağı taşır; Right dalı açar veya ilk çocuğa gider; Left dalı kapatır veya üst alana gider. Enter/Space seçer. Arama eşleşen alt öğenin atalarını da gösterir; arama temizlenince önceki açılım korunur. Açılım durumu bu workspace oturumuna aittir; reload ile sıfırlanır.

Kayıt hâlâ origin/ortam bazlı yereldir. Eski v1 düz alanlar kök olarak okunur. Okumada hiçbir kayıt değişmez. İlk açık düzenlemede eski ham kayıt `.before-hierarchy.v1` son ekli ortam yedeğine yazılır; bu yedek zaten varsa değiştirilmez. Yedek veya yeni kayıt yazılamazsa işlem başarısız olur. Geçersiz döngü, kayıp üst alan, mükerrer kimlik veya bilinmeyen sürümde eski veri korunur ve düzenleme kapanır. Bu yedek bir cloud backup/export özelliği değildir; eski uygulamayla aynı origin üzerinde eşzamanlı düzenleme yapmayın.

Kokpit ve hastane kayıtları ayrı kalır. Dünya ölçeği provisional; kamera yolu sınırlı ışın kontrolüdür, otomatik engel etrafından rota veya tam çarpışma güvenliği değildir. Her kaydı gerçek ortamda deneyin.
