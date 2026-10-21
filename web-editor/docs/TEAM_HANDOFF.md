# Ekip ve AI asistanları için başlangıç

Önce AGENTS.md, README.md, PRODUCT.md, DESIGN.md, docs/ARCHITECTURE.md, docs/PROJECT_STATUS.md, docs/ENVIRONMENT_CONTRACT.md ve docs/ASSETS.md okuyun. Gerçek kaynak/testler güncel doğrudur. Web çalışması yalnız web-editor/ altında yapılır; Unity ayrı uygulamadır.

## Neyi yaptık?

Birincil kullanıcı eğitim hazırlayan eğitmen/uzman. Dashboard iki eşit ortam kartı ve ayrı yerel proje listesinden oluşur. Kokpit ve ameliyathane aynı React/Babylon editörünü kullanır. Editor: gerçek yüzeye hotspot ekleme, başlık/açıklama, isteğe bağlı inceleme görünümü bağlantısı; Save view gerçek kamera pozunu kaydeder. Preview kayıtlı eğitim bilgisini gösterir. Explorer arama ve contextual Inspector ortak araçlardır.

Son UI çalışması Studio Neutral: koyu nötr yüzeyler, ölçülü mavi vurgu, 14–15px normal metin, 40–44px kontroller, 260px Explorer, 352px Inspector ve 56px üst bar. Eski zeytin/acid tema kullanıcı tarafından reddedildi. Ortam kartı görseline tıklama hatası düzeltildi; stretched button :active transform istisnası korunmalı.

İki ortama girişte gerçek uygulamada 4 saniye “Breathe in”, 6 saniye “Slowly exhale” hazırlığı vardır. Sonra model yüklenir; Escape Dashboard’a döner. Hazırlık sırasında Babylon mount edilmez. Önizleme resmi yoksa arka plan görseli de yoktur; model ve görsel ayrı dosyalardır.

## Neler henüz yok?

Proje CRUD yereldir; proje açılması placeholder gösterir, projeye ortam bağlama yoktur. Backend/auth, ekip senkronizasyonu, export/import UI, reconstruction/AI ve web→Unity aktarımı uygulanmadı. Web WISP entegrasyonu yoktur; Unity’de ayrı WISP çalışması olması bunu değiştirmez. Walking sabit düzlem prototipidir; duvar/ekipman collision ve gravity yoktur. Dünya ölçeği/kamera kalibrasyonu provisional, marker occlusion sınırlıdır.

Ayrı Resq/Aurora animasyonlu landing denemesi bu ürünün parçası DEĞİLDİR. Kullanıcı uyarlama/entegrasyonu reddetti. Yeni açık görev olmadan Dashboard’a veya ürün girişine taşımayın. İngilizce nefes hazırlığı ise ayrıca onaylanıp gerçek uygulamaya eklendi.

## Düşündüklerimiz — öneri, uygulama izni değil

Öncelikle izinli asset teslimini ve temiz ekip kurulumunu çözmek; sonra kullanıcıyla proje–ortam ilişkisini tasarlamak, browser/E2E kapsamını artırmak ve düşük donanım performansını ölçmek mantıklı sıradır. WISP, backend ve native VR için ayrı veri/lisans/yaşam döngüsü sözleşmesi gerekir. Çalışmayan planları çalışan özellik gibi anlatmayın.

## AI ile çalışırken

Impeccable ve emil-design-eng yerel tasarım araçları olarak kullanıldı; uygulamanın çalışması için gerekli değiller. İhtiyacı olan ekip üyesi onaylı kaynaktan kendi AI aracına kurmalı, gerçek SKILL.md talimatlarını okumalı. Üçüncü taraf skill paketleri, yerel hook/cache dosyaları bu yayına eklenmez.

Storage anahtarları ve şema, ortak registry/editör, kamera hassasiyeti/yönü, navigation Promise/cancellation, Babylon cleanup ve Unity kökü korunur. localStorage origin/porta özeldir; Git eğitim notlarını taşımaz. localStorage.clear kullanmayın. Kamera/runtime dosyalarını paralel değiştirmeden ekip kapsamını kararlaştırın.

AI görevine hedef, izinli dosyalar, korunacak davranışlar ve kabul senaryolarını yazın. Küçük değişiklikten sonra build/lint/Node testleri ve ilgili browser akışını doğrulatın. Yapılmayan test açıkça yazılmalı; AI çıktısı insan incelemesinden geçmeli. main push, force push, merge ve deployment ayrı yetki gerektirir.

Kurulum README.md; model yolları/hash’leri docs/ASSETS.md; son gerçek yayın sonucu GITHUB_LATEST_SYNC_REPORT.md içindedir. Model dosyaları teslim edilmeden kaynak kodun kurulması tek başına ortamları açmaz.
