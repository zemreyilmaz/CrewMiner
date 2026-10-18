# Arkadaşınızın oyun içi kabul testleri

Durum: **Henüz uygulanmadı.** Yerel derleme veya birim testi bu listeyi geçmiş sayılmaz.

İlk testte kayıt yedeği ve iki bot kullanın; oyunu tek oyunculu açın. Bilgileri doldurun:

- Oyun sürümü: ____ (hedef 1.13.4.0)
- OS: ____ / Luatrauma commit/sürüm: ____ (hedef 85ded59c)
- Mod sürümü: 0.1.0 / Denizaltı: ____ / Diğer AI modları: ____
- Tarih ve görev/harita seed'i: ____

| # | İşlem | Beklenen | Sonuç |
|---|---|---|---|
| 1 | Eksik kıyafet/oksijen/kesiciyle içeride Hazırlan verin; malzemeler gemide erişilebilir olsun. | Derinliğe uygun kıyafet giyilir; oksijen ve kesici sarfı tamamlanır; dışarıdan ekipman aranmaz. | ☐ |
| 2 | Hazırlığın tamamlanmasını bekleyin. | Bot airlock/komşu güvenli iç waypoint'e gelir; “Hazırım” bildirir. | ☐ |
| 3 | Hiç Çık vermeden en az 2 dakika bekleyin; dışarıda maden olsun, başka biri airlock'u kullansın. | Bot madencilik için çıkmaz, dış kapağı bekleme amacıyla açmaz; genel kapı kilidi yoktur. | ☐ |
| 4 | Hazırlanmamış bota veya staging'deki bota Çık verin. Sonra hazırlanmasına izin verin. | Anlaşılır ret; hazır olduğunda bile yeni Çık olmadan çıkmaz. Hazır botun tüpünü alın ve Çık vererek tekrar kontrolü deneyin. | ☐ |
| 5 | Hazır bota Çık verin; hiçbir maden seçmeyin. | Kendisi uygun ore hedefi seçer, normal yol bulmayla çıkar. | ☐ |
| 6 | Gemi yakınında sabit durun; menzilin içinde ve dışında kaynaklar kullanın. Menzili küçük ayarlayıp sefer sırasında değiştirin. | Sadece çıkıştaki merkez/menzil içindekiler seçilir. Ayar yeni sefere uygulanır. Bitkiler/enkaz dolapları hedeflenmez. | ☐ |
| 7 | Kesme ve toplamayı izleyin; bir ayrılmış parçayı da yakın yere bırakın. | Kesici elde hedefe doğrulur; gerçek ışın/sarf tüketimiyle duvar madeni ayrılır; normal yakın etkileşimle toplanır. Maden yaratılmaz/ışınlanmaz. | ☐ |
| 8 | İki botu hazırlayıp ayrı ayrı Çık verin. Bir botun hedefi varken onu kontrol edin veya öldürün (yalnız test kaydında). | Ayrı bekleme noktaları ve ayrı hedefler; rezervasyon oyuncu kontrolü/ölümde bırakılır. Hedef kaybı diğer botları kilitlemez. | ☐ |
| 9 | Çalışırken boş yuvaları doldurun; ayrı denemede oksijeni azaltın; ayrı denemede Geri dön verin. | Kesme durur; güvenlik payı bitmeden dönüş başlar; kıyafet/oksijen/kesici kapasite açmak için rastgele atılmaz. | ☐ |
| 10 | Bot dışarıdayken gemiyi yavaşça hareket ettirin; sonra daha uzağa götürün. | Eski sabit alanın dışına yeni hedef peşinde sürüklenmez. Güncel gemiye mesafe/oksijen bütçesi nedeniyle gerekirse erken döner. | ☐ |
| 11 | Dönüşü ve depolamayı izleyin. Depo dolu/kilitli durumunu da deneyin. | Aynı gemiye girer, erişilebilir uygun kaba bırakır. Depo yoksa maden üzerinde kalır ve bildirir. Bekleyince tekrar kendiliğinden çıkmaz. | ☐ |
| 12 | Kıyafet/kesici/sarfı kaldırın; yanlış airlock waypoint'i kullanın; ulaşılamaz maden/kapı oluşturun. | Somut neden, sınırlı deneme/süre, hedefin atlanması veya güvenli sonlandırma. Dönüş yolu açılınca yeniden Geri dön denenebilir. | ☐ |
| 13 | Beklerken airlock'u su/ateş açısından tehlikeli yapın. | Acil AI önceliği korunur; güvenli iç bölgeye çekilir. Tehlike geçince yeni Çık olmadan madencilik başlamaz. | ☐ |
| 14 | Keserken botun kontrolünü alın, sonra başka karaktere geçin. | Araç/hareket otomasyonu durur; bot bırakılınca kendiliğinden madencilik sürmez. Geri dön emriyle dönüş/depolama yapılır. | ☐ |
| 15 | Bot dışarıdayken emir simgesini normal arayüzden silin/değiştirin; turu bitirin/yeniden başlatın. | Dışarıda görev silinip bırakılmaz; dönüş başlar. Yeni turda eski hedef/rezervasyon kalmaz. | ☐ |
| 16 | Küçük airlock'ta `crewminer_wait` noktaları, dolapta `crewminer_storage` etiketini deneyin. | Etiket seçimi tutarlı; iki bot kapının ağzında toplanmaz. Yetersiz alan açıkça bildirilir. | ☐ |

Test 6 için Editör test oturumunda kaynakları bilinen mesafelere koymak yardımcı olur; dünya/display ölçüsünde 100 birim = 1 m. Radyal sınır, kaynak Item.WorldPosition merkezine göre değerlendirilir. Küçük menzilde hiç kaynak yoksa çıkıştan sonra görev güvenle biter; bu hata değildir.

Hata paylaşımı: `LocalMods/CrewMiner/CrewMiner.log`, varsa `CrewMiner.previous.log`, F3 exception metni, bu doldurulmuş tablo, denizaltı adı/seed, son verilen emir ve görülen davranışı paylaşın. Konsolda CrewMiner başlangıç mesajı yoksa önce istemci Luatrauma/C# yükleme ayarını kontrol edin. Kayıtların içinde oyuncu/bot adları olabilir; paylaşmadan önce gerekirse bu adları değiştirin.
