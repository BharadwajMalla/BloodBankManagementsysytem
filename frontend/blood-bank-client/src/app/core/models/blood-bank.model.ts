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
