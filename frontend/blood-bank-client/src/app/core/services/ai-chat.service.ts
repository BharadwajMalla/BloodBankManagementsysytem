import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChatTurn {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatReply {
  answer: string;
  dataAsOfUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private readonly endpoint = `${environment.apiUrl}/Chat`;

  constructor(private readonly http: HttpClient) {}

  ask(message: string, history: ChatTurn[]): Observable<ChatReply> {
    return this.http.post<ChatReply>(this.endpoint, { message, history });
  }
}
