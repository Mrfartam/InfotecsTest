import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Result, ValueData, ResultFilter } from '../models/api.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:7223/api';

  // 1. Метод загрузки CSV
  uploadFile(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post(`${this.apiUrl}/upload`, formData, {
      responseType: 'text'
    });
  }

  // 2. Метод получения Results с фильтрацией
  getResults(filter: ResultFilter): Observable<Result[]> {
    let params = new HttpParams();

    if (filter.fileName?.trim()) {
      params = params.set('fileName', filter.fileName.trim());
    }
    if (filter.minStartDateTime) {
      const minDate = new Date(filter.minStartDateTime).toISOString();
      params = params.set('minStartDateTime', minDate);
    }
    if (filter.maxStartDateTime) {
      const maxDate = new Date(filter.maxStartDateTime).toISOString();
      params = params.set('maxStartDateTime', maxDate);
    }
    if (filter.minAverageValue !== undefined && filter.minAverageValue !== null) {
      params = params.set('minAverageValue', filter.minAverageValue);
    }
    if (filter.maxAverageValue !== undefined && filter.maxAverageValue !== null) {
      params = params.set('maxAverageValue', filter.maxAverageValue);
    }
    if (filter.minAverageExecutionTime !== undefined && filter.minAverageExecutionTime !== null) {
      params = params.set('minAverageExecutionTime', filter.minAverageExecutionTime);
    }
    if (filter.maxAverageExecutionTime !== undefined && filter.maxAverageExecutionTime !== null) {
      params = params.set('maxAverageExecutionTime', filter.maxAverageExecutionTime);
    }
    if (filter.pageNumber) {
      params = params.set('pageNumber', filter.pageNumber);
    }
    if (filter.pageSize) {
      params = params.set('pageSize', filter.pageSize);
    }

    return this.http.get<Result[]>(`${this.apiUrl}/results`, { params });
  }

  // 3. Метод получения последних 10 записей (Values)
  getLast10Values(fileName: string): Observable<ValueData[]> {
    return this.http.get<ValueData[]>(`${this.apiUrl}/last10values`, {
      params: new HttpParams().set('fileName', fileName.trim())
    });
  }
}
