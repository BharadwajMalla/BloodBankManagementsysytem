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
}

export interface DonorDetails {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  bloodGroup?: string;
  registeredDate: string;
  totalDonations: number;
  lastDonationDate?: string;
  isEligibleToDonate: boolean;
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
