# Güvenli ilk web-editor yayını

**Durum: GitHub erişim/kimlik doğrulaması engelli; bu adımlar uygulanmadı.** Hedef private Unity deposu: https://github.com/Cengaverler/interactive-vr-simulation. İstenen branch feature/web-editor-integration, hedef main. Bu belge deployment veya merge talimatı değildir.

## Önce erişim

Mevcut GitHub hesabının repository read/write ve gerekiyorsa organization SSO iznini güvenli Git Credential Manager/ekip yönteminden tamamlayın. Sohbete, URL'ye veya komut çıktısına token koymayın. GitHub connector erişimi ile yerel Git kimliği ayrı olabilir.

```powershell
git ls-remote --symref https://github.com/Cengaverler/interactive-vr-simulation.git HEAD refs/heads/main
```

Main gerçekten okunamıyorsa durun. Yeni, boş, **orijinal web dizininin kardeşi** olacak checkout kullanın; mevcut dizinin üstüne clone yapmayın:

```powershell
git clone --branch main --single-branch https://github.com/Cengaverler/interactive-vr-simulation.git ..\interactive-vr-simulation-web-integration
cd ..\interactive-vr-simulation-web-integration
git status --short
git remote -v
git fetch origin main
git switch -c feature/web-editor-integration origin/main
```

Hedef klasör veya branch zaten varsa reset/overwrite yerine durup inceleyin. Yeni checkout'un root AGENTS/README kurallarını okuyun. `web-editor/` zaten varsa **durun ve raporlayın**; mevcut uygulamayı değiştirmeyin. Unity kökü Assets/Packages/ProjectSettings dahil aynen kalmalı. Bu görev için root README/ignore değişikliği gerekli değil: web alt dizininin kendi belgeleri/ignore'u yeterli.

## Kopyalama izin listesi

Yalnız aşağıdakileri yeni checkout'un yeni web-editor/ klasörüne, göreli yapılarını koruyarak kopyalayın:

- src/, tests/, scripts/.
- docs/ içindeki Markdown ekip/sözleşme/asset belgeleri.
- AGENTS.md, README.md, GITHUB_TEAM_HANDOFF_PUBLISH_REPORT.md.
- package.json, package-lock.json, index.html, vite.config.ts, tsconfig.json, tsconfig.app.json, tsconfig.node.json, .oxlintrc.json.
- .gitignore, .dockerignore, Dockerfile, compose.yaml.
- public/environments/cockpit/ASSET_NOTES.md, public/environments/HOSPITAL_ASSET_NOTES.md, public/previews/environments/README.md.

Kaynak .git, node_modules, dist, tsbuildinfo, .env, cache/tmp, IDE/AI oturum klasörleri, model/animasyon/doku kaynakları, JPEG önizlemeler ve makine yolları içeren geçmiş CODEX_*/GPT_* raporları kopyalanmaz. Geçmiş raporlar yerelde korunur; silinmez. Güncel durum/kararlar yeni docs içinde özetlenmiştir. Yeni tanınmayan dosyaları izin listesine otomatik eklemeyin.

Kopyadan önce/sonra sır, özel eğitim içeriği, kişisel makine yolları ve >20 MiB dosya taraması yapın. Regex taraması kusursuz secret detection değildir; staged kaynak/metin manuel incelenir. Lisanslı asset'ler sonra ayrı, onaylı süreçle teslim edilir. Assetsiz Dashboard fallback ve loader hata davranışı test edilmelidir.

## Kopya içinde doğrulama ve yayın

```powershell
cd web-editor
npm ci
npm run build
npm run lint
node --experimental-strip-types --test tests/*.test.ts
docker compose config
cd ..
git status --short
git diff --name-status origin/main
git add -- web-editor
git diff --cached --stat
git diff --cached --name-status
git diff --cached --check
git diff --cached --name-only -- . ':!web-editor/**'
```

Son komutun çıktısı **boş** olmalı; tüm staged değişiklikler yalnız web-editor/ altında beklenen A dosyaları olmalı. D/R veya Unity kök değişikliği varsa **commit etmeden durun**. İzin listesinden sapma, sır/asset veya beklenmeyen büyük dosya varsa durun. Orijinal web klasöründe stage yapılmaz.

```powershell
git commit -m "Add Workplace Studio web editor and team documentation"
git push -u origin feature/web-editor-integration
git rev-parse HEAD
git ls-remote origin refs/heads/feature/web-editor-integration
```

Remote SHA ile yerel SHA eşleşmeli. GitHub'da main ← feature/web-editor-integration PR açın; merge etmeyin. PR açıklamasında Unity kökü korunması, assetsiz kaynak teslimi, gerçek testler ve eksik modellerin kurulumunu belirtin. Gerçek PR URL ve commit'i yayın raporuna yazın. Force push/main push yoktur. Erişim/LFS/lisans sorunu çıkarsa durup minimal manuel adımları raporlayın.

Ekip PR branch'ini klonladıktan sonra README kurulumunu izler; branch main'e merge edilene kadar main'de web-editor bulunacağı varsayılmaz. Kaynak dizini web-editor olsa da Vite origin kökünden sunar; alt URL deployment bu işin kapsamında değildir.
