# Ayarlar

`LocalMods/CrewMiner/Config/CrewMiner.xml` — noktalı ondalık kullanılır. Geçersiz/sınır dışı değer, kayda neden yazılarak varsayılana döner. Çıkışta ayarlar sefere kopyalanır; dosyayı düzenlemek aktif seferin bütçesini değiştirmez. Hazırlan emri de ayarı yeniden okur.

| Alan | Varsayılan | Sınır | Anlam |
|---|---:|---:|---|
| SearchRadiusMetres | 40 | 5–150 | Çıkıştaki gemi merkezi etrafında sabit yarıçap |
| ScanIntervalSeconds | 3 | 1–30 | Dünya maden listesi tarama aralığı |
| MinimumFreeSlots | 2 | 1–6 | Çıkışta boş genel envanter yuvası |
| MinimumDepartureOxygenPercent | 85 | 50–100 | Kıyafetin çalışan oksijen yuvasındaki tüp |
| OxygenReservePercent | 15 | 10–40 | Dönüş sonrası bırakılacak pay |
| OxygenDrainPercentPerSecond | 0.5 | 0.1–5 | Oksijen tüketim tahmini için alt sınır |
| MinimumSwimSpeedMetresPerSecond | 1.2 | 0.2–5 | Muhafazakâr dönüş hızı tahmini |
| RouteSafetyFactor | 1.5 | 1–5 | Yol uzunluğuna uygulanan pay |
| ReturnMarginSeconds | 45 | 20–180 | Airlock ve gecikme zaman payı |
| PrepareTimeoutSeconds | 180 | 30–600 | Aktif hazırlık süresi |
| TargetTimeoutSeconds | 90 | 20–300 | Yaklaşma/kesme/toplama aşaması üst süresi |
| ReturnTimeoutSeconds | 180 | 30–900 | Bir dönüş denemesi üst süresi |
| StuckTimeoutSeconds | 25 | 10–120 | Hareket/kesme ilerlemiyor sayılma süresi |
| WaitingWaypointTag | crewminer_wait | metin | Editörde iç Path waypoint etiketi |
| StorageTag | crewminer_storage | metin | Öncelikli depo eşyası etiketi |

Staging/bekleme noktasına ulaşma 90 saniye; depolama 90 saniye ile sınırlı. Hazır bekleme bilerek süresizdir. Yaklaşma en çok iki, dönüş en çok üç deneme; ulaşılamayan maden o sefer için atlanır. Acil görevler çalışırken aşama zamanlayıcısı ilerlemez. Kapı/oda şartları beklerken her saniye yeniden değerlendirilir.

Oksijen modeli:

`gerekli yüzde = rezerv + (yol_metre × yol_payı / hız + zaman_payı) × max(ayar_tüketim, gözlenen_tüketim)`

Dönüş yolunda oyunun yol bulucusunun uzunluğu ve uç bağlantı mesafeleri kullanılır. Yeni hedefte gidiş yolu + hedefin güncel gemiye mesafesi değerlendirilir. Oyun içinde gözlenen daha hızlı tüp azalması, tahmini artırır. Bu bir kaynak planlama modelidir; akıntı, hasar, enfeksiyon veya gelecekteki kapı kapanmalarını kesin hesaplayamaz. Modifiye oksijen tüketimi olan oyunlarda tüketim alt sınırını artırın. Kesici sarfı hazırlık/çıkışta en az %60; çalışma sırasında %15 altına inerse dönüş gerekir.

Ölçek tahmin edilmedi: hedef oyun kaynağında `Physics.DisplayToRealWorldRatio = 1 / 100`. Dünya/display konumundan metreye bu alan kullanılır. `AIObjectiveGoTo.CloseEnough`, `RepairTool.Range` ve `SteeringPath.TotalLength` display birimleridir. Yol bulucu başlangıç/bitiş değerleri ise `SimPosition`/`GetRelativeSimPosition` üzerinden verilir.
