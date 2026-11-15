# Unity masaüstü sürümü — 14 Kasım 2026

Bu belge 14 Kasım'daki eski ekip deposu tesliminin tarihsel kaydıdır.
15 Kasım okul deposu hazırlığı için [güncel değişiklik notuna](CHANGES_UNIVERSITY_NURSING_2026_11_15.md)
bakın. Güncel okul dalı normal Git model dosyalarını kullanır; aşağıdaki
LFS talimatları eski teslim dalına aittir.

Branch: `codex/unity-desktop-2026-11-14`.
Taban: `codex/combined-final`, commit `8f1c2d68193d802edfafd7663a6f105f1ea33731`.

## Yapılan değişiklikler

- Windows'ta yerel çalışan Unity uygulaması ve İngilizce UI Toolkit arayüzü.
- Diskten hastane/kokpit GLB'lerini ve yeni yerel GLB dosyalarını açma.
- Alan, alt alan ve hotspot düzenleme; kayıtlı kamera bakışları ve Preview akışı.
- Projeleri model dosyasıyla birlikte yerel klasöre kaydetme ve tekrar açma.
- Hastanenin 10 alanı/10 bilgi noktası ve kaynak/lisans bilgilerinin korunması.
- Piko'nun Idle ve tıklamayla Wave animasyonları.
- Sağ tıkla WASD/fare kontrolü, Esc ile bırakma; hareket sırasında arayüz
  düğmelerine klavye odağı geçmesini engelleme.
- Spectator'da Shift ile yükselme, Ctrl ile alçalma; yürüyüşte Shift ile hızlanma.
- Daha geniş 3D görünüm, açılır/kapanır Explorer ve Inspector, Focus view.
- Hastanede 2,4 sahne birimi/sn hareket hızı, Shift ile 4,8 ve 2,4 göz yüksekliği.
- Eski düşük kamera kayıtlarıyla yürüyüş başlatıldığında karakterin zeminin
  altında doğmasını düzeltme; gerçek model yüzeyine ayak yerleştirme.
- Unity 6000.6.7f1 paket/ayar güncellemesi, Windows derleme araçları ve testler.

Mevcut web kaynakları ve önceki Unity sahneleri korunur. AI ile 3D üretimi ve
VR gözlük entegrasyonu sonraki aşamaların kapsamındadır.

## Branch'i indiren ekip arkadaşı için

1. Git LFS kurulu olarak bu branch'i klonlayın ve `git lfs pull` çalıştırın.
   Hastane/kokpit GLB'leri Git LFS'de, Piko modeli de repository içindedir.
2. Unity Hub'da **6000.6.7f1** ve Windows build desteğini kurun.
3. Repository kökünü Unity projesi olarak açın.
4. **Workplace Studio → Open Desktop Application Scene** menüsünü seçin.
   Bu işlem modellerin çalışma zamanı kopyalarını hazırlar. Play ile deneyin.
5. Bağımsız uygulama için **Workplace Studio → Build Windows Application**
   seçin. Ardından `Unity_Uygulama_Baslat.cmd` kullanılabilir.

Builds/Library/Logs ve kişisel eğitim kayıtları Git'e dahil değildir. Hazır
uygulamayı Unity kurmadan kullanacak kişiye `Builds/WorkplaceStudio-Windows`
klasörünün tamamı ayrıca gönderilmelidir.

## Doğrulama

- 20 çekirdek/sözdizimi kontrolü başarılı.
- 16 Unity testi başarılı; gerçek hastane GLB'siyle zemin/yürüyüş testi dahil.
- Windows uygulaması başarıyla derlendi ve bu bilgisayarda kontrol edildi.

Ayrıntılar: [kurulum/kullanım](DESKTOP_APP.md),
[ilk sürüm kontrolleri](DESKTOP_VALIDATION_2026_11_02.md),
[zemin düzeltmesi](DESKTOP_VALIDATION_2026_11_14.md).
