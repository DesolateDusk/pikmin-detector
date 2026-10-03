import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { fetchNearbySpots, recognizeImage } from './tracker-api';
import { COLORS, type CollectionStatus, type PageId, type PikminColor, type RecognizedSeriesResponse, type SpotResponse } from './tracker-data';
import { TrackerStore } from './tracker.store';

@Component({
  selector: 'app-tracker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tracker.component.html',
  styleUrl: './tracker.component.scss',
})
export class TrackerComponent {
  readonly store = inject(TrackerStore);
  readonly standaloneMode = inject(ActivatedRoute).snapshot.data['standalone'] === true;
  readonly colors = COLORS;
  readonly page = signal<PageId>('overview');
  readonly search = signal('');
  readonly trackingFilter = signal<'missing' | 'all' | 'collected'>('missing');
  readonly selectedSeriesId = signal('');
  readonly selectedCostumeId = signal('');
  readonly selectedColorId = signal<PikminColor>('red');
  readonly selectedOverviewSeriesId = signal('');
  readonly uploadLoading = signal(false);
  readonly uploadMessage = signal('');
  readonly nearbyLoading = signal(false);
  readonly nearbyRequested = signal(false);
  readonly nearbyMessage = signal('');
  readonly spots = signal<SpotResponse[]>([]);
  readonly resetArmed = signal(false);
  readonly notice = signal('');

  readonly selectedSeries = computed<RecognizedSeriesResponse | null>(() =>
    this.store.series().find((item) => item.decorTypeKey === this.selectedSeriesId()) ?? this.store.series()[0] ?? null,
  );
  readonly selectedCostume = computed<RecognizedSeriesResponse['costumes'][number] | null>(() =>
    this.selectedSeries()?.costumes.find((item) => item.costumeTypeKey === this.selectedCostumeId())
      ?? this.selectedSeries()?.costumes[0] ?? null,
  );
  readonly selectedColors = computed(() =>
    COLORS.filter((color) => this.selectedCostume()?.availableTypes.some((type) => type.pikminType === color.id)),
  );
  readonly selectedColor = computed<(typeof COLORS)[number] | null>(() =>
    this.selectedColors().find((item) => item.id === this.selectedColorId()) ?? this.selectedColors()[0] ?? null,
  );
  readonly visibleSeries = computed(() => {
    const term = this.search().trim().toLocaleLowerCase();
    return this.store.series().filter((item) => {
      const matchesTerm = !term || `${item.decorTypeName} ${item.costumes.map((costume) => costume.costumeTypeName).join(' ')}`
        .toLocaleLowerCase().includes(term);
      const missing = this.store.seriesMissingCount(item.decorTypeKey);
      return matchesTerm && (this.trackingFilter() === 'all' ||
        (this.trackingFilter() === 'missing' ? missing > 0 : missing === 0));
    });
  });
  readonly prioritySeries = computed(() =>
    this.store.series().filter((item) => this.store.seriesMissingCount(item.decorTypeKey) > 0).slice(0, 4),
  );
  readonly overviewSeries = computed<RecognizedSeriesResponse | null>(() =>
    this.prioritySeries().find((item) => item.decorTypeKey === this.selectedOverviewSeriesId())
      ?? this.prioritySeries()[0] ?? null,
  );
  readonly nearbySpots = computed(() => this.spots().filter((spot) =>
    spot.decorTypes.some((type) => this.store.seriesMissingCount(type.key) > 0)));
  readonly overviewSpots = computed(() => this.nearbySpots().filter((spot) =>
    spot.decorTypes.some((type) => type.key === this.overviewSeries()?.decorTypeKey)).slice(0, 4));
  readonly completionPercent = computed(() => {
    const total = this.store.collectedCount() + this.store.missingCount();
    return total ? Math.round(this.store.collectedCount() / total * 100) : 0;
  });

  navigate(page: PageId): void {
    this.page.set(page);
    this.resetArmed.set(false);
    window.scrollTo({ top: 0, behavior: 'instant' });
  }

