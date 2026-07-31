import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from './services/api.service';
import { Result, ValueData, ResultFilter } from './models/api.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private apiService = inject(ApiService);
  private cdr = inject(ChangeDetectorRef);

  // 1. Секция загрузки CSV
  selectedFile: File | null = null;
  isUploading = false;
  uploadSuccess: string | null = null;
  uploadError: string | null = null;

  // 2. Таблица Results и фильтры
  results: Result[] = [];
  isLoadingResults = false;

  filter: ResultFilter = {
    fileName: '',
    minStartDateTime: '',
    maxStartDateTime: '',
    minAverageValue: undefined,
    maxAverageValue: undefined,
    minAverageExecutionTime: undefined,
    maxAverageExecutionTime: undefined,
    pageNumber: 1,
    pageSize: 10
  };

  // 3. Получение последних 10 значений из Values по названию файла
  searchFileNameFor10: string = '';
  searchedFileName: string | null = null;
  last10Values: ValueData[] = [];
  isLoadingDetails = false;
  detailsError: string | null = null;

  ngOnInit(): void {
    this.loadResults();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.uploadSuccess = null;
      this.uploadError = null;
    }
  }

  // МЕТОД 1: Загрузка файла
  uploadFile(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadSuccess = null;
    this.uploadError = null;

    this.apiService.uploadFile(this.selectedFile).subscribe({
      next: (response) => {
        let successMsg = 'Файл успешно обработан и сохранен!';

        if (typeof response === 'string') {
          try {
            const parsed = JSON.parse(response);
            successMsg = parsed.message || parsed.title || response;
          } catch {
            successMsg = response;
          }
        } else if (typeof response === 'object' && response !== null) {
          successMsg = (response as any).message || JSON.stringify(response);
        }

        this.uploadSuccess = successMsg;
        this.selectedFile = null;
        this.isUploading = false;

        const input = document.getElementById('csvFileInput') as HTMLInputElement;
        if (input) input.value = '';

        this.loadResults();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isUploading = false;
        let errorMsg = 'Ошибка валидации или обработки CSV файла.';

        if (err.error) {
          if (typeof err.error === 'string') {
            try {
              const parsed = JSON.parse(err.error);
              errorMsg = parsed.message || parsed.title || err.error;
            } catch {
              errorMsg = err.error;
            }
          } else if (typeof err.error === 'object') {
            errorMsg = err.error.message || err.error.title || JSON.stringify(err.error);
          }
        }

        this.uploadError = errorMsg;
        this.cdr.detectChanges();
      }
    });
  }

  // МЕТОД 2: Получение записей из Results по фильтрам
  loadResults(): void {
    this.isLoadingResults = true;
    this.apiService.getResults(this.filter).subscribe({
      next: (data) => {
        this.results = data;
        this.isLoadingResults = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Ошибка при получении списка Results:', err);
        this.isLoadingResults = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilter(): void {
    this.filter.pageNumber = 1;
    this.loadResults();
  }

  resetFilter(): void {
    this.filter = {
      fileName: '',
      minStartDateTime: '',
      maxStartDateTime: '',
      minAverageValue: undefined,
      maxAverageValue: undefined,
      minAverageExecutionTime: undefined,
      maxAverageExecutionTime: undefined,
      pageNumber: 1,
      pageSize: 10
    };
    this.loadResults();
  }

  changePage(delta: number): void {
    if (this.filter.pageNumber + delta >= 1) {
      this.filter.pageNumber += delta;
      this.loadResults();
    }
  }

  // МЕТОД 3: Поиск последних 10 записей из Values
  fetchLast10Values(): void {
    const fileName = this.searchFileNameFor10.trim();
    if (!fileName) return;

    this.searchedFileName = fileName;
    this.isLoadingDetails = true;
    this.detailsError = null;
    this.last10Values = [];

    this.apiService.getLast10Values(fileName).subscribe({
      next: (data) => {
        this.last10Values = data;
        this.isLoadingDetails = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Ошибка при получении 10 записей:', err);
        this.isLoadingDetails = false;
        this.detailsError = 'Не удалось загрузить записи для данного файла.';
        this.cdr.detectChanges();
      }
    });
  }
}
