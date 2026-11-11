import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddStockPayload, BloodBag, BloodRequest, BloodStock, CompleteDonationPayload,
  ComponentCollection, ComponentDiscard, ComponentReturn, CreateBloodRequestPayload,
  CreateCampPayload, CreateComponentCollectionPayload, CreateComponentDiscardPayload,
  CreateComponentReturnPayload, CreateDonorPayload, CreateDonorResponse, DonationCamp,
  DonationCompletionResult, DonationHistory, DonorDetails,
  TtiScreeningRecord, CreateTtiScreeningPayload,
  PatientGroupingRecord, CreatePatientGroupingPayload,
  BloodBagStockRegister, CreateBloodBagStockRegisterPayload,
  DiagnosticKitRegister, CreateDiagnosticKitRegisterPayload,
  ApheresisRecord, CreateApheresisRecordPayload,
  MasterRecordItem
} from '../models/blood-bank.model';

@Injectable({
  providedIn: 'root'
})
export class BloodBankService {
  private readonly baseUrl = `${environment.apiUrl}/BloodBank`;

  constructor(private http: HttpClient) {}

  // 1. Stock details & replenishment
  public getStock(bloodGroup?: string, componentType?: string): Observable<BloodStock[]> {
    let params = new HttpParams();
    if (bloodGroup) params = params.set('bloodGroup', bloodGroup);
    if (componentType) params = params.set('componentType', componentType);
    return this.http.get<BloodStock[]>(`${this.baseUrl}/stock`, { params });
  }

  public addStock(payload: AddStockPayload): Observable<BloodStock> {
    return this.http.post<BloodStock>(`${this.baseUrl}/stock`, payload);
  }

  public replenishStock(payload: { bloodGroup: string; componentType: string; quantityUnits: number }): Observable<BloodStock> {
    return this.http.post<BloodStock>(`${this.baseUrl}/replenish`, payload);
  }

  public updateStock(id: string, payload: { quantityUnits: number }): Observable<BloodStock> {
    return this.http.put<BloodStock>(`${this.baseUrl}/stock/${id}`, payload);
  }

