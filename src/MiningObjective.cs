using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using FarseerPhysics;
using Microsoft.Xna.Framework;

namespace CrewMiner;

internal sealed class MiningObjective : AIObjective
{
    public override Identifier Identifier { get; set; } = "crewminer".ToIdentifier();
    public override bool KeepDivingGearOn => true;
    public override bool KeepDivingGearOnAlsoWhenInactive => true;
    public override bool AbandonWhenCannotCompleteSubObjectives => false;
    public override bool AllowOutsideSubmarine => true;
    public override bool AllowInAnySub => true;
    public override bool ConcurrentObjectives => true;
    public override bool AllowWhileHandcuffed => false;
    public readonly Submarine Home;
    internal Phase Stage { get; private set; } = Phase.Preparing;
    public bool Finished => Stage == Phase.Stopped || Abandon;
    private Settings settings;
    private Area area;
    private WayPoint? station;
    private Item? suit, cutter, target, oxygenTank;
    private RepairTool? tool;
    private AIObjective? child;
    private Action? success, failure;
    private readonly HashSet<Item> cargo = new();
    private readonly HashSet<Item> initialOre = new();
    private readonly HashSet<Item> skipped = new();
    private readonly HashSet<Item> ignoredStorage = new();
    private readonly HashSet<WayPoint> ignoredStations = new();
    private float clock, phaseElapsed, scanAt, checkAt, lastProgress, bestDistance=float.MaxValue, lastDetach;
    private float observedDrain, previousOxygen=-1, oxygenSampleAt;
    private int returnAttempts, targetAttempts;
    private bool returningBlocked, withdrawing, suspended, ownsToolInput, hasDeparted;
    private string lastStatus="";
    private bool outside => character.CurrentHull == null || character.Submarine != Home;

    public MiningObjective(Character character, AIObjectiveManager manager, Submarine home) : base(character, manager, 1)
    { Home=home;settings=Plugin.Config; }

