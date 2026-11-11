using Microsoft.EntityFrameworkCore;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public sealed class EfBloodBankRepository : IBloodBankRepository
{
    private readonly ApplicationDbContext _db;
    public EfBloodBankRepository(ApplicationDbContext db) => _db = db;

    public Task<User?> FindUserAsync(Guid id) => _db.Users.FindAsync(id).AsTask();
    public Task<User?> FindUserByEmailAsync(string email) => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
    public Task AddUserAsync(User user) => _db.Users.AddAsync(user).AsTask();
    public Task UpdateUserAsync(User user) { _db.Users.Update(user); return Task.CompletedTask; }

    public Task<List<BloodStock>> GetStocksAsync(string? group, string? component) =>
        _db.BloodStocks.AsNoTracking().Where(s => string.IsNullOrWhiteSpace(group) || s.BloodGroup == group)
            .Where(s => string.IsNullOrWhiteSpace(component) || s.ComponentType == component).OrderBy(s => s.BloodGroup).ThenBy(s => s.ComponentType).ToListAsync();
    public Task<BloodStock?> FindStockAsync(string group, string component) => _db.BloodStocks.FirstOrDefaultAsync(s => s.BloodGroup == group && s.ComponentType == component);
    public Task<BloodStock?> FindStockByIdAsync(Guid id) => _db.BloodStocks.FindAsync(id).AsTask();
    public Task AddStockAsync(BloodStock stock) => _db.BloodStocks.AddAsync(stock).AsTask();
    public Task UpdateStockAsync(BloodStock stock) { _db.BloodStocks.Update(stock); return Task.CompletedTask; }

    public Task<List<BloodBag>> GetBagsAsync(string? component) => _db.BloodBags.AsNoTracking().Where(b => string.IsNullOrWhiteSpace(component) || b.ComponentType == component).OrderByDescending(b => b.AddedDate).ToListAsync();
    public Task<bool> BagNumberExistsAsync(string number) => _db.BloodBags.AnyAsync(b => b.BagNumber == number);
    public Task<BloodBag?> FindBagByNumberAsync(string number) => _db.BloodBags.FirstOrDefaultAsync(b => b.BagNumber == number);
    public Task AddBagAsync(BloodBag bag) => _db.BloodBags.AddAsync(bag).AsTask();
    public Task UpdateBagAsync(BloodBag bag) { _db.BloodBags.Update(bag); return Task.CompletedTask; }
    public void RemoveBag(BloodBag bag) => _db.BloodBags.Remove(bag);
    public Task<List<BloodBag>> GetAvailableBagsAsync(string group, string component, int count) => _db.BloodBags.Where(b => !b.IsIssued && b.BloodGroup == group && b.ComponentType == component).OrderBy(b => b.AddedDate).Take(count).ToListAsync();

    public Task<List<DonationCamp>> GetCampsAsync(bool upcoming = false) => upcoming
        ? _db.DonationCamps.Where(c => c.CampDate >= DateTime.UtcNow.Date || c.Status == "Scheduled").OrderBy(c => c.CampDate).AsNoTracking().ToListAsync()
        : _db.DonationCamps.OrderByDescending(c => c.CampDate).AsNoTracking().ToListAsync();
    public Task<DonationCamp?> FindCampAsync(Guid id) => _db.DonationCamps.FindAsync(id).AsTask();
    public Task AddCampAsync(DonationCamp camp) => _db.DonationCamps.AddAsync(camp).AsTask();
    public Task UpdateCampAsync(DonationCamp camp) { _db.DonationCamps.Update(camp); return Task.CompletedTask; }
    public void RemoveCamp(DonationCamp camp) => _db.DonationCamps.Remove(camp);

    public Task<List<User>> GetDonorsAsync() => _db.Users.AsNoTracking().Where(u => u.Role == "Donor").OrderBy(u => u.FullName).ToListAsync();
    public Task<List<DonationRecord>> GetDonationsAsync(IEnumerable<Guid> ids) => _db.DonationRecords.AsNoTracking().Where(r => ids.Contains(r.DonorId)).ToListAsync();
    public Task<List<DonationRecord>> GetDonationsForDonorAsync(Guid donorId) => _db.DonationRecords.AsNoTracking().Where(r => r.DonorId == donorId).OrderByDescending(r => r.DonationDate).ToListAsync();
    public Task AddDonationAsync(DonationRecord donation) => _db.DonationRecords.AddAsync(donation).AsTask();

    public Task<List<BloodRequest>> GetRequestsAsync(Guid? requesterId = null, string? status = null) => _db.BloodRequests.AsNoTracking().Where(r => !requesterId.HasValue || r.RequesterId == requesterId).Where(r => string.IsNullOrWhiteSpace(status) || r.Status == status).OrderByDescending(r => r.CreatedAt).ToListAsync();
    public Task<BloodRequest?> FindRequestAsync(Guid id) => _db.BloodRequests.FindAsync(id).AsTask();
    public Task AddRequestAsync(BloodRequest request) => _db.BloodRequests.AddAsync(request).AsTask();
    public Task UpdateRequestAsync(BloodRequest request) { _db.BloodRequests.Update(request); return Task.CompletedTask; }

    public Task<BloodInventory?> FindInventoryAsync(string group, string bank) => _db.BloodInventories.FirstOrDefaultAsync(b => b.BloodGroup.ToUpper() == group && b.BloodBankName.ToLower() == bank.ToLower());
    public Task<List<BloodInventory>> GetInventoryAsync(string? group) => _db.BloodInventories.AsNoTracking().Where(b => string.IsNullOrWhiteSpace(group) || b.BloodGroup.ToUpper() == group).OrderBy(b => b.BloodGroup).ThenByDescending(b => b.QuantityUnits).ToListAsync();
    public Task AddInventoryAsync(BloodInventory inventory) => _db.BloodInventories.AddAsync(inventory).AsTask();
    public Task UpdateInventoryAsync(BloodInventory inventory) { _db.BloodInventories.Update(inventory); return Task.CompletedTask; }

    public Task<List<ComponentCollection>> GetComponentCollectionsAsync() => _db.ComponentCollections.AsNoTracking().OrderByDescending(x => x.CollectionDate).ToListAsync();
    public Task<ComponentCollection?> FindComponentCollectionAsync(Guid id) => _db.ComponentCollections.FirstOrDefaultAsync(x => x.Id == id);
    public Task<ComponentCollection?> FindComponentCollectionByBarcodeAsync(string barcode) => _db.ComponentCollections.FirstOrDefaultAsync(x => x.BagBarcode == barcode);
    public Task AddComponentCollectionAsync(ComponentCollection item) => _db.ComponentCollections.AddAsync(item).AsTask();
    public Task UpdateComponentCollectionAsync(ComponentCollection item) { _db.ComponentCollections.Update(item); return Task.CompletedTask; }
    public void RemoveComponentCollection(ComponentCollection item) => _db.ComponentCollections.Remove(item);

    public Task<List<ComponentDiscard>> GetComponentDiscardsAsync() => _db.ComponentDiscards.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
    public Task<ComponentDiscard?> FindComponentDiscardAsync(Guid id) => _db.ComponentDiscards.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddComponentDiscardAsync(ComponentDiscard item) => _db.ComponentDiscards.AddAsync(item).AsTask();
    public Task UpdateComponentDiscardAsync(ComponentDiscard item) { _db.ComponentDiscards.Update(item); return Task.CompletedTask; }
    public void RemoveComponentDiscard(ComponentDiscard item) => _db.ComponentDiscards.Remove(item);

    public Task<List<ComponentReturn>> GetComponentReturnsAsync() => _db.ComponentReturns.AsNoTracking().OrderByDescending(x => x.ReturnDate).ToListAsync();
    public Task<ComponentReturn?> FindComponentReturnAsync(Guid id) => _db.ComponentReturns.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddComponentReturnAsync(ComponentReturn item) => _db.ComponentReturns.AddAsync(item).AsTask();
    public Task UpdateComponentReturnAsync(ComponentReturn item) { _db.ComponentReturns.Update(item); return Task.CompletedTask; }
    public void RemoveComponentReturn(ComponentReturn item) => _db.ComponentReturns.Remove(item);

    // Uddanam Register 1: TTI Screening
    public Task<List<TtiScreeningRecord>> GetTtiScreeningsAsync(string? status = null) =>
        _db.TtiScreeningRecords.AsNoTracking().Where(x => string.IsNullOrWhiteSpace(status) || x.OverallStatus == status).OrderByDescending(x => x.TestingDate).ToListAsync();
    public Task<TtiScreeningRecord?> FindTtiScreeningAsync(Guid id) => _db.TtiScreeningRecords.FirstOrDefaultAsync(x => x.Id == id);
    public Task<TtiScreeningRecord?> FindTtiScreeningByBarcodeAsync(string barcode) => _db.TtiScreeningRecords.FirstOrDefaultAsync(x => x.BagBarcode == barcode);
    public Task AddTtiScreeningAsync(TtiScreeningRecord item) => _db.TtiScreeningRecords.AddAsync(item).AsTask();
    public Task UpdateTtiScreeningAsync(TtiScreeningRecord item) { _db.TtiScreeningRecords.Update(item); return Task.CompletedTask; }
    public void RemoveTtiScreening(TtiScreeningRecord item) => _db.TtiScreeningRecords.Remove(item);

    // Uddanam Register 2: Patient Grouping & Crossmatch
    public Task<List<PatientGroupingRecord>> GetPatientGroupingsAsync() =>
        _db.PatientGroupingRecords.AsNoTracking().OrderByDescending(x => x.Date).ToListAsync();
    public Task<PatientGroupingRecord?> FindPatientGroupingAsync(Guid id) => _db.PatientGroupingRecords.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddPatientGroupingAsync(PatientGroupingRecord item) => _db.PatientGroupingRecords.AddAsync(item).AsTask();
    public Task UpdatePatientGroupingAsync(PatientGroupingRecord item) { _db.PatientGroupingRecords.Update(item); return Task.CompletedTask; }
    public void RemovePatientGrouping(PatientGroupingRecord item) => _db.PatientGroupingRecords.Remove(item);

    // Uddanam Register 3: CPDA Blood Bags Stock Register
    public Task<List<BloodBagStockRegister>> GetBloodBagStockRegistersAsync() =>
        _db.BloodBagStockRegisters.AsNoTracking().OrderByDescending(x => x.Date).ToListAsync();
    public Task<BloodBagStockRegister?> FindBloodBagStockRegisterAsync(Guid id) => _db.BloodBagStockRegisters.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddBloodBagStockRegisterAsync(BloodBagStockRegister item) => _db.BloodBagStockRegisters.AddAsync(item).AsTask();
    public Task UpdateBloodBagStockRegisterAsync(BloodBagStockRegister item) { _db.BloodBagStockRegisters.Update(item); return Task.CompletedTask; }
    public void RemoveBloodBagStockRegister(BloodBagStockRegister item) => _db.BloodBagStockRegisters.Remove(item);

    // Uddanam Register 4: Diagnostic Kits & Reagents
    public Task<List<DiagnosticKitRegister>> GetDiagnosticKitRegistersAsync() =>
        _db.DiagnosticKitRegisters.AsNoTracking().OrderByDescending(x => x.Date).ToListAsync();
    public Task<DiagnosticKitRegister?> FindDiagnosticKitRegisterAsync(Guid id) => _db.DiagnosticKitRegisters.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddDiagnosticKitRegisterAsync(DiagnosticKitRegister item) => _db.DiagnosticKitRegisters.AddAsync(item).AsTask();
    public Task UpdateDiagnosticKitRegisterAsync(DiagnosticKitRegister item) { _db.DiagnosticKitRegisters.Update(item); return Task.CompletedTask; }
    public void RemoveDiagnosticKitRegister(DiagnosticKitRegister item) => _db.DiagnosticKitRegisters.Remove(item);

    // Uddanam Register 5: Apheresis / SDP Records
    public Task<List<ApheresisRecord>> GetApheresisRecordsAsync() =>
        _db.ApheresisRecords.AsNoTracking().OrderByDescending(x => x.Date).ToListAsync();
    public Task<ApheresisRecord?> FindApheresisRecordAsync(Guid id) => _db.ApheresisRecords.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddApheresisRecordAsync(ApheresisRecord item) => _db.ApheresisRecords.AddAsync(item).AsTask();
    public Task UpdateApheresisRecordAsync(ApheresisRecord item) { _db.ApheresisRecords.Update(item); return Task.CompletedTask; }
    public void RemoveApheresisRecord(ApheresisRecord item) => _db.ApheresisRecords.Remove(item);

    public async Task ClearOperationalDataAsync()
    {
        _db.BloodRequests.RemoveRange(_db.BloodRequests);
        _db.DonationRecords.RemoveRange(_db.DonationRecords);
        _db.DonationCamps.RemoveRange(_db.DonationCamps);
        _db.BloodBags.RemoveRange(_db.BloodBags);
        _db.BloodStocks.RemoveRange(_db.BloodStocks);
        _db.BloodInventories.RemoveRange(_db.BloodInventories);
        _db.ComponentCollections.RemoveRange(_db.ComponentCollections);
        _db.ComponentDiscards.RemoveRange(_db.ComponentDiscards);
        _db.ComponentReturns.RemoveRange(_db.ComponentReturns);
        _db.TtiScreeningRecords.RemoveRange(_db.TtiScreeningRecords);
        _db.PatientGroupingRecords.RemoveRange(_db.PatientGroupingRecords);
        _db.BloodBagStockRegisters.RemoveRange(_db.BloodBagStockRegisters);
        _db.DiagnosticKitRegisters.RemoveRange(_db.DiagnosticKitRegisters);
        _db.ApheresisRecords.RemoveRange(_db.ApheresisRecords);
        await _db.SaveChangesAsync();
    }
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
