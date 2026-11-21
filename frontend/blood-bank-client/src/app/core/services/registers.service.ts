import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BloodRequest } from '../models/blood-bank.model';

@Injectable({
  providedIn: 'root'
})
export class RegistersService {
  constructor(private readonly http: HttpClient) {}

  public getIssueRecords(status?: string): Observable<BloodRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<BloodRequest[]>(`${environment.apiUrl}/issues`, { params });
  }
}
