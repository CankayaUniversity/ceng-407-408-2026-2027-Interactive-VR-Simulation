# Mevcut ekip yayını

21 Ekim 2026: mevcut feature/web-editor-integration ve PR #2 kullanılır. Eski ilk yayın yönergeleri tarihsel kayıttır; yeni PR açılmaz. Önce iki çalışma ağacı, uzak referanslar ve SHA-256 farkları incelenir. Ekip değişikliğiyle çakışmada durulur. Yalnız incelenen src/config/test/belgeler aktarılır; hedefe özel dosyalar silinmez, Unity kökü korunur.

.git, node_modules, dist, env/sırlar, makine raporları, görüntüler, skill/hook paketleri ve ayrı Resq denemesi aktarılmaz. Asset izni docs/ASSETS.md içinde; LFS izin sağlamaz.

Hedefte npm ci, build, lint ve mevcut Node testleri çalıştırılır; browser sonucu ayrıca yazılır. Yalnız açık dosya listesi stage edilir; push öncesi fetch yeniden yapılır. Yalnız feature branch push edilir, remote SHA ve refs/pull/2/head karşılaştırılır. Force push/main/merge/deployment yapılmaz. Son sonuç GITHUB_LATEST_SYNC_REPORT.md içindedir.
