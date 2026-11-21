namespace BloodBank.Application.Services;

public sealed class MasterDataConflictException(string message) : Exception(message);

public sealed class MasterDataNotFoundException(string message) : Exception(message);
