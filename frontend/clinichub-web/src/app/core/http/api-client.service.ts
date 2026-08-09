import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);

  get<T>(path: string, params?: HttpParams): Observable<T> { return this.http.get<T>(`${environment.apiUrl}${path}`, { params }); }
  post<T>(path: string, body: unknown): Observable<T> { return this.http.post<T>(`${environment.apiUrl}${path}`, body); }
  put<T>(path: string, body: unknown): Observable<T> { return this.http.put<T>(`${environment.apiUrl}${path}`, body); }
  delete(path: string): Observable<void> { return this.http.delete<void>(`${environment.apiUrl}${path}`); }
}
