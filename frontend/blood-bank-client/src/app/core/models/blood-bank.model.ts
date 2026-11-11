export type BloodGroup = 'A+' | 'A-' | 'B+' | 'B-' | 'AB+' | 'AB-' | 'O+' | 'O-';

export type ComponentType = 
  | 'Whole Blood' 
  | 'Red Blood Cells (PRBC)' 
  | 'Platelets' 
  | 'Fresh Frozen Plasma (FFP)' 
  | 'Cryoprecipitate';

export type RequestStatus = 'Pending' | 'Approved' | 'Fulfilled' | 'Rejected';
export type UrgencyLevel = 'Normal' | 'Urgent' | 'Critical';

export interface BloodStock {
  id: string;
  bloodGroup: BloodGroup;
  componentType: ComponentType;
  quantityUnits: number;
  bloodBankName: string;
  lastUpdated: string;
  status: 'Critical Shortage' | 'Low Stock' | 'Adequate';
}

export interface BloodBag {
  id: string;
  bagNumber: string;
  bloodGroup: BloodGroup;
  componentType: ComponentType;
  quantityUnits: number;
  addedDate: string;
  isIssued?: boolean;
  issuedDate?: string;
}

export interface DonationCamp {
  id: string;
  campName: string;
  location: string;
  campDate: string;
  targetUnits: number;
  collectedUnits: number;
  organizedBy: string;
  contactPhone: string;
  status: 'Scheduled' | 'Ongoing' | 'Completed' | 'Cancelled';
  notes?: string;
  createdAt: string;
}

export interface BloodRequest {
  id: string;
  requesterId: string;
  requesterType: 'Hospital' | 'Donor';
  requesterName: string;
  requesterContact?: string;
  hospitalName?: string;
  patientName: string;
  bloodGroup: BloodGroup;
  componentType: ComponentType;
  quantityUnits: number;
  urgency: UrgencyLevel;
  status: RequestStatus;
  hospitalDepartment?: string;
  medicalReason?: string;
  adminComments?: string;
  createdAt: string;
  processedAt?: string;
  issuedBagNumbers?: string;
  paymentMode?: 'Cash' | 'UPI';
  paymentStatus: 'Pending' | 'Completed';
  transactionId?: string;
  amountPaid?: number;
}

export interface DonorDetails {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  dateOfBirth?: string;
  sex?: string;
  address?: string;
  bloodGroup?: string;
  registeredDate: string;
  totalDonations: number;
  lastDonationDate?: string;
  isEligibleToDonate: boolean;
  eligibilityStatus: string;
  donationStatus: string;
}

export interface CreateDonorPayload {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  sex: string;
  address: string;
  bloodGroup: BloodGroup;
}

export interface CreateDonorResponse {
  donor: DonorDetails;
  generatedPassword: string;
}

export interface CompleteDonationPayload {
  donorConfirmed: boolean;
  weightKg?: number;
  pulsePerMinute?: number;
  hemoglobin?: number;
  hematocrit?: number;
  plateletCount?: number;
  bloodPressure?: string;
  temperatureCelsius?: number;
  screeningStaffName?: string;
  screeningStaffSignature?: string;
  screeningDate?: string;
  screeningTime?: string;
  medicalExamAccepted: boolean;
  deferralReason?: string;
  medicalOfficerName?: string;
  medicalOfficerSignature?: string;
  medicalOfficerDate?: string;
  donorNumber?: string;
  bagType?: string;
  segmentNumber?: string;
  donationStartTime?: string;
  donationEndTime?: string;
  venipunctureSite?: string;
  phlebotomistName?: string;
  phlebotomistSignature?: string;
  visualInspectionPassed: boolean;
  feelsHealthyToday: boolean;
  donatedRecently: boolean;
  hadDonationDiscomfort: boolean;
  ateRecently: boolean;
  sleptWell: boolean;
  hasInfectionRisk: boolean;
  hasColdFeverOrSinusitis: boolean;
  tookAntibioticsRecently: boolean;
  tookAlcoholRecently: boolean;
  smokesFrequently: boolean;
  tookMedicationRecently: boolean;
  hadRecentVaccination: boolean;
  hadRecentIllness: boolean;
  hadRecentSurgeryOrTattoo: boolean;
  hadRecentMalariaOrEndemicExposure: boolean;
  hasChronicDisease: boolean;
  isPregnantOrBreastfeeding: boolean;
  hadRecentAbortion: boolean;
  hasUnsafeSexualExposure: boolean;
  hasHivOrHepatitisRisk: boolean;
  hasPermanentDisqualification: boolean;
  unitsDonated: number;
  campName?: string;
}

