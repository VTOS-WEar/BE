namespace VTOS.Application.Features.Users.DTOs;

public record UpsertParentAddressRequest(
    string Label,
    string RecipientName,
    string RecipientPhone,
    string AddressLine,
    bool IsDefault);

public record ParentAddressResponse(
    Guid AddressId,
    string Label,
    string RecipientName,
    string RecipientPhone,
    string AddressLine,
    bool IsDefault);
