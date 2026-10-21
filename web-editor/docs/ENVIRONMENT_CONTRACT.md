# Ortam görüntüleyici sözleşmesi

`src/features/environments/types.ts` Babylon nesnelerinden bağımsız JSON veri tiplerini içerir. `registry.ts` ortam tanımlarını tutar; render bileşeni model adına göre koşul çalıştırmaz. Kokpit seated ve Charité ameliyathane walking kayıtları aynı viewport/loader/editor altyapısını kullanır. Walking için açık interaction.walking ayarları zorunludur; fiziksel collision içermeyen prototip olarak etiketlenir.

## Koordinatlar

Sahne sağ el sistemindedir; Y yukarıdır. Kaynak GLB düğüm dönüşümleri korunur. `modelTransform` kök dönüşümüdür: öteleme, XYZ Euler dönüş (radyan), ölçek. Kamera position/target değerleri bu dönüşüm uygulandıktan sonraki sahne koordinatlarındadır. FOV derece; yakın/uzak düzlemler sahne birimindedir. Dünya ölçeğinin metreye karşılığı henüz doğrulanmamıştır.

Hotspot konumu GLB köküne göre yerel XYZ koordinatıdır; `modelTransform` uygulanmadan önce saklanır. `targetNode` isteğe bağlı GLB düğüm adıdır; konumu düğüm yereline dönüştürmez. Tekrarlanan düğüm adları için düzenleme fazında kararlı düğüm kimliği/yolu tasarlanmalıdır.

## Kayıt sözleşmesi

`SerializedSceneV1`: `schemaVersion: 1`, `environmentId`, `modelTransform`, `navigationMode`, `camera`, `hotspots`. Bu yalnızca tip sözleşmesidir; mevcut projelerin localStorage şeması değiştirilmez. Harici JSON okuma eklendiğinde çalışma zamanı doğrulaması ve sürüm geçişi yazılmalıdır. Bilinmeyen sürümler sessizce kabul edilmemelidir.

## Sonraki hotspot fazı

1. Onaylanan pilot kamera konumu ve model dönüşümünü registry içinde sabitleyin.
2. Canvas seçim ışınını mesh kesişimiyle hesaplayın; dünya noktasını kök dönüşümün tersiyle model yereline dönüştürün.
3. Kararlı hotspot kimliği ve gerekirse düğüm yolu üretin; başlık/açıklama alanlarını doğrulayın.
4. Seçim, etiket erişilebilirliği, silme/geri alma ve düzenleme akışlarını gereksinimlerle tasarlayın.
5. Yeni kayıt alanını mevcut proje CRUD verisini göç ettirmeden açıkça sürümleyin; içe/dışa aktarım doğrulaması ve model sürümü eşleşmesini test edin.

## Etkileşim ve editör kayıtları

Etkileşim fazında `InspectionZone` ve `SerializedEditorV1` eklendi. Önceki `SerializedSceneV1` sözleşmesi korunur. Editör kayıtları `workplace-studio.environment-editor.v1.<encodeURIComponent(environmentId)>` anahtarındadır; proje CRUD anahtarına yazılmaz. Şema sürümü, ortam/model URL'si, kök dönüşümü, koordinatlar, metin uzunlukları, kimlik tekilliği ve zone referansları doğrulanır. Bozuk veya uyumsuz veri korunur ve kayıt üzerine yazılması engellenir. Model dönüşümü veya URL değişirse eski veri otomatik taşınmaz.

InspectionZone kamera position/target değerleri **sağ el sahne koordinatlarındadır**, model yereli değildir. FOV derecedir. Bölge, gerçek görünüm incelenerek mevcut kameradan kaydedilir; otomatik üretilmiş ekipman adı veya yakın plan yoktur. `calibration: reviewed` editörün görsel onayıdır, fiziksel/mesleki doğruluk garantisi değildir. Veriler 32 bölge ve 100 hotspot ile sınırlanır.

Hotspotlar GLB'yi değiştirmez. Editor modunda açık yerleştirme işlemiyle gerçek mesh yüzeyine ray picking yapılır; kesişim kök world matrisinin tersiyle model yereline dönüştürülür. Taslak marker ve bilgi formundaki Save kaydı onaylar; Cancel/Escape taslağı atar. Preview salt okunurdur. `zoneId` isteğe bağlıdır; bölge silinirse hotspotlar zonesuz korunur. `targetNode` yalnızca referans metaverisidir; koordinatlar hâlâ model kökündedir.

