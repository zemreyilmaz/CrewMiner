# API ve sürüm kanıtı

Tüm bağlantılar hedef yükleyicinin gerçek oyun kaynaklarını içeren sabit commit’ine gider. API imzaları ayrıca indirilen upstream publicized DLL’lerde `tools/ApiAudit` ile doğrulandı. Oyun API’sini taklit eden bir referans/kütüphane üretilmedi.

| Davranış | Kaynak |
|---|---|
| Emirler/gerçek factory | [AIObjectiveManager.cs:473](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectiveManager.cs#L473) |
| Emir XML biçimi/seçenekler | [Order.cs:163](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Order.cs#L163) |
| Hazırlık davranışı (araştırıldı; dış hedef aramamak için özel hazırlık kullanıldı) | [AIObjectivePrepare.cs:9](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectivePrepare.cs#L9) |
| İç/dış gezinme ve nodeFilter | [AIObjectiveGoTo.cs:639](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectiveGoTo.cs#L639) |
| Eşyayı normal yürüme/erişimle alma | [AIObjectiveGetItem.cs:312](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectiveGetItem.cs#L312) |
| Kıyafetin basınç koruması | [AIObjectiveFindDivingGear.cs:219](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectiveFindDivingGear.cs#L219) |
| Oksijen/sarfı kaba koyma | [AIObjectiveContainItem.cs:64](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/Objectives/AIObjectiveContainItem.cs#L64) |
| Maden çıkarma mekanizması | [RepairTool.cs:671](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Items/Components/Holdable/RepairTool.cs#L671) |
| Kaynağın gerçek bağlılık durumu | [LevelResource.cs:75](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Items/Components/Holdable/LevelResource.cs#L75) |
| Normal etkileşim ve normal araç kullanımı | [Item.cs:3173](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Items/Item.cs#L3173) |
| Normal yığın toplama; dar kapsamlı engelin gerekçesi | [Pickable.cs:164](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Items/Components/Holdable/Pickable.cs#L164) |
| Kapı/topolojiyle dışarı bağlantı | [Hull.cs:1465](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Map/Hull.cs#L1465) |
| Gerçek maden/bitki sınıflandırması | [Level.cs:2930](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Map/Levels/Level.cs#L2930) |
| Metre ölçeği | [Physics.cs:22](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Physics/Physics.cs#L22) |
| Yol uzunluğu dünya/display biriminde | [SteeringPath.cs:61](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/SteeringPath.cs#L61) |
| Dış yol bulucuda hareket eden gemi ofsetleri | [PathFinder.cs:173](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/Characters/AI/PathFinder.cs#L173) |
| Plugin giriş sözleşmesi | [IAssemblyPlugin.cs:6](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/LuaCs/_Plugins/IAssemblyPlugin.cs#L6) |
| Yükleyici manifest yolu | [ModConfigService.cs:138](https://github.com/evilfactory/LuaCsForBarotrauma/blob/85ded59c4efed4d139159c4cb056cc9705a5096e/Barotrauma/BarotraumaShared/SharedSource/LuaCs/_Services/ModConfigService.cs#L138) |

## Sürüm zinciri

- Resmî oyun kaynak commit’i: `e6e95da83b1b5ca8b55ec52653c7cf6b40e30247`. [WindowsClient.csproj](https://github.com/FakeFishGames/Barotrauma/blob/e6e95da83b1b5ca8b55ec52653c7cf6b40e30247/Barotrauma/BarotraumaClient/WindowsClient.csproj) sürüm `1.13.4.0` ve hedef `net8.0` gösterir. GitHub releases/latest oyun etiketi eski `v1.8.6.2` kaldığı için o etiket hedef alınmadı.
- Yükleyici yayın commit’i: `85ded59c4efed4d139159c4cb056cc9705a5096e`; [2026-08-01 tarihli latest yayını](https://github.com/evilfactory/LuaCsForBarotrauma/releases/tag/latest). Ayrı semantik yükleyici sürümü yayımlanmamış; commit kimliği kullanıldı.
- Referanslar: upstream `luacsforbarotrauma_refs.zip`, asset `497491772`, 19,362,151 bayt. SHA-256: `d23f72990c4ab1d9e8a798d2cf1bb5a57094f52db43b8e21b12015318adbcd1f`. GitHub asset digest’iyle yerel hash eşleşti.
- Üç platformun Barotrauma.dll metadata sürümü: `1.13.4.0`. Assembly informational version: `1.13.4.0+85ded59c4efed4d139159c4cb056cc9705a5096e`. Böylece referans/kaynak eşleşmesi doğrudan metadata’dan doğrulandı.
- Tüm referans DLL hash’leri `dependencies.lock.json` içinde. Bağımlılık arşivi kaynak paketinde özgün haliyle bulunur; runtime mod paketine taşınmaz.
- Derleme .NET SDK `8.0.421`; `net8.0`, yönetilen DLL; yeni NuGet bağımlılığı yok. `0Harmony`, `XNATypes`, `Farseer`, `MonoGame`, `BarotraumaCore`, `Barotrauma` gerçek referans paketinden gelir. `Mono.Cecil` yalnız metadata doğrulama aracında kullanılır.

## Dar kapsamlı müdahaleler

Harmony hedefleri: `AIObjectiveManager.CreateObjective` (yalnız crewminer emri), `AIObjectiveManager.UpdateObjectives` (yalnız etkin/askıdaki CrewMiner botları), `GameSession.Update` (saniyede bir görev temizliği), `GameSession.EndRound` (temizlik), `Character.Controlled` setter (yalnız görevi olan botun kontrolünü alma) ve `Pickable.OnPicked(Character,bool,bool)` (yalnız kendi toplama hedefi).

Son yama zorunlu kapsam korumasıdır: vanilla normal toplama, `DroppedStack` içindeki diğer parçaları da alır. Bu, alan dışındaki veya başka bota rezerve edilmiş komşu parçayı da alabilirdi. Mevcut CrewMiner hedefi alınırken yalnız `pickDroppedStack=false` yapılır; normal Pick/etkileşim/mesafe/erişim/animasyon yoluna müdahale edilmez. Diğer botlar/oyuncular etkilenmez.

Kaynakta `AIObjectiveGetItem` dışarıdaki karakter için görevi bırakır; bu yüzden dış maden toplama ona devredilmez. `TryInteract` normal mesafe/erişim kontrolü sonrasında kullanılır. Kesme, normal `Aim` ve `Shoot`/`Use` girdileriyle `Character` → `Item.Use` → `RepairTool` yolundan yürür; `DeattachTimer` mod tarafından değiştirilmez.

Depolamada vanilla `AIObjectiveContainItem` başarısız aktarımda eşyayı yere bırakabildiği için madenler bu göreve verilmez. Bot normal GoTo ile depoya yürür; fiziksel `CanInteractWith`, erişim ve kapasite kontrolünden sonra normal `Inventory.TryPutItem` çağrılır. Depolama uzaktan yapılmaz.

[Resmî assembly kurulum belgesi](https://luatrauma.github.io/Luatrauma.Docs/cs/setup-assembly-csharp/), [RepairTool](https://regalis11.github.io/BaroModDoc/ItemComponents/RepairTool.html), [LevelResource](https://regalis11.github.io/BaroModDoc/ItemComponents/LevelResource.html).
