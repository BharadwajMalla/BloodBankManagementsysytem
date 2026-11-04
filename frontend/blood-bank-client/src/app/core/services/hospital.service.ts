import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BloodRequest, BloodStock, CreateBloodRequestPayload } from '../models/blood-bank.model';

@Injectable({
  providedIn: 'root'
})
export class HospitalService {
  private readonly baseUrl = `${environment.apiUrl}/Hospital`;

  constructor(private http: HttpClient) {}

  public submitRequisition(payload: CreateBloodRequestPayload): Observable<BloodRequest> {
    return this.http.post<BloodRequest>(`${this.baseUrl}/request`, payload);
  }

  public getMyRequests(): Observable<BloodRequest[]> {
    return this.http.get<BloodRequest[]>(`${this.baseUrl}/my-requests`);
  }

  public getAvailableStockSummary(): Observable<BloodStock[]> {
    return this.http.get<BloodStock[]>(`${this.baseUrl}/available-stock-summary`);
  }
}
