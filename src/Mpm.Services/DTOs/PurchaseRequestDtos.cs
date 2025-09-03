namespace Mpm.Services.DTOs;

public record SendPrRequest(string UserId, string UserName, string? Reason);

public record CancelPrRequest(string UserId, string UserName, string Reason);

public record SelectWinnerRequest(int SupplierId, string UserId, string UserName, string? Reason);