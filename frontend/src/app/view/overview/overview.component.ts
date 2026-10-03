import { Component, computed, inject, output, signal } from '@angular/core';
import type { PageId, RecognizedSeriesResponse } from '../../core/tracker/tracker-data';
import { TrackerStore } from '../../core/tracker/tracker.store';
import { NearbyStore } from '../../core/tracker/nearby.store';
import { PageHeadingComponent } from '../../shared/page-heading/page-heading.component';
import { SeriesRowComponent } from '../../shared/series-row/series-row.component';
import { spotArea, distanceKm, directionsUrl } from '../../shared/spot-format';

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [PageHeadingComponent, SeriesRowComponent],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class OverviewComponent {
  readonly store = inject(TrackerStore);
  readonly nearby = inject(NearbyStore);
  readonly navigate = output<PageId>();
  readonly selectedOverviewSeriesId = signal('');
  readonly spotArea = spotArea;
  readonly distanceKm = distanceKm;
  readonly directionsUrl = directionsUrl;
  readonly prioritySeries = computed(() =>
    this.store.series().filter((item) => this.store.seriesMissingCount(item.decorTypeKey) > 0).slice(0, 4),
  );
  readonly overviewSeries = computed<RecognizedSeriesResponse | null>(() =>
    this.prioritySeries().find((item) => item.decorTypeKey === this.selectedOverviewSeriesId())
      ?? this.prioritySeries()[0] ?? null,
  );
  readonly overviewSpots = computed(() => this.nearby.matchingSpots().filter((spot) =>
    spot.decorTypes.some((type) => type.key === this.overviewSeries()?.decorTypeKey)).slice(0, 4));
  readonly completionPercent = computed(() => {
    const total = this.store.collectedCount() + this.store.missingCount();
    return total ? Math.round(this.store.collectedCount() / total * 100) : 0;
  });

  selectOverviewSeries(series: RecognizedSeriesResponse): void {
    this.selectedOverviewSeriesId.set(series.decorTypeKey);
  }

}
