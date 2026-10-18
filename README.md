# CrewMiner 0.1.0 — oyun içi test adayı

İki aşamalı bot madenciliği: ekipman hazırlanır ve airlock yakınında beklenir; yalnızca ayrı **Çık ve maden topla** emriyle madencilik başlar. Hazır olmayan bota verilen çıkış emri saklanmaz.

**Hedef:** Barotrauma **1.13.4.0**, Luatrauma/LuaCsForBarotrauma **85ded59c4efed4d139159c4cb056cc9705a5096e**, .NET **8**. Windows/Linux/macOS istemci DLL'leri dahildir. **Tek oyunculu**; aynı oturumda birden fazla bot desteklenir. Çok oyunculu oturumlarda davranış çalıştırılmaz.

Yerel derleme ve mantık kontrolleri yapıldı; gerçek oyun açılmadı. Airlock geçişi, araç animasyonu, raycast ve fizik davranışları arkadaşınızın bilgisayarında aşağıdaki testlerden geçmelidir.

## Kurulum

1. Oyunun ana menüsünden sürümün **1.13.4.0** olduğunu kontrol edin. İşletim sistemi, oyun/yükleyici sürümü ve tek oyunculu kullanım bilgilerini test raporuna yazın. Geliştirme sırasında arkadaşınızın bu bilgileri bilinmiyordu; hedef sürüm varsayımı yayımlanan gerçek DLL'lere göre belirlendi.
2. Uyumlu **istemci Luatrauma** kurulumunu yapın. [Resmî kurulum belgesi](https://luatrauma.github.io/Luatrauma.Docs/installation/) ve [hedef yayın](https://github.com/evilfactory/LuaCsForBarotrauma/releases/tag/latest). Elle kurulum için işletim sisteminize uygun `luacsforbarotrauma_patch_windows_client.zip`, `..._linux_client.zip` veya `..._mac_client.zip` kullanılır. `latest` etiketi değişebilir; hedef commit yukarıdadır. Farklı oyun sürümüne bu DLL'leri zorla kurmayın.
3. Mod ZIP'ini oyun klasörüne açın. Sonuç **`LocalMods/CrewMiner/filelist.xml`** olmalı; araya ikinci bir `CrewMiner` klasörü girmemeli. macOS'ta oyun dosyaları genellikle `Barotrauma.app/Contents/MacOS` altındadır; Steam > Yönet > Yerel dosyalara göz at ile bulun.
4. Luatrauma ayarlarında C# modlarının çalışmasına izin verin. Oyunun Mods ekranında CrewMiner paketini etkinleştirin, oyunu yeniden başlatın.
5. F3 konsolunda `[CrewMiner] CrewMiner 0.1.0; oyun=1.13.4.0; tek oyunculu; uyumlu=True` başlangıç satırını kontrol edin. Luatrauma kuruluysa `cl_reloadluacs` komutu tanınır. İlk testte başka bot AI modlarını kapatın.

Oyuncunun SDK/Python kurmasına gerek yoktur. Bunlar yalnızca kaynak kodu yeniden derlemek içindir. ZIP oyun veya Luatrauma yükleyicisini içermez.

## Emirler

Oyun içindeki standart bot emir menüsünde **Hareket / Movement > CrewMiner** grubunu açın. Oyunun atanmış emir menüsü kısayolunu veya mürettebat satırındaki emir ekleme düğmesini kullanın. Üç ayrı menü seçeneği vardır:

1. **Madenciliğe hazırlan / Prepare for mining:** ekipman yalnızca kendi gemisinden alınır. Kıyafet giyilir, oksijen/kesici sarfı yüklenir, boş yuvalar kontrol edilir. Bot erişilebilir airlock bekleme noktasına gider.
2. **Çık ve maden topla / Depart and mine:** yalnızca “Hazırım, airlock'ta bekliyorum” durumuna ulaşmış botu başlatır. Ekipman ve konum tekrar kontrol edilir. Maden veya tür seçmeniz gerekmez.
3. **Geri dön / İptal / Return / Cancel:** içeride hazırlık/beklemeyi bitirir; dışarıda kesmeyi bırakır ve aynı denizaltıya döner. Normal emir silme/değiştirme de dışarıdaki bot için dönüşe çevrilir. Dönüş yolu üç denemede bulunamazsa durum bildirilir; kapıyı/yolu düzelttikten sonra **Geri dön** emrini yeniden verin.

Her bota ayrı emir verilebilir. Hazır olmadan verilmiş Çık emri, hazırlık tamamlandığında kendiliğinden çalışmaz. Sefer sonunda yeni **Hazırlan**, ardından ayrı **Çık** gerekir. Botun kontrolünü aldığınızda hareket/araç kullanımı durur; kontrolü bota bıraktıktan sonra **Geri dön** ile seferi kapatın. Kendiliğinden madenciliğe devam etmez.

## Airlock ve depolama

Algılama oda adına bağlı değildir: gerçek dış kapısı olan oda veya bu odaya iç kapıyla komşu bekleme odası, iç yol noktaları ve erişim kontrolü kullanılır. Botlar kapılardan en az 1,1 m uzaktaki, birbirinden en az 1,4 m ayrılmış Path waypoint'lerini paylaşır. Aynı nokta iki bota verilmez. Yeterli yer yoksa hata bildirilir; bot dışarı gönderilmez.

Otomatik seçim uygun değilse Denizaltı Editörü'nde güvenli iç odada, airlock'a yakın **Path waypoint** seçin, Tags alanına **`crewminer_wait`** ekleyin. Birkaç bot için birbirinden ve kapılardan uzak birkaç nokta ekleyin. Botun merkezinin tam noktaya gelmesi gerekmez; aynı odada 0,9 m yakınlık aranır. Waypoint'leri gemi kopyasında kaydedip o kopyayla yeni tur başlatın. Etiketli geçerli noktalar varsa otomatik oda seçiminin önüne geçer.

Madeni normalde kabul eden, gemide erişilebilir ve boş bir kap seçilir. İsteğe bağlı olarak bir dolap/kasa eşyasının Tags alanına **`crewminer_storage`** ekleyerek onu önceliklendirin. Eşyanın gerçek kap kapasitesi/uygunluğu yine kontrol edilir. Depo dolu, kilitli veya erişilemezse başka kap denenir; hiçbiri uygun değilse maden botta kalır. Sefer öncesi botun zaten taşıdığı madenler otomatik boşaltılmaz.

## Maden arama menzili

Mod klasöründeki **`Config/CrewMiner.xml`** dosyasında `SearchRadiusMetres="40"` değerini değiştirin. Aralık **5–150 metre**. Ayar sonraki seferin Çık emrinde okunur; devam eden seferin merkezi/menzili değişmez.

Merkez, çıkış anındaki denizaltının dünya konumudur. Bot ilerledikçe arama alanı kaymaz. Gemi hareket etse de sefer alanı sabit kalır; dönüş hedefi geminin **güncel** konumudur. Yalnız `ore` etiketli, `LevelResource` bileşenli, dünya üzerinde sahipsiz madenler hedeflenir. Bitkiler, canlılar, gemi ekipmanı ve enkaz konteynerleri hedef değildir. Ayrılmış maden parçaları da toplanır.

Oksijen ve yol bütçesi menzili daha da sınırlar; 150 m ayarı, botun 150 m'ye mutlaka gideceği anlamına gelmez. Ayrıntılar: [ayarlar](docs/SETTINGS.md).

## Doğrulama ve kayıtlar

- [Yerel doğrulama raporu](docs/VERIFICATION.md)
- [Arkadaşınız için oyun içi test listesi](docs/IN_GAME_TESTS.md)
- [API kanıtı ve sürüm kaynakları](docs/API_PROVENANCE.md)
- [Bilinen sınırlar](docs/LIMITATIONS.md)

Mod dizininde **`CrewMiner.log`** tutulur; aşama, hedef kimliği, sabit çalışma merkezi ve başarısızlık nedenleri kaydedilir. Mesajlar ayrıca F3 konsoluna ve bot konuşmasına gider. Hata durumunda oyun/yükleyici sürümü, işletim sistemi, denizaltı adı, son emir ve bu dosyayı gönderin. Gerekirse F3'teki Luatrauma exception metnini de kopyalayın. Kayıt saatleri UTC'dir.

## Kaynaktan derleme

Kaynak ZIP'ini açın; .NET 8 SDK ve Python 3.9+ ile kaynak kökünde:

```sh
python3 scripts/build.py
```

Windows'ta gerekirse `py -3 scripts/build.py`. Oyun kurulum dizini gerekmez. Yayımlanmış gerçek referans ZIP'i `dependencies/` içinde bulunur; SHA-256 ve tüm DLL hash'leri doğrulanarak `Refs/` altına açılır. NuGet paketi gerekmez. Script karar mantığı testlerini ve üç platformun metadata kontrolünü çalıştırır, sonra platform DLL'lerini ve kurulabilir mod ZIP'ini üretir:

- `build/LocalMods/CrewMiner/`
- `build/CrewMiner-0.1.0-mod.zip`
- `build/verification.log`

Geliştirme referanslarını oyun dizinine kopyalamayın. Runtime paketinde yalnız mod DLL'leri vardır.

## Kaldırma

Botlara Geri dön verip seferleri bitirin, kaydedin ve oyunu kapatın. Mods listesinden CrewMiner'ı kapatın; `LocalMods/CrewMiner` klasörünü silin. Mod kayıt dosyasına özel veri veya yeni oyun eşyası eklemez. Yalnız Luatrauma'yı da kaldıracaksanız onun kurulum yönteminin kaldırma adımlarını izleyin; resmî belge başlatma seçeneğini kaldırıp Steam dosya doğrulaması yapmayı açıklar.
