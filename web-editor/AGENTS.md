# Proje talimatları

Bu kurallar web projesine aittir; Unity deposunda kapsamı web-editor/ olur.

## Önce oku ve sınırı koru

- README.md, docs/ARCHITECTURE.md, docs/PROJECT_STATUS.md ve docs/ENVIRONMENT_CONTRACT.md okuyun. Gerçek kaynak/testleri inceleyin; eski raporlar güncel gerçek sayılmaz.
- React + strict TypeScript + Vite + npm ve ortak Babylon editörünü koruyun. Başlangıç src/main.tsx, kompozisyon src/app/, ortak CSS src/styles/.
- Her GLB için ayrı hardcoded editör oluşturmayın; SceneDefinition ve registry kullanın.
- İlgisiz UI işlerinde kamera yönü/hassasiyeti, scene yaşam döngüsü, navigation Promise/cancellation ve zone geçişlerini değiştirmeyin.
- Mevcut storage anahtarlarını/şemalarını koruyun; açıkça onaylanmış plan olmadan migration yapmayın. Projeler ve ortam kayıtları ayrıdır.
- İstenen görevle sınırlı kalın; onaysız backend, AI, VR, WISP veya yeni özellik eklemeyin. Gelecek planlarını çalışan özellik olarak sunmayın.
- Tasarım kararlarını, semantic HTML ve erişilebilirliği koruyun; teknik sorunları ve alternatifleri açıklayın. Küçük, okunabilir bileşenler kullanın.
- Model, animasyon, eğitim içeriği, test veya ölçüm uydurmayın. Üçüncü taraf kaynakları silmeyin.

## Değişiklik ve doğrulama

- Gereksiz bağımlılık/soyutlama ve ikinci dev server açmayın. npm ci kullanın; bağımlılık değişirse package-lock.json birlikte incelensin.
- İlgili değişikliklerden sonra npm run build, npm run lint ve node --experimental-strip-types --test tests/*.test.ts çalıştırın; ilgili browser senaryolarını ayrıca doğrulayın.
- Gerçek komut/sonuçları ve doğrulanmayan durumları ayırın. Belgeleri/raporları Türkçe yazın; PROJECT_STATUS.md güncel kalsın.
- Silme/üzerine yazmayı önceden açıklayın. Yıkıcı işlem, migration, büyük refactor veya deployment açıkça yetkilendirilmemişse önce onay isteyin. Branch/PR yayın yetkisi merge/deployment izni sayılmaz.

## Git ve ekip çalışması

- Önce status/branch/remotes inceleyin; uncommitted işi koruyun. docs/CONTRIBUTING.md ve docs/PUBLISHING.md izleyin.
- Unity kökünü silmeyin, taşımayın veya üzerlerine yazmayın. Web yalnız web-editor/ altında; nested .git kopyalamayın.
- Göreve özel feature branch + PR + insan incelemesi kullanın. Varsayılan main'e doğrudan push, force push veya otomatik merge yoktur.
- Sırları, .env, node_modules, dist, yerel çıktıları veya izinsiz üçüncü taraf asset'leri commit etmeyin. GLB/WISP/önizlemeler için docs/ASSETS.md okuyun.

## Docker geliştirme sözleşmesi

- Resmî Node.js 24 imajı, npm ci, yalnız frontend; /app bind mount ve /app/node_modules named volume korunsun.
- Vite container içinde 0.0.0.0:5173, host 127.0.0.1:5175; Compose Windows polling VITE_DOCKER_POLLING=true. Başlangıçta npm ci volume'u lock dosyasıyla eşleştirir.
- Başlatma: docker compose up -d --build; durum: docker compose ps; günlük: docker compose logs; durdurma: docker compose down.
- Docker değişirse compose config, başlatma, durum, günlük, HTTP, build/lint doğrulayın; CODEX_DOCKER_REPORT.md kaydedin. Engine yoksa çalıştığını iddia etmeyin.
- docker compose down -v bağımlılık volume'unu da siler; etkisini açıklamadan kullanmayın.
