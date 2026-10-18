# Yerel doğrulama — 18 Ekim 2026

| Kontrol | Sonuç |
|---|---|
| Oyun kurulu olmadan gerçek yayımlanmış Luatrauma referansları | Doğrulandı; SHA-256 lock dosyasında |
| SDK | .NET SDK 8.0.421; hedef net8.0 |
| Windows client Release | Başarılı, 0 hata / 0 uyarı |
| Linux client Release | Başarılı, 0 hata / 0 uyarı |
| macOS/OSX client Release | Başarılı, 0 hata / 0 uyarı |
| Karar mantığı ve config kontrolleri | 70 kontrol geçti |
| Gerçek DLL metadata imzaları (3 platform) | 51 kontrol geçti |
| XML, assembly/sprite dosya yolları | Başarılı |
| Sahte oyun sınıfları / stub DLL | Kullanılmadı |
| Oyun içi kabul testleri | Yapılmadı; arkadaşınızın bilgisayarında bekliyor |

Tekrarlama: kaynak kökünde `python3 scripts/build.py`. Ham çıktı: kaynak teslimatındaki `verification.log`. Script ayrıca yerelde `build/verification.log` üretir.

Karar kontrolleri: bütün aşamalarda çıkış için Ready + yerinde olma + güncel ekipman zorunluluğu, sabit yarıçapın kenarları, hareket eden geminin alanı genişletmemesi, yeni ayarın eski snapshot'ı değiştirmemesi, geçersiz/NaN/sonsuz XML değerleri, hedeflerin iki bot arasında ayrılması, iptal/ölüm/hedef kaybı/tur temizliği ve mesafe-hız-tüketim-oksijen rezervi ilişkileri.

Metadata kontrolleri gerçek upstream DLL'lerde sürüm ve kullanılan Harmony hedeflerinin imzalarını doğrular. Oyunu başlatmaz, Harmony patch kurulumunun veya fizik/AI davranışının oyun içindeki sonucunu kanıtlamaz. Derleme, publicized referanslarda erişim ve tür uyumunu doğrular; runtime'daki iç API erişimi Luatrauma'nın standart IgnoresAccessChecksTo düzenini kullanır.

Erişilemeyen derleme bağımlılığı yoktur. Runtime için lisanslı oyun ve uyumlu istemci Luatrauma arkadaşınızın bilgisayarında gereklidir. Kaynak bundle'daki upstream referans arşivi yeniden derlemeyi oyun kurulumundan ve mutable latest indirmesinden bağımsız yapar.
