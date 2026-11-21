namespace BloodBank.Application.Contracts;

public interface IPasswordHasher
{
    string HashPassword(string password, int workFactor);
}
