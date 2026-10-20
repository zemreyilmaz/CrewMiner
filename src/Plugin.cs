using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.LuaCs;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace CrewMiner;
public sealed class Plugin : IAssemblyPlugin
{
    public IPluginManagementService PluginService { get; set; } = null!;
    internal static Plugin? Instance;
    internal static readonly Dictionary<Character, MiningObjective> Jobs = new();
    internal static readonly Reservations<Item, Character> Claims = new();
    private Harmony? harmony;
    private DebugConsole.Command? statusCommand;
    private bool patchesReady;
    private string startupFailure = "";
    private string root = "";
    private bool compatible;
    private static float sweepTimer;
    internal static string ConfigPath => System.IO.Path.Combine(Instance?.root ?? "", "Config", "CrewMiner.xml");
    internal static Settings Config { get; private set; } = new();
    internal static bool Authorized => Instance is { compatible:true, patchesReady:true } && GameMain.NetworkMember == null;
    public void PreInitPatching() { }
    public void Initialize()
    {
        Instance=this;
        if (PluginService.TryGetPackageForPlugin<Plugin>(out var package)) root=System.IO.Path.GetFullPath(package.Dir);
        compatible = GameMain.Version == new Version(1,13,4,0);
        statusCommand = new DebugConsole.Command("crewminer_status",
            "CrewMiner yükleme durumunu ve botların mevcut görev aşamasını gösterir; hiçbir botu hareket ettirmez.",
            _ => PrintStatus(), isCheat:false);
        DebugConsole.Commands.Add(statusCommand);
        Log($"CrewMiner 0.1.1; oyun={GameMain.Version}; tek oyunculu; uyumlu={compatible}");
        Config=Settings.Load(System.IO.Path.Combine(root,"Config","CrewMiner.xml"),Log);
        harmony=new Harmony("crewminer.singleplayer.v1");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(AIObjectiveManager),nameof(AIObjectiveManager.CreateObjective)), prefix: new HarmonyMethod(typeof(Plugin),nameof(CreateOrder)));
            harmony.Patch(AccessTools.Method(typeof(AIObjectiveManager),nameof(AIObjectiveManager.UpdateObjectives)), postfix: new HarmonyMethod(typeof(Plugin),nameof(AfterObjectives)));
            harmony.Patch(AccessTools.Method(typeof(GameSession),nameof(GameSession.Update)), postfix: new HarmonyMethod(typeof(Plugin),nameof(Sweep)));
            harmony.Patch(AccessTools.Method(typeof(Pickable),nameof(Pickable.OnPicked),new[]{typeof(Character),typeof(bool),typeof(bool)}), prefix:new HarmonyMethod(typeof(Plugin),nameof(PickOne)));
            harmony.Patch(AccessTools.PropertySetter(typeof(Character),nameof(Character.Controlled)), prefix: new HarmonyMethod(typeof(Plugin),nameof(ControlTaken)));
            harmony.Patch(AccessTools.Method(typeof(GameSession),nameof(GameSession.EndRound)), prefix: new HarmonyMethod(typeof(Plugin),nameof(EndRound)));
            patchesReady=true;
        }
        catch(Exception e)
        {
            startupFailure=e.ToString();
            harmony.UnpatchSelf();
            Log("Yükleme tamamlanamadı; bot otomasyonu kapalı. " + startupFailure);
        }
        Log("Tanılama: F3 konsoluna crewminer_status yazın.");
    }
    private void PrintStatus()
    {
        Log($"STATUS mod=0.1.1; oyun={GameMain.Version}; hedef=1.13.4.0; uyumlu={compatible}; yamalar={patchesReady}; tekOyunculu={GameMain.NetworkMember==null}; etkin={Authorized}; karakter={Character.Controlled?.Name ?? "yok"}");
        if(!compatible) Log("ENGEL: Oyun sürümü hedefle eşleşmiyor. Sürüm kontrolü kaldırılmadan, gerçek sürüme uygun DLL/API ile yeniden derleme gerekir.");
        if(!patchesReady) Log("ENGEL: Davranış yamaları yüklenemedi. " + startupFailure);
        if(GameMain.NetworkMember!=null) Log("ENGEL: Bu paket çok oyunculu oturumlarda çalışmaz.");
        var team=Character.Controlled?.TeamID;
        int count=0;
        foreach(var c in Character.CharacterList.Where(c=>!c.Removed && c.AIController is HumanAIController && (!team.HasValue || c.TeamID==team.Value)))
        {
            count++;
            var ai=(HumanAIController)c.AIController;
            var currentOrders=c.CurrentOrders?.AsEnumerable() ?? Enumerable.Empty<Order>();
            var orders=string.Join(",",currentOrders.Select(o=>$"{o.Identifier}.{o.Option}"));
            Log($"BOT {c.Name}#{c.ID}: bot={c.IsBot}; ölü={c.IsDead}; emirler=[{orders}]; aktif={ai.ObjectiveManager.CurrentObjective?.Identifier.ToString() ?? "yok"}");
            if(Jobs.TryGetValue(c,out var job)) Log(job.DescribeStatus());
            else if(currentOrders.Any(o=>o.Identifier=="crewminer".ToIdentifier()))
                Log("ENGEL: CrewMiner emri var ama etkin madencilik görevi yok. Önce Madenciliğe hazırlan verin; ardından crewminer_status çıktısını paylaşın.");
            else Log("DURUM: Bu karakterde etkin CrewMiner görevi/emri yok.");
        }
        if(count==0) Log("DURUM: Bu oturumda görülebilen insan bot yok; oyunda bir tur başlatın.");
        Log($"STATUS SONU; günlük={System.IO.Path.Combine(root,"CrewMiner.log")}");
    }
    public void OnLoadCompleted() { }
    private static bool CreateOrder(AIObjectiveManager __instance, Order order, ref AIObjective? __result)
    {
        if(order?.Identifier != "crewminer".ToIdentifier()) return true;
        __result=null;
        var c=__instance.character;
        Log($"EMİR bot={c.Name}#{c.ID}; seçenek={order.Option}; etkin={Authorized}; isBot={c.IsBot}");
        if(!Authorized)
        {
            Tell(c,Instance?.patchesReady != true ? "Davranış kodu yüklenemedi. F3: crewminer_status." :
                Instance?.compatible != true ? $"Sürüm uyumsuz: oyun {GameMain.Version}, gereken 1.13.4.0. F3: crewminer_status." :
                "Bu paket çok oyunculu oturumda çalışmaz. F3: crewminer_status.");
            return false;
        }
        if(!c.IsBot || c.IsDead) { Tell(c,"Bu emir yaşayan, oyuncunun kontrol etmediği bir bota verilmelidir.");return false; }
        Jobs.TryGetValue(c,out var job);
        if(job?.Finished == true) { job.Stop("Önceki görev kapatıldı.",false); Jobs.Remove(c); job=null; }
        if(order.Option == "prepare".ToIdentifier())
        {
            if(job == null)
            {
                if(c.Submarine == null || c.CurrentHull == null) { Tell(c,"Hazırlık için önce denizaltıya dönmeliyim."); return false; }
                job=new MiningObjective(c,__instance,c.Submarine); Jobs[c]=job;
            }
            job.Prepare();
        }
        else if(order.Option == "depart".ToIdentifier())
        {
            if(job == null) { Tell(c,"Hazır değilim: önce Madenciliğe hazırlan emri gerekli."); return false; }
            job.Depart(); // Rejection never creates a pending departure.
        }
        else if(order.Option == "return".ToIdentifier())
        {
            if(job==null)
            {
                Tell(c,"Etkin madencilik seferim yok." + (c.CurrentHull==null ? " Normal Geri dön davranışını kullanıyorum." : ""));
                if(c.CurrentHull==null) __result=new AIObjectiveReturn(c,order.OrderGiver,__instance);
                return false;
            }
            job.RetryReturn();
        }
        if(job!=null && __instance.ForcedOrder==job) __instance.ClearForcedOrder();
        __result=job;
        return false;
    }
    private static void AfterObjectives(AIObjectiveManager __instance)
    {
        var c=__instance.character;
        if(!Jobs.TryGetValue(c,out var job)) return;
        if(c.IsPlayer) { job.SuspendForPlayer(); return; }
        if(!Authorized || c.Removed || c.IsDead || !c.IsBot)
        {
            job.Stop(c.IsPlayer ? "Oyuncu kontrolü aldı; madencilik durdu. Dışarıdaysanız kontrolü bıraktıktan sonra Geri dön emri verin." : "Görev temizlendi.", c.IsPlayer);
            Jobs.Remove(c); return;
        }
        if(job.Finished)
        {
            Claims.Release(c);Jobs.Remove(c);
            var old=__instance.CurrentOrders.FirstOrDefault(o=>o.Objective==job);
            if(old!=null) c.SetOrder(old.GetDismissal(),true,false,force:true);
            return;
        }
        if(!__instance.CurrentOrders.Any(o=>o.Objective==job))
        {
            // Dismissal/replacement outside is a return request, never a deletion.
            job.Cancel();
            if(!job.Finished && __instance.ForcedOrder==null) __instance.SetForcedOrder(job);
        }
    }
    private static void PickOne(Pickable __instance, Character picker, ref bool pickDroppedStack)
    {
        // Vanilla stack pickup also picks unreserved/out-of-radius neighbours. Limit only our own current target.
        if(Authorized && picker.IsBot && Jobs.TryGetValue(picker,out var job) && job.OwnsPickup(__instance.Item)) pickDroppedStack=false;
    }
    private static void ControlTaken(Character? value)
    {
        if(value!=null && Jobs.TryGetValue(value,out var job)) job.SuspendForPlayer();
    }
    private static void Sweep(float deltaTime)
    {
        sweepTimer-=deltaTime;if(sweepTimer>0) return;sweepTimer=1;
        foreach(var pair in Jobs.ToArray())
        {
            var c=pair.Key;
            if(c.Removed || c.IsDead || !Authorized) { pair.Value.Stop("Görev temizlendi.",false);Jobs.Remove(c); }
            else if(c.IsPlayer) pair.Value.SuspendForPlayer();
        }
    }
    private static void EndRound()
    { Log($"Tur sonu: {Jobs.Count} görev ve {Claims.Count} rezervasyon temizleniyor.");Clear(false); }
    private static void Clear(bool returnOutside)
    {
        foreach(var pair in Jobs.ToArray())
        {
            var c=pair.Key;var job=pair.Value;job.Stop("CrewMiner kapatıldı.",false);
            if(returnOutside && c.IsBot && !c.IsDead && c.AIController is HumanAIController cleanupAi)
            {
                var current=cleanupAi.ObjectiveManager.CurrentOrders.FirstOrDefault(o=>o.Objective==job);
                if(current!=null) c.SetOrder(current.GetDismissal(),true,false,force:true);
            }
            if(returnOutside && Authorized && c.IsBot && !c.IsDead && c.CurrentHull==null && c.AIController is HumanAIController ai)
            {
                var entry=Hull.HullList.Where(h=>h.Submarine==job.Home && h.LeadsOutside(c))
                    .OrderBy(h=>Vector2.DistanceSquared(h.WorldPosition,c.WorldPosition)).FirstOrDefault();
                if(entry!=null)
                {
                    // Hand off to a vanilla objective that survives unloading this assembly.
                    ai.ObjectiveManager.AddObjective(new AIObjectiveGoTo(entry,c,ai.ObjectiveManager)
                    {AllowGoingOutside=true,OverridePriority=AIObjectiveManager.HighestOrderPriority-1});
                }
                else Tell(c,"Mod kapatıldı, kendi gemime uygun giriş bulunamadı; normal Geri dön emri gerekli.");
            }
        }
        Jobs.Clear();Claims.Clear();
    }
    public void Dispose()
    {
        Clear(true);harmony?.UnpatchSelf();harmony=null;patchesReady=false;
        if(statusCommand!=null) DebugConsole.Commands.Remove(statusCommand);
        statusCommand=null;Instance=null;
    }
    internal static void Tell(Character c,string text)
    {
        Log($"{c.Name}#{c.ID}: {text}");
        if(!c.Removed && !c.IsDead) c.Speak("[CrewMiner] "+text, identifier:"crewminer.status".ToIdentifier(), minDurationBetweenSimilar:1);
    }
    internal static void Log(string text)
    {
        DebugConsole.NewMessage("[CrewMiner] "+text,Color.LightSkyBlue);
        var path=Instance?.root;
        if(string.IsNullOrEmpty(path)) return;
        try
        {
            var log=System.IO.Path.Combine(path,"CrewMiner.log");
            if(System.IO.File.Exists(log) && new System.IO.FileInfo(log).Length>5*1024*1024)
                System.IO.File.Move(log,System.IO.Path.Combine(path,"CrewMiner.previous.log"),overwrite:true);
            System.IO.File.AppendAllText(log,$"{DateTime.UtcNow:O} {text}{Environment.NewLine}");
        }
        catch(System.IO.IOException) { } catch(UnauthorizedAccessException) { }
    }
}
