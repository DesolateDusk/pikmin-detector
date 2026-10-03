import { Component, computed, inject, output, signal } from '@angular/core';
import { TrackerApi } from '../../core/tracker/tracker-api';
import { COLORS, type CollectionStatus, type PikminColor, type RecognizedSeriesResponse } from '../../core/tracker/tracker-data';
import { TrackerStore } from '../../core/tracker/tracker.store';
import { PageHeadingComponent } from '../../shared/page-heading/page-heading.component';
import { SeriesRowComponent } from '../../shared/series-row/series-row.component';
import { SeriesDetailComponent } from './series-detail/series-detail.component';

@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [PageHeadingComponent, SeriesRowComponent, SeriesDetailComponent],
  templateUrl: './tracking.component.html',
  styleUrl: './tracking.component.scss',
})
export class TrackingComponent {
  private readonly api = inject(TrackerApi);
  readonly store = inject(TrackerStore);
  readonly notice = output<string>();
  readonly search = signal('');
  readonly trackingFilter = signal<'missing' | 'all' | 'collected'>('missing');
  readonly selectedSeriesId = signal('');
  readonly selectedCostumeId = signal('');
  readonly selectedColorId = signal<PikminColor>('red');
  readonly uploadLoading = signal(false);
  readonly uploadMessage = signal('');
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
  chooseSeries(series: RecognizedSeriesResponse): void {
    this.selectedSeriesId.set(series.decorTypeKey);
    this.selectedCostumeId.set(series.costumes[0]?.costumeTypeKey ?? '');
    this.selectedColorId.set(series.costumes[0]?.availableTypes[0]?.pikminType ?? 'red');
    if (window.innerWidth < 900) {
      setTimeout(() => document.getElementById('series-detail')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
    }
  }

  chooseCostume(costumeTypeKey: string): void {
    this.selectedCostumeId.set(costumeTypeKey);
    const costume = this.selectedSeries()?.costumes.find((item) => item.costumeTypeKey === costumeTypeKey);
    this.selectedColorId.set(costume?.availableTypes[0]?.pikminType ?? 'red');
  }

  setStatus(status: CollectionStatus): void {
    const series = this.selectedSeries();
    const costume = this.selectedCostume();
    const color = this.selectedColor();
    if (!series || !costume || !color) return;
    this.store.setStatus(series.decorTypeKey, costume.costumeTypeKey, color.id, status);
    this.notice.emit(`${series.decorTypeName}・${color.label}已更新`);
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
      const result = await this.api.recognizeImage(file);
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

  setSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }
}
