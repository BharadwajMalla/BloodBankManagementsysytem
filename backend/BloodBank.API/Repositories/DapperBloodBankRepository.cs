using System.Data;
using Dapper;
using BloodBank.API.Data;
using BloodBank.API.Models;

namespace BloodBank.API.Repositories;

public sealed class DapperBloodBankRepository : IBloodBankRepository
{
    private readonly IDbConnectionFactory _factory;

    // Tracked entities within the scoped request lifecycle
    private readonly Dictionary<Guid, User> _trackedUsers = new();
    private readonly Dictionary<Guid, BloodStock> _trackedStocks = new();
    private readonly Dictionary<Guid, BloodBag> _trackedBags = new();
    private readonly Dictionary<Guid, BloodInventory> _trackedInventories = new();
    private readonly Dictionary<Guid, DonationCamp> _trackedCamps = new();
    private readonly Dictionary<Guid, BloodRequest> _trackedRequests = new();
    private readonly Dictionary<Guid, ComponentCollection> _trackedCollections = new();
    private readonly Dictionary<Guid, ComponentDiscard> _trackedDiscards = new();
    private readonly Dictionary<Guid, ComponentReturn> _trackedReturns = new();

    // Pending Inserts & Deletes
    private readonly List<object> _pendingInserts = new();
    private readonly List<(string TableName, Guid Id)> _pendingDeletes = new();

    public DapperBloodBankRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    private void Track<T>(T? entity) where T : class
    {
        if (entity == null) return;
        switch (entity)
        {
            case User u: _trackedUsers[u.Id] = u; break;
            case BloodStock s: _trackedStocks[s.Id] = s; break;
            case BloodBag b: _trackedBags[b.Id] = b; break;
            case BloodInventory i: _trackedInventories[i.Id] = i; break;
            case DonationCamp c: _trackedCamps[c.Id] = c; break;
            case BloodRequest r: _trackedRequests[r.Id] = r; break;
            case ComponentCollection cc: _trackedCollections[cc.Id] = cc; break;
            case ComponentDiscard cd: _trackedDiscards[cd.Id] = cd; break;
            case ComponentReturn cr: _trackedReturns[cr.Id] = cr; break;
        }
    }

    // ==========================================
    // 1. Users
    // ==========================================
    public async Task<User?> FindUserAsync(Guid id)
    {
        if (_trackedUsers.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        var user = await conn.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        Track(user);
        return user;
    }

    public async Task<User?> FindUserByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var tracked = _trackedUsers.Values.FirstOrDefault(u => u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (tracked != null) return tracked;

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE LOWER(Email) = LOWER(@Email)";
        var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { Email = normalized });
        Track(user);
        return user;
    }

    public Task AddUserAsync(User user)
    {
        _trackedUsers[user.Id] = user;
        _pendingInserts.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user)
    {
        _trackedUsers[user.Id] = user;
        return Task.CompletedTask;
    }

    // ==========================================
    // 2. Blood Stock
    // ==========================================
    public async Task<List<BloodStock>> GetStocksAsync(string? bloodGroup, string? componentType)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM BloodStocks 
            WHERE (@BloodGroup IS NULL OR BloodGroup = @BloodGroup) 
              AND (@ComponentType IS NULL OR ComponentType = @ComponentType)
            ORDER BY BloodGroup, ComponentType";

        var list = (await conn.QueryAsync<BloodStock>(sql, new
        {
            BloodGroup = string.IsNullOrWhiteSpace(bloodGroup) ? null : bloodGroup.Trim(),
            ComponentType = string.IsNullOrWhiteSpace(componentType) ? null : componentType.Trim()
        })).AsList();