export interface DonationCompletionResult {
  isEligible: boolean;
  message: string;
  donor?: DonorDetails;
}

export interface DonationHistory {
  id: string;
  donorId: string;
  donorName: string;
  bloodGroup: string;
  unitsDonated: number;
  donationDate: string;
  campName?: string;
  weightKg?: number;
  pulsePerMinute?: number;
  hemoglobin?: number;
  hematocrit?: number;
  plateletCount?: number;
  bloodPressure?: string;
  temperatureCelsius?: number;
  screeningStaffName?: string;
  screeningDate?: string;
  screeningTime?: string;
  medicalExamAccepted: boolean;
  medicalOfficerName?: string;
  donorNumber?: string;
  bagType?: string;
  segmentNumber?: string;
  donationStartTime?: string;
  donationEndTime?: string;
  venipunctureSite?: string;
  phlebotomistName?: string;
  visualInspectionPassed: boolean;
  donorConfirmed: boolean;
  feelsHealthyToday: boolean;
  donatedRecently: boolean;
  hadDonationDiscomfort: boolean;
  ateRecently: boolean;
  sleptWell: boolean;
  hasInfectionRisk: boolean;
  hasColdFeverOrSinusitis: boolean;
  tookAntibioticsRecently: boolean;
  tookAlcoholRecently: boolean;
  smokesFrequently: boolean;
  tookMedicationRecently: boolean;
  hadRecentVaccination: boolean;
  hadRecentIllness: boolean;
  hadRecentSurgeryOrTattoo: boolean;
  hadRecentMalariaOrEndemicExposure: boolean;
  hasChronicDisease: boolean;
  isPregnantOrBreastfeeding: boolean;
  hadRecentAbortion: boolean;
  hasUnsafeSexualExposure: boolean;
  hasHivOrHepatitisRisk: boolean;
  hasPermanentDisqualification: boolean;
}

export interface CreateBloodRequestPayload {
  hospitalName?: string;
  patientName: string;
  bloodGroup: BloodGroup;
  componentType: ComponentType;
  quantityUnits: number;
  urgency: UrgencyLevel;
  hospitalDepartment?: string;
  medicalReason?: string;
}

export interface AddStockPayload {
  bagNumber: string;
  bloodGroup: BloodGroup;
  componentType: ComponentType;
  addedDate: string;
}

export interface CreateCampPayload {
  campName: string;
  location: string;
  campDate: string;
  targetUnits: number;
  contactPhone: string;
  notes?: string;
}

export interface ComponentCollection {
  id: string;
  componentType: string;
  bloodGroup: string;
  bagType: string;
  volumeMl: number;
  bagBarcode: string;
  collectionDate: string;
  expiryDate: string;
  status: string;
}

export interface CreateComponentCollectionPayload {
  componentType: string;
  bloodGroup: string;
  bagType: string;
  volumeMl: number;
  bagBarcode: string;
  collectionDate: string;
  expiryDate: string;
}

export interface ComponentDiscard {
  id: string;
  bagBarcode: string;
  reason: string;
  discardedBy: string;
  approvalStatus: string;
  approvedBy?: string;
  createdAt: string;
  approvedAt?: string;
}

export interface CreateComponentDiscardPayload {
  bagBarcode: string;
  reason: string;
  discardedBy: string;
}

