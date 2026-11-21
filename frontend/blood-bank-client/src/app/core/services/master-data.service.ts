import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MasterDataCommand, MasterDataEntry, MasterDataType } from '../models/blood-bank.model';

@Injectable({ providedIn: 'root' })
export class MasterDataService {
  private readonly baseUrl = `${environment.apiUrl}/master-data`;

  constructor(private readonly http: HttpClient) {}

  public get(type: MasterDataType, search: string): Observable<MasterDataEntry[]> {
    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<MasterDataEntry[]>(`${this.baseUrl}/${type}`, { params });
  }

  public create(type: MasterDataType, command: MasterDataCommand): Observable<MasterDataEntry> {
    return this.http.post<MasterDataEntry>(`${this.baseUrl}/${type}`, command);
  }

  public update(
    type: MasterDataType,
    id: string,
    command: MasterDataCommand
  ): Observable<MasterDataEntry> {
    return this.http.put<MasterDataEntry>(`${this.baseUrl}/${type}/${id}`, command);
  }

  public setActive(type: MasterDataType, id: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${type}/${id}/active`, { isActive });
  }
}
