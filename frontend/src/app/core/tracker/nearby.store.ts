import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { TrackerApi } from './tracker-api';
import type { SpotResponse } from './tracker-data';
import { TrackerStore } from './tracker.store';

@Injectable()
export class NearbyStore {
  private readonly tracker = inject(TrackerStore);
  private readonly api = inject(TrackerApi);
  private requestId = 0;
  readonly loading = signal(false);
  readonly requested = signal(false);
  readonly message = signal('');
  readonly spots = signal<SpotResponse[]>([]);
  readonly matchingSpots = computed(() => this.spots().filter(spot => spot.decorTypes.some(type => this.tracker.seriesMissingCount(type.key) > 0)));

  constructor() {
    inject(DestroyRef).onDestroy(() => this.requestId++);
  }

  async findNearby(): Promise<void> {
    if (this.loading()) return;
    const requestId = ++this.requestId;
    this.spots.set([]);
    this.requested.set(false);
    if (!navigator.geolocation) {
      this.message.set('此瀏覽器不支援 GPS 定位。');
      return;
    }
    this.loading.set(true);
    this.message.set('正在取得位置並查詢 5 公里內的純點…');
    try {
      const position = await new Promise<GeolocationPosition>((resolve, reject) =>
        navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }));
      if (requestId !== this.requestId) return;
      const missingSeriesKeys = this.tracker.series()
        .filter((series) => this.tracker.seriesMissingCount(series.decorTypeKey) > 0)
        .map((series) => series.decorTypeKey);
      const spots = await this.api.fetchNearbySpots(position.coords.latitude, position.coords.longitude, missingSeriesKeys);
      if (requestId !== this.requestId) return;
      this.spots.set(spots);
      this.requested.set(true);
      this.message.set(`已查詢 5 公里內的純點，找到 ${this.matchingSpots().length} 個符合缺項系列的地點。`);
    } catch (error) {
      if (requestId !== this.requestId) return;
      this.message.set(typeof error === 'object' && error !== null && 'code' in error
        ? '無法取得 GPS 位置。請允許定位後再試。'
        : error instanceof Error ? `查詢失敗：${error.message}` : '查詢失敗，請稍後再試。');
    } finally {
      if (requestId === this.requestId) this.loading.set(false);
    }
  }
  reset(): void {
    this.requestId++;
    this.loading.set(false);
    this.spots.set([]);
    this.requested.set(false);
    this.message.set('');
  }
}
