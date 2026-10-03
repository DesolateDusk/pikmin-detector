import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { isRecognizedSeries, type RecognitionResponse, type SpotResponse } from './tracker-data';

@Injectable({ providedIn: 'root' })
export class TrackerApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  async recognizeImage(image: File): Promise<RecognitionResponse> {
    const body = new FormData();
    body.append('image', image);
    const result = await this.request<RecognitionResponse>(
      firstValueFrom(this.http.post<RecognitionResponse>(`${this.baseUrl}/api/recognitions`, body)));
    if (!result || !Array.isArray(result.series) || !result.series.every(isRecognizedSeries))
      throw new Error('辨識 API 回傳格式不正確，紀錄未更新。');
    return result;
  }

  async fetchNearbySpots(latitude: number, longitude: number, decorTypeKeys: readonly string[]): Promise<SpotResponse[]> {
    const results = await Promise.all([...new Set(decorTypeKeys)].map(async decorTypeKey => {
      const params = new HttpParams().set('latitude', latitude).set('longitude', longitude)
        .set('radiusMeters', 5000).set('limit', 100).set('decorTypeKey', decorTypeKey);
      const result = await this.request(firstValueFrom(this.http.get<SpotResponse[]>(
        `${this.baseUrl}/api/spots/nearby`, { params })));
      if (!Array.isArray(result)) throw new Error('附近純點 API 回傳格式不正確。');
      return result;
    }));
    return [...new Map(results.flat().map(spot => [spot.id, spot])).values()]
      .sort((a, b) => (a.distanceMeters ?? Infinity) - (b.distanceMeters ?? Infinity));
  }

  private async request<T>(response: Promise<T>): Promise<T> {
    try {
      return await response;
    } catch (error) {
      if (!(error instanceof HttpErrorResponse)) throw error;
      const problem: unknown = error.error;
      const message = typeof problem === 'object' && problem !== null && 'detail' in problem
        && typeof problem.detail === 'string' ? problem.detail : `API 回應錯誤 (${error.status})`;
      throw new Error(message);
    }
  }
}
