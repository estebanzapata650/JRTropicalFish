using Microsoft.AspNetCore.Identity;
using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Auth;

/// <summary>Envuelve el PasswordHasher de ASP.NET Core Identity sin adoptar el resto de Identity.</summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object DummyUser = new();

    public string Hash(string password) => _hasher.HashPassword(DummyUser, password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(DummyUser, hash, password) != PasswordVerificationResult.Failed;
}
