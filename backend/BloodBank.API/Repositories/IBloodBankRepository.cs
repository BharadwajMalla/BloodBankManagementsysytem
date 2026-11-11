using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public interface IBloodBankRepository
{
    // Users
    Task<User?> FindUserAsync(Guid id);
    Task<User?> FindUserByEmailAsync(string email);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);

    // Blood Stocks
    Task<List<BloodStock>> GetStocksAsync(string? bloodGroup, string? componentType);
    Task<BloodStock?> FindStockAsync(string bloodGroup, string componentType);
    Task<BloodStock?> FindStockByIdAsync(Guid id);
    Task AddStockAsync(BloodStock stock);
    Task UpdateStockAsync(BloodStock stock);

    // Blood Bags
    Task<List<BloodBag>> GetBagsAsync(string? componentType);
    Task<bool> BagNumberExistsAsync(string bagNumber);
    Task<BloodBag?> FindBagByNumberAsync(string number);
    Task AddBagAsync(BloodBag bag);
    Task UpdateBagAsync(BloodBag bag);
    void RemoveBag(BloodBag bag);
    Task<List<BloodBag>> GetAvailableBagsAsync(string bloodGroup, string componentType, int count);

    // Donation Camps
    Task<List<DonationCamp>> GetCampsAsync(bool upcoming = false);
    Task<DonationCamp?> FindCampAsync(Guid id);
    Task AddCampAsync(DonationCamp camp);
    Task UpdateCampAsync(DonationCamp camp);
    void RemoveCamp(DonationCamp camp);

    // Donors & Donation Records
    Task<List<User>> GetDonorsAsync();
    Task<List<DonationRecord>> GetDonationsAsync(IEnumerable<Guid> donorIds);
    Task<List<DonationRecord>> GetDonationsForDonorAsync(Guid donorId);
    Task AddDonationAsync(DonationRecord donation);

    // Blood Requests
    Task<List<BloodRequest>> GetRequestsAsync(Guid? requesterId = null, string? status = null);
    Task<BloodRequest?> FindRequestAsync(Guid id);
    Task AddRequestAsync(BloodRequest request);
    Task UpdateRequestAsync(BloodRequest request);

    // Blood Inventory
    Task<BloodInventory?> FindInventoryAsync(string bloodGroup, string bankName);
    Task<List<BloodInventory>> GetInventoryAsync(string? bloodGroup);
    Task AddInventoryAsync(BloodInventory inventory);
    Task UpdateInventoryAsync(BloodInventory inventory);

    // Component Collections
    Task<List<ComponentCollection>> GetComponentCollectionsAsync();
    Task<ComponentCollection?> FindComponentCollectionAsync(Guid id);
    Task<ComponentCollection?> FindComponentCollectionByBarcodeAsync(string barcode);
    Task AddComponentCollectionAsync(ComponentCollection item);
    Task UpdateComponentCollectionAsync(ComponentCollection item);
    void RemoveComponentCollection(ComponentCollection item);

    // Component Discards
    Task<List<ComponentDiscard>> GetComponentDiscardsAsync();
    Task<ComponentDiscard?> FindComponentDiscardAsync(Guid id);
    Task AddComponentDiscardAsync(ComponentDiscard item);
    Task UpdateComponentDiscardAsync(ComponentDiscard item);
    void RemoveComponentDiscard(ComponentDiscard item);

    // Component Returns
    Task<List<ComponentReturn>> GetComponentReturnsAsync();
    Task<ComponentReturn?> FindComponentReturnAsync(Guid id);
    Task AddComponentReturnAsync(ComponentReturn item);
    Task UpdateComponentReturnAsync(ComponentReturn item);
    void RemoveComponentReturn(ComponentReturn item);

    // Bulk & Transactional operations
    Task ClearOperationalDataAsync();
    Task SaveChangesAsync();
}
