export type BloodGroup = 'A+' | 'A-' | 'B+' | 'B-' | 'AB+' | 'AB-' | 'O+' | 'O-';

export interface BloodInventory {
  id: string;
  bloodGroup: BloodGroup;
  quantityUnits: number;
  bloodBankName: string;
  lastUpdated: string;
  status?: 'Critical Shortage' | 'Low Stock' | 'Adequate';
}

export interface AddStockRequest {
  bloodGroup: BloodGroup;
  quantityUnits: number;
  bloodBankName: string;
}
