CREATE TABLE IF NOT EXISTS DonorIdSequences (
    Id INTEGER NOT NULL PRIMARY KEY,
    NextValue INTEGER NOT NULL
);
INSERT OR IGNORE INTO DonorIdSequences (Id, NextValue) VALUES (1, 1);

CREATE TABLE IF NOT EXISTS Doctors (
    Id TEXT NOT NULL PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    RegistrationNumber TEXT NULL,
    Specialty TEXT NULL,
    HospitalName TEXT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    CreatedBy TEXT NULL,
    UpdatedBy TEXT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Doctors_RegistrationNumber
    ON Doctors(RegistrationNumber) WHERE RegistrationNumber IS NOT NULL;

CREATE TABLE IF NOT EXISTS StaffMembers (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL UNIQUE REFERENCES Users(Id),
    EmployeeNumber TEXT NOT NULL UNIQUE,
    Department TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    CreatedBy TEXT NULL,
    UpdatedBy TEXT NULL
);

CREATE TABLE IF NOT EXISTS Products (
    Id TEXT NOT NULL PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Category TEXT NULL,
    ShelfLifeDays INTEGER NOT NULL,
    Description TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    CreatedBy TEXT NULL,
    UpdatedBy TEXT NULL
);

CREATE TABLE IF NOT EXISTS BagTypes (
    Id TEXT NOT NULL PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    VolumeMl REAL NOT NULL,
    Anticoagulant TEXT NULL,
    Description TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    CreatedBy TEXT NULL,
    UpdatedBy TEXT NULL
);

CREATE TABLE IF NOT EXISTS Suppliers (
    Id TEXT NOT NULL PRIMARY KEY,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    ContactPerson TEXT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    Address TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL,
    CreatedBy TEXT NULL,
    UpdatedBy TEXT NULL
);
