IF COL_LENGTH('Users', 'DonorId') IS NULL
    ALTER TABLE Users ADD DonorId nvarchar(50) NULL;
IF COL_LENGTH('Users', 'IsActive') IS NULL
    ALTER TABLE Users ADD IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive_Master DEFAULT 1;
IF COL_LENGTH('Users', 'UpdatedAt') IS NULL
    ALTER TABLE Users ADD UpdatedAt datetime2 NULL;
IF COL_LENGTH('Users', 'CreatedBy') IS NULL
    ALTER TABLE Users ADD CreatedBy nvarchar(200) NULL;
IF COL_LENGTH('Users', 'UpdatedBy') IS NULL
    ALTER TABLE Users ADD UpdatedBy nvarchar(200) NULL;

IF COL_LENGTH('BloodBags', 'Status') IS NULL
    ALTER TABLE BloodBags ADD Status nvarchar(30) NOT NULL CONSTRAINT DF_BloodBags_Status DEFAULT 'Available';
IF COL_LENGTH('BloodBags', 'ExpiryDate') IS NULL
    ALTER TABLE BloodBags ADD ExpiryDate datetime2 NULL;
IF COL_LENGTH('BloodBags', 'ScreeningStatus') IS NULL
    ALTER TABLE BloodBags ADD ScreeningStatus nvarchar(30) NOT NULL CONSTRAINT DF_BloodBags_ScreeningStatus DEFAULT 'Unscreened';
IF COL_LENGTH('BloodBags', 'ReservedUntil') IS NULL
    ALTER TABLE BloodBags ADD ReservedUntil datetime2 NULL;
IF COL_LENGTH('BloodBags', 'IssuedBy') IS NULL
    ALTER TABLE BloodBags ADD IssuedBy nvarchar(150) NULL;
IF COL_LENGTH('BloodBags', 'IssuedAt') IS NULL
    ALTER TABLE BloodBags ADD IssuedAt datetime2 NULL;
IF COL_LENGTH('BloodBags', 'CpdaBatchNumber') IS NULL
    ALTER TABLE BloodBags ADD CpdaBatchNumber nvarchar(100) NULL;
IF COL_LENGTH('ApheresisRecords', 'YearlySlNo') IS NULL
    ALTER TABLE ApheresisRecords ADD YearlySlNo int NULL;
IF COL_LENGTH('ApheresisRecords', 'MonthlySlNo') IS NULL
    ALTER TABLE ApheresisRecords ADD MonthlySlNo int NULL;

IF COL_LENGTH('TtiScreeningRecords', 'HivIResult') IS NULL
    ALTER TABLE TtiScreeningRecords ADD HivIResult nvarchar(20) NOT NULL CONSTRAINT DF_TTI_HivI DEFAULT 'Non-Reactive';
IF COL_LENGTH('TtiScreeningRecords', 'HivIiResult') IS NULL
    ALTER TABLE TtiScreeningRecords ADD HivIiResult nvarchar(20) NOT NULL CONSTRAINT DF_TTI_HivII DEFAULT 'Non-Reactive';
IF COL_LENGTH('TtiScreeningRecords', 'ElisaKitBatch') IS NULL
    ALTER TABLE TtiScreeningRecords ADD ElisaKitBatch nvarchar(100) NULL;
IF COL_LENGTH('TtiScreeningRecords', 'ElisaSignOffBy') IS NULL
    ALTER TABLE TtiScreeningRecords ADD ElisaSignOffBy nvarchar(150) NULL;
IF COL_LENGTH('TtiScreeningRecords', 'ElisaSignOffAt') IS NULL
    ALTER TABLE TtiScreeningRecords ADD ElisaSignOffAt datetime2 NULL;

IF COL_LENGTH('BloodRequests', 'CrossMatchingMethod') IS NULL
    ALTER TABLE BloodRequests ADD CrossMatchingMethod nvarchar(20) NULL;
IF COL_LENGTH('BloodRequests', 'HbGdl') IS NULL
    ALTER TABLE BloodRequests ADD HbGdl decimal(18,2) NULL;
IF COL_LENGTH('BloodRequests', 'PcvPercent') IS NULL
    ALTER TABLE BloodRequests ADD PcvPercent decimal(18,2) NULL;
