using System.Globalization;
using System.Xml.Linq;
namespace CrewMiner;
internal sealed class Settings
{
    public float SearchRadiusMetres { get; private set; } = 40;
    public float ScanIntervalSeconds { get; private set; } = 3;
    public float PrepareTimeoutSeconds { get; private set; } = 180;
    public float TargetTimeoutSeconds { get; private set; } = 90;
    public float ReturnTimeoutSeconds { get; private set; } = 180;
    public float StuckTimeoutSeconds { get; private set; } = 25;
    public float MinimumDepartureOxygenPercent { get; private set; } = 85;
    public float OxygenReservePercent { get; private set; } = 15;
    public float OxygenDrainPercentPerSecond { get; private set; } = 0.5f;
    public float MinimumSwimSpeedMetresPerSecond { get; private set; } = 1.2f;
    public float RouteSafetyFactor { get; private set; } = 1.5f;
    public float ReturnMarginSeconds { get; private set; } = 45;
    public int MinimumFreeSlots { get; private set; } = 2;
    public string WaitingWaypointTag { get; private set; } = "crewminer_wait";
    public string StorageTag { get; private set; } = "crewminer_storage";
    public static Settings Load(string path, Action<string> log)
    {
        var s = new Settings();
        try
        {
            var x = XDocument.Load(path).Root ?? throw new FormatException("XML root eksik");
            float Read(string key, float fallback, float min, float max)
            {
                var raw=(string?)x.Attribute(key); if(raw is null) return fallback;
                if(!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !float.IsFinite(n) || n<min || n>max)
                { log($"Geçersiz {key}='{raw}'; {min}..{max} gerekli; varsayılan {fallback} kullanıldı."); return fallback; }
                return n;
            }
            s.SearchRadiusMetres=Read(nameof(SearchRadiusMetres),40,5,150);
            s.ScanIntervalSeconds=Read(nameof(ScanIntervalSeconds),3,1,30);
            s.PrepareTimeoutSeconds=Read(nameof(PrepareTimeoutSeconds),180,30,600);
            s.TargetTimeoutSeconds=Read(nameof(TargetTimeoutSeconds),90,20,300);
            s.ReturnTimeoutSeconds=Read(nameof(ReturnTimeoutSeconds),180,30,900);
            s.StuckTimeoutSeconds=Read(nameof(StuckTimeoutSeconds),25,10,120);
            s.MinimumDepartureOxygenPercent=Read(nameof(MinimumDepartureOxygenPercent),85,50,100);
            s.OxygenReservePercent=Read(nameof(OxygenReservePercent),15,10,40);
            s.OxygenDrainPercentPerSecond=Read(nameof(OxygenDrainPercentPerSecond),0.5f,0.1f,5);
            s.MinimumSwimSpeedMetresPerSecond=Read(nameof(MinimumSwimSpeedMetresPerSecond),1.2f,0.2f,5);
            s.RouteSafetyFactor=Read(nameof(RouteSafetyFactor),1.5f,1,5);
            s.ReturnMarginSeconds=Read(nameof(ReturnMarginSeconds),45,20,180);
            var freeSlots=Read(nameof(MinimumFreeSlots),2,1,6);
            if(freeSlots!=MathF.Truncate(freeSlots)) { log("MinimumFreeSlots tam sayı olmalı; varsayılan 2 kullanıldı.");freeSlots=2; }
            s.MinimumFreeSlots=(int)freeSlots;
            s.WaitingWaypointTag=(string?)x.Attribute(nameof(WaitingWaypointTag)) ?? s.WaitingWaypointTag;
            s.StorageTag=(string?)x.Attribute(nameof(StorageTag)) ?? s.StorageTag;
        }
        catch(Exception e) when(e is System.IO.IOException or System.Xml.XmlException or FormatException or UnauthorizedAccessException)
        { log($"Ayar okunamadı ({path}): {e.Message}. Varsayılanlar kullanılıyor."); }
        return s;
    }
}
