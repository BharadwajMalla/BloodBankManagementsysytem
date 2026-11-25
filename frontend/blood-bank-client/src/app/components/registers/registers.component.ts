import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import {
  BLOOD_GROUPS,
  AddStockPayload,
  CreateApheresisRecordPayload,
  CreateBloodBagStockRegisterPayload,
  CreateBloodRequestPayload,
  CreateComponentCollectionPayload,
  CreateComponentDiscardPayload,
  CreateComponentReturnPayload,
  CompleteDonationPayload,
  CreateDiagnosticKitRegisterPayload,
  CreateDonorPayload,
  CreatePatientGroupingPayload,
  CreateTtiScreeningPayload,
  DonorDetails
} from '../../core/models/blood-bank.model';
import { BloodBankService } from '../../core/services/blood-bank.service';
import { AuthService } from '../../core/services/auth.service';
import { RegistersService } from '../../core/services/registers.service';

export type RegisterKey =
  | 'tti' | 'grouping' | 'bag-stock' | 'diagnostic-kits' | 'apheresis'
  | 'master-record' | 'requisitions' | 'collections' | 'discards' | 'returns'
  | 'issues' | 'bags' | 'donors' | 'donations';

type FieldType = 'text' | 'email' | 'tel' | 'number' | 'date' | 'time' | 'textarea' | 'select' | 'checkbox';

interface RegisterField {
  key: string;
  label: string;
  type?: FieldType;
  required?: boolean;
  min?: number;
  max?: number;
  step?: number;
  options?: string[];
  initial?: string | number | boolean;
}

interface RegisterColumn {
  key: string;
  label: string;
}

interface RegisterRow {
  id: string;
  [key: string]: unknown;
}

const yesNo = ['Non-Reactive', 'Reactive', 'Inconclusive', 'Negative', 'Positive'];
const groupingResult = ['+', '-', 'Positive', 'Negative', 'Not tested'];