  public seedLiveData(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/seed-live-data`, {});
  }

  public clearAllData(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/clear-all-data`, {});
  }

  public getBloodBags(componentType?: string): Observable<BloodBag[]> {
    let params = new HttpParams();
    if (componentType) params = params.set('componentType', componentType);
    return this.http.get<BloodBag[]>(`${this.baseUrl}/stock/bags`, { params });
  }

  // 2. Camp details & creation
  public getCamps(): Observable<DonationCamp[]> {
    return this.http.get<DonationCamp[]>(`${this.baseUrl}/getcamps`);
  }

  public createCamp(payload: CreateCampPayload): Observable<DonationCamp> {
    return this.http.post<DonationCamp>(`${this.baseUrl}/addcamps`, payload);
  }

  public updateCamp(id: string, payload: CreateCampPayload): Observable<DonationCamp> {
    return this.http.put<DonationCamp>(`${this.baseUrl}/updatecamps/${id}`, payload);
  }

  public deleteCamp(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/deletecamps/${id}`);
  }

  // 3. Donor details directory
  public getDonors(): Observable<DonorDetails[]> {
    return this.http.get<DonorDetails[]>(`${this.baseUrl}/donors`);
  }

  public createDonor(payload: CreateDonorPayload): Observable<CreateDonorResponse> {
    return this.http.post<CreateDonorResponse>(`${this.baseUrl}/donors`, payload);
  }

  public updateDonor(id: string, payload: Partial<CreateDonorPayload>): Observable<DonorDetails> {
    return this.http.put<DonorDetails>(`${this.baseUrl}/donors/${id}`, payload);
  }

  public completeDonation(id: string, payload: CompleteDonationPayload): Observable<DonationCompletionResult> {
    return this.http.post<DonationCompletionResult>(`${this.baseUrl}/donors/${id}/donation`, payload);
  }

  public getDonationHistory(id: string): Observable<DonationHistory[]> {
    return this.http.get<DonationHistory[]>(`${this.baseUrl}/donors/${id}/donations`);
  }

  // 4. Request forms management
  public getRequests(status?: string): Observable<BloodRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<BloodRequest[]>(`${this.baseUrl}/requests`, { params });
  }

  public createRequest(payload: CreateBloodRequestPayload): Observable<BloodRequest> {
    return this.http.post<BloodRequest>(`${this.baseUrl}/requests`, payload);
  }

  public updateRequest(id: string, payload: Partial<CreateBloodRequestPayload> & { status?: string; adminComments?: string }): Observable<BloodRequest> {
    return this.http.put<BloodRequest>(`${this.baseUrl}/requests/${id}`, payload);
  }

  public processRequest(id: string, status: 'Approved' | 'Fulfilled' | 'Rejected', adminComments?: string, paymentMode?: 'Cash' | 'UPI', transactionId?: string, amountPaid?: number): Observable<BloodRequest> {
    return this.http.put<BloodRequest>(`${this.baseUrl}/requests/${id}/process`, { status, adminComments, paymentMode, transactionId, amountPaid });
  }

  public getComponentCollections(): Observable<ComponentCollection[]> { return this.http.get<ComponentCollection[]>(`${this.baseUrl}/components/collections`); }
  public createComponentCollection(payload: CreateComponentCollectionPayload): Observable<ComponentCollection> { return this.http.post<ComponentCollection>(`${this.baseUrl}/components/collections`, payload); }
  public updateComponentCollection(id: string, payload: CreateComponentCollectionPayload): Observable<ComponentCollection> { return this.http.put<ComponentCollection>(`${this.baseUrl}/components/collections/${id}`, payload); }
  public deleteComponentCollection(id: string): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/components/collections/${id}`); }
  public getComponentDiscards(): Observable<ComponentDiscard[]> { return this.http.get<ComponentDiscard[]>(`${this.baseUrl}/components/discards`); }
  public createComponentDiscard(payload: CreateComponentDiscardPayload): Observable<ComponentDiscard> { return this.http.post<ComponentDiscard>(`${this.baseUrl}/components/discards`, payload); }
  public updateComponentDiscard(id: string, payload: CreateComponentDiscardPayload & { approvalStatus?: string }): Observable<ComponentDiscard> { return this.http.put<ComponentDiscard>(`${this.baseUrl}/components/discards/${id}`, payload); }
  public reviewComponentDiscard(id: string, approve: boolean): Observable<ComponentDiscard> { return this.http.put<ComponentDiscard>(`${this.baseUrl}/components/discards/${id}/review`, null, { params: { approve } }); }
  public deleteComponentDiscard(id: string): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/components/discards/${id}`); }
  public getComponentReturns(): Observable<ComponentReturn[]> { return this.http.get<ComponentReturn[]>(`${this.baseUrl}/components/returns`); }
  public createComponentReturn(payload: CreateComponentReturnPayload): Observable<ComponentReturn> { return this.http.post<ComponentReturn>(`${this.baseUrl}/components/returns`, payload); }
  public updateComponentReturn(id: string, payload: CreateComponentReturnPayload): Observable<ComponentReturn> { return this.http.put<ComponentReturn>(`${this.baseUrl}/components/returns/${id}`, payload); }
  public deleteComponentReturn(id: string): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/components/returns/${id}`); }

  // Uddanam Clinical Registers & Master Audit Methods
  public getTtiScreenings(): Observable<TtiScreeningRecord[]> {
    return this.http.get<TtiScreeningRecord[]>(`${this.baseUrl}/registers/tti`);
  }
  public createTtiScreening(payload: CreateTtiScreeningPayload): Observable<TtiScreeningRecord> {
    return this.http.post<TtiScreeningRecord>(`${this.baseUrl}/registers/tti`, payload);
  }
  public updateTtiScreening(id: string, payload: CreateTtiScreeningPayload): Observable<TtiScreeningRecord> {
    return this.http.put<TtiScreeningRecord>(`${this.baseUrl}/registers/tti/${id}`, payload);
  }
  public deleteTtiScreening(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/registers/tti/${id}`);
  }

  public getPatientGroupings(): Observable<PatientGroupingRecord[]> {
    return this.http.get<PatientGroupingRecord[]>(`${this.baseUrl}/registers/grouping`);
  }
  public createPatientGrouping(payload: CreatePatientGroupingPayload): Observable<PatientGroupingRecord> {
    return this.http.post<PatientGroupingRecord>(`${this.baseUrl}/registers/grouping`, payload);
  }
  public updatePatientGrouping(id: string, payload: CreatePatientGroupingPayload): Observable<PatientGroupingRecord> {
    return this.http.put<PatientGroupingRecord>(`${this.baseUrl}/registers/grouping/${id}`, payload);
  }
  public deletePatientGrouping(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/registers/grouping/${id}`);
  }

  public getBloodBagStockRegisters(): Observable<BloodBagStockRegister[]> {
    return this.http.get<BloodBagStockRegister[]>(`${this.baseUrl}/registers/bag-stock`);
  }
  public createBloodBagStockRegister(payload: CreateBloodBagStockRegisterPayload): Observable<BloodBagStockRegister> {
    return this.http.post<BloodBagStockRegister>(`${this.baseUrl}/registers/bag-stock`, payload);
  }
  public updateBloodBagStockRegister(id: string, payload: CreateBloodBagStockRegisterPayload): Observable<BloodBagStockRegister> {
    return this.http.put<BloodBagStockRegister>(`${this.baseUrl}/registers/bag-stock/${id}`, payload);
  }
  public deleteBloodBagStockRegister(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/registers/bag-stock/${id}`);
  }

  public getDiagnosticKitRegisters(): Observable<DiagnosticKitRegister[]> {
    return this.http.get<DiagnosticKitRegister[]>(`${this.baseUrl}/registers/diagnostic-kits`);
  }
  public createDiagnosticKitRegister(payload: CreateDiagnosticKitRegisterPayload): Observable<DiagnosticKitRegister> {
    return this.http.post<DiagnosticKitRegister>(`${this.baseUrl}/registers/diagnostic-kits`, payload);
  }
  public updateDiagnosticKitRegister(id: string, payload: CreateDiagnosticKitRegisterPayload): Observable<DiagnosticKitRegister> {
    return this.http.put<DiagnosticKitRegister>(`${this.baseUrl}/registers/diagnostic-kits/${id}`, payload);
  }
  public deleteDiagnosticKitRegister(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/registers/diagnostic-kits/${id}`);
  }

  public getApheresisRecords(): Observable<ApheresisRecord[]> {
    return this.http.get<ApheresisRecord[]>(`${this.baseUrl}/registers/apheresis`);
  }
  public createApheresisRecord(payload: CreateApheresisRecordPayload): Observable<ApheresisRecord> {
    return this.http.post<ApheresisRecord>(`${this.baseUrl}/registers/apheresis`, payload);
  }
  public updateApheresisRecord(id: string, payload: CreateApheresisRecordPayload): Observable<ApheresisRecord> {
    return this.http.put<ApheresisRecord>(`${this.baseUrl}/registers/apheresis/${id}`, payload);
  }
  public deleteApheresisRecord(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/registers/apheresis/${id}`);
  }

  public getMasterRecords(): Observable<MasterRecordItem[]> {
    return this.http.get<MasterRecordItem[]>(`${this.baseUrl}/registers/master-record`);
  }

}
