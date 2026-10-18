using CrewMiner;

int passed=0;
void Check(string name,bool condition)
{
    if(!condition) throw new Exception("FAIL: "+name);
    Console.WriteLine("PASS: "+name);passed++;
}
// Every possible phase must reject departure unless both fresh checks pass at Ready.
foreach(var phase in Enum.GetValues<Phase>())
foreach(var atStation in new[]{false,true})
foreach(var equipped in new[]{false,true})
    Check($"departure/{phase}/station={atStation}/equipment={equipped}",
        Policy.MayDepart(phase,atStation,equipped)==(phase==Phase.Ready && atStation && equipped));
var fixedArea=new Area(1000,-3000,4000);
Check("range includes exact boundary",fixedArea.Contains(5000,-3000));
Check("range rejects just beyond boundary",!fixedArea.Contains(5000.1f,-3000));
Check("range rejects diagonal outside",!fixedArea.Contains(4000,0));
Check("negative world coordinates",fixedArea.Contains(-3000,-3000));
// A moving bot or ship never changes the captured area; no mutable reference is retained.
var movedShip=new Area(20000,0,4000);
Check("moving submarine cannot extend expedition area",!fixedArea.Contains(movedShip.X,movedShip.Y));
Check("new expedition can capture new center",movedShip.Contains(movedShip.X,movedShip.Y));
var settingsFile=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".xml");
try
{
    File.WriteAllText(settingsFile,"<CrewMiner SearchRadiusMetres='50' />");
    var first=Settings.Load(settingsFile,_=>{});
    File.WriteAllText(settingsFile,"<CrewMiner SearchRadiusMetres='100' />");
    var second=Settings.Load(settingsFile,_=>{});
    Check("settings reload applies to next snapshot",first.SearchRadiusMetres==50 && second.SearchRadiusMetres==100);
    var warnings=new List<string>();
    File.WriteAllText(settingsFile,"<CrewMiner SearchRadiusMetres='NaN' ScanIntervalSeconds='0' MinimumSwimSpeedMetresPerSecond='Infinity' MinimumFreeSlots='-1' />");
    var invalid=Settings.Load(settingsFile,warnings.Add);
    Check("invalid floats fail to safe defaults",invalid.SearchRadiusMetres==40 && invalid.ScanIntervalSeconds==3 && invalid.MinimumSwimSpeedMetresPerSecond==1.2f && invalid.MinimumFreeSlots==2 && warnings.Count==4);
    foreach(var edge in new[]{"5","150"})
    {
        File.WriteAllText(settingsFile,$"<CrewMiner SearchRadiusMetres='{edge}' />");
        Check("valid radius boundary "+edge,Settings.Load(settingsFile,_=>{}).SearchRadiusMetres==float.Parse(edge));
    }
    foreach(var outside in new[]{"4.9","150.1","-1","garbage"})
    {
        File.WriteAllText(settingsFile,$"<CrewMiner SearchRadiusMetres='{outside}' />");
        Check("invalid radius "+outside,Settings.Load(settingsFile,_=>{}).SearchRadiusMetres==40);
    }
    File.WriteAllText(settingsFile,"<CrewMiner MinimumFreeSlots='1.5' />");
    Check("fractional inventory slot count is rejected",Settings.Load(settingsFile,_=>{}).MinimumFreeSlots==2);
    File.WriteAllText(settingsFile,"<broken");
    Check("broken XML has usable defaults",Settings.Load(settingsFile,_=>{}).SearchRadiusMetres==40);
}
finally { File.Delete(settingsFile); }
var claims=new Reservations<int,string>();
Check("bot A reserves mineral",claims.TryReserve(7,"A"));
Check("same bot may retain reservation",claims.TryReserve(7,"A"));
Check("bot B cannot reserve same mineral",!claims.TryReserve(7,"B"));
Check("bot B reserves different mineral",claims.TryReserve(8,"B"));
claims.Release("A");
Check("cancel/completion releases owner only",claims.TryReserve(7,"B") && claims.Count==2);
claims.Prune(target=>target!=7,owner=>true);
Check("removed target releases claim",claims.Count==1);
claims.Prune(_=>true,owner=>owner!="B");
Check("dead/suspended owner releases claims",claims.Count==0);
claims.TryReserve(3,"C");claims.Clear();
Check("round cleanup releases all claims",claims.Count==0);
var near=Policy.RequiredOxygen(20,1.2f,0.5f,15,45);
var far=Policy.RequiredOxygen(80,1.2f,0.5f,15,45);
Check("longer return requires more oxygen",far>near);
Check("slower swimming increases required oxygen",Policy.RequiredOxygen(80,0.8f,0.5f,15,45)>far);
Check("observed fast drain never lowers floor",Policy.EffectiveDrain(0.5f,0.8f)==0.8f && Policy.EffectiveDrain(0.5f,0.1f)==0.5f);
Check("exhausted budget rejected before empty tank",near>15);
Check("impossible oxygen budget cannot pass with full tank",Policy.RequiredOxygen(10000,1.2f,0.5f,15,45)>100);
Check("only mining phases count as work",!Policy.Working(Phase.Ready) && !Policy.Working(Phase.Preparing) && !Policy.Working(Phase.Returning) && Policy.Working(Phase.Cutting));
Console.WriteLine($"{passed} checks passed. No game runtime was started.");
