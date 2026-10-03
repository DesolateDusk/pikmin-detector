import { Injectable, computed, signal } from '@angular/core';
import { COLORS, mergeRecognition, type CollectionStatus, type PikminColor, type RecognizedSeriesResponse } from './tracker-data';

const STORAGE_KEY = 'pikmin-detector-recognitions-v1';

function loadEntries(): RecognizedSeriesResponse[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
    if (Array.isArray(parsed)) return parsed as RecognizedSeriesResponse[];
  } catch {
    // Storage can be unavailable; state still works for this session.
  }
  return [];
}

@Injectable({ providedIn: 'root' })
export class TrackerStore {
  readonly series = signal<RecognizedSeriesResponse[]>(loadEntries());
  readonly missingCount = computed(() => this.count('missing'));
  readonly collectedCount = computed(() => this.count('collected'));
  readonly trackedSeriesCount = computed(() => this.series().filter((item) => this.seriesMissingCount(item.decorTypeKey) > 0).length);
  readonly completedSeriesCount = computed(() => this.series().filter((item) => this.seriesMissingCount(item.decorTypeKey) === 0).length);

  seriesMissingCount(decorTypeKey: string): number {
    return this.series().find((item) => item.decorTypeKey === decorTypeKey)?.costumes
      .flatMap((costume) => costume.availableTypes)
      .filter((type) => type.status === 'missing').length ?? 0;
  }

  status(decorTypeKey: string, costumeTypeKey: string, pikminType: PikminColor): CollectionStatus | null {
    return this.series().find((item) => item.decorTypeKey === decorTypeKey)?.costumes
      .find((item) => item.costumeTypeKey === costumeTypeKey)?.availableTypes
      .find((item) => item.pikminType === pikminType)?.status ?? null;
  }

  hasType(decorTypeKey: string, costumeTypeKey: string, pikminType: PikminColor): boolean {
    return this.status(decorTypeKey, costumeTypeKey, pikminType) !== null;
  }

  applyRecognition(incoming: readonly RecognizedSeriesResponse[]): void {
    this.series.update((current) => mergeRecognition(current, incoming));
    this.persist();
  }

  setStatus(decorTypeKey: string, costumeTypeKey: string, pikminType: PikminColor, status: CollectionStatus): void {
    if (!this.hasType(decorTypeKey, costumeTypeKey, pikminType)) return;
    this.series.update((current) => mergeRecognition(current, [{
      decorTypeKey,
      decorTypeName: current.find((item) => item.decorTypeKey === decorTypeKey)?.decorTypeName ?? decorTypeKey,
      costumes: [{
        costumeTypeKey,
        costumeTypeName: current.find((item) => item.decorTypeKey === decorTypeKey)?.costumes
          .find((item) => item.costumeTypeKey === costumeTypeKey)?.costumeTypeName ?? costumeTypeKey,
        availableTypes: [{ pikminType, status }],
      }],
    }]));
    this.persist();
  }

  reset(): void {
    this.series.set([]);
    this.persist();
  }

  private count(status: CollectionStatus): number {
    return this.series().flatMap((series) => series.costumes)
      .flatMap((costume) => costume.availableTypes)
      .filter((type) => type.status === status && COLORS.some((color) => color.id === type.pikminType)).length;
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.series()));
    } catch {
      // The current session remains usable when browser storage is unavailable.
    }
  }
}