Markerlar model kopyası olmayan React düğmeleridir. Konumları render sonrasında doğrudan DOM stiline yansıtılır; her karede React state güncellenmez. Otomatik pointer-move picking kapalıdır. Yerleştirmede tek pick; kamera öteleme geçişi isteğinde beş yol ışını kullanılır. Markerların geometri arkasında kalmasını çözmek için her karede occlusion picking yapılmaz; kaydedilmiş liste klavye erişimi sunar. Bu nedenle uzaktaki/örtülü yüzey markerları bazı açılarda önde görünebilir.

Zoom sınırı, hassasiyeti ve süreleri `SceneDefinition.interaction` üzerinden yapılandırılır. Örnek kokpitte 25–85°, hassasiyet 0.035 derece/piksel, zoom 180 ms, kamera geçişi 650 ms. Geçiş süresi 100–1500 ms aralığına sınırlanır; smoothstep konum/roll içermeyen en kısa yaw + pitch/FOV geçişi kullanılır. Reduced motion tercihinde süre kısalır. Düz yolu kesen geometri geçişi engeller; otomatik dolambaçlı yol veya tam hacim çarpışması yoktur. Güvenli başlangıç için sabit kamera konumuyla yalnız yön/FOV kalibrasyonu tercih edilmelidir.

Escape önce bilgi paneli/yerleştirmeyi kapatır, panel yoksa aktif yakın plandan genel görünüme döner. Native pointer-lock Escape engellenmez. Kamera mouse input yönü, handedness ve 2200 hassasiyet ayarı değiştirilmemiştir. Sayfa tekerleği yalnız canvas üzerinde zoom amacıyla kullanıldığında engellenir; canvas dışı ve Ctrl/Meta tarayıcı zoom'u korunur.

## Bölgeye bağlı tek seçimle hotspot açma

Marker ve Saved hotspots listesi aynı açık `select → navigateAndOpenHotspot` eylemini kullanır. Geçerli zoneId varsa kamera önce ilgili bölgeye gider; controller'ın son animasyon karesinde sonuçlanan `completed` cevabından sonra yalnız istenen hotspot paneli açılır. Preview bilgi, Editor property formunu açar. Kameranın konum/yön/FOV'u zaten hedefe yakınsa animasyon ve gereksiz history kaydı atlanır. Active zone etiketi tek başına yeterli sayılmaz; gerçek kamera pozu kontrol edilir.

Zonesuz hotspot kamerayı hareket ettirmez. Eksik bölge referansı artık tüm kaydı kullanılamaz yapmaz: geçerli şema korunarak uyarı gösterilir ve bilgi mevcut görünümde açılır. Referans otomatik silinmez, şema sürümü veya storage anahtarı değişmez. Geçersiz veri şekli/koordinat/sürüm gibi diğer doğrulama hataları önceki korumayı sürdürür. Editör eksik referansı seçicide `Unavailable zone` olarak gösterir.

Controller `move()` sonucu `completed | cancelled | blocked` Promise'ıdır; zamanlayıcı tahminiyle panel açılmaz. Yeni seçim, doğrudan zone/overview/previous eylemi, mod değişimi, kalibrasyon, Escape veya unmount bekleyen isteği iptal/geçersiz kılar. İstek sıra kimliği eski Promise'in panel açmasını önler. Geometri yolu engellenirse hareket yapılmaz; uyarıyla bilgi mevcut görünümde açılır. Doğrudan zone düğmesi hiçbir hotspotu kendiliğinden seçmez.

Bu davranış selected/draft değişimini izleyen genel bir auto-zoom effect'i değildir. Kayıt yükleme, form yazma/iptal ve şema parse işlemleri kamera geçişi başlatmaz. Görüntüleyici hazır olmadan liste seçimi kapalıdır; hazırken yeni seçim devam eden geçişi supersede edebilir.

## Spectator Mode ve Preview bilgi kartı

Spectator Mode (önceki UI adı Inspector Mode), yalnız Editor iş akışında açılan **oturumluk bir serbest kamera aracıdır**. Editor/Preview yetkisini ve `NavigationMode: seated | walking` sözleşmesini değiştirmez; storage şemasına yazılmaz. Spectator kapalıyken eski seated/mouse-drag/wheel davranışı korunur. Mevcut ters mouse yönü değiştirilmez.

Editor içindeki Spectator Mode düğmesi canvas'a odak verir. W/S kameranın tam bakış yönünde ileri/geri, A/D yaw'a göre yatay sol/sağ ekseninde; Q/E dünya Y ekseninde aşağı/yukarı taşır. Yukarı bakarken W yükselmeye de yol açar; roll uygulanmaz. Shift normal hızın iki katıdır. Konum değiştirilir; kamera rotation/target yönü yeniden ayarlanmaz. Save current view, mevcut controller capture üzerinden taşınmış gerçek pozu kaydeder.