IF COL_LENGTH('BloodRequests', 'PlateletCount') IS NULL
    ALTER TABLE BloodRequests ADD PlateletCount decimal(18,2) NULL;
IF COL_LENGTH('BloodRequests', 'IndicationDiagnosis') IS NULL
    ALTER TABLE BloodRequests ADD IndicationDiagnosis nvarchar(500) NULL;
IF COL_LENGTH('BloodRequests', 'RequiredResponseTime') IS NULL
    ALTER TABLE BloodRequests ADD RequiredResponseTime nvarchar(30) NULL;
IF COL_LENGTH('BloodRequests', 'RequiredDateTime') IS NULL
    ALTER TABLE BloodRequests ADD RequiredDateTime datetime2 NULL;
IF COL_LENGTH('BloodRequests', 'IssueOrReserve') IS NULL
    ALTER TABLE BloodRequests ADD IssueOrReserve nvarchar(20) NULL;
IF COL_LENGTH('BloodRequests', 'ConsentTakenBy') IS NULL
    ALTER TABLE BloodRequests ADD ConsentTakenBy nvarchar(150) NULL;
IF COL_LENGTH('BloodRequests', 'ConsentDate') IS NULL
    ALTER TABLE BloodRequests ADD ConsentDate datetime2 NULL;
IF COL_LENGTH('BloodRequests', 'ConsentForIssue') IS NULL
    ALTER TABLE BloodRequests ADD ConsentForIssue bit NOT NULL CONSTRAINT DF_BloodRequests_Consent DEFAULT 0;
IF COL_LENGTH('BloodRequests', 'BloodBankUseOnly') IS NULL
    ALTER TABLE BloodRequests ADD BloodBankUseOnly bit NOT NULL CONSTRAINT DF_BloodRequests_Use DEFAULT 0;
IF COL_LENGTH('BloodRequests', 'IssueSerial') IS NULL
    ALTER TABLE BloodRequests ADD IssueSerial nvarchar(50) NULL;
IF COL_LENGTH('BloodRequests', 'ReceiverName') IS NULL
    ALTER TABLE BloodRequests ADD ReceiverName nvarchar(150) NULL;
IF COL_LENGTH('BloodRequests', 'IssuedByName') IS NULL
    ALTER TABLE BloodRequests ADD IssuedByName nvarchar(150) NULL;
IF COL_LENGTH('BloodRequests', 'BloodBankOfficerSignOff') IS NULL
    ALTER TABLE BloodRequests ADD BloodBankOfficerSignOff nvarchar(150) NULL;

