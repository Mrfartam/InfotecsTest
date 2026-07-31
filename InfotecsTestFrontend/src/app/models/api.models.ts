export interface Result {
  name: string;
  deltaDate: number;
  startDateTime: string;
  averageExecutionTime: number;
  averageValue: number;
  medianValue: number;
  minValue: number;
  maxValue: number;
}

export interface ValueData {
  id: number;
  name: string;
  date: string;
  executionTime: number;
  value: number;
}

export interface ResultFilter {
  fileName?: string;
  minStartDateTime?: string;
  maxStartDateTime?: string;
  minAverageExecutionTime?: number;
  maxAverageExecutionTime?: number;
  minAverageValue?: number;
  maxAverageValue?: number;
  pageNumber: number;
  pageSize: number;
}