  chooseSeries(series: RecognizedSeriesResponse): void {
    this.selectedSeriesId.set(series.decorTypeKey);
    this.selectedCostumeId.set(series.costumes[0]?.costumeTypeKey ?? '');
    this.selectedColorId.set(series.costumes[0]?.availableTypes[0]?.pikminType ?? 'red');
    this.page.set('tracking');
    if (window.innerWidth < 900) {
      setTimeout(() => document.getElementById('series-detail')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
    }
  }

  chooseCostume(costumeTypeKey: string): void {
    this.selectedCostumeId.set(costumeTypeKey);
    const costume = this.selectedSeries()?.costumes.find((item) => item.costumeTypeKey === costumeTypeKey);
    this.selectedColorId.set(costume?.availableTypes[0]?.pikminType ?? 'red');
  }

  selectOverviewSeries(series: RecognizedSeriesResponse): void {
    this.selectedOverviewSeriesId.set(series.decorTypeKey);
  }

  setStatus(status: CollectionStatus): void {
    const series = this.selectedSeries();
    const costume = this.selectedCostume();
    const color = this.selectedColor();
    if (!series || !costume || !color) return;
    this.store.setStatus(series.decorTypeKey, costume.costumeTypeKey, color.id, status);
    this.flash(`${series.decorTypeName}・${color.label}已更新`);
  }

  async uploadImage(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || this.uploadLoading()) return;
    if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 10_000_000) {
      this.uploadMessage.set('請選擇 10 MB 以下的 PNG 或 JPEG 圖鑑截圖。');
      return;
    }
    this.uploadLoading.set(true);
    this.uploadMessage.set('正在辨識圖片…');
    try {
      const result = await recognizeImage(file);
      const count = result.series.flatMap((series) => series.costumes)
        .reduce((sum, costume) => sum + costume.availableTypes.length, 0);
      if (count === 0) {
        this.uploadMessage.set('沒有辨識到完整格位，原有紀錄未變更。請上傳完整的圖鑑卡片截圖。');
        return;
      }
      this.store.applyRecognition(result.series);
      this.uploadMessage.set(`已更新 ${count} 個格位；其他紀錄保留。`);
    } catch (error) {
      this.uploadMessage.set(error instanceof Error ? `辨識失敗：${error.message}` : '辨識失敗，原有紀錄未變更。');
    } finally {
      this.uploadLoading.set(false);
    }
  }

  async findNearby(): Promise<void> {
    if (this.nearbyLoading()) return;
    this.spots.set([]);
    this.nearbyRequested.set(false);
    if (!navigator.geolocation) {
      this.nearbyMessage.set('此瀏覽器不支援 GPS 定位。');
      return;
    }
    this.nearbyLoading.set(true);
    this.nearbyMessage.set('正在取得位置並查詢 5 公里內的純點…');
    try {
      const position = await new Promise<GeolocationPosition>((resolve, reject) =>
        navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }));
      const missingSeriesKeys = this.store.series()
        .filter((series) => this.store.seriesMissingCount(series.decorTypeKey) > 0)
        .map((series) => series.decorTypeKey);
      const spots = await fetchNearbySpots(position.coords.latitude, position.coords.longitude, missingSeriesKeys);
      this.spots.set(spots);
      this.nearbyRequested.set(true);
      this.nearbyMessage.set(`已查詢 5 公里內的純點，找到 ${this.nearbySpots().length} 個符合缺項系列的地點。`);
    } catch (error) {
      this.nearbyMessage.set(typeof error === 'object' && error !== null && 'code' in error
        ? '無法取得 GPS 位置。請允許定位後再試。'
        : error instanceof Error ? `查詢失敗：${error.message}` : '查詢失敗，請稍後再試。');
    } finally {
      this.nearbyLoading.set(false);
    }
  }

  setSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  matchingDecorNames(spot: SpotResponse): string {
    return spot.decorTypes.filter((type) => this.store.seriesMissingCount(type.key) > 0)
      .map((type) => this.store.series().find((series) => series.decorTypeKey === type.key)?.decorTypeName
        ?? type.name['zh-TW'] ?? type.name['en'] ?? type.key).join('、');
  }

  spotArea(spot: SpotResponse): string {
    return [spot.city, spot.area].filter(Boolean).join('・') || spot.country || '位置未標示';
  }

  distanceKm(spot: SpotResponse): string {
    return spot.distanceMeters === null ? '距離未知' : `約 ${(spot.distanceMeters / 1000).toFixed(1)} 公里`;
  }

  directionsUrl(spot: SpotResponse): string {
    return `https://www.google.com/maps/search/?api=1&query=${spot.latitude},${spot.longitude}`;
  }

  resetRecords(): void {
    if (!this.resetArmed()) {
      this.resetArmed.set(true);
      return;
    }
    this.store.reset();
    this.spots.set([]);
    this.nearbyRequested.set(false);
    this.resetArmed.set(false);
    this.flash('已清除本機辨識紀錄');
  }

  private flash(message: string): void {
    this.notice.set(message);
    setTimeout(() => {
      if (this.notice() === message) this.notice.set('');
    }, 3600);
  }
}
