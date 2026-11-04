using Microsoft.EntityFrameworkCore;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public sealed class EfBloodBankRepository : IBloodBankRepository
{
    private readonly ApplicationDbContext _db;
    public EfBloodBankRepository(ApplicationDbContext db) => _db = db;

    public Task<User?> FindUserAsync(Guid id) => _db.Users.FindAsync(id).AsTask();
    public Task<User?> FindUserByEmailAsync(string email) => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email);
    public Task AddUserAsync(User user) => _db.Users.AddAsync(user).AsTask();
    public Task<List<BloodStock>> GetStocksAsync(string? group, string? component) =>
        _db.BloodStocks.AsNoTracking().Where(s => string.IsNullOrWhiteSpace(group) || s.BloodGroup == group)
            .Where(s => string.IsNullOrWhiteSpace(component) || s.ComponentType == component).OrderBy(s => s.BloodGroup).ThenBy(s => s.ComponentType).ToListAsync();
    public Task<BloodStock?> FindStockAsync(string group, string component) => _db.BloodStocks.FirstOrDefaultAsync(s => s.BloodGroup == group && s.ComponentType == component);
    public Task AddStockAsync(BloodStock stock) => _db.BloodStocks.AddAsync(stock).AsTask();
    public Task<List<BloodBag>> GetBagsAsync(string? component) => _db.BloodBags.AsNoTracking().Where(b => string.IsNullOrWhiteSpace(component) || b.ComponentType == component).OrderByDescending(b => b.AddedDate).ToListAsync();
    public Task<bool> BagNumberExistsAsync(string number) => _db.BloodBags.AnyAsync(b => b.BagNumber == number);
    public Task AddBagAsync(BloodBag bag) => _db.BloodBags.AddAsync(bag).AsTask();
    public Task<List<DonationCamp>> GetCampsAsync(bool upcoming = false) => upcoming
        ? _db.DonationCamps.Where(c => c.CampDate >= DateTime.UtcNow.Date || c.Status == "Scheduled").OrderBy(c => c.CampDate).AsNoTracking().ToListAsync()
        : _db.DonationCamps.OrderByDescending(c => c.CampDate).AsNoTracking().ToListAsync();
    public Task<DonationCamp?> FindCampAsync(Guid id) => _db.DonationCamps.FindAsync(id).AsTask();
    public Task AddCampAsync(DonationCamp camp) => _db.DonationCamps.AddAsync(camp).AsTask();
    public void RemoveCamp(DonationCamp camp) => _db.DonationCamps.Remove(camp);
    public Task<List<User>> GetDonorsAsync() => _db.Users.AsNoTracking().Where(u => u.Role == "Donor").OrderBy(u => u.FullName).ToListAsync();
    public Task<List<DonationRecord>> GetDonationsAsync(IEnumerable<Guid> ids) => _db.DonationRecords.AsNoTracking().Where(r => ids.Contains(r.DonorId)).ToListAsync();
    public Task<List<BloodRequest>> GetRequestsAsync(Guid? requesterId = null, string? status = null) => _db.BloodRequests.AsNoTracking().Where(r => !requesterId.HasValue || r.RequesterId == requesterId).Where(r => string.IsNullOrWhiteSpace(status) || r.Status == status).OrderByDescending(r => r.CreatedAt).ToListAsync();
    public Task<BloodRequest?> FindRequestAsync(Guid id) => _db.BloodRequests.FindAsync(id).AsTask();
    public Task AddRequestAsync(BloodRequest request) => _db.BloodRequests.AddAsync(request).AsTask();
    public Task<List<BloodBag>> GetAvailableBagsAsync(string group, string component, int count) => _db.BloodBags.Where(b => !b.IsIssued && b.BloodGroup == group && b.ComponentType == component).OrderBy(b => b.AddedDate).Take(count).ToListAsync();
    public Task<BloodInventory?> FindInventoryAsync(string group, string bank) => _db.BloodInventories.FirstOrDefaultAsync(b => b.BloodGroup.ToUpper() == group && b.BloodBankName.ToLower() == bank.ToLower());
    public Task<List<BloodInventory>> GetInventoryAsync(string? group) => _db.BloodInventories.AsNoTracking().Where(b => string.IsNullOrWhiteSpace(group) || b.BloodGroup.ToUpper() == group).OrderBy(b => b.BloodGroup).ThenByDescending(b => b.QuantityUnits).ToListAsync();
    public Task AddInventoryAsync(BloodInventory inventory) => _db.BloodInventories.AddAsync(inventory).AsTask();
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
