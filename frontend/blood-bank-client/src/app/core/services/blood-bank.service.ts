import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddStockPayload, BloodBag, BloodRequest, BloodStock, CreateBloodRequestPayload, CreateCampPayload, DonationCamp, DonorDetails } from '../models/blood-bank.model';

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

  // 4. Request forms management
  public getRequests(status?: string): Observable<BloodRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<BloodRequest[]>(`${this.baseUrl}/requests`, { params });
  }

  public createRequest(payload: CreateBloodRequestPayload): Observable<BloodRequest> {
    return this.http.post<BloodRequest>(`${this.baseUrl}/requests`, payload);
  }

  public processRequest(id: string, status: 'Approved' | 'Fulfilled' | 'Rejected', adminComments?: string): Observable<BloodRequest> {
    return this.http.put<BloodRequest>(`${this.baseUrl}/requests/${id}/process`, { status, adminComments });
  }
}