    private void Say(string text)
    {
        if(lastStatus==text) return;
        lastStatus=text;Plugin.Tell(character,text);
    }
    private void SetPhase(Phase phase, string message)
    {
        var previous=Stage;
        ClearChild();ReleaseTrigger();Stage=phase;phaseElapsed=0;lastProgress=clock;bestDistance=float.MaxValue;
        if(previous!=phase) Plugin.Log($"{character.Name}#{character.ID}: {previous} -> {phase}; hedef={target?.ID.ToString() ?? "-"}");
        if(message.Length>0) Say(message);
    }
    public void Prepare()
    {
        if(suspended && !outside) { suspended=false;Stage=Phase.Preparing; }
        if(Policy.Working(Stage) || Stage==Phase.Returning || Stage==Phase.Unloading || outside)
        { Say("Devam eden sefer için önce Geri dön emri verilmeli.");return; }
        Plugin.Claims.Release(character);station=null;ignoredStations.Clear();skipped.Clear();ignoredStorage.Clear();
        settings=Settings.Load(Plugin.ConfigPath,Plugin.Log);
        SetPhase(Phase.Preparing,"Ekipmanı gemi içinde tamamlıyorum.");
    }
    public void Depart()
    {
        if(suspended) { Say("Oyuncu müdahalesinden sonra önce Hazırlan veya Geri dön emri gerekli.");return; }
        if(Stage==Phase.Ready) settings=Settings.Load(Plugin.ConfigPath,Plugin.Log);
        if(Stage==Phase.Ready && (!SafeHull(character.CurrentHull) || station==null || !SafeHull(station.CurrentHull)))
        { withdrawing=true;station=null;SetPhase(Phase.Staging,"Çıkış reddedildi: airlock güvenli değil; içeri çekiliyorum. Yeni Çık emri gerekli.");return; }
        var reason=EquipmentReason(departure:true);
        var at=AtStation();
        if(!Policy.MayDepart(Stage,at,reason==null))
        {
            if(Stage==Phase.Ready && reason!=null) SetPhase(Phase.Preparing,$"Çıkış iptal: {reason} Yeniden hazırlanıp ayrı Çık emri bekleyeceğim.");
            else Say($"Çıkış reddedildi: {(reason ?? (at ? "hazırlık henüz tamamlanmadı." : "bekleme noktasına henüz ulaşmadım."))}");
            return;
        }
        // Immutable world area for this expedition. The moving submarine remains the return target.
        hasDeparted=true;
        area=new Area(Home.WorldPosition.X,Home.WorldPosition.Y,settings.SearchRadiusMetres/Physics.DisplayToRealWorldRatio);
        skipped.Clear();cargo.Clear();initialOre.Clear();
        initialOre.UnionWith(character.Inventory.FindAllItems(i=>i.HasTag("ore".ToIdentifier()),true));
        returnAttempts=0;returningBlocked=false;targetAttempts=0;
        station=null;scanAt=clock;previousOxygen=-1;observedDrain=0;
        Plugin.Log($"{character.Name} sefer alanı: merkez=({area.X},{area.Y}) display; yarıçap={area.Radius} display; denizaltı={Home.ID}");
        SetPhase(Phase.Searching,$"Çıkış onaylandı. Sabit çalışma yarıçapı {settings.SearchRadiusMetres:0} m.");
    }
    public void Cancel()
    {
        if(Finished || Stage is Phase.Returning or Phase.Unloading) return;
        if(outside) BeginReturn("İptal emri: kesmeyi bıraktım, kendi denizaltıma dönüyorum.");
        else Stop("Hazırlık/bekleme iptal edildi.",true);
    }
    private void BeginReturn(string why)
    {
        CaptureCargo();Plugin.Claims.Release(character);target=null;returningBlocked=false;returnAttempts=0;
        SetPhase(Phase.Returning,why);
    }
    public void Stop(string why,bool announce)
    {
        ClearChild();ReleaseTrigger();Plugin.Claims.Release(character);station=null;target=null;
        Stage=Phase.Stopped;Plugin.Log($"{character.Name}#{character.ID}: görev bitti; {why}");
        if(announce) Say(why);
        IsCompleted=true;
        if(objectiveManager.ForcedOrder==this) objectiveManager.ClearForcedOrder();
    }
    public void SuspendForPlayer()
    {
        if(suspended || Finished) return;
        // Called before Controlled's setter, while we can still release our own input.
        ReleaseTrigger();ClearChild();Plugin.Claims.Release(character);target=null;station=null;
        suspended=true;Stage=Phase.Returning;phaseElapsed=0;
        if(objectiveManager.ForcedOrder==this) objectiveManager.ClearForcedOrder();
        Say("Oyuncu kontrolü aldı; madencilik durdu. Bot kontrolüne geçince Geri dön emriyle seferi kapatın.");
    }
    private void ClearChild()
    {
        subObjectives.Clear();child=null;success=null;failure=null;
        if(character.IsBot && objectiveManager.CurrentObjective==this && character.AIController is HumanAIController ai) ai.PathSteering.ResetPath();
    }
    private void ReleaseTrigger()
    {
        if(!ownsToolInput) return;
        ownsToolInput=false;
        if(!character.IsBot) return;
        character.SetInput(InputType.Shoot,false,false);
        character.SetInput(InputType.Use,false,false);
        character.SetInput(InputType.Aim,false,false);
    }
    public override bool CheckObjectiveState() => Finished;
    public override float GetPriority()
    {
        if(!Plugin.Authorized || !character.IsBot || character.IsDead || Finished || returningBlocked || suspended) return 0;
        // Keep vanilla emergencies (90..100) ahead of this task (<=70).
        return objectiveManager.IsOrder(this) ? objectiveManager.GetOrderPriority(this) : AIObjectiveManager.HighestOrderPriority-1;
    }
    public override void OnAbandon()
    {
        ClearChild();ReleaseTrigger();Plugin.Claims.Release(character);base.OnAbandon();
    }
    public override void OnDeselected()
    {
        ReleaseTrigger();base.OnDeselected();
    }
    public override void Update(float dt)
    {
        if(!Plugin.Authorized || !character.IsBot || character.IsDead || Finished || suspended) return;
        base.Update(dt);clock+=dt;
        var selected=objectiveManager.CurrentObjective==this;
        if(selected) phaseElapsed+=dt;
        if(clock<checkAt) return;
        checkAt=clock+1;
        if(Home.Removed) { Stop("Hedef denizaltı artık mevcut değil; mevcut hayatta kalma davranışına geçiyorum.",true);return; }
        if(Policy.Working(Stage))
        {
            SampleOxygen();
            var why=EquipmentReason(false);
            if(why!=null) { BeginReturn("Geri dönüyorum: "+why);return; }
            float distance=RouteHomeMetres();
            if(!float.IsFinite(distance) || OxygenPercent()<=RequiredForReturn(distance))
            { BeginReturn("Dönüş yolu/oksijen güvenlik payı nedeniyle madenciliği bitiriyorum.");return; }
            if(target!=null && !ValidMineral(target)) { SkipTarget("Hedef artık kullanılabilir değil.");return; }
        }
        if(Stage==Phase.Ready)
        {
            var why=EquipmentReason(true);
            if(why!=null) { SetPhase(Phase.Preparing,"Hazırlık durumu değişti: "+why+" Yeni Çık emri gerekli.");return; }
        }
        if(Stage is Phase.Preparing or Phase.Staging or Phase.Ready)
        {
            if(outside) { BeginReturn("Hazırlık sırasında gemi dışına taşındım; içeri dönüyorum. Yeni çıkış emri gerekecek.");return; }
            if(!SafeHull(character.CurrentHull))
            {
                if(!withdrawing)
                { withdrawing=true;station=null;SetPhase(Phase.Staging,"Bekleme alanı tehlikeli; güvenli iç bölgeye çekiliyorum. Acil durum davranışları öncelikli."); }
            }
        }
        if(Stage==Phase.Preparing && phaseElapsed>settings.PrepareTimeoutSeconds)
        { Stop("Hazırlık süresi doldu: "+(EquipmentReason(true) ?? "erişilebilir ekipman/yol bulunamadı."),true);return; }
        if(Stage==Phase.Staging && phaseElapsed>90)
        { Stop("Bekleme alanına erişemedim. İç yol noktalarını ve crewminer_wait etiketini kontrol edin.",true);return; }
        if(Policy.Working(Stage) && Stage!=Phase.Searching && phaseElapsed>settings.TargetTimeoutSeconds)
        { SkipTarget("Hedef süre sınırına takıldı; atlanıyor.");return; }
        if(Stage==Phase.Returning && phaseElapsed>settings.ReturnTimeoutSeconds)
        { ReturnFailed();return; }
        if(Stage==Phase.Unloading && phaseElapsed>90)
        { Stop("Depolama süresi doldu; kalan madenleri üzerimde tutuyorum.",true);return; }
        if(selected && child is AIObjectiveGoTo move)
        {
            var d=Vector2.Distance(character.WorldPosition,move.Target.WorldPosition);
            if(d<bestDistance-30) { bestDistance=d;lastProgress=clock; }
            if(clock-lastProgress>settings.StuckTimeoutSeconds)
            {
                var fail=failure;ClearChild();fail?.Invoke();lastProgress=clock;
            }
        }
    }
    public override void Act(float dt)
    {
        if(!Plugin.Authorized || !character.IsBot || Finished || suspended) return;
        if(child!=null)
        {
            if(child.Abandon || child.IsCompleted)
            { var done=child.IsCompleted && !child.Abandon ? success:failure;ClearChild();done?.Invoke(); }
            return;
        }
        switch(Stage)
        {
            case Phase.Preparing: PrepareEquipment();break;
            case Phase.Staging: StageInside();break;
            case Phase.Ready:
                if(!AtStation()) SetPhase(Phase.Staging,"Bekleme noktasına dönüyorum; Çık emri gerekli.");
                else HumanAIController.SteeringManager.Reset();
                break;
            case Phase.Searching: Search();break;
            case Phase.Approaching: Approach();break;
            case Phase.Cutting: Cut(dt);break;
            case Phase.Collecting: Collect();break;
            case Phase.Returning: Return();break;
            case Phase.Unloading: Unload();break;
        }
    }
    private void Run(AIObjective objective,Action completed,Action failed)
    {
        child=objective;success=completed;failure=failed;lastProgress=clock;bestDistance=float.MaxValue;
        AddSubObjective(objective);
    }
    private void Move(ISpatialEntity destination,bool exterior,float reach,Action completed,Action failed)
    {
        Run(new AIObjectiveGoTo(destination,character,objectiveManager,getDivingGearIfNeeded:false,closeEnough:reach)
        { AllowGoingOutside=exterior,UsePathingOutside=true,SpeakIfFails=false,
          ExtraDistanceOutsideSub=0,ExtraDistanceWhileSwimming=0,
          endNodeFilter=n=>exterior || n.Waypoint.CurrentHull!=null && n.Waypoint.Submarine==Home },completed,failed);
    }
    private bool SafeHull(Hull? h) => h!=null && h.Submarine==Home && h.FireCount==0 && h.WaterPercentage<65 && h.LethalPressure<1;
    private bool AtStation() => station!=null && character.Submarine==Home && character.CurrentHull==station.CurrentHull && Vector2.Distance(character.WorldPosition,station.WorldPosition)<=90;
    private bool InternalItem(Item i)
    {
        if(i.Removed || i.Condition<=0 || !i.HasAccess(character)) return false;
        var owner=i.GetRootInventoryOwner();
        return owner==character || owner is Item root && root.Submarine==Home && root.CurrentHull!=null && owner is not Character;
    }
    private IEnumerable<Item> Supplies(Func<Item,bool> predicate) => Item.ItemList.Where(i=>InternalItem(i) && predicate(i))
        .OrderBy(i=>character.HasItem(i)?0:1).ThenBy(i=>Vector2.DistanceSquared(character.WorldPosition,i.WorldPosition)).ThenBy(i=>i.ID);
    private Item? EquippedSuit() => character.Inventory.FindAllItems(i=>i.HasTag(Tags.HeavyDivingGear) && i.Condition>0 && character.HasEquippedItem(i) && AIObjectiveFindDivingGear.IsSuitablePressureProtection(i,Tags.HeavyDivingGear,character),true).FirstOrDefault();
    private static bool IsCutter(Item i) => i.HasTag("cuttingequipment".ToIdentifier()) && i.GetComponent<RepairTool>() is {DeattachSpeed:>0,Range:>0,HitItems:true} t && t.UsableIn is RepairTool.UseEnvironment.Both or RepairTool.UseEnvironment.Water;
    private IEnumerable<RelatedItem> FuelRequirements() => tool?.RequiredItems.TryGetValue(RelatedItem.RelationType.Contained,out var list)==true ? list.Where(r=>!r.IsOptional && !r.RequireEmpty && !r.MatchOnEmpty) : Enumerable.Empty<RelatedItem>();
    private float OxygenPercent()
    {
        suit=EquippedSuit();
        var oxygenSlot=suit?.GetComponent<ItemContainer>()?.FindSuitableSubContainerIndex(Tags.OxygenSource);
        oxygenTank=suit?.OwnInventory?.FindAllItems(i=>i.HasTag(Tags.OxygenSource) && i.Condition>0 &&
            (!oxygenSlot.HasValue || i.ParentInventory.IsInSlot(i,oxygenSlot.Value)),false).OrderByDescending(i=>i.ConditionPercentage).FirstOrDefault();
        return oxygenTank?.ConditionPercentage ?? 0;
    }
    private int FreeSlots()
    {
        int count=0;
        for(int i=0;i<character.Inventory.Capacity;i++)
            if(character.Inventory.SlotTypes[i]==InvSlotType.Any && character.Inventory.GetItemAt(i)==null) count++;
        return count;
    }
    private string? EquipmentReason(bool departure)
    {
        suit=EquippedSuit();
        if(suit==null) return "Derinliğe uygun, giyilmiş dalış kıyafeti eksik.";
        if(suit.HasTag(Tags.PoweredDivingSuit) && suit.GetComponent<Powered>() is not {HasPower:true}) return "Motorlu dalış kıyafetinin enerjisi/yakıtı yetersiz.";
        var oxygen=OxygenPercent();
        if(oxygen<(departure?settings.MinimumDepartureOxygenPercent:settings.OxygenReservePercent)) return "Dalış kıyafetinin oksijeni yetersiz.";
        cutter=character.Inventory.FindAllItems(i=>IsCutter(i) && i.Condition>0,true).FirstOrDefault();tool=cutter?.GetComponent<RepairTool>();
        if(cutter==null || tool==null) return "Çalışır, su altında kullanılabilir kesici eksik.";
        if(!tool.HasRequiredContainedItems(character,false)) return "Kesicinin gerekli sarf malzemesi eksik.";
        foreach(var requirement in FuelRequirements())
            if(cutter.OwnInventory?.FindAllItems(i=>requirement.MatchesItem(i) && i.ConditionPercentage >= (departure?60:15),true).Any()!=true)
                return "Kesici sarf malzemesi güvenlik sınırının altında.";
        if(FreeSlots()<(departure?settings.MinimumFreeSlots:1)) return "Maden için boş taşıma yuvası kalmadı.";
        if(character.IsUnconscious || character.Oxygen<50) return "Sağlık/solunum durumu uygun değil.";
        return null;
    }
    private void PrepareEquipment()
    {
        suit=EquippedSuit();
        if(suit==null)
        {
            var item=Supplies(i=>i.HasTag(Tags.HeavyDivingGear) && AIObjectiveFindDivingGear.IsSuitablePressureProtection(i,Tags.HeavyDivingGear,character)).FirstOrDefault();
            if(item==null) { Stop("Gemide erişilebilir ve derinliğe uygun dalış kıyafeti yok.",true);return; }
            Run(new AIObjectiveGetItem(character,item,objectiveManager,true)
            { Wear=true,EquipSlotType=InvSlotType.OuterClothes|InvSlotType.InnerClothes|InvSlotType.Head,AllowToFindDivingGear=false },()=>{},()=>Stop("Dalış kıyafetine gemi içinden ulaşamadım/giyemedim.",true));return;
        }
        if(OxygenPercent()<settings.MinimumDepartureOxygenPercent)
        {
            LoadSupply(suit,i=>i.HasTag(Tags.OxygenSource),settings.MinimumDepartureOxygenPercent,"Dalış oksijeni",suit.GetComponent<ItemContainer>()?.FindSuitableSubContainerIndex(Tags.OxygenSource));return;
        }
        if(suit.HasTag(Tags.PoweredDivingSuit) && suit.GetComponent<Powered>() is not {HasPower:true})
        { LoadSupply(suit,i=>i.HasTag(Tags.DivingSuitFuel),60,"Motorlu dalış kıyafeti yakıtı",suit.GetComponent<ItemContainer>()?.FindSuitableSubContainerIndex(Tags.DivingSuitFuel));return; }
        cutter=character.Inventory.FindAllItems(i=>IsCutter(i) && i.Condition>0,true).FirstOrDefault();
        if(cutter==null)
        {
            var item=Supplies(IsCutter).FirstOrDefault();
            if(item==null) { Stop("Gemide erişilebilir, maden kesebilen kesici yok.",true);return; }
            Run(new AIObjectiveGetItem(character,item,objectiveManager,false){AllowToFindDivingGear=false},()=>{},()=>Stop("Kesiciye gemi içinden ulaşamadım.",true));return;
        }
        tool=cutter.GetComponent<RepairTool>();
        foreach(var requirement in FuelRequirements())
        {
            if(cutter.OwnInventory?.FindAllItems(i=>requirement.MatchesItem(i) && i.ConditionPercentage>=60,true).Any()!=true)
            { LoadSupply(cutter,requirement.MatchesItem,60,"Kesici sarfı",requirement.TargetSlot>=0?requirement.TargetSlot:null);return; }
        }
        var why=EquipmentReason(true);
        if(why!=null) { Stop("Hazırlanamadım: "+why,true);return; }
        station=ChooseStation();
        if(station==null)
        { Stop("Ulaşılabilir, kapıları açık tutacak boş airlock bekleme noktası yok. İç Path waypoint'lerine crewminer_wait etiketi ekleyin.",true);return; }
        SetPhase(Phase.Staging,"Ekipman tamam; airlock bekleme noktasına gidiyorum.");
    }
    private void LoadSupply(Item equipment,Func<Item,bool> matches,float min,string label,int? slot=null)
    {
        var container=equipment.GetComponent<ItemContainer>();
        if(container==null) { Stop(label+": uyumlu sarf yuvası yok.",true);return; }
        var fuel=Supplies(i=>matches(i) && i.ConditionPercentage>=min && i.Container!=equipment &&
            (container.Inventory.CanBePut(i) || equipment.OwnInventory.AllItems.Any(old=>matches(old) && old.ConditionPercentage<min))).FirstOrDefault();
        if(fuel==null) { Stop(label+": gemide yeterli dolulukta erişilebilir sarf bulunamadı.",true);return; }
        if(!character.HasItem(fuel))
        {
            Run(new AIObjectiveGetItem(character,fuel,objectiveManager,false){AllowToFindDivingGear=false},()=>{},()=>Stop(label+": gemi içinden sarfa ulaşamadım.",true));return;
        }
        Run(new AIObjectiveContainItem(character,fuel,container,objectiveManager)
        { AllowToFindDivingGear=false,RemoveEmpty=false,RemoveExistingWhenNecessary=true,
          RemoveExistingPredicate=i=>matches(i) && i.ConditionPercentage<min,TargetSlot=slot },()=>{},()=>Stop(label+": sarfı normal envanter işlemiyle yükleyemedim.",true));
    }
    private bool AwayFromDoors(WayPoint wp) => !Item.ItemList.Any(i=>i.Submarine==Home && i.GetComponent<Door>()!=null && Vector2.DistanceSquared(i.WorldPosition,wp.WorldPosition)<110*110);
    private bool StationFree(WayPoint wp) => !Plugin.Jobs.Values.Any(j=>j!=this && !j.Finished && j.station!=null && Vector2.DistanceSquared(j.station.WorldPosition,wp.WorldPosition)<140*140);
    private WayPoint? ChooseStation()
    {
        var candidates=WayPoint.WayPointList.Where(w=>w.Submarine==Home && w.SpawnType==SpawnType.Path && SafeHull(w.CurrentHull) && !ignoredStations.Contains(w) && w.Ladders==null && AwayFromDoors(w) && StationFree(w)).ToArray();
        var hasExplicit=WayPoint.WayPointList.Any(w=>w.Submarine==Home && w.Tags.Contains(settings.WaitingWaypointTag.ToIdentifier()));
        var tagged=candidates.Where(w=>w.Tags.Contains(settings.WaitingWaypointTag.ToIdentifier())).ToArray();
        // Explicit tags override detection; otherwise require real outside-door topology, not Hull.IsAirlock (name/tag heuristic).
        bool NearAirlock(WayPoint w) => w.CurrentHull.LeadsOutside(character) ||
            w.CurrentHull.ConnectedGaps.Any(g=>g.IsRoomToRoom && g.ConnectedDoor!=null && g.ConnectedDoor.HasAccess(character) &&
                g.linkedTo.OfType<Hull>().Any(h=>h!=w.CurrentHull && h.Submarine==Home && h.LeadsOutside(character)));
        var selected=hasExplicit ? tagged : candidates.Where(NearAirlock).ToArray();
        foreach(var w in selected.OrderBy(w=>w.CurrentHull.ID).ThenBy(w=>w.ID))
            if(float.IsFinite(PathMetres(w,true))) return w;
        return null;
    }
    private void StageInside()
    {
        if(withdrawing)
        {
            var safe=WayPoint.WayPointList.Where(w=>w.Submarine==Home && w.SpawnType==SpawnType.Path && SafeHull(w.CurrentHull) && !w.CurrentHull.LeadsOutside(character) && AwayFromDoors(w))
                .OrderBy(w=>Vector2.DistanceSquared(w.WorldPosition,character.WorldPosition)).FirstOrDefault(w=>float.IsFinite(PathMetres(w,true)));
            if(safe==null) { Stop("Güvenli iç bekleme alanı yok; acil hayatta kalma davranışına geçiyorum.",true);return; }
            Move(safe,false,70,()=>{withdrawing=false;SetPhase(Phase.Preparing,"Güvenli iç bölgedeyim; yeniden hazırlanıyorum. Ayrı Çık emri gerekli.");},()=>Stop("Güvenli iç bölgeye ulaşamadım; acil davranışlar öncelikli.",true));return;
        }
        station ??= ChooseStation();
        if(station==null) { Stop("Uygun/boş airlock bekleme noktası bulunamadı.",true);return; }
        if(!SafeHull(station.CurrentHull)) { withdrawing=true;return; }
        if(AtStation()) { SetPhase(Phase.Ready,"Hazırım, airlock'ta bekliyorum. Çık emrini bekliyorum.");return; }
        Move(station,false,65,()=>
        {
            if(AtStation()) SetPhase(Phase.Ready,"Hazırım, airlock'ta bekliyorum. Çık emrini bekliyorum.");
        },()=>{ if(station!=null) ignoredStations.Add(station);station=null;Say("Bu airlock noktasına erişemedim; diğer uygun noktayı deniyorum."); });
    }
    private bool ValidMineral(Item i) => !i.Removed && i.Condition>0 && i.Submarine==null && i.ParentInventory==null && i.HasTag("ore".ToIdentifier()) && i.GetComponent<LevelResource>()!=null && area.Contains(i.WorldPosition.X,i.WorldPosition.Y);
    private bool PressureSafeAt(Item i)
    {
        var protection=suit?.GetComponent<Wearable>()?.PressureProtection ?? 0;
        return protection>=(Level.Loaded?.GetRealWorldDepth(i.WorldPosition.Y) ?? 0)+Steering.PressureWarningThreshold;
    }
    private float PathMetres(ISpatialEntity destination,bool indoors)
    {
        try
        {
            var path=HumanAIController.PathSteering.PathFinder.FindPath(character.SimPosition,character.GetRelativeSimPosition(destination),character.Submarine,
                errorMsgStr:"CrewMiner",nodeFilter:indoors ? n=>n.Waypoint.CurrentHull!=null && n.Waypoint.Submarine==Home : null);
            if(path==null || path.Unreachable || path.Nodes.Count==0) return float.PositiveInfinity;
            // SteeringPath.TotalLength is display/world units, including offsets for moving submarines.
            var length=path.TotalLength+Vector2.Distance(character.WorldPosition,path.Nodes[0].WorldPosition)+Vector2.Distance(path.Nodes[^1].WorldPosition,destination.WorldPosition);
            return length*Physics.DisplayToRealWorldRatio;
        }
        catch(InvalidOperationException) { return float.PositiveInfinity; }
    }
    private float RouteHomeMetres()
    {
        if(!outside) return 0;
        var hull=ReturnHull();
        return hull==null ? float.PositiveInfinity : PathMetres(hull,false);
    }
    private float RequiredForReturn(float metres) => Policy.RequiredOxygen(metres*settings.RouteSafetyFactor,settings.MinimumSwimSpeedMetresPerSecond,
        Policy.EffectiveDrain(settings.OxygenDrainPercentPerSecond,observedDrain),settings.OxygenReservePercent,settings.ReturnMarginSeconds);
    internal bool OwnsPickup(Item item) => !suspended && Stage==Phase.Collecting && target==item && ValidMineral(item);
    private void CaptureCargo()
    {
        if(!hasDeparted) return;
        foreach(var i in character.Inventory.FindAllItems(i=>i.HasTag("ore".ToIdentifier()) && i.GetComponent<LevelResource>()!=null,true))
            if(!initialOre.Contains(i)) cargo.Add(i);
    }
    private void SampleOxygen()
    {
        CaptureCargo();var now=OxygenPercent();
        if(oxygenTank!=null && previousOxygen>=now && previousOxygen>=0 && clock>oxygenSampleAt)
            observedDrain=Math.Max(observedDrain,(previousOxygen-now)/(clock-oxygenSampleAt));
        previousOxygen=now;oxygenSampleAt=clock;
    }
    private void Search()
    {
        if(clock<scanAt) return;
        scanAt=clock+settings.ScanIntervalSeconds;
        Plugin.Claims.Prune(i=>!i.Removed && i.ParentInventory==null,c=>Plugin.Jobs.TryGetValue(c,out var job) && c.IsBot && !c.IsDead && !job.suspended && !job.Finished && Policy.Working(job.Stage));
        bool otherReserved=false;
        foreach(var item in Item.ItemList.Where(i=>ValidMineral(i) && !skipped.Contains(i)).OrderBy(i=>Vector2.DistanceSquared(i.WorldPosition,character.WorldPosition)).ThenBy(i=>i.ID))
        {
            if(!Plugin.Claims.IsAvailable(item,character)) { otherReserved=true;continue; }
            if(!PressureSafeAt(item) || !character.Inventory.CanBePut(item)) { skipped.Add(item);continue; }
            var path=PathMetres(item,false);
            if(!float.IsFinite(path)) { skipped.Add(item);Plugin.Log($"{character.Name}: erişilemeyen maden #{item.ID} sefer boyunca atlandı.");continue; }
            var homeDistance=Vector2.Distance(item.WorldPosition,Home.WorldPosition)*Physics.DisplayToRealWorldRatio;
            var minimum=RequiredForReturn(path+homeDistance);
            if(OxygenPercent()<minimum) { skipped.Add(item);continue; }
            if(!Plugin.Claims.TryReserve(item,character)) continue;
            target=item;targetAttempts=0;SetPhase(Phase.Approaching,$"Maden hedefi: {item.Name} #{item.ID}");return;
        }
        if(otherReserved && phaseElapsed<settings.TargetTimeoutSeconds) return;
        BeginReturn("Çalışma alanında erişilebilir ve kaynaklara uygun boş maden hedefi kalmadı; dönüyorum.");
    }
    private void Approach()
    {
        if(target==null || !ValidMineral(target)) { SkipTarget("Hedef kayboldu/alındı.");return; }
        var reach=target.GetComponent<LevelResource>().Attached ? Math.Clamp((tool?.Range ?? 100)*0.55f,55,120):65;
        Move(target,true,reach,()=>
        {
            if(target==null) return;
            SetPhase(target.GetComponent<LevelResource>().Attached ? Phase.Cutting:Phase.Collecting,"Hedefe ulaştım.");
            lastDetach=target.GetComponent<LevelResource>().DeattachTimer;lastProgress=clock;
        },()=>
        {
            if(++targetAttempts<2) Say("Hedef yolu başarısız; bir kez daha deniyorum.");
            else SkipTarget("Hedefe ulaşamadım; bu sefer için atlıyorum.");
        });
    }
    private void Cut(float dt)
    {
        if(target==null || !ValidMineral(target)) { SkipTarget("Kesilecek hedef artık geçerli değil.");return; }
        var resource=target.GetComponent<LevelResource>();
        if(!resource.Attached) { ReleaseTrigger();SetPhase(Phase.Collecting,"Maden ayrıldı; normal etkileşimle topluyorum.");return; }
        if(cutter==null || cutter.Removed || cutter.body==null || tool==null) { BeginReturn("Kesici kayboldu.");return; }
        if(!character.HasEquippedItem(cutter,InvSlotType.LeftHand|InvSlotType.RightHand))
        {
            // Store other held items. Never drop diving gear/tool to gain cargo space.
            if(!HumanAIController.TakeItem(cutter,character.Inventory,true,dropOtherIfCannotMove:false,storeUnequipped:true)) { BeginReturn("Kesiciyi güvenli biçimde elime alamadım.");return; }
        }
        var delta=target.WorldPosition-character.AnimController.AimSourceWorldPos;
        if(delta.Length()>tool.Range*0.85f) { SetPhase(Phase.Approaching,"");return; }
        HumanAIController.SteeringManager.Reset();
        character.CursorPosition=target.WorldPosition-(character.Submarine?.Position ?? Vector2.Zero);
        ownsToolInput=true;character.SetInput(InputType.Aim,false,true);
        var direction=VectorExtensions.Forward(cutter.body.TransformedRotation);
        var aligned=VectorExtensions.Angle(direction,target.WorldPosition-cutter.WorldPosition)<MathHelper.PiOver4/2;
        var friendly=Character.CharacterList.Any(c=>c!=character && !c.Removed && !c.IsDead && c.TeamID==character.TeamID &&
            Vector2.DistanceSquared(c.WorldPosition,target.WorldPosition)<150*150);
        // Shoot uses Character's normal Item.Use path: real raycasts, skills, fuel and status effects.
        var fire=aligned && !friendly && resource.HasRequiredItems(character,false);
        character.SetInput(InputType.Shoot,false,fire && cutter.IsShootable);
        character.SetInput(InputType.Use,false,fire && !cutter.IsShootable);
        if(resource.DeattachTimer>lastDetach+0.02f) { lastDetach=resource.DeattachTimer;lastProgress=clock; }
        if(clock-lastProgress>settings.StuckTimeoutSeconds) SkipTarget("Kesiciyle ilerleme sağlanamadı (açı/engel/erişim); hedef atlandı.");
    }
    private void Collect()
    {
        ReleaseTrigger();
        if(target==null || !ValidMineral(target)) { SkipTarget("Toplanacak maden artık geçerli değil.");return; }
        if(target.GetComponent<LevelResource>().Attached) { SetPhase(Phase.Cutting,"");return; }
        if(!character.Inventory.CanBePut(target)) { BeginReturn("Envanter doldu; ekipmanı bırakmadan dönüyorum.");return; }
        if(!character.CanInteractWith(target,false))
        { Move(target,true,60,()=>{},()=>SkipTarget("Ayrılan parçaya ulaşamadım."));return; }
        target.TryInteract(character,forceSelectKey:true);
        if(character.HasItem(target))
        {
            cargo.Add(target);Plugin.Claims.Release(character);target=null;
            SetPhase(Phase.Searching,"Maden toplandı; sıradaki hedefi arıyorum.");scanAt=clock+settings.ScanIntervalSeconds;
        }
        else if(phaseElapsed>10) SkipTarget("Normal toplama etkileşimi başarısız; parça atlandı.");
    }
    private void SkipTarget(string reason)
    {
        if(target!=null) skipped.Add(target);
        Plugin.Claims.Release(character);target=null;SetPhase(Phase.Searching,reason);scanAt=clock+settings.ScanIntervalSeconds;
    }
    private Hull? ReturnHull() => Hull.HullList.Where(h=>h.Submarine==Home && h.LeadsOutside(character))
        .OrderBy(h=>Vector2.DistanceSquared(h.WorldPosition,character.WorldPosition)).ThenBy(h=>h.ID).FirstOrDefault();
    private void Return()
    {
        if(returningBlocked) return;
        if(!outside) { SetPhase(Phase.Unloading,"Kendi denizaltıma döndüm; topladığım madenleri depoluyorum.");return; }
        var h=ReturnHull();
        if(h==null) { ReturnFailed();return; }
        // Hull target remains attached to Home, so the vanilla objective follows its current position.
        Move(h,true,60,()=>{ if(!outside) SetPhase(Phase.Unloading,"Denizaltıya girdim; depolamaya geçiyorum.");else ReturnFailed(); },ReturnFailed);
    }
    private void ReturnFailed()
    {
        ClearChild();ReleaseTrigger();phaseElapsed=0;
        if(++returnAttempts<3) { Say($"Dönüş yolu başarısız; yeniden deniyorum ({returnAttempts}/3).");return; }
        returningBlocked=true;
        Say("Denizaltıya dönüş yolu üç denemede bulunamadı. Airlock/yol noktalarını kontrol edin; yolu açtıktan sonra yeniden Geri dön emri verin. Hayatta kalma davranışları öncelikli.");
    }
    public void RetryReturn()
    {
        if(Stage==Phase.Returning && (returningBlocked || suspended)) { suspended=false;returningBlocked=false;returnAttempts=0;phaseElapsed=0;Say("Dönüş yolunu yeniden deniyorum."); }
        else Cancel();
    }
    private void Unload()
    {
        if(outside) { BeginReturn("Depolama sırasında gemiden ayrıldım; geri dönüyorum.");return; }
        CaptureCargo();cargo.RemoveWhere(i=>i.Removed || !character.HasItem(i));
        if(cargo.Count==0) { Stop("Sefer tamamlandı. Yeni madencilik için Hazırlan ve ayrı Çık emri gerekli.",true);return; }
        var item=cargo.OrderBy(i=>i.ID).First();
        var containers=Item.ItemList.Where(i=>!i.Removed && !ignoredStorage.Contains(i) && i.Submarine==Home && i.CurrentHull!=null && i.GetRootInventoryOwner() is not Character && i.HasAccess(character) && !(i.RootContainer?.OwnInventory?.Locked ?? false))
            .Select(i=>i.GetComponent<ItemContainer>()).Where(c=>c!=null && !c.Inventory.Locked && c.Inventory.CanBePut(item) &&
                (c.Item.HasTag(settings.StorageTag.ToIdentifier()) || c.ShouldBeContained(item,out _)))
            .OrderBy(c=>c.Item.HasTag(settings.StorageTag.ToIdentifier())?0:1).ThenBy(c=>Vector2.DistanceSquared(c.Item.WorldPosition,character.WorldPosition));
        var container=containers.FirstOrDefault(c=>float.IsFinite(PathMetres(c.Item,true)));
        if(container==null) { Stop("Uygun, erişilebilir boş depo yok; madenleri envanterimde tutuyorum.",true);return; }
        void Store()
        {
            // Normal inventory operation only while physically at an accessible container.
            if(character.CanInteractWith(container.Item,false) && container.Item.HasAccess(character) && !container.Inventory.Locked &&
                !(container.Item.RootContainer?.OwnInventory?.Locked ?? false) && container.Inventory.CanBePut(item) && container.Inventory.TryPutItem(item,character))
                cargo.Remove(item);
            else { ignoredStorage.Add(container.Item);Say("Depoya aktaramadım; maden üzerimde, diğer uygun depoyu deniyorum."); }
        }
        if(character.CanInteractWith(container.Item,false)) Store();
        else Move(container.Item,false,70,Store,()=>{ignoredStorage.Add(container.Item);Say("Depoya iç yoldan ulaşamadım; diğer depoyu deniyorum.");});
    }
}
