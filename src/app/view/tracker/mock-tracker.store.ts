import { Injectable, computed, signal } from '@angular/core';
import {
  COLORS,
  INITIAL_STATUSES,
  RECOGNITION_RESULTS,
  SERIES,
  entryId,
  type CollectionStatus,
  type RecognitionResult,
} from './tracker-data';

const STORAGE_KEY = 'pikmin-detector-preview-v2';

interface SavedPreview {
  statuses: Record<string, CollectionStatus>;
  appliedResultIds: string[];
}

function initialPreview(): SavedPreview {
  const firstResult = RECOGNITION_RESULTS[0];
  return {
    statuses: { ...INITIAL_STATUSES, [entryId(firstResult.seriesId, firstResult.variantId, firstResult.color)]: 'collected' },
    appliedResultIds: [firstResult.id],
  };
}

function loadPreview(): SavedPreview {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed: unknown = JSON.parse(raw);
      if (typeof parsed === 'object' && parsed !== null && 'statuses' in parsed && 'appliedResultIds' in parsed) {
        const saved = parsed as SavedPreview;
        if (saved.statuses && typeof saved.statuses === 'object' && Array.isArray(saved.appliedResultIds)) {
          return saved;
        }
      }
    }
  } catch {
    // Browser storage can be unavailable; the preview remains usable in memory.
  }
  return initialPreview();
}

@Injectable({ providedIn: 'root' })
export class MockTrackerStore {
  private readonly initial = loadPreview();
  readonly statuses = signal<Record<string, CollectionStatus>>(this.initial.statuses);
  readonly appliedResultIds = signal<readonly string[]>(this.initial.appliedResultIds);

  readonly collectedCount = computed(() => this.count('collected'));
  readonly missingCount = computed(() => this.count('missing'));
  readonly unknownCount = computed(() => this.count('unknown'));
  readonly trackedSeriesCount = computed(() => SERIES.filter((series) => this.seriesMissingCount(series.id) > 0).length);
  readonly recognitionHistory = computed(() =>
    RECOGNITION_RESULTS.filter((result) => this.appliedResultIds().includes(result.id)).slice().reverse(),
  );
  readonly nextRecognition = computed(() =>
    RECOGNITION_RESULTS.find((result) => !this.appliedResultIds().includes(result.id)) ?? null,
  );

  status(seriesId: string, variantId: string, color: string): CollectionStatus {
    return this.statuses()[`${seriesId}/${variantId}/${color}`] ?? 'unknown';
  }

  seriesMissingCount(seriesId: string): number {
    const series = SERIES.find((item) => item.id === seriesId);
    if (!series) return 0;
    return series.variants.reduce(
      (count, variant) => count + COLORS.filter((color) => this.status(seriesId, variant.id, color.id) === 'missing').length,
      0,
    );
  }

  seriesCollectedCount(seriesId: string): number {
    const series = SERIES.find((item) => item.id === seriesId);
    if (!series) return 0;
    return series.variants.reduce(
      (count, variant) => count + COLORS.filter((color) => this.status(seriesId, variant.id, color.id) === 'collected').length,
      0,
    );
  }

  setStatus(seriesId: string, variantId: string, color: string, status: CollectionStatus): void {
    this.statuses.update((current) => ({ ...current, [`${seriesId}/${variantId}/${color}`]: status }));
    this.persist();
  }

  applyRecognition(result: RecognitionResult): void {
    if (this.appliedResultIds().includes(result.id)) return;
    this.statuses.update((current) => ({ ...current, [entryId(result.seriesId, result.variantId, result.color)]: 'collected' }));
    this.appliedResultIds.update((current) => [...current, result.id]);
    this.persist();
  }

  reset(): void {
    const initial = initialPreview();
    this.statuses.set(initial.statuses);
    this.appliedResultIds.set(initial.appliedResultIds);
    this.persist();
  }

  private count(status: CollectionStatus): number {
    return SERIES.reduce(
      (seriesCount, series) => seriesCount + series.variants.reduce(
        (variantCount, variant) => variantCount + COLORS.filter((color) => this.status(series.id, variant.id, color.id) === status).length,
        0,
      ),
      0,
    );
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ statuses: this.statuses(), appliedResultIds: this.appliedResultIds() }));
    } catch {
      // The in-memory preview remains usable when storage is unavailable.
    }
  }
}
