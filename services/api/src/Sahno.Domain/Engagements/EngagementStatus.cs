namespace Sahno.Domain.Engagements;

/// <summary>
/// The MVP engagement lifecycle (ENGAGEMENT_STATE_MACHINE.md, D-025 onward).
/// "Engagement" is the internal term for the whole lifecycle; people see
/// Enquiry, Booking, or Event depending on context (D-039).
///
/// Preparation is deliberately absent: readiness is tracked as items on a
/// Confirmed engagement, not as a state of its own (D-031).
/// </summary>
public enum EngagementStatus
{
    /// <summary>Private to Owner and Admins; Members have not been told (D-025).</summary>
    Draft = 1,

    CheckingAvailability = 2,

    Tentative = 3,

    Confirmed = 4,

    Completed = 5,

    Cancelled = 6,

    Postponed = 7,
}
