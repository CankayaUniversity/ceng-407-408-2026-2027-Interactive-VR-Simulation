27 Ekim 2026 görev istisnası: kullanıcı iki sürümün birleştirilmesini ve doğrulama sonrası main’e gönderilmesini açıkça yetkilendirdi. Genel ekip akışı feature branch ve insan incelemeli PR olarak kalır. Bu görevde main güncellemesi force push veya Unity içeriği silme yetkisi vermez.

# Ekip katkı rehberi

README, ARCHITECTURE, PROJECT_STATUS ve AGENTS okuyun. Kod/testler gerçek kaynaktır. AI çıktısı dahil her değişiklik insan tarafından incelenir.

## Branch ve PR

Unity kökünü koruyun; web işi web-editor/ altında. Göreve özel feature/fix branch kullanın; AI branch'lerinde aksi istenmedikçe codex/ öneki. İlk entegrasyon için istenen branch feature/web-editor-integration'dır.

Temiz ve onaylı çalışma durumunda repository kökünden:

```powershell
git status --short
git fetch origin
git switch main
git merge --ff-only origin/main
git switch -c feature/task-name
```

Uncommitted işi önce koruyun; reset/clean ile temizlemeyin. Task branch'ini güncellemek için git fetch origin ve git merge origin/main kullanın; conflict'te dosya sahipleriyle konuşun. Paylaşılan geçmişi rewrite/force push yapmayın.

Dosyaları açık listelerle stage edin; staged diff'i inceleyin. Main'e doğrudan push varsayılan değildir. PR main'i hedefler; insan review ve kontrollerden sonra ekip merge eder. Branch push/PR izni deployment/merge izni değildir. İlk yayın [PUBLISHING](PUBLISHING.md) kapılarına tabidir.

PR açıklaması problem, son davranış, kapsam, gerçek testler, kanıt ve sınırları içermeli. Görsel/loglarda sır veya özel eğitim içeriği bulunmamalı.

## Paralel çalışma ve mimari değişiklikler

Dosya/scope sahipliği belirleyin. App.tsx, registry/types, BabylonViewport, viewerInteraction, useEnvironmentEditor ve editorStorage ortak çekirdektir; paralel düzenleme önceden koordine edilir. İlgisiz UI işinde kamera/storage/lifecycle değiştirmeyin. Her GLB için ayrı editör yazmayın.

Bağımsız alanlar: onboarding, erişilebilirlik, test fixture'ları, model kaynak/lisans envanteri ve performans ölçümü. Büyük refactor/migration/yeni servis önce kısa tasarım önerisi + insan onayı gerektirir. Alternatifler, veri etkisi, geriye uyumluluk, rollback ve kabul testlerini belirtin.

## Test ve asset sorumluluğu

web-editor/ içinde npm ci, npm run build, npm run lint ve `node --experimental-strip-types --test tests/*.test.ts`. npm test script'i yoktur. Bağımlılık/lock değişikliklerini birlikte inceleyin.

UI için keyboard/focus/responsive/reduced motion; navigation için zone→kart, rapid A→B cancellation, Escape/history; camera için overhead/world-up/odaklı hareket doğrulayın. Docker değişirse config, servis, log ve HTTP gerekir. Yapılmayanları yazın; eski raporu yeni test sonucu saymayın.

Model olmadan build/unit çalışır; gerçek loader/görsel kontrol lisanslı yerel varlık ister. Asset sorumlusu yazar, URL, lisans sürümü, atıf/değişiklik, hash ve izin kanıtını ASSETS içinde tutar. Hospital NC koşulları ayrıca doğrulanır; metadata hak doğrulaması değildir.

## Hata ve AI görevleri

Bug: branch/commit, Node/npm, browser/OS/origin, environmentId/mode, tekrar adımları, beklenen/gerçek sonuç ve güvenli console çıktısı. Storage için schema/anahtar; kamera için poz/FOV ve tuş/drag sırası. Özel veriyi ayıklayıp minimum örnek verin.

AI_TEAM_CONTEXT başlangıç prompt'unu kullanın: hedef, izinli dosyalar, sözleşmeler, kabul testleri ve rapor. AI önce Git/kodu incelesin; varlık/özellik/test uydurmasın. İnsan review olmadan merge/deployment yoktur.
