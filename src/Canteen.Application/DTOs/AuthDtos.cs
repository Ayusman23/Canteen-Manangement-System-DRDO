using Canteen.Domain.Enums;

namespace Canteen.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string EmployeeCode,
    string Department,
    UserRole Role
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string EmployeeCode,
    string Department,
    string Role
);
