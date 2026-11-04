import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BloodRequest, CreateBloodRequestPayload, DonationCamp, DonorDetails } from '../models/blood-bank.model';

@Injectable({
  providedIn: 'root'
})
export class DonorService {
  private readonly baseUrl = `${environment.apiUrl}/Donor`;

  constructor(private http: HttpClient) {}

  public getUpcomingCamps(): Observable<DonationCamp[]> {
    return this.http.get<DonationCamp[]>(`${this.baseUrl}/camps`);
  }

  public submitRequest(payload: CreateBloodRequestPayload): Observable<BloodRequest> {
    return this.http.post<BloodRequest>(`${this.baseUrl}/request`, payload);
  }

  public getMyRequests(): Observable<BloodRequest[]> {
    return this.http.get<BloodRequest[]>(`${this.baseUrl}/my-requests`);
  }

  public getProfile(): Observable<DonorDetails> {
    return this.http.get<DonorDetails>(`${this.baseUrl}/profile`);
  }
}
