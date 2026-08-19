namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: coarse thermal categories, matching the categories requested by
    /// Requirement R12.4 (nominal/light/moderate/severe/critical). Maps onto
    /// Android's PowerManager thermal status where available; other platforms
    /// (or Android below API 29) report <see cref="Unknown"/>.
    /// </summary>
    public enum ThermalStatus
    {
        Unknown = 0,
        Nominal = 1,
        Light = 2,
        Moderate = 3,
        Severe = 4,
        Critical = 5
    }
}
