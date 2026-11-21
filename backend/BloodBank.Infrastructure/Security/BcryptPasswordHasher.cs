using BloodBank.Application.Contracts;

namespace BloodBank.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password, int workFactor) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor);
}