Hareket mevcut Babylon onBeforeRender döngüsündedir. `interaction.inspector` yapılandırma adı geriye uyumluluk için korunur; bu alan kayıt şeması değildir. Varsayılan hız 0.12 sahne birimi/s, boost 2, azami delta 50 ms. Hız 0.01–1, boost 1–3, delta 1–100 ms aralıklarına da sınırlanır; çapraz hareket normalize edilir. İsteğe bağlı scene-space bounds desteklenir; doğrulanmış dünya ölçeği olmadığı için kokpit kaydında varsayımsal sınır tanımlanmamıştır.

Tuşlar yalnız etkin/görünür/odaklı canvas ve odaklı belge için yakalanır. Metin alanı, buton veya başka UI odağı tüm basılı tuşları temizler; typing tuşları engellenmez. Window blur veya gizlenen belge Spectator'ı kapatır. Viewport ekrandan çıkınca girdi temizlenir. Exit Spectator, Escape, sahneden ayrılma ve dispose hareketi durdurur. Zone/reset/numeric calibration/annotation navigation Spectator'dan çıkar; yerleştirme sırasında açılmaz. Ek render döngüsü veya her karede React state güncellemesi yoktur.

Bu araç **walking değildir**: yerçekimi, zemin takibi, karakter, tam çarpışma ve fizik yoktur; geometri içinden geçilebilir. Her karede ağır mesh picking yapılmaz. Düşük hız ve delta sınırı ani sıçramayı azaltır; çarpışma garantisi vermez. Gelecekteki walking bu araçtan ayrı gereksinim ve uygulama gerektirir. Sahne biriminin metre karşılığı hâlâ ekip doğrulamasına bağlıdır.

Preview, ayrı `HotspotInfoCard` bileşeni kullanır. Gerçek kayıtlı title/description, varsa zone adı ve Equipment guide / Read-only preview etiketleri gösterilir; form alanı veya uydurulmuş ekipman verisi yoktur. Masaüstünde kart viewport üzerine yerleşir, 3D canvas daralmaz. 760 px ve altında kaydırılabilir alt panel olur. Uzun ve kesintisiz metinler sarılır, sticky header Close erişimini korur. Odak kart başlığına taşınır, Close/Escape eski ortak kapanış eylemini kullanır; focus trap yoktur. Reduced motion tercihi giriş animasyonunu kapatır. Editor property formu ve tek seçimle zone→kart akışı korunur.

## Teknik kaynaklar

