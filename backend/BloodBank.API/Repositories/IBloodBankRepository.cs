using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public interface IBloodBankRepository
{
    Task<User?> FindUserAsync(Guid id);
    Task<User?> FindUserByEmailAsync(string email);
    Task AddUserAsync(User user);
    Task<List<BloodStock>> GetStocksAsync(string? bloodGroup, string? componentType);
    Task<BloodStock?> FindStockAsync(string bloodGroup, string componentType);
    Task AddStockAsync(BloodStock stock);
    Task<List<BloodBag>> GetBagsAsync(string? componentType);
    Task<bool> BagNumberExistsAsync(string bagNumber);
    Task AddBagAsync(BloodBag bag);
    Task<List<DonationCamp>> GetCampsAsync(bool upcoming = false);
    Task<DonationCamp?> FindCampAsync(Guid id);
    Task AddCampAsync(DonationCamp camp);
    void RemoveCamp(DonationCamp camp);
    Task<List<User>> GetDonorsAsync();
    Task<List<DonationRecord>> GetDonationsAsync(IEnumerable<Guid> donorIds);
    Task<List<BloodRequest>> GetRequestsAsync(Guid? requesterId = null, string? status = null);
    Task<BloodRequest?> FindRequestAsync(Guid id);
    Task AddRequestAsync(BloodRequest request);
    Task<List<BloodBag>> GetAvailableBagsAsync(string bloodGroup, string componentType, int count);
    Task<BloodInventory?> FindInventoryAsync(string bloodGroup, string bankName);
    Task<List<BloodInventory>> GetInventoryAsync(string? bloodGroup);
    Task AddInventoryAsync(BloodInventory inventory);
    Task SaveChangesAsync();
}