IF COL_LENGTH('DonationRecords', 'YearlySlNo') IS NULL ALTER TABLE DonationRecords ADD YearlySlNo int NULL;
IF COL_LENGTH('DonationRecords', 'MonthlySlNo') IS NULL ALTER TABLE DonationRecords ADD MonthlySlNo int NULL;
IF COL_LENGTH('DonationRecords', 'DonationCategory') IS NULL ALTER TABLE DonationRecords ADD DonationCategory nvarchar(30) NOT NULL CONSTRAINT DF_DonationCategory DEFAULT 'Voluntary';
IF COL_LENGTH('DonationRecords', 'ReplacementPatientName') IS NULL ALTER TABLE DonationRecords ADD ReplacementPatientName nvarchar(150) NULL;
IF COL_LENGTH('DonationRecords', 'ReplacementPatientHospital') IS NULL ALTER TABLE DonationRecords ADD ReplacementPatientHospital nvarchar(150) NULL;
IF COL_LENGTH('DonationRecords', 'ReplacementPatientIpNo') IS NULL ALTER TABLE DonationRecords ADD ReplacementPatientIpNo nvarchar(50) NULL;
IF COL_LENGTH('DonationRecords', 'BagNumber') IS NULL ALTER TABLE DonationRecords ADD BagNumber nvarchar(50) NULL;
IF COL_LENGTH('DonationRecords', 'HbTestMethod') IS NULL ALTER TABLE DonationRecords ADD HbTestMethod nvarchar(50) NULL;
IF COL_LENGTH('DonationRecords', 'HbEligibilityStatus') IS NULL ALTER TABLE DonationRecords ADD HbEligibilityStatus nvarchar(30) NULL;
IF COL_LENGTH('DonationRecords', 'DeferralRecords') IS NULL ALTER TABLE DonationRecords ADD DeferralRecords nvarchar(500) NULL;
IF COL_LENGTH('DonationRecords', 'CpdaBatchNumber') IS NULL ALTER TABLE DonationRecords ADD CpdaBatchNumber nvarchar(100) NULL;
IF COL_LENGTH('DonationRecords', 'WeightKg') IS NULL ALTER TABLE DonationRecords ADD WeightKg decimal(18,2) NULL;
IF COL_LENGTH('DonationRecords', 'PulsePerMinute') IS NULL ALTER TABLE DonationRecords ADD PulsePerMinute int NULL;
IF COL_LENGTH('DonationRecords', 'Hemoglobin') IS NULL ALTER TABLE DonationRecords ADD Hemoglobin decimal(18,2) NULL;
IF COL_LENGTH('DonationRecords', 'Hematocrit') IS NULL ALTER TABLE DonationRecords ADD Hematocrit decimal(18,2) NULL;
IF COL_LENGTH('DonationRecords', 'PlateletCount') IS NULL ALTER TABLE DonationRecords ADD PlateletCount int NULL;
IF COL_LENGTH('DonationRecords', 'BloodPressure') IS NULL ALTER TABLE DonationRecords ADD BloodPressure nvarchar(30) NULL;
IF COL_LENGTH('DonationRecords', 'TemperatureCelsius') IS NULL ALTER TABLE DonationRecords ADD TemperatureCelsius decimal(18,2) NULL;
IF COL_LENGTH('DonationRecords', 'ScreeningStaffName') IS NULL ALTER TABLE DonationRecords ADD ScreeningStaffName nvarchar(150) NULL;
IF COL_LENGTH('DonationRecords', 'ScreeningStaffSignature') IS NULL ALTER TABLE DonationRecords ADD ScreeningStaffSignature nvarchar(200) NULL;
IF COL_LENGTH('DonationRecords', 'ScreeningDate') IS NULL ALTER TABLE DonationRecords ADD ScreeningDate datetime2 NULL;
IF COL_LENGTH('DonationRecords', 'ScreeningTime') IS NULL ALTER TABLE DonationRecords ADD ScreeningTime nvarchar(20) NULL;
IF COL_LENGTH('DonationRecords', 'MedicalExamAccepted') IS NULL ALTER TABLE DonationRecords ADD MedicalExamAccepted bit NOT NULL CONSTRAINT DF_Donation_Medical DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'DeferralReason') IS NULL ALTER TABLE DonationRecords ADD DeferralReason nvarchar(500) NULL;
IF COL_LENGTH('DonationRecords', 'MedicalOfficerName') IS NULL ALTER TABLE DonationRecords ADD MedicalOfficerName nvarchar(150) NULL;
IF COL_LENGTH('DonationRecords', 'MedicalOfficerSignature') IS NULL ALTER TABLE DonationRecords ADD MedicalOfficerSignature nvarchar(200) NULL;
IF COL_LENGTH('DonationRecords', 'MedicalOfficerDate') IS NULL ALTER TABLE DonationRecords ADD MedicalOfficerDate datetime2 NULL;
IF COL_LENGTH('DonationRecords', 'DonorNumber') IS NULL ALTER TABLE DonationRecords ADD DonorNumber nvarchar(100) NULL;
IF COL_LENGTH('DonationRecords', 'BagType') IS NULL ALTER TABLE DonationRecords ADD BagType nvarchar(50) NULL;
IF COL_LENGTH('DonationRecords', 'SegmentNumber') IS NULL ALTER TABLE DonationRecords ADD SegmentNumber nvarchar(100) NULL;
IF COL_LENGTH('DonationRecords', 'DonationStartTime') IS NULL ALTER TABLE DonationRecords ADD DonationStartTime nvarchar(20) NULL;
IF COL_LENGTH('DonationRecords', 'DonationEndTime') IS NULL ALTER TABLE DonationRecords ADD DonationEndTime nvarchar(20) NULL;
IF COL_LENGTH('DonationRecords', 'VenipunctureSite') IS NULL ALTER TABLE DonationRecords ADD VenipunctureSite nvarchar(50) NULL;
IF COL_LENGTH('DonationRecords', 'PhlebotomistName') IS NULL ALTER TABLE DonationRecords ADD PhlebotomistName nvarchar(150) NULL;
IF COL_LENGTH('DonationRecords', 'PhlebotomistSignature') IS NULL ALTER TABLE DonationRecords ADD PhlebotomistSignature nvarchar(200) NULL;
IF COL_LENGTH('DonationRecords', 'VisualInspectionPassed') IS NULL ALTER TABLE DonationRecords ADD VisualInspectionPassed bit NOT NULL CONSTRAINT DF_Donation_Visual DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'DonorConfirmed') IS NULL ALTER TABLE DonationRecords ADD DonorConfirmed bit NOT NULL CONSTRAINT DF_Donation_Confirmed DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'FeelsHealthyToday') IS NULL ALTER TABLE DonationRecords ADD FeelsHealthyToday bit NOT NULL CONSTRAINT DF_Donation_Healthy DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'DonatedRecently') IS NULL ALTER TABLE DonationRecords ADD DonatedRecently bit NOT NULL CONSTRAINT DF_Donation_Recent DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadDonationDiscomfort') IS NULL ALTER TABLE DonationRecords ADD HadDonationDiscomfort bit NOT NULL CONSTRAINT DF_Donation_Discomfort DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'AteRecently') IS NULL ALTER TABLE DonationRecords ADD AteRecently bit NOT NULL CONSTRAINT DF_Donation_Ate DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'SleptWell') IS NULL ALTER TABLE DonationRecords ADD SleptWell bit NOT NULL CONSTRAINT DF_Donation_Sleep DEFAULT 1;
IF COL_LENGTH('DonationRecords', 'HasInfectionRisk') IS NULL ALTER TABLE DonationRecords ADD HasInfectionRisk bit NOT NULL CONSTRAINT DF_Donation_Infection DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HasColdFeverOrSinusitis') IS NULL ALTER TABLE DonationRecords ADD HasColdFeverOrSinusitis bit NOT NULL CONSTRAINT DF_Donation_Cold DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'TookAntibioticsRecently') IS NULL ALTER TABLE DonationRecords ADD TookAntibioticsRecently bit NOT NULL CONSTRAINT DF_Donation_Antibiotics DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'TookAlcoholRecently') IS NULL ALTER TABLE DonationRecords ADD TookAlcoholRecently bit NOT NULL CONSTRAINT DF_Donation_Alcohol DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'SmokesFrequently') IS NULL ALTER TABLE DonationRecords ADD SmokesFrequently bit NOT NULL CONSTRAINT DF_Donation_Smoke DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'TookMedicationRecently') IS NULL ALTER TABLE DonationRecords ADD TookMedicationRecently bit NOT NULL CONSTRAINT DF_Donation_Medication DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadRecentVaccination') IS NULL ALTER TABLE DonationRecords ADD HadRecentVaccination bit NOT NULL CONSTRAINT DF_Donation_Vaccine DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadRecentIllness') IS NULL ALTER TABLE DonationRecords ADD HadRecentIllness bit NOT NULL CONSTRAINT DF_Donation_Illness DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadRecentSurgeryOrTattoo') IS NULL ALTER TABLE DonationRecords ADD HadRecentSurgeryOrTattoo bit NOT NULL CONSTRAINT DF_Donation_Surgery DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadRecentMalariaOrEndemicExposure') IS NULL ALTER TABLE DonationRecords ADD HadRecentMalariaOrEndemicExposure bit NOT NULL CONSTRAINT DF_Donation_Malaria DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HasChronicDisease') IS NULL ALTER TABLE DonationRecords ADD HasChronicDisease bit NOT NULL CONSTRAINT DF_Donation_Chronic DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'IsPregnantOrBreastfeeding') IS NULL ALTER TABLE DonationRecords ADD IsPregnantOrBreastfeeding bit NOT NULL CONSTRAINT DF_Donation_Pregnant DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HadRecentAbortion') IS NULL ALTER TABLE DonationRecords ADD HadRecentAbortion bit NOT NULL CONSTRAINT DF_Donation_Abortion DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HasUnsafeSexualExposure') IS NULL ALTER TABLE DonationRecords ADD HasUnsafeSexualExposure bit NOT NULL CONSTRAINT DF_Donation_Sex DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HasHivOrHepatitisRisk') IS NULL ALTER TABLE DonationRecords ADD HasHivOrHepatitisRisk bit NOT NULL CONSTRAINT DF_Donation_Hiv DEFAULT 0;
IF COL_LENGTH('DonationRecords', 'HasPermanentDisqualification') IS NULL ALTER TABLE DonationRecords ADD HasPermanentDisqualification bit NOT NULL CONSTRAINT DF_Donation_Disqualified DEFAULT 0;

