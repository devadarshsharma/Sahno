namespace Sahno.Contracts.Organisations;

/// <summary>
/// One customer in the organisation's directory (D-022: organisers only).
/// BookingCount is how many bookings are linked to them — the "they have had
/// us four times" number.
/// </summary>
public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Notes,
    int BookingCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>A booking as a line in a customer's history.</summary>
public sealed record CustomerBookingResponse(
    Guid EngagementId,
    string Title,
    string Status,
    DateOnly? StartDate,
    string? Venue);

/// <summary>The customer with every booking they have had.</summary>
public sealed record CustomerDetailResponse(
    CustomerResponse Customer,
    IReadOnlyList<CustomerBookingResponse> Bookings);

public sealed record SaveCustomerRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Notes);
