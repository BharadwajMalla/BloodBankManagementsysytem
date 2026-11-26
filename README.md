# Blood Bank A - Single Facility Management System

A dedicated, production-ready system tailored specifically for **Blood Bank A** to manage its internal depository inventory, donation camps, donor directory, and request processing, with dedicated workflows for **Hospitals** and **Donors**.

---

## 🏛️ System Architecture: 3 Distinct Portals & Controllers

```
                                  +-----------------------+
                                  |     BLOOD BANK A      |
                                  |   CENTRAL DATABASE    |
                                  +-----------+-----------+
                                              |
                     +------------------------+------------------------+
                     |                                                 |
                     v                                                 v
         +-----------------------+                         +-----------------------+
         |  BloodBankController  |                         |  HospitalController   |
         |  (Roles: BloodBank)   |                         |   (Roles: Hospital)   |
         +-----------+-----------+                         +-----------+-----------+
                     |                                                 |
   +-----------------+-----------------+               +---------------+---------------+
   |                 |                 |               |                               |
   v                 v                 v               v                               v
[Stock Details] [Camp Details] [Donor Details]  [Request Blood/Components]   [Track Requisitions]
   |
   +-----> [Process Incoming Request Forms]
           (Approve, Fulfill & Deduct Stock, Reject)

                     |
                     v
         +-----------------------+
         |    DonorController    |
         |    (Roles: Donor)     |
         +-----------+-----------+
                     |
         +-----------+-----------+
         |                       |
         v                       v
[Request Blood/Components] [Track My Requests] & [Upcoming Donation Camps]
```

### 1. Blood Bank A Operator (`BloodBankController`)
- **Stock Details**: Live reserves across blood groups (`A+`, `A-`, `B+`, `B-`, `AB+`, `AB-`, `O+`, `O-`) and components (`Whole Blood`, `Red Blood Cells (PRBC)`, `Platelets`, `Fresh Frozen Plasma (FFP)`, `Cryoprecipitate`), with stock replenishment.
- **Camp Details**: Organize and schedule blood collection camps (Camp Name, Venue, Date, Target Units, Status).
- **Donor Details**: Directory of registered donors with contact info, blood groups, total donations, and donation cooldown eligibility.
- **Request Forms**: Process and dispatch requests submitted by Hospitals and Donors with automatic inventory deduction upon fulfillment.

### 2. Hospital Portal (`HospitalController`)
- **Strictly Dedicated to Requisitions**: Hospitals can **only** request blood or blood components from Blood Bank A (Patient Name, Ward/Dept, Blood Group, Component Type, Units, Urgency: Normal / Urgent / Critical).
- **Live Requisition Tracking**: View status (`Pending`, `Approved`, `Fulfilled`, `Rejected`) and remarks from Blood Bank A.
- **Blood Bank A Availability View**: Live read-only stock levels to verify availability before placing an order.

### 3. Donor Portal (`DonorController`)
- **Blood or Component Request**: Donors can submit personal/emergency blood or component requests for themselves or family members.
- **Track My Requests**: Live tracking of submitted requests and admin notes.
- **Upcoming Donation Camps**: Discover upcoming blood donation drives organized by Blood Bank A and pledge to donate.

---

## 🔑 Pre-Seeded Evaluation Credentials

The application includes 1-click login buttons on the login screen with these pre-seeded accounts:

| Portal | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Blood Bank A Admin** | `admin@bloodbankA.org` | `BloodBank@123` | Stock, Camps, Donors, Request Forms (Full Control) |
| **Hospital** | `cityhospital@hospital.org` | `Hospital@123` | Can **only** request blood/components & track orders |
| **Donor** | `john.donor@gmail.com` | `Donor@123` | Request blood/components, track requests, view camps |

---

## Developer setup and verification

The backend targets .NET 10. Run commands below from the repository root in PowerShell.

### Restore, build, and test the backend

```powershell
dotnet restore backend\BloodBank.Application.Tests\BloodBank.Application.Tests.csproj
dotnet build backend\BloodBank.API\BloodBank.API.csproj --no-restore
dotnet test backend\BloodBank.Application.Tests\BloodBank.Application.Tests.csproj --no-restore
```

### Database initialization and migration

The API uses SQL Server only. The Master Data slice applies an additive migration at API startup: existing donor accounts receive globally sequential `UBC-D-000001`-style IDs, existing staff accounts are backfilled into the staff directory, and the new reference tables are created. No tables or existing records are dropped. Startup applies the SQL Server schema script and performs safe conditional column additions/backfills.

For local development on Windows, the default connection uses SQL Server LocalDB. For a hosted or customer installation, store the SQL Server connection string in local .NET User Secrets (never in source control), then run the API in Development:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<database>;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=True" --project backend\BloodBank.API\BloodBank.API.csproj
Set-Location backend\BloodBank.API
dotnet run
```

The API requires the configured SQL Server connection. Startup applies the existing EF migration and SQL Server DDL/backfills. Back up the target database before the first startup migration.

### Backend safety rules

- Donor eligibility is enforced for ages 18–60, weight at least 45 kg, hemoglobin at least 12.5 g/dL, and sex-based donation intervals (male: 90 days; female: 120 days).
- Apheresis records require eligible donor age/weight/Hb/platelets and a 48-hour interval between procedures.
- Bag issuance is atomic: only passed, unexpired, available, and unreserved bags may be issued; reactive TTI screening quarantines the bag.
- HIV/hepatitis/TTI screening records remain in blood-bank-staff-authorized routes. Issue actions and TTI/reactive updates are audited by staff identity and timestamps.
- Donation, request, and apheresis registers receive automatic yearly/monthly serials.

### Run the frontend

```powershell
Set-Location frontend\blood-bank-client
npm ci
npm start
```

To build the frontend, run `npm run build` in `frontend\blood-bank-client`. The frontend currently has no test script.