const fieldSets: Partial<Record<RegisterKey, RegisterField[]>> = {
  tti: [
    { key: 'bagBarcode', label: 'Bag barcode', required: true },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'testingDate', label: 'Testing date', type: 'date', required: true },
    { key: 'hivResult', label: 'HIV result', type: 'select', required: true, options: yesNo, initial: 'Non-Reactive' },
    { key: 'hbsAgResult', label: 'HBsAg result', type: 'select', required: true, options: yesNo, initial: 'Non-Reactive' },
    { key: 'hcvResult', label: 'HCV result', type: 'select', required: true, options: yesNo, initial: 'Non-Reactive' },
    { key: 'vdrlResult', label: 'VDRL result', type: 'select', required: true, options: yesNo, initial: 'Non-Reactive' },
    { key: 'malariaResult', label: 'Malaria result', type: 'select', required: true, options: ['Negative', 'Positive', 'Inconclusive'], initial: 'Negative' },
    { key: 'atypicalAntibodies', label: 'Atypical antibodies', type: 'select', required: true, options: yesNo, initial: 'Negative' },
    { key: 'overallStatus', label: 'Overall status', type: 'select', required: true, options: ['Passed', 'Failed', 'Pending'], initial: 'Passed' },
    { key: 'testedBy', label: 'Tested by', required: true },
    { key: 'approvedBy', label: 'Approved by' },
    { key: 'remarks', label: 'Remarks', type: 'textarea' }
  ],
  grouping: [
    { key: 'slNo', label: 'Serial number', type: 'number', min: 0 },
    { key: 'date', label: 'Date', type: 'date', required: true },
    { key: 'patientName', label: 'Patient name', required: true },
    { key: 'ageSex', label: 'Age / sex' },
    { key: 'hospitalName', label: 'Hospital' },
    { key: 'ipNumber', label: 'IP number' },
    { key: 'bagNumber', label: 'Bag number' },
    { key: 'bagType', label: 'Bag type' },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'rhType', label: 'Rh type', type: 'select', required: true, options: ['Positive', 'Negative'] },
    ...['antiA', 'antiB', 'antiAb', 'antiD', 'lectinA1', 'lectinH', 'aCells', 'bCells', 'oCells'].map(key => ({
      key, label: labelFor(key), type: 'select' as const, options: groupingResult
    })),
    { key: 'duTest', label: 'Du test', type: 'select', options: ['Not tested', ...yesNo] },
    { key: 'irregularAntibodies', label: 'Irregular antibodies', type: 'select', options: ['Not tested', ...yesNo] },
    { key: 'autoAntibodies', label: 'Auto antibodies', type: 'select', options: ['Not tested', ...yesNo] },
    { key: 'crossmatchSaline', label: 'Crossmatch saline', type: 'select', options: ['Compatible', 'Incompatible', 'Not tested'] },
    { key: 'crossmatchAlbumin', label: 'Crossmatch albumin', type: 'select', options: ['Compatible', 'Incompatible', 'Not tested'] },
    { key: 'crossmatchAhg', label: 'Crossmatch AHG', type: 'select', options: ['Compatible', 'Incompatible', 'Not tested'] }
  ],
  'bag-stock': [
    { key: 'slNo', label: 'Serial number', type: 'number', min: 0 },
    { key: 'date', label: 'Register date', type: 'date', required: true },
    { key: 'supplierName', label: 'Supplier', required: true },
    { key: 'invoiceNo', label: 'Invoice number', required: true },
    { key: 'invoiceDate', label: 'Invoice date', type: 'date', required: true },
    { key: 'manufacturer', label: 'Manufacturer', required: true },
    { key: 'batchNo', label: 'Batch number', required: true },
    { key: 'dateOfSupply', label: 'Date of supply', type: 'date', required: true },
    { key: 'dateOfManufacture', label: 'Manufacture date', type: 'date', required: true },
    { key: 'dateOfExpiry', label: 'Expiry date', type: 'date', required: true },
    { key: 'codeSpecification', label: 'Code / specification', required: true, initial: 'CPDA-1 Triple (450ml)' },
    { key: 'resultCoa', label: 'COA result', initial: 'Satisfactory' },
    ...['openingStock', 'quantityReceived', 'quantityUsed', 'wastage', 'balance'].map(key => ({
      key, label: labelFor(key), type: 'number' as const, min: 0
    })),
    { key: 'remarks', label: 'Remarks', type: 'textarea' }
  ],
  'diagnostic-kits': [
    { key: 'slNo', label: 'Serial number', type: 'number', min: 0 },
    { key: 'date', label: 'Register date', type: 'date', required: true },
    { key: 'supplierName', label: 'Supplier', required: true },
    { key: 'kitName', label: 'Kit / reagent name', required: true },
    { key: 'manufacturer', label: 'Manufacturer', required: true },
    { key: 'invoiceNo', label: 'Invoice number', required: true },
    { key: 'batchNo', label: 'Batch number', required: true },
    { key: 'dateOfManufacture', label: 'Manufacture date', type: 'date', required: true },
    { key: 'dateOfExpiry', label: 'Expiry date', type: 'date', required: true },
    ...['quantityReceived', 'openingBalance', 'noOfTests', 'controlsUsed', 'wastage', 'closingBalance'].map(key => ({
      key, label: labelFor(key), type: 'number' as const, min: 0
    })),
    { key: 'remarks', label: 'Remarks', type: 'textarea' }
  ],
  apheresis: [
    { key: 'registrationNo', label: 'Registration number', required: true },
    { key: 'date', label: 'Date', type: 'date', required: true },
    { key: 'donorName', label: 'Donor name', required: true },
    { key: 'ageSex', label: 'Age / sex' },
    { key: 'weightKg', label: 'Weight (kg)', type: 'number', min: 0, step: 0.1 },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'phone', label: 'Phone', type: 'tel' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'timeOfMeal', label: 'Time of meal', type: 'time' },
    { key: 'patientName', label: 'Patient name' },
    { key: 'patientIpNo', label: 'Patient IP number' },
    { key: 'patientBedNo', label: 'Patient bed number' },
    { key: 'patientBloodGroup', label: 'Patient blood group', type: 'select', options: BLOOD_GROUPS },
    { key: 'takenAspirinLast3Days', label: 'Took aspirin in last 3 days', type: 'checkbox' },
    { key: 'haemoglobinGms', label: 'Haemoglobin (g/dL)', type: 'number', min: 0, step: 0.1 },
    { key: 'plateletCountLakhs', label: 'Platelet count (lakhs)', type: 'number', min: 0, step: 0.01 },
    { key: 'wbcCount', label: 'WBC count', type: 'number', min: 0 },
    { key: 'bloodPressure', label: 'Blood pressure' },
    { key: 'pulse', label: 'Pulse', type: 'number', min: 0 },
    { key: 'temperatureCelsius', label: 'Temperature (°C)', type: 'number', step: 0.1 },
    { key: 'totalProteinsGms', label: 'Total proteins (g/dL)', type: 'number', min: 0, step: 0.1 },
    { key: 'procedureStartTime', label: 'Procedure start', type: 'time' },
    { key: 'procedureEndTime', label: 'Procedure end', type: 'time' },
    { key: 'durationMinutes', label: 'Duration (minutes)', type: 'number', min: 0 },
    { key: 'venipunctureSite', label: 'Venipuncture site', required: true },
    { key: 'wholeBloodPumpRateMlMin', label: 'Pump rate (mL/min)', type: 'number', min: 0, step: 0.1 },
    { key: 'acdRatio', label: 'ACD ratio', required: true },
    { key: 'totalBloodVolumeProcessedMl', label: 'Blood processed (mL)', type: 'number', min: 0, step: 0.1 },
    { key: 'totalProductCollectedMl', label: 'Product collected (mL)', type: 'number', min: 0, step: 0.1 },
    { key: 'totalVolumeAcdUsedMl', label: 'ACD used (mL)', type: 'number', min: 0, step: 0.1 },
    { key: 'adverseReaction', label: 'Adverse reaction', type: 'textarea' },
    { key: 'qualityControlPassed', label: 'Quality control passed', type: 'checkbox' },
    { key: 'remarks', label: 'Remarks', type: 'textarea' }
  ],
  bags: [
    { key: 'bagNumber', label: 'Bag number', required: true },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'componentType', label: 'Component type', type: 'select', required: true, options: ['Whole Blood', 'Red Blood Cells (PRBC)', 'Platelets', 'Fresh Frozen Plasma (FFP)', 'Cryoprecipitate'] },
    { key: 'addedDate', label: 'Date added', type: 'date', required: true }
  ],
  collections: [
    { key: 'componentType', label: 'Component type', required: true },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'bagType', label: 'Bag type', required: true },
    { key: 'volumeMl', label: 'Volume (mL)', type: 'number', required: true, min: 0.1, step: 0.1 },
    { key: 'bagBarcode', label: 'Bag barcode', required: true },
    { key: 'collectionDate', label: 'Collection date', type: 'date', required: true },
    { key: 'expiryDate', label: 'Expiry date', type: 'date', required: true }
  ],
  discards: [
    { key: 'bagBarcode', label: 'Bag barcode', required: true },
    { key: 'reason', label: 'Reason', type: 'textarea', required: true },
    { key: 'discardedBy', label: 'Discarded by', required: true }
  ],
  returns: [
    { key: 'bagBarcode', label: 'Bag barcode', required: true },
    { key: 'reason', label: 'Reason', type: 'textarea', required: true },
    { key: 'returnedBy', label: 'Returned by', required: true }
  ],
  requisitions: [
    { key: 'patientName', label: 'Patient name', required: true },
    { key: 'patientAge', label: 'Patient age', type: 'number', required: true, min: 0, max: 1440 },
    { key: 'patientAgeUnit', label: 'Age unit', type: 'select', required: true, options: ['Years', 'Months'], initial: 'Years' },
    { key: 'patientSex', label: 'Patient sex', type: 'select', required: true, options: ['Male', 'Female', 'Other', 'Unknown'] },
    { key: 'isThalassemiaPatient', label: 'Thalassemia patient', type: 'checkbox' },
    { key: 'hospitalName', label: 'Hospital name' },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS },
    { key: 'componentType', label: 'Component type', type: 'select', required: true, options: ['Whole Blood', 'Red Blood Cells (PRBC)', 'Platelets', 'Fresh Frozen Plasma (FFP)', 'Cryoprecipitate'] },
    { key: 'quantityUnits', label: 'Units', type: 'number', required: true, min: 1 },
    { key: 'urgency', label: 'Urgency', type: 'select', required: true, options: ['Normal', 'Urgent', 'Critical'] },
    { key: 'hospitalDepartment', label: 'Department' },
    { key: 'medicalReason', label: 'Medical reason', type: 'textarea' }
  ],
  donations: [
    { key: 'donorConfirmed', label: 'Donor confirmed', type: 'checkbox' },
    { key: 'weightKg', label: 'Weight (kg)', type: 'number', min: 0, step: 0.1 },
    { key: 'pulsePerMinute', label: 'Pulse (per minute)', type: 'number', min: 0 },
    { key: 'hemoglobin', label: 'Hemoglobin', type: 'number', min: 0, step: 0.1 },
    { key: 'bloodPressure', label: 'Blood pressure' },
    { key: 'temperatureCelsius', label: 'Temperature (°C)', type: 'number', step: 0.1 },
    { key: 'screeningStaffName', label: 'Screening staff' },
    { key: 'screeningDate', label: 'Screening date', type: 'date' },
    { key: 'screeningTime', label: 'Screening time', type: 'time' },
    { key: 'medicalExamAccepted', label: 'Medical examination accepted', type: 'checkbox' },
    { key: 'deferralReason', label: 'Deferral reason', type: 'textarea' },
    { key: 'medicalOfficerName', label: 'Medical officer' },
    { key: 'donorNumber', label: 'Donor number' },
    { key: 'bagType', label: 'Bag type' },
    { key: 'segmentNumber', label: 'Segment number' },
    { key: 'donationStartTime', label: 'Donation start time', type: 'time' },
    { key: 'donationEndTime', label: 'Donation end time', type: 'time' },
    { key: 'venipunctureSite', label: 'Venipuncture site' },
    { key: 'phlebotomistName', label: 'Phlebotomist' },
    { key: 'visualInspectionPassed', label: 'Visual inspection passed', type: 'checkbox' },
    { key: 'feelsHealthyToday', label: 'Feels healthy today', type: 'checkbox' },
    { key: 'donatedRecently', label: 'Donated recently', type: 'checkbox' },
    { key: 'hadDonationDiscomfort', label: 'Donation discomfort', type: 'checkbox' },
    { key: 'ateRecently', label: 'Ate recently', type: 'checkbox' },
    { key: 'sleptWell', label: 'Slept well', type: 'checkbox' },
    { key: 'hasInfectionRisk', label: 'Infection risk', type: 'checkbox' },
    { key: 'hasColdFeverOrSinusitis', label: 'Cold, fever or sinusitis', type: 'checkbox' },
    { key: 'tookAntibioticsRecently', label: 'Recent antibiotics', type: 'checkbox' },
    { key: 'tookAlcoholRecently', label: 'Recent alcohol use', type: 'checkbox' },
    { key: 'smokesFrequently', label: 'Smokes frequently', type: 'checkbox' },
    { key: 'tookMedicationRecently', label: 'Recent medication', type: 'checkbox' },
    { key: 'hadRecentVaccination', label: 'Recent vaccination', type: 'checkbox' },
    { key: 'hadRecentIllness', label: 'Recent illness', type: 'checkbox' },
    { key: 'hadRecentSurgeryOrTattoo', label: 'Recent surgery or tattoo', type: 'checkbox' },
    { key: 'hadRecentMalariaOrEndemicExposure', label: 'Malaria / endemic exposure', type: 'checkbox' },
    { key: 'hasChronicDisease', label: 'Chronic disease', type: 'checkbox' },
    { key: 'isPregnantOrBreastfeeding', label: 'Pregnant or breastfeeding', type: 'checkbox' },
    { key: 'hadRecentAbortion', label: 'Recent abortion', type: 'checkbox' },
    { key: 'hasUnsafeSexualExposure', label: 'Unsafe sexual exposure', type: 'checkbox' },
    { key: 'hasHivOrHepatitisRisk', label: 'HIV / hepatitis risk', type: 'checkbox' },
    { key: 'hasPermanentDisqualification', label: 'Permanent disqualification', type: 'checkbox' },
    { key: 'unitsDonated', label: 'Units donated', type: 'number', required: true, min: 1, initial: 1 },
    { key: 'campName', label: 'Camp name' }
  ],
  donors: [
    { key: 'firstName', label: 'First name', required: true },
    { key: 'lastName', label: 'Last name', required: true },
    { key: 'email', label: 'Email', type: 'email', required: true },
    { key: 'phone', label: 'Phone', type: 'tel', required: true },
    { key: 'dateOfBirth', label: 'Date of birth', type: 'date', required: true },
    { key: 'sex', label: 'Sex', required: true },
    { key: 'address', label: 'Address', type: 'textarea', required: true },
    { key: 'bloodGroup', label: 'Blood group', type: 'select', required: true, options: BLOOD_GROUPS }
  ]
};

