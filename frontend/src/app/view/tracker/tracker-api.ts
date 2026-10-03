import { environment } from '../../../environments/environment';
import { COLORS, type RecognitionResponse, type SpotResponse } from './tracker-data';

const apiBase = environment.apiBaseUrl.replace(/\/$/, '');

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem: unknown = await response.json().catch(() => null);
    const detail = typeof problem === 'object' && problem !== null && 'detail' in problem && typeof problem.detail === 'string'
      ? problem.detail : `API 回應錯誤 (${response.status})`;
    throw new Error(detail);
  }
  return response.json() as Promise<T>;
}

export async function recognizeImage(image: File): Promise<RecognitionResponse> {
  const body = new FormData();
  body.append('image', image);
  const response = await fetch(`${apiBase}/api/recognitions`, { method: 'POST', body });
  const result = await readResponse<RecognitionResponse>(response);
  if (!Array.isArray(result.series) || result.series.some((series) =>
    typeof series.decorTypeKey !== 'string' || typeof series.decorTypeName !== 'string' || !Array.isArray(series.costumes) ||
    series.costumes.some((costume) => typeof costume.costumeTypeKey !== 'string' ||
      typeof costume.costumeTypeName !== 'string' || !Array.isArray(costume.availableTypes) ||
      costume.availableTypes.some((type) => !COLORS.some((color) => color.id === type.pikminType) ||
        (type.status !== 'collected' && type.status !== 'missing'))))) {
    throw new Error('辨識 API 回傳格式不正確，紀錄未更新。');
  }
  return result;
}

export async function fetchNearbySpots(latitude: number, longitude: number, decorTypeKeys: readonly string[]): Promise<SpotResponse[]> {
  const results = await Promise.all([...new Set(decorTypeKeys)].map(async (decorTypeKey) => {
    const params = new URLSearchParams({
      latitude: String(latitude), longitude: String(longitude), radiusMeters: '5000', limit: '100', decorTypeKey,
    });
    const response = await fetch(`${apiBase}/api/spots/nearby?${params}`);
    const result = await readResponse<SpotResponse[]>(response);
    if (!Array.isArray(result)) throw new Error('附近純點 API 回傳格式不正確。');
    return result;
  }));
  return [...new Map(results.flat().map((spot) => [spot.id, spot])).values()]
    .sort((a, b) => (a.distanceMeters ?? Infinity) - (b.distanceMeters ?? Infinity));
}
