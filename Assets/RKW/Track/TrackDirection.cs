namespace RKW.Track
{
    /// <summary>
    /// Direction is a canonical property of TrackConfiguration (Requirement 16 /
    /// Requirement 19): "reverse" is never implemented by flipping checkpoint
    /// order at runtime — each direction is its own independently validated
    /// TrackConfiguration with its own data.
    /// </summary>
    public enum TrackDirection
    {
        Clockwise = 0,
        CounterClockwise = 1
    }
}