const columns: Record<RegisterKey, RegisterColumn[]> = {
  tti: [
    { key: 'bagBarcode', label: 'Bag barcode' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'testingDate', label: 'Testing date' }, { key: 'hivResult', label: 'HIV' },
    { key: 'hbsAgResult', label: 'HBsAg' }, { key: 'hcvResult', label: 'HCV' },
    { key: 'vdrlResult', label: 'VDRL' }, { key: 'malariaResult', label: 'Malaria' },
    { key: 'overallStatus', label: 'Overall status' }, { key: 'testedBy', label: 'Tested by' }
  ],
  grouping: [
    { key: 'slNo', label: 'Sl. No.' }, { key: 'date', label: 'Date' },
    { key: 'patientName', label: 'Patient' }, { key: 'hospitalName', label: 'Hospital' },
    { key: 'ipNumber', label: 'IP number' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'rhType', label: 'Rh' }, { key: 'crossmatchAhg', label: 'AHG crossmatch' }
  ],
  'bag-stock': [
    { key: 'slNo', label: 'Sl. No.' }, { key: 'date', label: 'Date' },
    { key: 'supplierName', label: 'Supplier' }, { key: 'invoiceNo', label: 'Invoice' },
    { key: 'batchNo', label: 'Batch' }, { key: 'dateOfExpiry', label: 'Expiry' },
    { key: 'quantityReceived', label: 'Received' }, { key: 'balance', label: 'Balance' }
  ],
  'diagnostic-kits': [
    { key: 'slNo', label: 'Sl. No.' }, { key: 'date', label: 'Date' },
    { key: 'kitName', label: 'Kit / reagent' }, { key: 'supplierName', label: 'Supplier' },
    { key: 'batchNo', label: 'Batch' }, { key: 'dateOfExpiry', label: 'Expiry' },
    { key: 'noOfTests', label: 'Tests' }, { key: 'closingBalance', label: 'Closing balance' }
  ],
  apheresis: [
    { key: 'registrationNo', label: 'Registration' }, { key: 'date', label: 'Date' },
    { key: 'donorName', label: 'Donor' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'weightKg', label: 'Weight (kg)' }, { key: 'plateletCountLakhs', label: 'Platelets (lakhs)' },
    { key: 'durationMinutes', label: 'Duration (min)' }, { key: 'qualityControlPassed', label: 'QC passed' }
  ],
  'master-record': [
    { key: 'yearlySlNo', label: 'Sl. No.' }, { key: 'bagNumber', label: 'Bag number' },
    { key: 'donorName', label: 'Donor' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'componentType', label: 'Component' }, { key: 'dateOfCollection', label: 'Collected' },
    { key: 'dateOfExpiry', label: 'Expiry' }, { key: 'hivResult', label: 'HIV' },
    { key: 'hbsAgResult', label: 'HBsAg' }, { key: 'hcvResult', label: 'HCV' },
    { key: 'vdrlResult', label: 'VDRL' }, { key: 'malariaResult', label: 'Malaria' },
    { key: 'ttiOverallStatus', label: 'TTI status' }, { key: 'status', label: 'Status' },
    { key: 'utilisationIssueNo', label: 'Issue number' }
  ],
  requisitions: [
    { key: 'createdAt', label: 'Requested' }, { key: 'patientName', label: 'Patient' },
    { key: 'patientAge', label: 'Age' }, { key: 'patientAgeUnit', label: 'Age unit' },
    { key: 'patientSex', label: 'Sex' }, { key: 'isThalassemiaPatient', label: 'Thalassemia' },
    { key: 'hospitalName', label: 'Hospital' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'componentType', label: 'Component' }, { key: 'quantityUnits', label: 'Units' },
    { key: 'adminComments', label: 'Review notes' },
    { key: 'urgency', label: 'Urgency' }, { key: 'status', label: 'Status' }
  ],
  collections: [
    { key: 'bagBarcode', label: 'Bag barcode' }, { key: 'componentType', label: 'Component' },
    { key: 'bloodGroup', label: 'Blood group' }, { key: 'volumeMl', label: 'Volume (mL)' },
    { key: 'collectionDate', label: 'Collection date' }, { key: 'expiryDate', label: 'Expiry' },
    { key: 'status', label: 'Status' }
  ],
  discards: [
    { key: 'bagBarcode', label: 'Bag barcode' }, { key: 'reason', label: 'Reason' },
    { key: 'discardedBy', label: 'Discarded by' }, { key: 'approvalStatus', label: 'Approval' },
    { key: 'createdAt', label: 'Reported' }, { key: 'approvedBy', label: 'Approved by' }
  ],
  returns: [
    { key: 'bagBarcode', label: 'Bag barcode' }, { key: 'reason', label: 'Reason' },
    { key: 'returnedBy', label: 'Returned by' }, { key: 'returnDate', label: 'Returned' }
  ],
  issues: [
    { key: 'createdAt', label: 'Date' }, { key: 'patientName', label: 'Patient' },
    { key: 'patientAge', label: 'Age' }, { key: 'patientAgeUnit', label: 'Age unit' },
    { key: 'patientSex', label: 'Sex' }, { key: 'isThalassemiaPatient', label: 'Thalassemia' },
    { key: 'hospitalName', label: 'Hospital' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'componentType', label: 'Component' }, { key: 'issuedBags', label: 'Issued bags / expiry' },
    { key: 'paymentMode', label: 'Payment mode' }, { key: 'paymentStatus', label: 'Payment status' },
    { key: 'transactionId', label: 'Transaction ID' }, { key: 'discountDonorId', label: 'Donor ID' },
    { key: 'discountAmount', label: 'Discount' }, { key: 'amountPaid', label: 'Amount paid' },
    { key: 'status', label: 'Status' }, { key: 'processedAt', label: 'Processed' }
  ],
  bags: [
    { key: 'bagNumber', label: 'Bag number' }, { key: 'bloodGroup', label: 'Blood group' },
    { key: 'componentType', label: 'Component' }, { key: 'addedDate', label: 'Date added' },
    { key: 'quantityUnits', label: 'Units' }
  ],
  donors: [
    { key: 'donorId', label: 'Donor ID' }, { key: 'fullName', label: 'Donor' },
    { key: 'bloodGroup', label: 'Blood group' }, { key: 'phone', label: 'Phone' },
    { key: 'totalDonations', label: 'Donations' }, { key: 'eligibilityStatus', label: 'Eligibility' },
    { key: 'registeredDate', label: 'Created' }, { key: 'createdBy', label: 'Created by' },
    { key: 'updatedAt', label: 'Updated' }, { key: 'updatedBy', label: 'Updated by' }
  ],
  donations: [
    { key: 'donationDate', label: 'Donation date' }, { key: 'donorName', label: 'Donor' },
    { key: 'bloodGroup', label: 'Blood group' }, { key: 'unitsDonated', label: 'Units' },
    { key: 'bagType', label: 'Bag type' }, { key: 'campName', label: 'Camp' },
    { key: 'medicalExamAccepted', label: 'Medical exam' }
  ]
};

