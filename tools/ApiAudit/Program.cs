using Mono.Cecil;
if(args.Length!=1) throw new ArgumentException("Usage: ApiAudit <Refs directory>");
int checks=0;
foreach(var platform in new[]{"Windows","Linux","OSX"})
{
    using var asm=AssemblyDefinition.ReadAssembly(Path.Combine(args[0],platform,"Barotrauma.dll"));
    if(asm.Name.Version!=new Version(1,13,4,0)) throw new Exception("Wrong game version: "+asm.Name);
    Console.WriteLine($"{platform}: {asm.Name.Name} {asm.Name.Version}");checks++;
    void Method(string type,string method,params string[] parameters)
    {
        var t=asm.MainModule.GetType(type) ?? throw new Exception("Missing type "+type);
        var found=t.Methods.SingleOrDefault(m=>m.Name==method && m.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(parameters));
        if(found==null) throw new Exception($"Missing signature {type}.{method}({string.Join(',',parameters)})");
        Console.WriteLine($"  {found.FullName}");checks++;
    }
    Method("Barotrauma.AIObjectiveManager","CreateObjective","Barotrauma.Order","System.Single");
    Method("Barotrauma.AIObjectiveManager","UpdateObjectives","System.Single");
    Method("Barotrauma.GameSession","Update","System.Single");
    Method("Barotrauma.GameSession","EndRound","System.String","Barotrauma.CampaignMode/TransitionType","System.Nullable`1<Barotrauma.TraitorManager/TraitorResults>","System.Boolean");
    Method("Barotrauma.Character","set_Controlled","Barotrauma.Character");
    Method("Barotrauma.AIObjectiveGoTo",".ctor","Barotrauma.ISpatialEntity","Barotrauma.Character","Barotrauma.AIObjectiveManager","System.Boolean","System.Boolean","System.Single","System.Single");
    Method("Barotrauma.AIObjectiveGetItem",".ctor","Barotrauma.Character","Barotrauma.Item","Barotrauma.AIObjectiveManager","System.Boolean","System.Single");
    Method("Barotrauma.AIObjectiveContainItem",".ctor","Barotrauma.Character","Barotrauma.Item","Barotrauma.Items.Components.ItemContainer","Barotrauma.AIObjectiveManager","System.Single");
    Method("Barotrauma.Items.Components.LevelResource","get_Attached");
    Method("Barotrauma.Items.Components.Pickable","OnPicked","Barotrauma.Character","System.Boolean","System.Boolean");
    Method("Barotrauma.Items.Components.RepairTool","Use","System.Single","Barotrauma.Character");
    Method("Barotrauma.Item","TryInteract","Barotrauma.Character","System.Boolean","System.Boolean","System.Boolean");
    Method("Barotrauma.Character","SetInput","Barotrauma.InputType","System.Boolean","System.Boolean");
    Method("Barotrauma.LuaCs.IPluginManagementService","TryGetPackageForPlugin","Barotrauma.ContentPackage&");
    var create=asm.MainModule.GetType("Barotrauma.AIObjectiveManager").Methods.Single(m=>m.Name=="CreateObjective");
    if(string.Join(',',create.Parameters.Select(p=>p.Name))!="order,priorityModifier") throw new Exception("Harmony argument name mismatch");
    checks++;
    if(asm.MainModule.GetType("Barotrauma.Items.Components.LevelResource").Methods.Single(m=>m.Name=="set_DeattachTimer").IsPublic!=true) throw new Exception("Expected genuine publicized references");
    checks++;
}
Console.WriteLine($"{checks} metadata checks passed; DLLs were inspected without running the game.");
