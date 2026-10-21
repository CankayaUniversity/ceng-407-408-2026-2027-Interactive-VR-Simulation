# AI ekip bağlamı

Amaç: farklı işyerleri için eğitim hotspotları ve inspection view hazırlayan tekrar kullanılabilir web editörü. Fotoğraf/video reconstruction ayrı gelecek pipeline; Unity/OpenXR ayrı gelecek native VR hedefidir. WISP uygulanmadı.

## Gerçek stack ve girişler

React 19, strict TypeScript 6, Vite 8, Babylon 9, npm/lock; Node 24 ekip standardı. Backend/auth yoktur. Docker tek frontend geliştirme servisidir.

- src/main.tsx → src/app/App.tsx: bellek içi gezinme, Dashboard/proje/ortam.
- src/features/projects/: Dashboard ve localStorage CRUD; proje workspace'i placeholder.
- src/features/environments/: types/registry, ortak BabylonViewport, editor state/storage, navigation/kamera yardımcıları, paneller.
- src/styles/: Studio Neutral tasarım; kesin token ve ölçüler DESIGN.md içinde.
- public/environments/: lisanslı yerel GLB'ler Git'ten hariç; docs/ASSETS.md.
- tests/: Node çekirdek testleri; scripts/inspect-glb.mjs asset incelemesi.

## Değişmeyecek sınırlar

Tek SceneDefinition/registry/editör yolu; her model için ayrı uygulama yok. React UI/JSON ile Babylon runtime/lifecycle ayrıdır. Ortam GLB kaynak transform'ları korunur. Dünya sağ el/Y-up; ölçek provisional.

Project key workplace-studio.projects.v1; editor key workplace-studio.environment-editor.v1.<encodedId>, schemaVersion=1. Onaysız migration/anahtar değişikliği yok. Proje CRUD ortam kayıtlarından bağımsızdır.

Zone-aware seçim yalnız explicit eylemle yapılır; controller completion/cancellation korunur. selected state'e global auto-zoom effect eklemeyin. Kamera ters drag/hassasiyet, ±85 pitch/world-up ve mevcut hareket odağı sözleşmesini ilgisiz işlerde değiştirmeyin.

Preview read-only, Editor değişiklik yapar; Spectator Editor aracı, Walking düz zemin Preview prototipi. Bunlar tam fizik değildir. Dashboard, editör layout'u, modeller veya Unity kökü görev dışında yeniden yazılmaz.

## Beklenen iş akışı

Git status/branch/remotes → belgeler → gerçek kod/test → kapsamlı küçük değişiklik → build/lint/ilgili testler → gerekli browser doğrulaması → Türkçe rapor → insan review. Yeni dev server/bağımlılık gereksizse eklenmez. Çalıştırılmayan test, yapılmayan ölçüm ve eksik asset açıkça belirtilir. Büyük refactor/migration/deployment önceden yetki ister. Sırlar/özel veriler AI prompt'una veya commit'e konmaz.

## Standart görev isteği

- Hedef: kullanıcı problemi ve beklenen davranış.
- İzinli dosya/modüller: somut scope.
- Kısıtlar: korunacak şema/kamera/layout/Unity dosyaları.
- Kabul testleri: komutlar ve manuel tekrar senaryoları.
- Rapor: değişen dosyalar, gerçek sonuçlar, riskler ve doğrulanmayanlar.

## Her AI asistanı için başlangıç prompt'u

> Workplace Studio görevine başlamadan web-editor/AGENTS.md, README.md, docs/ARCHITECTURE.md, docs/PROJECT_STATUS.md, docs/AI_TEAM_CONTEXT.md ve docs/ENVIRONMENT_CONTRACT.md oku. Bağımsız web checkout'undaysan web-editor/ öneki olmadan proje kökünü kullan. Git durumunu ve görevle ilgili gerçek kaynak/testleri incele; eski raporları doğrulanmış güncel özellik sayma. Verilen scope içinde ilerle; storage, ortak editör, kamera ve Unity kökünü koru. Varlık/özellik/test sonucu uydurma. Build, lint ve ilgili testleri çalıştır; doğrulanmayanları açıkça yaz ve Türkçe rapor üret. AI kodu insan tarafından incelenmelidir; main push, force push, otomatik merge veya yetkisiz deployment yapma.