EXEC sys.sp_executesql N'
    UPDATE BloodBags
    SET ExpiryDate = CASE
        WHEN LOWER(COALESCE(ComponentType, '''')) LIKE ''%platelet%'' THEN DATEADD(DAY, 5, COALESCE(AddedDate, GETUTCDATE()))
        WHEN LOWER(COALESCE(ComponentType, '''')) LIKE ''%whole%'' THEN DATEADD(DAY, 35, COALESCE(AddedDate, GETUTCDATE()))
        WHEN LOWER(COALESCE(ComponentType, '''')) LIKE ''%plasma%'' THEN DATEADD(DAY, 365, COALESCE(AddedDate, GETUTCDATE()))
        ELSE DATEADD(DAY, 42, COALESCE(AddedDate, GETUTCDATE()))
    END
    WHERE ExpiryDate IS NULL;';

IF OBJECT_ID('DonorIdSequences', 'U') IS NULL
    CREATE TABLE DonorIdSequences (Id int NOT NULL PRIMARY KEY, NextValue bigint NOT NULL);
IF NOT EXISTS (SELECT 1 FROM DonorIdSequences WHERE Id = 1)
    INSERT INTO DonorIdSequences (Id, NextValue) VALUES (1, 1);

IF OBJECT_ID('Doctors', 'U') IS NULL
    CREATE TABLE Doctors (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(150) NOT NULL,
        RegistrationNumber nvarchar(100) NULL,
        Specialty nvarchar(100) NULL,
        HospitalName nvarchar(150) NULL,
        Phone nvarchar(30) NULL,
        Email nvarchar(200) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        CreatedBy nvarchar(200) NULL,
        UpdatedBy nvarchar(200) NULL
    );
IF OBJECT_ID('StaffMembers', 'U') IS NULL
    CREATE TABLE StaffMembers (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        UserId uniqueidentifier NOT NULL UNIQUE,
        EmployeeNumber nvarchar(50) NOT NULL UNIQUE,
        Department nvarchar(100) NOT NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        CreatedBy nvarchar(200) NULL,
        UpdatedBy nvarchar(200) NULL,
        CONSTRAINT FK_StaffMembers_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
IF OBJECT_ID('Products', 'U') IS NULL
    CREATE TABLE Products (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL UNIQUE,
        Name nvarchar(150) NOT NULL,
        Category nvarchar(100) NULL,
        ShelfLifeDays int NOT NULL,
        Description nvarchar(500) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        CreatedBy nvarchar(200) NULL,
        UpdatedBy nvarchar(200) NULL
    );
IF OBJECT_ID('BagTypes', 'U') IS NULL
    CREATE TABLE BagTypes (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL UNIQUE,
        Name nvarchar(150) NOT NULL,
        VolumeMl decimal(18,2) NOT NULL,
        Anticoagulant nvarchar(100) NULL,
        Description nvarchar(500) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        CreatedBy nvarchar(200) NULL,
        UpdatedBy nvarchar(200) NULL
    );
IF OBJECT_ID('Suppliers', 'U') IS NULL
    CREATE TABLE Suppliers (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL UNIQUE,
        Name nvarchar(200) NOT NULL,
        ContactPerson nvarchar(150) NULL,
        Phone nvarchar(30) NULL,
        Email nvarchar(200) NULL,
        Address nvarchar(500) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        CreatedBy nvarchar(200) NULL,
        UpdatedBy nvarchar(200) NULL
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_DonorId' AND object_id = OBJECT_ID('Users'))
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX IX_Users_DonorId ON Users(DonorId) WHERE DonorId IS NOT NULL';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Doctors_Code' AND object_id = OBJECT_ID('Doctors'))
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX IX_Doctors_Code ON Doctors(Code)';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Doctors_RegistrationNumber' AND object_id = OBJECT_ID('Doctors'))
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX IX_Doctors_RegistrationNumber ON Doctors(RegistrationNumber) WHERE RegistrationNumber IS NOT NULL';