export const registerList: { key: RegisterKey; label: string; description: string; icon: string }[] = [
  { key: 'tti', label: 'TTI screening', description: 'Transfusion-transmitted infection screening results.', icon: 'M12 3l8 4v5c0 5-3.4 8-8 9-4.6-1-8-4-8-9V7l8-4zm-3 9 2 2 4-4' },
  { key: 'grouping', label: 'Patient grouping', description: 'Blood grouping, antibody screening and crossmatch register.', icon: 'M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2m6-10a4 4 0 1 0 0-8 4 4 0 0 0 0 8zm7-3a4 4 0 0 1 0 8m5 5v-2a4 4 0 0 0-3-3.87' },
  { key: 'bag-stock', label: 'Blood-bag stock register', description: 'Supplier, batch, expiry and balance ledger.', icon: 'M20 7l-8-4-8 4 8 4 8-4zm-16 0v10l8 4 8-4V7m-8 4v10' },
  { key: 'bags', label: 'Bag inventory', description: 'Individual collected / stocked blood bags.', icon: 'M6 3h12l1 5-2 2v10a1 1 0 0 1-1 1H8a1 1 0 0 1-1-1V10L5 8l1-5zm1 5h10M9 14h6' },
  { key: 'diagnostic-kits', label: 'Diagnostic kits & reagents', description: 'Kit receipts, tests performed and reagent balances.', icon: 'M9 3h6m-5 0v7l-5 8a2 2 0 0 0 2 3h10a2 2 0 0 0 2-3l-5-8V3m-7 11h12' },
  { key: 'apheresis', label: 'Apheresis', description: 'Donor assessment and procedure records.', icon: 'M12 3c-3 4-6 7-6 11a6 6 0 0 0 12 0c0-4-3-7-6-11zm-2 12a2 2 0 0 0 2 2' },
  { key: 'master-record', label: 'Master record', description: 'Consolidated bag, donor, TTI and utilization register.', icon: 'M6 3h9l4 4v14H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2zm8 1v4h4m-9 4h6m-6 4h6' },
  { key: 'requisitions', label: 'Requisitions', description: 'Hospital blood/component requests and processing.', icon: 'M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2m-6 0a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2 2 2 0 0 0-2-2h-2a2 2 0 0 0-2 2zm0 8h6m-6 4h6' },
  { key: 'issues', label: 'Issue records', description: 'Fulfillment and bag issue details on requests.', icon: 'M4 12h11m0 0-4-4m4 4-4 4m5-10h3a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-3' },
  { key: 'collections', label: 'Component collection', description: 'Collected components and expiry tracking.', icon: 'M12 3c-3 4-6 7-6 11a6 6 0 0 0 12 0c0-4-3-7-6-11zm0 7v8m-4-4h8' },
  { key: 'discards', label: 'Component discard', description: 'Discard reports and approval status.', icon: 'M4 7h16m-10 4v6m4-6v6M6 7l1 14h10l1-14M9 7V4h6v3' },
  { key: 'returns', label: 'Component returns', description: 'Returned component records.', icon: 'M9 14 4 9l5-5m-5 5h10a6 6 0 0 1 0 12h-2' },
  { key: 'donors', label: 'Donors', description: 'Donor directory, eligibility and donation history.', icon: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2m8-10a4 4 0 1 0 0-8 4 4 0 0 0 0 8z' },
  { key: 'donations', label: 'Donations', description: 'Donation screening and collection records.', icon: 'M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21l7.8-7.5 1.1-1.1a5.5 5.5 0 0 0-.1-7.8z' }
];

@Component({
  selector: 'app-registers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registers.component.html',
  styleUrl: './registers.component.css'
})
export class RegistersComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly bloodBankService = inject(BloodBankService);
  private readonly registersService = inject(RegistersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private hasInitializedRoute = false;
  private hasStartedDonationFromRoute = false;

  public readonly today = new Date();
  public selectedKey: RegisterKey = 'tti';
  @Input() public category?: RegisterKey;
  public rows: RegisterRow[] = [];
  public donors: DonorDetails[] = [];
  public selectedDonorId = '';
  public selectedDonorName = '';
  public search = '';
  public requestFilterStatus: 'Active' | 'Fulfilled' | 'All' = 'Active';
  public isLoading = false;
  public isSaving = false;
  public showForm = false;
  public editingId: string | null = null;
  public errorMessage = '';
  public successMessage = '';
  public processFormError = '';
  public form: Record<string, string | number | boolean | undefined> = {};
  public processForm: Record<string, string | number | boolean | undefined> = {};
  public processingId: string | null = null;
  public groupingReviewId: string | null = null;
  public groupingRejectionReason = '';

  public ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const requestedCategory = this.category ?? params.get('category');
      const category = this.categories.find(item => item.key === requestedCategory);
      const nextKey = category?.key ?? (this.canViewSensitiveRegisters ? 'tti' : 'grouping');
      const donorId = nextKey === 'donations' ? params.get('donorId') ?? '' : '';
      if (nextKey !== this.selectedKey) {
        this.activateRegister(nextKey, donorId);
      } else if (donorId !== this.selectedDonorId) {
        this.selectedDonorId = donorId;
        this.selectedDonorName = this.donors.find(donor => donor.id === donorId)?.fullName ?? '';
        this.loadRecords();
      } else if (!this.hasInitializedRoute) {
        this.loadRecords();
      }

      if (nextKey === 'donations' && params.get('recordDonation') === 'true' && !this.hasStartedDonationFromRoute) {
        this.hasStartedDonationFromRoute = true;
        this.beginCreate();
      }
      this.hasInitializedRoute = true;
    });
  }

  public get categories(): typeof registerList {
    return this.canViewSensitiveRegisters
      ? registerList
      : registerList.filter(category => category.key !== 'tti' && category.key !== 'master-record');
  }

  public get canViewSensitiveRegisters(): boolean {
    return this.authService.hasRole(['Admin', 'TechnicalSupervisor']);
  }

  public get selectedCategory(): { key: RegisterKey; label: string; description: string } {
    return this.categories.find(category => category.key === this.selectedKey) ?? this.categories[0];
  }

  public get fields(): RegisterField[] {
    return fieldSets[this.selectedKey] ?? [];
  }

  public get tableColumns(): RegisterColumn[] {
    let selectedColumns: RegisterColumn[];
    if (this.selectedKey === 'donations' && !this.selectedDonorId) {
      selectedColumns = columns.donors;
    } else if (this.selectedKey === 'master-record' || this.selectedKey === 'issues' || this.selectedKey === 'donors') {
      selectedColumns = columns[this.selectedKey];
    } else if (this.selectedKey === 'grouping') {
      selectedColumns = [
        ...this.fields.map(({ key, label }) => ({ key, label })),
        { key: 'bloodRequestId', label: 'Linked request' },
        { key: 'requestedComponentType', label: 'Requested component' },
        { key: 'requestedQuantityUnits', label: 'Requested units' },
        { key: 'reviewStatus', label: 'Review status' },
        { key: 'rejectionReason', label: 'Rejection reason' },
        { key: 'issuedBags', label: 'Issued bag ID / expiry' }
      ];
    } else if (fieldSets[this.selectedKey]) {
      selectedColumns = this.fields.map(({ key, label }) => ({ key, label }));
    } else {
      selectedColumns = columns[this.selectedKey];
    }
    return withAuditColumns(selectedColumns);
  }

  public get visibleRows(): RegisterRow[] {
    let rows = this.rows;
    if (this.selectedKey === 'requisitions') {
      if (this.requestFilterStatus === 'Active') {
        rows = rows.filter(row =>
          row['status'] === 'Pending' || row['status'] === 'Processing' || row['status'] === 'Approved'
        );
      } else if (this.requestFilterStatus === 'Fulfilled') {
        rows = rows.filter(row => row['status'] === 'Fulfilled');
      }
    }
    const query = this.search.trim().toLocaleLowerCase();
    if (!query) return rows;
    return rows.filter(row => Object.values(row)
      .some(value => String(value ?? '').toLocaleLowerCase().includes(query)));
  }

  public get activeRequestCount(): number {
    return this.rows.filter(row =>
      row['status'] === 'Pending' || row['status'] === 'Processing' || row['status'] === 'Approved'
    ).length;
  }

  public get fulfilledRequestCount(): number {
    return this.rows.filter(row => row['status'] === 'Fulfilled').length;
  }

  public get processingRequestStatus(): unknown {
    return this.rows.find(row => row.id === this.processingId)?.['status'];
  }

  public get issueUnitTotal(): number {
    return this.rows.reduce((total, row) => total + Number(row['quantityUnits'] ?? 0), 0);
  }

  public get completedIssuePaymentCount(): number {
    return this.rows.filter(row => row['paymentStatus'] === 'Completed').length;
  }

  public get isReadOnly(): boolean {
    return this.selectedKey === 'master-record' || this.selectedKey === 'issues' ||
      (this.selectedKey === 'donations' && !this.selectedDonorId);
  }

  private activateRegister(key: RegisterKey, donorId = ''): void {
    this.selectedKey = key;
    this.search = '';
    this.rows = [];
    this.showForm = false;
    this.editingId = null;
    this.selectedDonorId = donorId;
    this.selectedDonorName = this.donors.find(donor => donor.id === donorId)?.fullName ?? '';
    this.processingId = null;
    this.successMessage = '';
    this.loadRecords();
  }

  public loadRecords(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.getRows().subscribe({
      next: rows => {
        this.rows = rows as RegisterRow[];
        this.isLoading = false;
      },
      error: error => {
        this.rows = [];
        this.errorMessage = this.getErrorMessage(error);
        this.isLoading = false;
      }
    });
  }

  public beginCreate(): void {
    if (this.isReadOnly) return;
    this.editingId = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.form = {};
    for (const field of this.fields) {
      if (field.type === 'checkbox') this.form[field.key] = field.initial === true;
      else if (field.initial !== undefined) this.form[field.key] = field.initial;
      else if (field.type === 'date') this.form[field.key] = new Date().toISOString().slice(0, 10);
      else if (field.type === 'number') this.form[field.key] = 0;
    }
    if (this.selectedKey === 'donations') {
      this.form = { ...this.form, ...this.donationBooleanDefaults(), unitsDonated: 1 };
    }
    this.showForm = true;
  }

  public beginEdit(row: RegisterRow): void {
    if (this.isReadOnly || ['donations', 'issues', 'master-record', 'bags'].includes(this.selectedKey)) return;
    this.editingId = row.id;
    this.errorMessage = '';
    this.successMessage = '';
    const editableFields = this.fields;
    this.form = {};
    for (const field of editableFields) {
      const value = row[field.key];
      this.form[field.key] = field.type === 'date' && typeof value === 'string'
        ? value.slice(0, 10)
        : (value as string | number | boolean | undefined);
    }
    if (this.selectedKey === 'donors') {
      const parts = String(row['fullName'] ?? '').trim().split(/\s+/);
      this.form['firstName'] = parts.shift() ?? '';
      this.form['lastName'] = parts.join(' ');
    }
    this.showForm = true;
  }

  public save(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.errorMessage = '';
    const request = this.getSaveRequest();
    request.subscribe({
      next: result => {
        this.isSaving = false;
        this.showForm = false;
        this.successMessage = this.selectedKey === 'donors' && !this.editingId
          ? `Donor created${this.donorPassword(result) ? `. Generated password: ${this.donorPassword(result)}` : ''}.`
          : 'Register entry saved.';
        this.loadRecords();
      },
      error: error => {
        this.isSaving = false;
        this.errorMessage = this.getErrorMessage(error);
      }
    });
  }

  public delete(row: RegisterRow): void {
    if (!this.canDelete(row)) return;
    const confirmation = this.selectedKey === 'donors'
      ? 'Deactivate this donor account? Their donation history will be retained.'
      : this.selectedKey === 'requisitions'
        ? 'Delete this pending request? This action cannot be undone.'
        : 'Delete this register entry?';
    if (!window.confirm(confirmation)) return;
    this.errorMessage = '';
    this.getDeleteRequest(row).subscribe({
      next: () => {
        this.successMessage = this.selectedKey === 'donors'
          ? 'Donor account deactivated; donation history retained.'
          : 'Register entry deleted.';
        this.loadRecords();
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  public setSelectedDonor(donorId: string): void {
    this.selectedDonorId = donorId;
    const donor = this.donors.find(item => item.id === donorId);
    this.selectedDonorName = donor?.fullName ?? '';
    if (!donorId) {
      this.rows = this.donors as unknown as RegisterRow[];
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    this.bloodBankService.getDonationHistory(donorId).subscribe({
      next: records => {
        this.rows = records as unknown as RegisterRow[];
        this.isLoading = false;
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error);
        this.isLoading = false;
      }
    });
  }

  public startDonation(row: RegisterRow): void {
    this.router.navigate(['/registers/donations'], {
      queryParams: { donorId: row.id, recordDonation: true }
    });
  }

  public openDonations(row: RegisterRow): void {
    this.router.navigate(['/registers/donations'], {
      queryParams: { donorId: row.id }
    });
  }

  public startProcessing(row: RegisterRow): void {
    this.processingId = row.id;
    this.processFormError = '';
    this.processForm = {
      status: row['status'] === 'Approved' ? 'Fulfilled' : 'Approved',
      adminComments: '',
      paymentMode: '',
      transactionId: '',
      isThalassemiaPatient: row['isThalassemiaPatient'] === true,
      hasDonor: false,
      donorRegistered: false,
      discountDonorId: '',
      amountPaid: undefined
    };
    if (row['status'] === 'Approved') {
      this.refreshDonorsForProcessing();
    }
  }

  public refreshDonorsForProcessing(): void {
    this.bloodBankService.getDonors().subscribe({
      next: donors => this.donors = donors,
      error: error => this.processFormError = this.getErrorMessage(error)
    });
  }

  public updateProcessDiscountAmount(): void {
    this.processFormError = '';
    const request = this.rows.find(row => row.id === this.processingId);
    if (!request) return;

    const quantity = Number(request['quantityUnits']);
    const componentType = String(request['componentType'] ?? '');
    const amount = this.getComponentPrice(componentType) * quantity;
    const hasRegisteredDonor = this.processForm['hasDonor'] === true &&
      this.processForm['donorRegistered'] === true &&
      !!this.textValue('processForm', 'discountDonorId');
    this.processForm['amountPaid'] = hasRegisteredDonor ? Math.max(0, amount - 450) : amount;
  }

  public processRequest(): void {
    if (!this.processingId) return;
    const status = String(this.processForm['status']) as 'Approved' | 'Fulfilled' | 'Rejected';
    const request = this.rows.find(row => row.id === this.processingId);
    const isThalassemiaPatient = request?.['isThalassemiaPatient'] === true;
    const hasDonor = this.processForm['hasDonor'] === true;
    const donorRegistered = this.processForm['donorRegistered'] === true;
    const paymentMode = isThalassemiaPatient ? undefined : this.textValue('processForm', 'paymentMode');
    const transactionId = isThalassemiaPatient ? undefined : this.textValue('processForm', 'transactionId');
    const donorId = isThalassemiaPatient || !hasDonor || !donorRegistered
      ? undefined
      : this.textValue('processForm', 'discountDonorId');
    this.errorMessage = '';
    this.processFormError = '';
    if (status === 'Fulfilled' && !isThalassemiaPatient && !paymentMode) {
      this.processFormError = 'Select a payment mode before fulfilling the request.';
      return;
    }
    if (status === 'Fulfilled' && hasDonor && !donorRegistered) {
      this.processFormError = 'Register the donor before applying the donor discount.';
      return;
    }
    if (status === 'Fulfilled' && hasDonor && donorRegistered && !donorId) {
      this.processFormError = 'Select the registered donor before applying the discount.';
      return;
    }
    if (status === 'Fulfilled' && paymentMode === 'UPI' && !/^\d{12}$/.test(transactionId ?? '')) {
      this.processFormError = 'Enter a valid 12-digit UPI transaction ID / UTR.';
      return;
    }
    if (status === 'Rejected' && !this.textValue('processForm', 'adminComments')) {
      this.processFormError = 'A rejection reason is required.';
      return;
    }

    this.bloodBankService.processRequest(
      this.processingId,
      status,
      this.textValue('processForm', 'adminComments'),
      status === 'Fulfilled' ? paymentMode as 'Cash' | 'UPI' | undefined : undefined,
      status === 'Fulfilled' ? transactionId : undefined,
      status === 'Fulfilled'
        ? isThalassemiaPatient ? 0 : donorId ? undefined : this.numberValue('processForm', 'amountPaid')
        : undefined,
      status === 'Fulfilled' && isThalassemiaPatient,
      status === 'Fulfilled' ? donorId : undefined
    ).subscribe({
      next: request => {
        this.processingId = null;
        this.successMessage = status === 'Approved'
          ? 'Request approved and moved to Patient Grouping for crossmatch.'
          : status === 'Rejected'
            ? 'Request rejected with the reason recorded.'
            : request.paymentStatus === 'Waived'
              ? 'Requisition fulfilled free of cost for a verified thalassemia patient.'
              : `Requisition fulfilled. Amount paid: ₹${request.amountPaid ?? 0}.`;
        this.loadRecords();
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  private getComponentPrice(componentType: string): number {
    switch (componentType) {
      case 'Whole Blood': return 1500;
      case 'Red Blood Cells (PRBC)': return 1450;
      case 'Platelets': return 850;
      case 'Fresh Frozen Plasma (FFP)': return 600;
      case 'Cryoprecipitate': return 500;
      default: return 1000;
    }
  }

  public reviewDiscard(row: RegisterRow, approve: boolean): void {
    this.bloodBankService.reviewComponentDiscard(row.id, approve).subscribe({
      next: () => {
        this.successMessage = approve ? 'Discard approved.' : 'Discard rejected.';
        this.loadRecords();
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  public reviewGrouping(row: RegisterRow, status: 'Approved' | 'Rejected'): void {
    if (status === 'Rejected') {
      this.groupingReviewId = row.id;
      this.groupingRejectionReason = '';
      return;
    }
    this.submitGroupingReview(row.id, status);
  }

  public confirmGroupingRejection(): void {
    const reason = this.groupingRejectionReason.trim();
    if (!this.groupingReviewId || !reason) return;
    this.submitGroupingReview(this.groupingReviewId, 'Rejected', reason);
  }

  private submitGroupingReview(id: string, status: 'Approved' | 'Rejected', rejectionReason?: string): void {
    this.errorMessage = '';
    this.processFormError = '';
    this.bloodBankService.reviewPatientGrouping(id, status, rejectionReason).subscribe({
      next: () => {
        this.groupingReviewId = null;
        this.groupingRejectionReason = '';
        this.successMessage = status === 'Approved'
          ? 'Crossmatch approved. The request is now eligible for component issue.'
          : 'Crossmatch rejected and the reason recorded on the request.';
        this.loadRecords();
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  public printRegister(): void {
    window.print();
  }

  public displayValue(key: string, value: unknown): string {
    if (value === null || value === undefined || value === '') return '—';
    if (key === 'issuedBags' && Array.isArray(value)) {
      return value.map(item => {
        const bag = item as Record<string, unknown>;
        const expiry = bag['expiryDate'] ? this.displayValue('expiryDate', bag['expiryDate']) : 'expiry unknown';
        return `${String(bag['bagNumber'] ?? 'Bag')} (expires ${expiry})`;
      }).join(', ');
    }
    if (key.toLowerCase().includes('date') || key === 'createdAt' || key === 'processedAt' || key === 'returnDate') {
      const date = new Date(String(value));
      return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString();
    }
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return String(value);
  }

  public supportsDelete(): boolean {
    return ['tti', 'grouping', 'bag-stock', 'diagnostic-kits', 'apheresis', 'collections', 'discards'].includes(this.selectedKey);
  }

  public canDelete(row: RegisterRow): boolean {
    if (this.selectedKey === 'grouping' && row['bloodRequestId']) return false;
    if (this.supportsDelete()) return true;
    if (this.selectedKey === 'donors') return true;
    return this.selectedKey === 'requisitions' && row['status'] === 'Pending' &&
      !row['processedAt'] && !row['issuedBagNumbers'] && row['paymentStatus'] === 'Pending';
  }

  public canEdit(): boolean {
    return !this.isReadOnly && !['donations', 'issues', 'master-record', 'bags'].includes(this.selectedKey);
  }

  public canEditRow(row: RegisterRow): boolean {
    if (this.selectedKey === 'grouping' && row['bloodRequestId'] && row['reviewStatus'] !== 'Pending') return false;
    return this.canEdit() && (this.selectedKey !== 'requisitions' || row['status'] === 'Pending');
  }

  public labelFor = labelFor;

  private getRows(): Observable<unknown[]> {
    switch (this.selectedKey) {
      case 'tti': return this.bloodBankService.getTtiScreenings();
      case 'grouping': return this.bloodBankService.getPatientGroupings();
      case 'bag-stock': return this.bloodBankService.getBloodBagStockRegisters();
      case 'diagnostic-kits': return this.bloodBankService.getDiagnosticKitRegisters();
      case 'apheresis': return this.bloodBankService.getApheresisRecords();
      case 'master-record': return this.bloodBankService.getMasterRecords();
      case 'requisitions': return this.bloodBankService.getRequests();
      case 'issues': return this.registersService.getIssueRecords('Fulfilled');
      case 'collections': return this.bloodBankService.getComponentCollections();
      case 'discards': return this.bloodBankService.getComponentDiscards();
      case 'returns': return this.bloodBankService.getComponentReturns();
      case 'bags': return this.bloodBankService.getBloodBags();
      case 'donors':
        return this.bloodBankService.getDonors().pipe(map(donors => {
          this.donors = donors;
          return donors;
        }));
      case 'donations':
        return this.bloodBankService.getDonors().pipe(switchMap(donors => {
            this.donors = donors;
            this.selectedDonorName = donors.find(donor => donor.id === this.selectedDonorId)?.fullName ?? '';
            return this.selectedDonorId
              ? this.bloodBankService.getDonationHistory(this.selectedDonorId)
              : of(donors);
          }));
    }
  }

  private getSaveRequest(): Observable<unknown> {
    const payload = this.payload();
    const id = this.editingId;
    switch (this.selectedKey) {
      case 'tti':
        return id
          ? this.bloodBankService.updateTtiScreening(id, payload as unknown as CreateTtiScreeningPayload)
          : this.bloodBankService.createTtiScreening(payload as unknown as CreateTtiScreeningPayload);
      case 'grouping':
        return id
          ? this.bloodBankService.updatePatientGrouping(id, payload as unknown as CreatePatientGroupingPayload)
          : this.bloodBankService.createPatientGrouping(payload as unknown as CreatePatientGroupingPayload);
      case 'bag-stock':
        return id
          ? this.bloodBankService.updateBloodBagStockRegister(id, payload as unknown as CreateBloodBagStockRegisterPayload)
          : this.bloodBankService.createBloodBagStockRegister(payload as unknown as CreateBloodBagStockRegisterPayload);
      case 'diagnostic-kits':
        return id
          ? this.bloodBankService.updateDiagnosticKitRegister(id, payload as unknown as CreateDiagnosticKitRegisterPayload)
          : this.bloodBankService.createDiagnosticKitRegister(payload as unknown as CreateDiagnosticKitRegisterPayload);
      case 'apheresis':
        return id
          ? this.bloodBankService.updateApheresisRecord(id, payload as unknown as CreateApheresisRecordPayload)
          : this.bloodBankService.createApheresisRecord(payload as unknown as CreateApheresisRecordPayload);
      case 'bags':
        return this.bloodBankService.addStock(payload as unknown as AddStockPayload);
      case 'collections':
        return id
          ? this.bloodBankService.updateComponentCollection(id, payload as unknown as CreateComponentCollectionPayload)
          : this.bloodBankService.createComponentCollection(payload as unknown as CreateComponentCollectionPayload);
      case 'discards':
        return id
          ? this.bloodBankService.updateComponentDiscard(id, payload as unknown as CreateComponentDiscardPayload)
          : this.bloodBankService.createComponentDiscard(payload as unknown as CreateComponentDiscardPayload);
      case 'returns':
        return id
          ? this.bloodBankService.updateComponentReturn(id, payload as unknown as CreateComponentReturnPayload)
          : this.bloodBankService.createComponentReturn(payload as unknown as CreateComponentReturnPayload);
      case 'requisitions':
        return id
          ? this.bloodBankService.updateRequest(id, payload as unknown as Partial<CreateBloodRequestPayload>)
          : this.bloodBankService.createRequest(payload as unknown as CreateBloodRequestPayload);
      case 'donors': {
        const donorPayload = payload as unknown as CreateDonorPayload;
        return id
          ? this.bloodBankService.updateDonor(id, donorPayload)
          : this.bloodBankService.createDonor(donorPayload);
      }
      case 'donations':
        return this.bloodBankService.completeDonation(this.selectedDonorId, payload as unknown as CompleteDonationPayload);
      default:
        return new Observable(subscriber => subscriber.error(new Error('This register is read-only.')));
    }
  }

  private getDeleteRequest(row: RegisterRow): Observable<void> {
    switch (this.selectedKey) {
      case 'tti': return this.bloodBankService.deleteTtiScreening(row.id);
      case 'grouping': return this.bloodBankService.deletePatientGrouping(row.id);
      case 'bag-stock': return this.bloodBankService.deleteBloodBagStockRegister(row.id);
      case 'diagnostic-kits': return this.bloodBankService.deleteDiagnosticKitRegister(row.id);
      case 'apheresis': return this.bloodBankService.deleteApheresisRecord(row.id);
      case 'collections': return this.bloodBankService.deleteComponentCollection(row.id);
      case 'discards': return this.bloodBankService.deleteComponentDiscard(row.id);
      case 'requisitions': return this.bloodBankService.deletePendingRequest(row.id);
      case 'donors': return this.bloodBankService.deleteDonor(row.id);
      default: return new Observable(subscriber => subscriber.error(new Error('Delete is not supported for this register.')));
    }
  }

  private payload(): Record<string, unknown> {
    const payload: Record<string, unknown> = {};
    for (const field of this.fields) {
      const value = this.form[field.key];
      if (value === undefined || value === '') continue;
      payload[field.key] = field.type === 'number' && typeof value === 'string' ? Number(value) : value;
    }
    if (this.selectedKey === 'donations') {
      return { ...this.donationBooleanDefaults(), ...payload, unitsDonated: Number(payload['unitsDonated'] ?? 1) };
    }
    return payload;
  }

  private donationBooleanDefaults(): Record<string, boolean> {
    return {
      donorConfirmed: false, medicalExamAccepted: false, visualInspectionPassed: false,
      feelsHealthyToday: false, donatedRecently: false, hadDonationDiscomfort: false,
      ateRecently: false, sleptWell: false, hasInfectionRisk: false,
      hasColdFeverOrSinusitis: false, tookAntibioticsRecently: false, tookAlcoholRecently: false,
      smokesFrequently: false, tookMedicationRecently: false, hadRecentVaccination: false,
      hadRecentIllness: false, hadRecentSurgeryOrTattoo: false,
      hadRecentMalariaOrEndemicExposure: false, hasChronicDisease: false,
      isPregnantOrBreastfeeding: false, hadRecentAbortion: false,
      hasUnsafeSexualExposure: false, hasHivOrHepatitisRisk: false,
      hasPermanentDisqualification: false
    };
  }

  private textValue(source: 'form' | 'processForm', key: string): string | undefined {
    const value = this[source][key];
    return typeof value === 'string' && value.trim() ? value.trim() : undefined;
  }

  private numberValue(source: 'form' | 'processForm', key: string): number | undefined {
    const value = this[source][key];
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
  }

  private donorPassword(result: unknown): string {
    if (!result || typeof result !== 'object') return '';
    const value = (result as { generatedPassword?: unknown }).generatedPassword;
    return typeof value === 'string' ? value : '';
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as {
        detail?: unknown;
        message?: unknown;
        title?: unknown;
        errors?: Record<string, unknown>;
      } | null;
      const directMessage = [body?.detail, body?.message, body?.title]
        .find(value => typeof value === 'string' && value.trim());
      if (typeof directMessage === 'string') return directMessage;
      if (body?.errors) {
        const messages = Object.values(body.errors).flatMap(value =>
          Array.isArray(value) ? value : [value]
        ).filter((value): value is string => typeof value === 'string' && !!value.trim());
        if (messages.length) return messages.join(' ');
      }
      return error.message;
    }
    return 'An unexpected error occurred while loading or saving register data.';
  }
}

function labelFor(key: string): string {
  return key.replace(/([A-Z])/g, ' $1').replace(/^./, char => char.toUpperCase());
}

function withAuditColumns(columns: RegisterColumn[]): RegisterColumn[] {
  const auditColumns = [
    { key: 'createdAt', label: 'Created' },
    { key: 'createdBy', label: 'Created by' },
    { key: 'updatedAt', label: 'Updated' },
    { key: 'updatedBy', label: 'Updated by' }
  ];
  const existing = new Set(columns.map(column => column.key));
  const hasCreationDate = existing.has('createdAt') || existing.has('registeredDate') || existing.has('donationDate');
  return [
    ...columns,
    ...auditColumns.filter(column =>
      !existing.has(column.key) && !(column.key === 'createdAt' && hasCreationDate))
  ];
}
