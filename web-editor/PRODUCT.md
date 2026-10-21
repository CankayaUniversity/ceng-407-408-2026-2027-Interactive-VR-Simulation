# Workplace Studio

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Birincil kullanıcı, eğitim içeriği hazırlayan eğitmen / uzmandır. Bu öncelik kullanıcı tarafından 20 Ekim 2026 tarihinde doğrulandı. Kullanıcının mesleki alanı, teknik deneyimi ve kullanım sıklığı henüz netleştirilmedi.

## Product Purpose

Üniversite bitirme projesi olarak geliştirilen, farklı işyerlerinin mevcut GLB ortamlarında eğitim hotspotları ve kayıtlı inceleme görünümleri hazırlamayı sağlayan tarayıcı tabanlı editör.

Kullanıcının 20 Ekim 2026 uygulama isteğinde doğruladığı birincil sonuç: ortamı açmak, inceleme görünümü kaydetmek, gerçek model yüzeyine hotspot yerleştirmek, başlık/açıklama ve isteğe bağlı görünüm bağlantısıyla kaydetmek, Preview modunda eğitim bilgisini incelemek. Bu adımların bulunabilir ve tahmin gerektirmeyen bir akış oluşturması hedeflenir; sayısal kullanıcı başarı ölçütü henüz belirlenmedi.

## Operating Context

Dashboard mevcut ortamları ve yerel proje listesini sunar. Boeing 737-800 kokpit ve Charité ameliyathane aynı editör altyapısını kullanır. Proje kayıtları ile ortam eğitim kayıtları bağımsızdır; proje açılışı henüz placeholder çalışma alanıdır.

Kayıtlar tarayıcı ve origin kapsamındaki localStorage üzerinde tutulur. Başka cihaz, tarayıcı veya porta otomatik taşınmaz. Ekip senkronizasyonu ve bulut yedekleme yoktur.

## Capabilities and Constraints

- React, strict TypeScript, Vite, npm ve ortak Babylon.js editörü korunur. Ortamlar SceneDefinition ve registry üzerinden tanımlanır.
- Hotspot yerleştirme, düzenleme, silme; gerçek kamera pozundan Inspection Zone kaydetme; bölgeye bağlı hotspot açma ve salt okunur Preview mevcuttur.
- Editor Spectator oturumluk serbest kamera aracıdır. Hastane Preview Walking deneysel düz zemin hareketidir; tam fizik veya ekipman/duvar çarpışması sağlamaz. Dünya ölçeği ve başlangıç kameraları kesinleşmemiştir.
- Mevcut storage anahtarları ve şemaları korunur. Migration ayrı açık onay gerektirir. Kamera, navigation cancellation ve sahne yaşam döngüsü ilgisiz tasarım değişikliklerinde değiştirilmez.
- Fotoğraf/video reconstruction, AI recognition, backend, auth, çok kullanıcılı düzenleme, konuşan Piko eğitim rehberi ve Unity/OpenXR veri aktarımı uygulanmış özellikler değildir. Piko 3D köşe maskotu Idle oynatır; tıklama/dokunma/Enter/Space ile Wave oynayıp beklemeye döner.
- Unity deposuna entegrasyonda web kapsamı web-editor/ ile sınırlıdır; Unity kökü korunur.
- Sayısal kullanıcı başarı ölçütü ve kalıcı dil politikası açıktır. Güncel uygulama durumu için docs/PROJECT_STATUS.md ve gerçek kaynak/testler esas alınır.

## Brand Commitments

Ürün adı Workplace Studio'dur. Mevcut arayüz metinleri ağırlıklı İngilizcedir; ekip belgeleri ve raporları Türkçe yazılır. Ürün arayüzünün kalıcı dil politikası henüz doğrulanmadı. Bu belge yeni bir görsel yön belirlemez.

## Evidence on Hand

- src/features/projects/: Dashboard ve yerel proje CRUD uygulaması.
- src/features/environments/: ortak ortam editörü, registry, hotspot ve zone iş akışları.
- tests/: çekirdek davranışlar için Node testleri; tam browser/E2E kapsamı değildir.
- docs/ASSETS.md ve public/environments/ altındaki kaynak notları: üçüncü taraf modellerin atıf ve teslim bilgileri. Dağıtım izni ayrıca doğrulanmalıdır.
- public/previews/environments/: mevcut model önizlemeleri; lisans incelemesi yayın için ayrıca gereklidir.

Model, animasyon, eğitim içeriği, müşteri, test sonucu veya performans ölçümü uydurulmaz. Klinik ya da mesleki doğruluk iddiası kurulmaz.

## Product Principles

- Eğitmen / uzmanın gerçek ortam üzerinde eğitim içeriği hazırlama işini önceliklendir.
- Mevcut yetenekleri, deneysel araçları ve gelecek hedeflerini açıkça ayır.
- Kayıtlı kullanıcı verisini ve ortamlar arasındaki veri izolasyonunu koru.
- Ortam sayısı arttığında ortak editör yaklaşımını sürdür.
- Semantic HTML, erişilebilirlik ve mevcut etkileşim sözleşmelerini koru; özel erişilebilirlik gereksinimleri henüz belirlenmedi.