- [Babylon glTF yükleyici ve sağ el sistemi](https://doc.babylonjs.com/features/featuresDeepDive/importers/glTF/)
- [Kamera girdilerini özelleştirme](https://doc.babylonjs.com/features/featuresDeepDive/cameras/customizingCameraInputs/)

Kurulu paketlerin tipleri ve kaynak kodu da yaşam döngüsü ve yükleme API'si için incelenmiştir.


## Kararlı dünya-yukarı kamera yönelimi

cameraOrientation.ts kamera position/target verisini yaw/pitch'e çevirir. Sağ el sahnede yerel ileri -Z, sol elde +Z'dir. interaction.look.maxPitchDegrees varsayılan 85°, kabul edilen güvenli aralık 1–89°; uygulanan pitch simetrik ±sınırdır. Yaw kısıtlanmaz; geçişler en kısa yaw farkını izler. Roll 0, upVector dünya +Y, rotationQuaternion null kalır. Mouse input yönü/handedness çarpanı, angularSensibility 2200 ve inertia 0.65 değiştirilmez. Babylon girdisi uygulandıktan sonra onAfterCheckInputsObservable ile çizimden önce sınır kontrol edilir. Sınıra ulaşınca kalan rotation inertia temizlenir; içeri dönüş anında mümkün olur.

İlk kamera, numeric calibration, apply/reset, zone/history/hotspot geçişleri aynı yönelim yordamını kullanır. Önceki Quaternion.Slerp → Euler ara roll'ü kaldırıldı; geçişin completed/cancelled/blocked Promise sözleşmesi değişmez. Wheel sadece FOV'u değiştirir. Mevcut uygulamada Shift+drag pan veya Shift+wheel dolly bulunmadı; eklenmedi.

Babylon setTarget, hedef position.z ile eşitse camera.position.z'ye epsilon ekleyebilir; tam dikey LookAt yönünde dünya-up ile bakış doğrultusu paralel olur. Yeni yordam setTarget kullanmaz, konumu değiştirmez. Tam/neredeyse dikey doğrultunun yaw'ı matematiksel olarak belirsizdir: önceki yaw korunur, pitch sınırlandırılır. Bu nedenle tam 90° görüntü talebi 85° olarak gösterilir. Kalıcı kaynak kayıt değiştirilmez. Capture gerçek uygulanmış konum + birim ileri yön hedefi + FOV döndürür; view mesafesinin eski target uzunluğu saklanmaz, yön saklanır.

Spectator kapatma, navigation, placement, Editor→Preview, blur/gizlenme, unmount/hata ve pointer lock çıkışı basılı tuşları temizler. Kapatma aktif canvas pointer lock'unu bırakır. Kilit zorunlu değildir. Escape Spectator aktifken önce onu kapatır; sonraki Escape mevcut bilgi paneli→overview sırasını sürdürür. Save Current View alanına geçiş hareketi durdurur ve kaydetme gerçek controller.capture kullanır. Ek render döngüsü veya kare başına collision picking yoktur.


## Editor Workspace V2 ve çoklu ortam

EnvironmentWorkspace aynı BabylonViewport'u tam yükseklikte açar. Kompakt başlık, tek gruplu görev toolbar'ı, Scene Explorer, merkez viewport ve başlangıçta kapalı contextual Inspector vardır. Search gerçek zone/hotspot başlıklarını filtreler; listeler mevcut editor.goToZone/select eylemlerine bağlanır. Sağ panel hotspot formunu veya görünüm kaydını/özelliklerini eylem bağlamında gösterir; teknik pose/ortam bilgisi ve Advanced Camera Calibration ayrı details altındadır. Save view ve Add hotspot görünür metinli araçlardır. Preview authoring araçlarını gizler; HotspotInfoCard viewport overlay olarak kalır.

CSS yalnız editor-v2 kapsamındadır. Desktop body kaymaz; paneller kendi içinde kayar. 1200px ve altında tek seferde bir drawer açılır. Canvas JSX ağacında sabit kalır; panel, mod veya arama değişimi engine effect dependency'lerini değiştirmez. Picking callback'i mevcut place eylemini çağırdıktan sonra yalnız UI Inspector'ını açar. ResizeObserver mevcut engine.resize çağrısını sürdürür. Fullscreen gerçek tarayıcı API'sine bağlıdır.

App'in mevcut bellek içi gezinmesi ortam anahtarını taşır. EnvironmentWorkspace key'i ortam kimliğine bağlıdır; çıkışta observer/input/controller/scene/engine dispose olur. Yeni ortamda useEnvironmentEditor ayrı başlangıç kaydı yükler. Proje CRUD ve localStorage anahtarı değişmez.

## Hastane Preview Walking prototipi

Charité GLB identity modelTransform ile, kendi node transform'ları korunarak yüklenir. Ayrı collision mesh'i yoktur. Yatay yüzeylerin offline analizinde ana zemin Y≈0, tavan Y≈5.5 bulundu; gerçek metre ölçeği doğrulanmadı. interaction.walking groundHeight=0, eyeHeight=1.65, speed=.8, boostMultiplier=2, maxDeltaMs=50, bounds X/Z ±5 ayarlarını içerir. Bounds kaba emniyet kutusudur; duvar veya ekipman collider'ı değildir.

Preview'da Start walking açık bir aktivasyon eylemidir ve canvas'a odak verir. W/S yaw'ın yatay ileri/geri ekseninde, A/D yatay strafe; Shift boost. Q/E yürümede alınmaz. Ground constraint başlangıçta ve hareket adımlarında Y=groundHeight+eyeHeight uygular; yukarı bakmak uçurmaz. Mouse drag ve isteğe bağlı Capture mouse mevcut input/hassasiyet ile çalışır. Pitch ±85° ve world-up koruması aynıdır. Esc önce aktif hareketi durdurur, sonraki basış mevcut panel→overview davranışını sürdürür.

Walking ve Editor Spectator ayrı etkinlik durumlarıdır; ortak createInspectorNavigation çekirdeğini kullanır. Spectator tam bakış doğrultusunda W/S ve dünya-Y Q/E ile serbest uçuşu korur. İkisi aynı anda etkin olamaz. Mevcut tek before-render döngüsü ikisini günceller; yeni RAF/engine loop veya her kare triangle picking yoktur. Odak başka UI'ya geçince tuşlar temizlenir; blur/hidden/lock exit kapatır. Mode, placement, numeric apply, zone/hotspot navigation, reset, cancelMove ve unmount hareketi kapatır.

Güvenilir zemin takibi, basamak/eğim, gravity, duvar/ekipman çarpışması ve navmesh yoktur. Tarama modeli üzerinde hareket prototipidir. Klinik eğitim içeriği otomatik oluşturulmaz. Asset kaynağı/lisansı public/environments/HOSPITAL_ASSET_NOTES.md içindedir.
