import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  COLORS,
  PURE_SPOTS,
  SERIES,
  type CollectionStatus,
  type DecorSeries,
  type PageId,
  type PikminColor,
} from './tracker-data';
import { MockTrackerStore } from './mock-tracker.store';

@Component({
  selector: 'app-tracker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tracker.component.html',
  styleUrl: './tracker.component.scss',
})
export class TrackerComponent {
  readonly store = inject(MockTrackerStore);
  readonly standaloneMode = inject(ActivatedRoute).snapshot.data['standalone'] === true;
  readonly colors = COLORS;
  readonly series = SERIES;
  readonly page = signal<PageId>('overview');
  readonly search = signal('');
  readonly trackingFilter = signal<'missing' | 'all' | 'collected'>('missing');
  readonly selectedSeriesId = signal('park');
  readonly selectedVariantId = signal('clover');
  readonly selectedColorId = signal<PikminColor>('winged');
  readonly selectedArea = signal('中正區');
  readonly selectedOverviewSeriesId = signal('roadside');
  readonly resetArmed = signal(false);
  readonly notice = signal('');

  readonly selectedSeries = computed(() => SERIES.find((item) => item.id === this.selectedSeriesId()) ?? SERIES[0]);
  readonly selectedVariant = computed(() =>
    this.selectedSeries().variants.find((item) => item.id === this.selectedVariantId()) ?? this.selectedSeries().variants[0],
  );
  readonly selectedColor = computed(() => COLORS.find((item) => item.id === this.selectedColorId()) ?? COLORS[0]);
  readonly visibleSeries = computed(() => {
    const term = this.search().trim().toLocaleLowerCase();
    return SERIES.filter((item) => {
      const matchesTerm = !term || `${item.name} ${item.location} ${item.variants.map((variant) => variant.name).join(' ')}`.toLocaleLowerCase().includes(term);
      const filter = this.trackingFilter();
      const matchesStatus = filter === 'all' || (filter === 'missing' ? this.store.seriesMissingCount(item.id) > 0 : this.store.seriesMissingCount(item.id) === 0);
      return matchesTerm && matchesStatus;
    });
  });
  readonly nearbySpots = computed(() =>
    PURE_SPOTS.filter((spot) => spot.area.includes(this.selectedArea()) && this.store.seriesMissingCount(spot.seriesId) > 0),
  );
  readonly completionPercent = computed(() => {
    const confirmed = this.store.collectedCount() + this.store.missingCount();
    return confirmed ? Math.round((this.store.collectedCount() / confirmed) * 100) : 0;
  });
  readonly prioritySeries = computed(() => SERIES.filter((item) => this.store.seriesMissingCount(item.id) > 0).slice(0, 4));
  readonly activeOverviewSeriesId = computed(() =>
    this.prioritySeries().some((item) => item.id === this.selectedOverviewSeriesId())
      ? this.selectedOverviewSeriesId()
      : (this.prioritySeries()[0]?.id ?? this.selectedOverviewSeriesId()),
  );
  readonly overviewSeries = computed(() => SERIES.find((item) => item.id === this.activeOverviewSeriesId()) ?? SERIES[0]);
  readonly overviewSpots = computed(() =>
    PURE_SPOTS.filter((spot) => spot.seriesId === this.activeOverviewSeriesId() && this.store.seriesMissingCount(spot.seriesId) > 0)
      .sort((a, b) => a.distanceKm - b.distanceKm),
  );

  navigate(page: PageId): void {
    this.page.set(page);
    this.resetArmed.set(false);
    window.scrollTo({ top: 0, behavior: 'instant' });
  }

  chooseSeries(series: DecorSeries): void {
    this.selectedSeriesId.set(series.id);
    this.selectedVariantId.set(series.variants[0].id);
    this.selectedColorId.set(COLORS.find((color) => this.store.status(series.id, series.variants[0].id, color.id) === 'missing')?.id ?? 'red');
    this.page.set('tracking');
    if (window.innerWidth < 900) {
      setTimeout(() => document.getElementById('series-detail')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
    }
  }

  selectOverviewSeries(series: DecorSeries): void {
    this.selectedOverviewSeriesId.set(series.id);
  }

  chooseVariant(variantId: string): void {
    this.selectedVariantId.set(variantId);
    this.selectedColorId.set(COLORS.find((color) => this.store.status(this.selectedSeriesId(), variantId, color.id) === 'missing')?.id ?? 'red');
  }

  setStatus(status: CollectionStatus): void {
    this.store.setStatus(this.selectedSeriesId(), this.selectedVariantId(), this.selectedColorId(), status);
    this.flash(`${this.selectedSeries().name}・${this.selectedColor().label}已更新`);
  }

  simulateRecognition(): void {
    const result = this.store.nextRecognition();
    if (!result) return;
    this.store.applyRecognition(result);
    this.flash(`${this.seriesName(result.seriesId)}・${this.colorName(result.color)}已自動記為已收集`);
  }

  setSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  setArea(event: Event): void {
    this.selectedArea.set((event.target as HTMLSelectElement).value);
  }

  seriesName(id: string): string {
    return SERIES.find((item) => item.id === id)?.name ?? id;
  }

  colorName(id: PikminColor): string {
    return COLORS.find((item) => item.id === id)?.label ?? id;
  }

  resetPreview(): void {
    if (!this.resetArmed()) {
      this.resetArmed.set(true);
      return;
    }
    this.store.reset();
    this.resetArmed.set(false);
    this.flash('已重設示範資料');
  }

  private flash(message: string): void {
    this.notice.set(message);
    setTimeout(() => {
      if (this.notice() === message) this.notice.set('');
    }, 3600);
  }
}
