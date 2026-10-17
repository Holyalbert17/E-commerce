namespace Datamex.ECommerce.Application.Auth;

public record LoginRequest(string Usuario, string Password, long? IDEmpresa = null, string? TenantSlug = null);

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    long IDEmpresa,
    long IDUsuarioEcommerce,
    string Nombre,
    string CanalPreferido);

/// <summary>Espejo SignUpDTO del ERP.</summary>
public record SignUpRequest(
    string Nombre,
    string Apellido,
    long IDTipoDocumento,
    string NumeroDocumento,
    string Telefono,
    string Movil,
    string Email,
    string Password,
    string PasswordConfirmation,
    string? TenantSlug = null);

public record SignUpResult(long IDUsuarioEcommerce, string Email, string Mensaje, string? ActivationUrlDev);

public record ActivateRequest(string Codigo, string Checksum, string? TenantSlug = null);

public record ForgotPasswordRequest(string Email, string? TenantSlug = null);

public record ForgotPasswordResult(string Mensaje, string? ResetUrlDev);

public record ResetPasswordRequest(
    string Codigo,
    string Checksum,
    string Password,
    string PasswordConfirmation,
    string? TenantSlug = null);

public record ChangePasswordRequest(string PasswordActual, string PasswordNueva, string PasswordConfirmacion);

public record ResendActivationRequest(string Email, string? TenantSlug = null);

public record ResendActivationResult(string Mensaje, string? ActivationUrlDev);

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<SignUpResult> RegisterAsync(SignUpRequest request, CancellationToken ct = default);
    Task<TokenResponse?> ActivateAsync(ActivateRequest request, CancellationToken ct = default);
    Task<ResendActivationResult> ResendActivationAsync(ResendActivationRequest request, CancellationToken ct = default);
    Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(long idUsuarioEcommerce, ChangePasswordRequest request, CancellationToken ct = default);
}