export interface ComponentReturn {
  id: string;
  bagBarcode: string;
  reason: string;
  returnedBy: string;
  returnDate: string;
}

export interface CreateComponentReturnPayload {
  bagBarcode: string;
  reason: string;
  returnedBy: string;
}

// ================= Uddanam Clinical Registers & Audit Models =================

export interface TtiScreeningRecord {
  id: string;
  bagBarcode: string;
  bloodGroup: string;
  testingDate: string;
  hivResult: string;
  hbsAgResult: string;
  hcvResult: string;
  vdrlResult: string;
  malariaResult: string;
  atypicalAntibodies: string;
  overallStatus: string;
  testedBy: string;
  approvedBy?: string;
  remarks?: string;
  createdAt: string;
}

export interface CreateTtiScreeningPayload {
  bagBarcode: string;
  bloodGroup: string;
  testingDate?: string;
  hivResult?: string;
  hbsAgResult?: string;
  hcvResult?: string;
  vdrlResult?: string;
  malariaResult?: string;
  atypicalAntibodies?: string;
  overallStatus?: string;
  testedBy?: string;
  approvedBy?: string;
  remarks?: string;
}

export interface PatientGroupingRecord {
  id: string;
  slNo: number;
  date: string;
  patientName: string;
  ageSex?: string;
  hospitalName?: string;
  ipNumber?: string;
  bagNumber?: string;
  bagType?: string;
  bloodGroup: string;
  rhType: string;
  antiA: string;
  antiB: string;
  antiAb: string;
  antiD: string;
  lectinA1: string;
  lectinH: string;
  aCells: string;
  bCells: string;
  oCells: string;
  duTest: string;
  irregularAntibodies: string;
  autoAntibodies: string;
  crossmatchSaline: string;
  crossmatchAlbumin: string;
  crossmatchAhg: string;
  technicianSignature?: string;
  medicalOfficerSignature?: string;
  createdAt: string;
}

export interface CreatePatientGroupingPayload {
  slNo?: number;
  date?: string;
  patientName: string;
  ageSex?: string;
  hospitalName?: string;
  ipNumber?: string;
  bagNumber?: string;
  bagType?: string;
  bloodGroup: string;
  rhType: string;
  antiA?: string;
  antiB?: string;
  antiAb?: string;
  antiD?: string;
  lectinA1?: string;
  lectinH?: string;
  aCells?: string;
  bCells?: string;
  oCells?: string;
  duTest?: string;
  irregularAntibodies?: string;
  autoAntibodies?: string;
  crossmatchSaline?: string;
  crossmatchAlbumin?: string;
  crossmatchAhg?: string;
  technicianSignature?: string;
  medicalOfficerSignature?: string;
}

export interface BloodBagStockRegister {
  id: string;
  slNo: number;
  date: string;
  supplierName: string;
  invoiceNo: string;
  invoiceDate: string;
  manufacturer: string;
  batchNo: string;
  dateOfSupply: string;
  dateOfManufacture: string;
  dateOfExpiry: string;
  codeSpecification: string;
  resultCoa: string;
  openingStock: number;
  quantityReceived: number;
  quantityUsed: number;
  wastage: number;
  balance: number;
  techSign?: string;
  medicalOfficerSign?: string;
  remarks?: string;
  createdAt: string;
}

export interface CreateBloodBagStockRegisterPayload {
  slNo?: number;
  date?: string;
  supplierName: string;
  invoiceNo: string;
  invoiceDate?: string;
  manufacturer: string;
  batchNo: string;
  dateOfSupply?: string;
  dateOfManufacture?: string;
  dateOfExpiry?: string;
  codeSpecification: string;
  resultCoa?: string;
  openingStock: number;
  quantityReceived: number;
  quantityUsed: number;
  wastage: number;
  balance: number;
  techSign?: string;
  medicalOfficerSign?: string;
  remarks?: string;
}

