using FiapConnect.Application.DTOs.Auth;

namespace FiapConnect.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}