namespace CanHappy.Models.Api;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record TokenResponse(string Token, DateTime ExpiresAtUtc);