export interface DiagnosticKitRegister {
  id: string;
  slNo: number;
  date: string;
  supplierName: string;
  kitName: string;
  manufacturer: string;
  invoiceNo: string;
  batchNo: string;
  dateOfManufacture: string;
  dateOfExpiry: string;
  quantityReceived: number;
  openingBalance: number;
  noOfTests: number;
  controlsUsed: number;
  wastage: number;
  closingBalance: number;
  techSign?: string;
  medicalOfficerSign?: string;
  remarks?: string;
  createdAt: string;
}

export interface CreateDiagnosticKitRegisterPayload {
  slNo?: number;
  date?: string;
  supplierName: string;
  kitName: string;
  manufacturer: string;
  invoiceNo: string;
  batchNo: string;
  dateOfManufacture?: string;
  dateOfExpiry?: string;
  quantityReceived: number;
  openingBalance: number;
  noOfTests: number;
  controlsUsed: number;
  wastage: number;
  closingBalance: number;
  techSign?: string;
  medicalOfficerSign?: string;
  remarks?: string;
}

export interface ApheresisRecord {
  id: string;
  registrationNo: string;
  date: string;
  donorName: string;
  ageSex?: string;
  weightKg: number;
  bloodGroup: string;
  phone?: string;
  email?: string;
  timeOfMeal?: string;
  patientName?: string;
  patientIpNo?: string;
  patientBedNo?: string;
  patientBloodGroup?: string;
  takenAspirinLast3Days: boolean;
  haemoglobinGms: number;
  plateletCountLakhs: number;
  wbcCount: number;
  bloodPressure?: string;
  pulse: number;
  temperatureCelsius: number;
  totalProteinsGms: number;
  procedureStartTime?: string;
  procedureEndTime?: string;
  durationMinutes: number;
  venipunctureSite: string;
  wholeBloodPumpRateMlMin: number;
  acdRatio: string;
  totalBloodVolumeProcessedMl: number;
  totalProductCollectedMl: number;
  totalVolumeAcdUsedMl: number;
  adverseReaction?: string;
  qualityControlPassed: boolean;
  phlebotomistSignature?: string;
  medicalOfficerSignature?: string;
  remarks?: string;
  createdAt: string;
}

export interface CreateApheresisRecordPayload {
  registrationNo: string;
  date?: string;
  donorName: string;
  ageSex?: string;
  weightKg: number;
  bloodGroup: string;
  phone?: string;
  email?: string;
  timeOfMeal?: string;
  patientName?: string;
  patientIpNo?: string;
  patientBedNo?: string;
  patientBloodGroup?: string;
  takenAspirinLast3Days: boolean;
  haemoglobinGms: number;
  plateletCountLakhs: number;
  wbcCount: number;
  bloodPressure?: string;
  pulse: number;
  temperatureCelsius: number;
  totalProteinsGms: number;
  procedureStartTime?: string;
  procedureEndTime?: string;
  durationMinutes: number;
  venipunctureSite: string;
  wholeBloodPumpRateMlMin: number;
  acdRatio: string;
  totalBloodVolumeProcessedMl: number;
  totalProductCollectedMl: number;
  totalVolumeAcdUsedMl: number;
  adverseReaction?: string;
  qualityControlPassed: boolean;
  phlebotomistSignature?: string;
  medicalOfficerSignature?: string;
  remarks?: string;
}

export interface MasterRecordItem {
  id: string;
  yearlySlNo?: number;
  bagNumber: string;
  bloodGroup: string;
  componentType: string;
  dateOfCollection: string;
  dateOfExpiry: string;
  bagType: string;
  volumeMl: number;
  donorName: string;
  donorAddress?: string;
  ageSex?: string;
  hbPercentage?: number;
  hivResult: string;
  hbsAgResult: string;
  hcvResult: string;
  vdrlResult: string;
  malariaResult: string;
  atypicalAntibodies: string;
  ttiOverallStatus: string;
  status: string;
  utilisationIssueNo?: string;
  discardReason?: string;
  techSign?: string;
  moSign?: string;
}
