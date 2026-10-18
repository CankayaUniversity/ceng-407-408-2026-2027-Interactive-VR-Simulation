# Charité ameliyathane modeli — yerel asset incelemesi

- Dosya: `charite_university_hospital_-_operating_room.glb` (bu klasörde).
- Boyut: 46.555.672 bayt / 44,39895 MiB; GLB 2.0; tüm binary ve görseller gömülü. Harici dosya yok.
- SHA-256: `951401d851d45c1390a585bdba7ad14b020417e119a7fcf959f3133f8a0168b0`.
- 8 mesh, 15 node, 5 material, 4 texture / 4 gömülü JPEG; 334.808 üçgen. KHR_materials_unlit kullanır.
- Gömülü title: Charité University Hospital - Operating Room.
- Gömülü author: Digital Surgery (https://sketchfab.com/digitalsurgery).
- Gömülü source: https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3
- Gömülü license: CC-BY-NC-4.0 (https://creativecommons.org/licenses/by-nc/4.0/).

Bu bilgiler GLB asset.extras içindendir; kaynak web sayfası ayrıca doğrulanmadı. Yeniden dağıtım/ürünleştirme öncesinde kaynak ve lisans koşulları ekip tarafından doğrulanmalıdır. Dosya indirilmedi, dönüştürülmedi, yeniden adlandırılmadı veya optimize edilmedi. Git'e stage/commit yapılmadı.

Kaynak node dönüşümleri korunur; sağ el / Y-up sahneye identity modelTransform ile alınır. Hesaplanan scene bounds yaklaşık min [-13.426,-5.070,-13.200], max [13.453,10.629,13.781]. Bu genişlik tarama artıkları içerir; güvenli fizik alanı veya gerçek metre ölçüsü değildir.

Mesh'ler taranmış odayı ve Window/Door isimli parçaları içerir. Ayrı güvenilir basitleştirilmiş collision/floor mesh'i yok. Offline yatay üçgen alan histogramında büyük kümeler Y≈0/-0.1 (zemin) ve Y≈5.5 (tavan) görüldü. Bu ölçü fizik kalibrasyonu değildir. Preview yürüyüş prototipi ayarlanabilir Y=0 düz referans zemini ve 1.65 sahne birimi göz yüksekliği kullanır. X/Z ±5 kaba emniyet sınırı; duvar/ekipman collider'ı değildir. Tüm bu değerler registry içindeki interaction.walking ayarlarıdır.

Başlangıç kamerası [0,1.65,4.5], hedef [0,1.65,0], FOV70; provisional. Tarayıcıda odanın içinden kullanılabilir açık alan görünümü kontrol edilir; kesin ölçek, erişilebilir yürüyüş koridoru ve collision proxy çalışması ekip incelemesi gerektirir.
