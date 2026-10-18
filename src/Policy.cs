namespace CrewMiner;

// Pure decisions shared verbatim with tests; no game substitutes or fake game DLLs.
internal enum Phase { Preparing, Staging, Ready, Searching, Approaching, Cutting, Collecting, Returning, Unloading, Stopped }
internal readonly record struct Area(float X, float Y, float Radius)
{
    public bool Contains(float x, float y) => (x-X)*(x-X)+(y-Y)*(y-Y) <= Radius*Radius;
}
internal static class Policy
{
    public static bool MayDepart(Phase phase, bool atStation, bool equipped) => phase == Phase.Ready && atStation && equipped;
    public static bool Working(Phase p) => p is Phase.Searching or Phase.Approaching or Phase.Cutting or Phase.Collecting;
    public static float RequiredOxygen(float pathMetres, float speed, float drain, float reserve, float marginSeconds)
        => Math.Min(101, reserve + (pathMetres / speed + marginSeconds) * drain);
    public static float EffectiveDrain(float configured, float observed) => Math.Max(configured, observed);
}
internal sealed class Reservations<TTarget, TOwner> where TTarget : notnull where TOwner : notnull
{
    private readonly Dictionary<TTarget, TOwner> owners = new();
    public int Count => owners.Count;
    public bool IsAvailable(TTarget target, TOwner owner) => !owners.TryGetValue(target, out var current) || EqualityComparer<TOwner>.Default.Equals(current, owner);
    public bool TryReserve(TTarget target, TOwner owner)
    {
        if (!IsAvailable(target, owner)) return false;
        owners[target] = owner; return true;
    }
    public void Release(TOwner owner)
    {
        foreach (var key in owners.Where(p => EqualityComparer<TOwner>.Default.Equals(p.Value, owner)).Select(p => p.Key).ToArray()) owners.Remove(key);
    }
    public void Prune(Func<TTarget,bool> validTarget, Func<TOwner,bool> validOwner)
    {
        foreach (var pair in owners.ToArray()) if (!validTarget(pair.Key) || !validOwner(pair.Value)) owners.Remove(pair.Key);
    }
    public void Clear() => owners.Clear();
}
