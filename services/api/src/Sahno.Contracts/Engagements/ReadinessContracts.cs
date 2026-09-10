namespace Sahno.Contracts.Engagements;

/// <summary>
/// One checklist item and where it stands (D-048). A list of these, not a
/// score: it says what is left to do rather than how complete something is.
/// </summary>
public sealed record ReadinessEntryResponse(string Item, string State);

/// <summary>Marks a checklist item as not applying to this event, or puts it back.</summary>
public sealed record SetReadinessRequest(string Item, bool NotRequired);
