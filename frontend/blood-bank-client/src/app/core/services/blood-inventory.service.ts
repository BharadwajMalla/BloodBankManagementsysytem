import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddStockRequest, BloodInventory } from '../models/blood-inventory.model';

@Injectable({
  providedIn: 'root'
})
export class BloodInventoryService {
  private readonly endpoint = `${environment.apiUrl}/BloodInventory`;

  constructor(private http: HttpClient) {}

  public getAll(bloodGroup?: string): Observable<BloodInventory[]> {
    let params = new HttpParams();
    if (bloodGroup && bloodGroup.trim() !== '') {
      params = params.set('bloodGroup', bloodGroup.trim());
    }
    return this.http.get<BloodInventory[]>(this.endpoint, { params });
  }

  public addStock(payload: AddStockRequest): Observable<BloodInventory> {
    return this.http.post<BloodInventory>(this.endpoint, payload);
  }
}
