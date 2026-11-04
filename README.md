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

## 🚀 How to Run the Updated Application

### 1. Restart Backend (.NET 10 API)
If the backend is already running in your PowerShell terminal, stop it with **`Ctrl+C`**, then re-run:

```powershell
cd C:\Users\Deepika\.gemini\antigravity\scratch\BloodBankSaaS\backend\BloodBank.API
dotnet run
```
- Swagger UI available at: `http://localhost:5105/swagger` (displays all 3 controllers: `BloodBank`, `Donor`, `Hospital`).

### 2. Run Frontend (Angular Standalone + Tailwind CSS)
In a second terminal window:

```powershell
cd C:\Users\Deepika\.gemini\antigravity\scratch\BloodBankSaaS\frontend\blood-bank-client
npm.cmd install
npm.cmd start
```
- Open `http://localhost:4200` to access the portal.