        foreach (var item in list) Track(item);
        return list;
    }

    public async Task<BloodStock?> FindStockAsync(string bloodGroup, string componentType)
    {
        var bg = bloodGroup.Trim().ToUpperInvariant();
        var ct = componentType.Trim();

        var tracked = _trackedStocks.Values.FirstOrDefault(s =>
            s.BloodGroup.Equals(bg, StringComparison.OrdinalIgnoreCase) &&
            s.ComponentType.Equals(ct, StringComparison.OrdinalIgnoreCase));
        if (tracked != null) return tracked;

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM BloodStocks WHERE BloodGroup = @BloodGroup AND ComponentType = @ComponentType";
        var stock = await conn.QueryFirstOrDefaultAsync<BloodStock>(sql, new { BloodGroup = bg, ComponentType = ct });
        Track(stock);
        return stock;
    }

    public async Task<BloodStock?> FindStockByIdAsync(Guid id)
    {
        if (_trackedStocks.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM BloodStocks WHERE Id = @Id";
        var stock = await conn.QuerySingleOrDefaultAsync<BloodStock>(sql, new { Id = id });
        Track(stock);
        return stock;
    }

    public Task AddStockAsync(BloodStock stock)
    {
        _trackedStocks[stock.Id] = stock;
        _pendingInserts.Add(stock);
        return Task.CompletedTask;
    }

    public Task UpdateStockAsync(BloodStock stock)
    {
        _trackedStocks[stock.Id] = stock;
        return Task.CompletedTask;
    }

    // ==========================================
    // 3. Blood Bags
    // ==========================================
    public async Task<List<BloodBag>> GetBagsAsync(string? componentType)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM BloodBags 
            WHERE (@ComponentType IS NULL OR ComponentType = @ComponentType) 
            ORDER BY AddedDate DESC";

        var list = (await conn.QueryAsync<BloodBag>(sql, new
        {
            ComponentType = string.IsNullOrWhiteSpace(componentType) ? null : componentType.Trim()
        })).AsList();

        foreach (var b in list) Track(b);
        return list;
    }

    public async Task<bool> BagNumberExistsAsync(string bagNumber)
    {
        var num = bagNumber.Trim();
        if (_trackedBags.Values.Any(b => b.BagNumber.Equals(num, StringComparison.OrdinalIgnoreCase)))
            return true;

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM BloodBags WHERE BagNumber = @BagNumber";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { BagNumber = num });
        return count > 0;
    }

    public async Task<BloodBag?> FindBagByNumberAsync(string number)
    {
        var num = number.Trim();
        var tracked = _trackedBags.Values.FirstOrDefault(b => b.BagNumber.Equals(num, StringComparison.OrdinalIgnoreCase));
        if (tracked != null) return tracked;

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM BloodBags WHERE BagNumber = @BagNumber";
        var bag = await conn.QueryFirstOrDefaultAsync<BloodBag>(sql, new { BagNumber = num });
        Track(bag);
        return bag;
    }

    public Task AddBagAsync(BloodBag bag)
    {
        _trackedBags[bag.Id] = bag;
        _pendingInserts.Add(bag);
        return Task.CompletedTask;
    }

    public Task UpdateBagAsync(BloodBag bag)
    {
        _trackedBags[bag.Id] = bag;
        return Task.CompletedTask;
    }

    public void RemoveBag(BloodBag bag)
    {
        _trackedBags.Remove(bag.Id);
        _pendingDeletes.Add(("BloodBags", bag.Id));
    }

    public async Task<List<BloodBag>> GetAvailableBagsAsync(string bloodGroup, string componentType, int count)
    {
        using var conn = _factory.CreateConnection();
        var sql = _factory.IsSqlite
            ? @"SELECT * FROM BloodBags 
                WHERE IsIssued = 0 AND BloodGroup = @BloodGroup AND ComponentType = @ComponentType 
                ORDER BY AddedDate ASC LIMIT @Count"
            : @"SELECT TOP (@Count) * FROM BloodBags 
                WHERE IsIssued = 0 AND BloodGroup = @BloodGroup AND ComponentType = @ComponentType 
                ORDER BY AddedDate ASC";

        var list = (await conn.QueryAsync<BloodBag>(sql, new
        {
            BloodGroup = bloodGroup.Trim().ToUpperInvariant(),
            ComponentType = componentType.Trim(),
            Count = count
        })).AsList();

        foreach (var b in list) Track(b);
        return list;
    }

    // ==========================================
    // 4. Donation Camps
    // ==========================================
    public async Task<List<DonationCamp>> GetCampsAsync(bool upcoming = false)
    {
        using var conn = _factory.CreateConnection();
        var sql = upcoming
            ? @"SELECT * FROM DonationCamps 
                WHERE CampDate >= @Now OR Status = 'Scheduled' 
                ORDER BY CampDate ASC"
            : @"SELECT * FROM DonationCamps 
                ORDER BY CampDate DESC";

        var list = (await conn.QueryAsync<DonationCamp>(sql, new { Now = DateTime.UtcNow.Date })).AsList();
        foreach (var c in list) Track(c);
        return list;
    }

    public async Task<DonationCamp?> FindCampAsync(Guid id)
    {
        if (_trackedCamps.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM DonationCamps WHERE Id = @Id";
        var camp = await conn.QuerySingleOrDefaultAsync<DonationCamp>(sql, new { Id = id });
        Track(camp);
        return camp;
    }

    public Task AddCampAsync(DonationCamp camp)
    {
        _trackedCamps[camp.Id] = camp;
        _pendingInserts.Add(camp);
        return Task.CompletedTask;
    }

    public Task UpdateCampAsync(DonationCamp camp)
    {
        _trackedCamps[camp.Id] = camp;
        return Task.CompletedTask;
    }

    public void RemoveCamp(DonationCamp camp)
    {
        _trackedCamps.Remove(camp.Id);
        _pendingDeletes.Add(("DonationCamps", camp.Id));
    }

    // ==========================================
    // 5. Donors & Donation Records
    // ==========================================
    public async Task<List<User>> GetDonorsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Role = 'Donor' ORDER BY FullName ASC";
        var list = (await conn.QueryAsync<User>(sql)).AsList();
        foreach (var u in list) Track(u);
        return list;
    }

    public async Task<List<DonationRecord>> GetDonationsAsync(IEnumerable<Guid> donorIds)
    {
        var ids = donorIds.ToList();
        if (ids.Count == 0) return new List<DonationRecord>();

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM DonationRecords WHERE DonorId IN @Ids";
        var list = (await conn.QueryAsync<DonationRecord>(sql, new { Ids = ids })).AsList();
        return list;
    }

    public async Task<List<DonationRecord>> GetDonationsForDonorAsync(Guid donorId)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM DonationRecords WHERE DonorId = @DonorId ORDER BY DonationDate DESC";
        var list = (await conn.QueryAsync<DonationRecord>(sql, new { DonorId = donorId })).AsList();
        return list;
    }

    public Task AddDonationAsync(DonationRecord donation)
    {
        _pendingInserts.Add(donation);
        return Task.CompletedTask;
    }

    // ==========================================
    // 6. Blood Requests
    // ==========================================
    public async Task<List<BloodRequest>> GetRequestsAsync(Guid? requesterId = null, string? status = null)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM BloodRequests 
            WHERE (@RequesterId IS NULL OR RequesterId = @RequesterId) 
              AND (@Status IS NULL OR Status = @Status) 
            ORDER BY CreatedAt DESC";

        var list = (await conn.QueryAsync<BloodRequest>(sql, new
        {
            RequesterId = requesterId,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
        })).AsList();

        foreach (var r in list) Track(r);
        return list;
    }

    public async Task<BloodRequest?> FindRequestAsync(Guid id)
    {
        if (_trackedRequests.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM BloodRequests WHERE Id = @Id";
        var req = await conn.QuerySingleOrDefaultAsync<BloodRequest>(sql, new { Id = id });
        Track(req);
        return req;
    }

    public Task AddRequestAsync(BloodRequest request)
    {
        _trackedRequests[request.Id] = request;
        _pendingInserts.Add(request);
        return Task.CompletedTask;
    }

    public Task UpdateRequestAsync(BloodRequest request)
    {
        _trackedRequests[request.Id] = request;
        return Task.CompletedTask;
    }

    // ==========================================
    // 7. Blood Inventory (Standard Depository)
    // ==========================================
    public async Task<BloodInventory?> FindInventoryAsync(string bloodGroup, string bankName)
    {
        var bg = bloodGroup.Trim().ToUpperInvariant();
        var bn = bankName.Trim();

        var tracked = _trackedInventories.Values.FirstOrDefault(i =>
            i.BloodGroup.Equals(bg, StringComparison.OrdinalIgnoreCase) &&
            i.BloodBankName.Equals(bn, StringComparison.OrdinalIgnoreCase));
        if (tracked != null) return tracked;

        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM BloodInventories 
            WHERE UPPER(BloodGroup) = UPPER(@BloodGroup) 
              AND LOWER(BloodBankName) = LOWER(@BankName)";

        var inv = await conn.QueryFirstOrDefaultAsync<BloodInventory>(sql, new { BloodGroup = bg, BankName = bn });
        Track(inv);
        return inv;
    }

    public async Task<List<BloodInventory>> GetInventoryAsync(string? bloodGroup)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM BloodInventories 
            WHERE (@BloodGroup IS NULL OR UPPER(BloodGroup) = UPPER(@BloodGroup)) 
            ORDER BY BloodGroup, QuantityUnits DESC";

        var list = (await conn.QueryAsync<BloodInventory>(sql, new
        {
            BloodGroup = string.IsNullOrWhiteSpace(bloodGroup) ? null : bloodGroup.Trim()
        })).AsList();

        foreach (var i in list) Track(i);
        return list;
    }

    public Task AddInventoryAsync(BloodInventory inventory)
    {
        _trackedInventories[inventory.Id] = inventory;
        _pendingInserts.Add(inventory);
        return Task.CompletedTask;
    }

    public Task UpdateInventoryAsync(BloodInventory inventory)
    {
        _trackedInventories[inventory.Id] = inventory;
        return Task.CompletedTask;
    }

    // ==========================================
    // 8. Component Collections
    // ==========================================
    public async Task<List<ComponentCollection>> GetComponentCollectionsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentCollections ORDER BY CollectionDate DESC";
        var list = (await conn.QueryAsync<ComponentCollection>(sql)).AsList();
        foreach (var c in list) Track(c);
        return list;
    }

    public async Task<ComponentCollection?> FindComponentCollectionAsync(Guid id)
    {
        if (_trackedCollections.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentCollections WHERE Id = @Id";
        var item = await conn.QuerySingleOrDefaultAsync<ComponentCollection>(sql, new { Id = id });
        Track(item);
        return item;
    }

    public async Task<ComponentCollection?> FindComponentCollectionByBarcodeAsync(string barcode)
    {
        var code = barcode.Trim();
        var tracked = _trackedCollections.Values.FirstOrDefault(c => c.BagBarcode.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (tracked != null) return tracked;

        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentCollections WHERE BagBarcode = @Barcode";
        var item = await conn.QueryFirstOrDefaultAsync<ComponentCollection>(sql, new { Barcode = code });
        Track(item);
        return item;
    }

    public Task AddComponentCollectionAsync(ComponentCollection item)
    {
        _trackedCollections[item.Id] = item;
        _pendingInserts.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateComponentCollectionAsync(ComponentCollection item)
    {
        _trackedCollections[item.Id] = item;
        return Task.CompletedTask;
    }

    public void RemoveComponentCollection(ComponentCollection item)
    {
        _trackedCollections.Remove(item.Id);
        _pendingDeletes.Add(("ComponentCollections", item.Id));
    }

    // ==========================================
    // 9. Component Discards
    // ==========================================
    public async Task<List<ComponentDiscard>> GetComponentDiscardsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentDiscards ORDER BY CreatedAt DESC";
        var list = (await conn.QueryAsync<ComponentDiscard>(sql)).AsList();
        foreach (var d in list) Track(d);
        return list;
    }

    public async Task<ComponentDiscard?> FindComponentDiscardAsync(Guid id)
    {
        if (_trackedDiscards.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentDiscards WHERE Id = @Id";
        var item = await conn.QuerySingleOrDefaultAsync<ComponentDiscard>(sql, new { Id = id });
        Track(item);
        return item;
    }

    public Task AddComponentDiscardAsync(ComponentDiscard item)
    {
        _trackedDiscards[item.Id] = item;
        _pendingInserts.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateComponentDiscardAsync(ComponentDiscard item)
    {
        _trackedDiscards[item.Id] = item;
        return Task.CompletedTask;
    }

    public void RemoveComponentDiscard(ComponentDiscard item)
    {
        _trackedDiscards.Remove(item.Id);
        _pendingDeletes.Add(("ComponentDiscards", item.Id));
    }

    // ==========================================
    // 10. Component Returns
    // ==========================================
    public async Task<List<ComponentReturn>> GetComponentReturnsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentReturns ORDER BY ReturnDate DESC";
        var list = (await conn.QueryAsync<ComponentReturn>(sql)).AsList();
        foreach (var r in list) Track(r);
        return list;
    }

    public async Task<ComponentReturn?> FindComponentReturnAsync(Guid id)
    {
        if (_trackedReturns.TryGetValue(id, out var tracked)) return tracked;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ComponentReturns WHERE Id = @Id";
        var item = await conn.QuerySingleOrDefaultAsync<ComponentReturn>(sql, new { Id = id });
        Track(item);
        return item;
    }

    public Task AddComponentReturnAsync(ComponentReturn item)
    {
        _trackedReturns[item.Id] = item;
        _pendingInserts.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateComponentReturnAsync(ComponentReturn item)
    {
        _trackedReturns[item.Id] = item;
        return Task.CompletedTask;
    }

    public void RemoveComponentReturn(ComponentReturn item)
    {
        _trackedReturns.Remove(item.Id);
        _pendingDeletes.Add(("ComponentReturns", item.Id));
    }

    // ==========================================
    // 11. Clear Operational Data
    // ==========================================
    public async Task ClearOperationalDataAsync()
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        const string sql = @"
            DELETE FROM BloodRequests;
            DELETE FROM DonationRecords;
            DELETE FROM DonationCamps;
            DELETE FROM BloodBags;
            DELETE FROM BloodStocks;
            DELETE FROM BloodInventories;
            DELETE FROM ComponentCollections;
            DELETE FROM ComponentDiscards;
            DELETE FROM ComponentReturns;";

        await conn.ExecuteAsync(sql, transaction: tx);
        tx.Commit();

        _trackedStocks.Clear();
        _trackedBags.Clear();
        _trackedInventories.Clear();
        _trackedCamps.Clear();
        _trackedRequests.Clear();
        _trackedCollections.Clear();
        _trackedDiscards.Clear();
        _trackedReturns.Clear();
        _pendingInserts.Clear();
        _pendingDeletes.Clear();
    }

    // ==========================================
    // 12. Save Changes with ACID Transaction
    // ==========================================
    public async Task SaveChangesAsync()
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        // 1. Process Pending Deletes
        foreach (var (table, id) in _pendingDeletes)
        {
            var deleteSql = $"DELETE FROM [{table}] WHERE Id = @Id";
            await conn.ExecuteAsync(deleteSql, new { Id = id }, transaction: tx);
        }
        _pendingDeletes.Clear();

        // 2. Process Pending Inserts
        foreach (var item in _pendingInserts)
        {
            switch (item)
            {
                case User u:
                    const string insertUser = @"
                        INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, Phone, DateOfBirth, Sex, Address, BloodGroup, HospitalName, HospitalRegNumber, IsEmailVerified, CreatedAt)
                        VALUES (@Id, @FullName, @Email, @PasswordHash, @Role, @Phone, @DateOfBirth, @Sex, @Address, @BloodGroup, @HospitalName, @HospitalRegNumber, @IsEmailVerified, @CreatedAt)";
                    await conn.ExecuteAsync(insertUser, u, transaction: tx);
                    break;

                case BloodStock s:
                    const string insertStock = @"
                        INSERT INTO BloodStocks (Id, BloodGroup, ComponentType, QuantityUnits, BloodBankName, LastUpdated)
                        VALUES (@Id, @BloodGroup, @ComponentType, @QuantityUnits, @BloodBankName, @LastUpdated)";
                    await conn.ExecuteAsync(insertStock, s, transaction: tx);
                    break;

                case BloodBag b:
                    const string insertBag = @"
                        INSERT INTO BloodBags (Id, BagNumber, BloodGroup, ComponentType, QuantityUnits, AddedDate, IsIssued, IssuedDate)
                        VALUES (@Id, @BagNumber, @BloodGroup, @ComponentType, @QuantityUnits, @AddedDate, @IsIssued, @IssuedDate)";
                    await conn.ExecuteAsync(insertBag, b, transaction: tx);
                    break;

                case BloodInventory inv:
                    const string insertInv = @"
                        INSERT INTO BloodInventories (Id, BloodGroup, QuantityUnits, BloodBankName, LastUpdated)
                        VALUES (@Id, @BloodGroup, @QuantityUnits, @BloodBankName, @LastUpdated)";
                    await conn.ExecuteAsync(insertInv, inv, transaction: tx);
                    break;

                case DonationCamp c:
                    const string insertCamp = @"
                        INSERT INTO DonationCamps (Id, CampName, Location, CampDate, TargetUnits, CollectedUnits, OrganizedBy, ContactPhone, Status, Notes, CreatedAt)
                        VALUES (@Id, @CampName, @Location, @CampDate, @TargetUnits, @CollectedUnits, @OrganizedBy, @ContactPhone, @Status, @Notes, @CreatedAt)";
                    await conn.ExecuteAsync(insertCamp, c, transaction: tx);
                    break;

                case DonationRecord d:
                    const string insertRecord = @"
                        INSERT INTO DonationRecords (
                            Id, DonorId, DonorName, BloodGroup, UnitsDonated, DonationDate, CampName, BloodBankName,
                            WeightKg, PulsePerMinute, Hemoglobin, Hematocrit, PlateletCount, BloodPressure, TemperatureCelsius,
                            ScreeningStaffName, ScreeningStaffSignature, ScreeningDate, ScreeningTime, MedicalExamAccepted,
                            DeferralReason, MedicalOfficerName, MedicalOfficerSignature, MedicalOfficerDate, DonorNumber,
                            BagType, SegmentNumber, DonationStartTime, DonationEndTime, VenipunctureSite, PhlebotomistName,
                            PhlebotomistSignature, VisualInspectionPassed, DonorConfirmed, FeelsHealthyToday, DonatedRecently,
                            HadDonationDiscomfort, AteRecently, SleptWell, HasInfectionRisk, HasColdFeverOrSinusitis,
                            TookAntibioticsRecently, TookAlcoholRecently, SmokesFrequently, TookMedicationRecently,
                            HadRecentVaccination, HadRecentIllness, HadRecentSurgeryOrTattoo, HadRecentMalariaOrEndemicExposure,
                            HasChronicDisease, IsPregnantOrBreastfeeding, HadRecentAbortion, HasUnsafeSexualExposure,
                            HasHivOrHepatitisRisk, HasPermanentDisqualification
                        ) VALUES (
                            @Id, @DonorId, @DonorName, @BloodGroup, @UnitsDonated, @DonationDate, @CampName, @BloodBankName,
                            @WeightKg, @PulsePerMinute, @Hemoglobin, @Hematocrit, @PlateletCount, @BloodPressure, @TemperatureCelsius,
                            @ScreeningStaffName, @ScreeningStaffSignature, @ScreeningDate, @ScreeningTime, @MedicalExamAccepted,
                            @DeferralReason, @MedicalOfficerName, @MedicalOfficerSignature, @MedicalOfficerDate, @DonorNumber,
                            @BagType, @SegmentNumber, @DonationStartTime, @DonationEndTime, @VenipunctureSite, @PhlebotomistName,
                            @PhlebotomistSignature, @VisualInspectionPassed, @DonorConfirmed, @FeelsHealthyToday, @DonatedRecently,
                            @HadDonationDiscomfort, @AteRecently, @SleptWell, @HasInfectionRisk, @HasColdFeverOrSinusitis,
                            @TookAntibioticsRecently, @TookAlcoholRecently, @SmokesFrequently, @TookMedicationRecently,
                            @HadRecentVaccination, @HadRecentIllness, @HadRecentSurgeryOrTattoo, @HadRecentMalariaOrEndemicExposure,
                            @HasChronicDisease, @IsPregnantOrBreastfeeding, @HadRecentAbortion, @HasUnsafeSexualExposure,
                            @HasHivOrHepatitisRisk, @HasPermanentDisqualification
                        )";
                    await conn.ExecuteAsync(insertRecord, d, transaction: tx);
                    break;

                case BloodRequest r:
                    const string insertReq = @"
                        INSERT INTO BloodRequests (
                            Id, RequesterId, RequesterType, RequesterName, RequesterContact, HospitalName, PatientName,
                            BloodGroup, ComponentType, QuantityUnits, Urgency, Status, HospitalDepartment, MedicalReason,
                            AdminComments, CreatedAt, ProcessedAt, IssuedBagNumbers, PaymentMode, PaymentStatus, TransactionId, AmountPaid
                        ) VALUES (
                            @Id, @RequesterId, @RequesterType, @RequesterName, @RequesterContact, @HospitalName, @PatientName,
                            @BloodGroup, @ComponentType, @QuantityUnits, @Urgency, @Status, @HospitalDepartment, @MedicalReason,
                            @AdminComments, @CreatedAt, @ProcessedAt, @IssuedBagNumbers, @PaymentMode, @PaymentStatus, @TransactionId, @AmountPaid
                        )";
                    await conn.ExecuteAsync(insertReq, r, transaction: tx);
                    break;

                case ComponentCollection cc:
                    const string insertCol = @"
                        INSERT INTO ComponentCollections (Id, ComponentType, BloodGroup, BagType, VolumeMl, BagBarcode, CollectionDate, ExpiryDate, Status)
                        VALUES (@Id, @ComponentType, @BloodGroup, @BagType, @VolumeMl, @BagBarcode, @CollectionDate, @ExpiryDate, @Status)";
                    await conn.ExecuteAsync(insertCol, cc, transaction: tx);
                    break;

                case ComponentDiscard cd:
                    const string insertDis = @"
                        INSERT INTO ComponentDiscards (Id, BagBarcode, Reason, DiscardedBy, ApprovalStatus, ApprovedBy, CreatedAt, ApprovedAt)
                        VALUES (@Id, @BagBarcode, @Reason, @DiscardedBy, @ApprovalStatus, @ApprovedBy, @CreatedAt, @ApprovedAt)";
                    await conn.ExecuteAsync(insertDis, cd, transaction: tx);
                    break;

                case ComponentReturn cr:
                    const string insertRet = @"
                        INSERT INTO ComponentReturns (Id, BagBarcode, Reason, ReturnedBy, ReturnDate)
                        VALUES (@Id, @BagBarcode, @Reason, @ReturnedBy, @ReturnDate)";
                    await conn.ExecuteAsync(insertRet, cr, transaction: tx);
                    break;
            }
        }
        _pendingInserts.Clear();

        // 3. Process Tracked Updates
        foreach (var u in _trackedUsers.Values)
        {
            const string updateUser = @"
                UPDATE Users SET 
                    FullName = @FullName, 
                    Email = @Email, 
                    PasswordHash = @PasswordHash, 
                    Role = @Role, 
                    Phone = @Phone, 
                    DateOfBirth = @DateOfBirth, 
                    Sex = @Sex, 
                    Address = @Address, 
                    BloodGroup = @BloodGroup, 
                    HospitalName = @HospitalName, 
                    HospitalRegNumber = @HospitalRegNumber, 
                    IsEmailVerified = @IsEmailVerified 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateUser, u, transaction: tx);
        }

        foreach (var s in _trackedStocks.Values)
        {
            const string updateStock = @"
                UPDATE BloodStocks SET 
                    BloodGroup = @BloodGroup, 
                    ComponentType = @ComponentType, 
                    QuantityUnits = @QuantityUnits, 
                    BloodBankName = @BloodBankName, 
                    LastUpdated = @LastUpdated 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateStock, s, transaction: tx);
        }

        foreach (var b in _trackedBags.Values)
        {
            const string updateBag = @"
                UPDATE BloodBags SET 
                    BagNumber = @BagNumber, 
                    BloodGroup = @BloodGroup, 
                    ComponentType = @ComponentType, 
                    QuantityUnits = @QuantityUnits, 
                    AddedDate = @AddedDate, 
                    IsIssued = @IsIssued, 
                    IssuedDate = @IssuedDate 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateBag, b, transaction: tx);
        }

        foreach (var inv in _trackedInventories.Values)
        {
            const string updateInv = @"
                UPDATE BloodInventories SET 
                    BloodGroup = @BloodGroup, 
                    QuantityUnits = @QuantityUnits, 
                    BloodBankName = @BloodBankName, 
                    LastUpdated = @LastUpdated 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateInv, inv, transaction: tx);
        }

        foreach (var c in _trackedCamps.Values)
        {
            const string updateCamp = @"
                UPDATE DonationCamps SET 
                    CampName = @CampName, 
                    Location = @Location, 
                    CampDate = @CampDate, 
                    TargetUnits = @TargetUnits, 
                    CollectedUnits = @CollectedUnits, 
                    OrganizedBy = @OrganizedBy, 
                    ContactPhone = @ContactPhone, 
                    Status = @Status, 
                    Notes = @Notes 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateCamp, c, transaction: tx);
        }

        foreach (var r in _trackedRequests.Values)
        {
            const string updateReq = @"
                UPDATE BloodRequests SET 
                    RequesterType = @RequesterType, 
                    RequesterName = @RequesterName, 
                    RequesterContact = @RequesterContact, 
                    HospitalName = @HospitalName, 
                    PatientName = @PatientName, 
                    BloodGroup = @BloodGroup, 
                    ComponentType = @ComponentType, 
                    QuantityUnits = @QuantityUnits, 
                    Urgency = @Urgency, 
                    Status = @Status, 
                    HospitalDepartment = @HospitalDepartment, 
                    MedicalReason = @MedicalReason, 
                    AdminComments = @AdminComments, 
                    ProcessedAt = @ProcessedAt, 
                    IssuedBagNumbers = @IssuedBagNumbers, 
                    PaymentMode = @PaymentMode, 
                    PaymentStatus = @PaymentStatus, 
                    TransactionId = @TransactionId, 
                    AmountPaid = @AmountPaid 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateReq, r, transaction: tx);
        }

        foreach (var cc in _trackedCollections.Values)
        {
            const string updateCol = @"
                UPDATE ComponentCollections SET 
                    ComponentType = @ComponentType, 
                    BloodGroup = @BloodGroup, 
                    BagType = @BagType, 
                    VolumeMl = @VolumeMl, 
                    BagBarcode = @BagBarcode, 
                    CollectionDate = @CollectionDate, 
                    ExpiryDate = @ExpiryDate, 
                    Status = @Status 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateCol, cc, transaction: tx);
        }

        foreach (var cd in _trackedDiscards.Values)
        {
            const string updateDis = @"
                UPDATE ComponentDiscards SET 
                    BagBarcode = @BagBarcode, 
                    Reason = @Reason, 
                    DiscardedBy = @DiscardedBy, 
                    ApprovalStatus = @ApprovalStatus, 
                    ApprovedBy = @ApprovedBy, 
                    ApprovedAt = @ApprovedAt 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateDis, cd, transaction: tx);
        }

        foreach (var cr in _trackedReturns.Values)
        {
            const string updateRet = @"
                UPDATE ComponentReturns SET 
                    BagBarcode = @BagBarcode, 
                    Reason = @Reason, 
                    ReturnedBy = @ReturnedBy, 
                    ReturnDate = @ReturnDate 
                WHERE Id = @Id";
            await conn.ExecuteAsync(updateRet, cr, transaction: tx);
        }

        tx.Commit();
    }
}
