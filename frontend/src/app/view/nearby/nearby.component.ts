import { Component, inject, output } from '@angular/core';
import type { PageId, SpotResponse } from '../../core/tracker/tracker-data';
import { TrackerStore } from '../../core/tracker/tracker.store';
import { NearbyStore } from '../../core/tracker/nearby.store';
import { PageHeadingComponent } from '../../shared/page-heading/page-heading.component';
import { spotArea, distanceKm, directionsUrl } from '../../shared/spot-format';

@Component({
  selector: 'app-nearby',
  standalone: true,
  imports: [PageHeadingComponent],
  templateUrl: './nearby.component.html',
  styleUrl: './nearby.component.scss',
})
export class NearbyComponent {
  readonly store = inject(TrackerStore);
  readonly nearby = inject(NearbyStore);
  readonly navigate = output<PageId>();
  readonly spotArea = spotArea;
  readonly distanceKm = distanceKm;
  readonly directionsUrl = directionsUrl;
  matchingDecorNames(spot: SpotResponse): string {
    return spot.decorTypes.filter((type) => this.store.seriesMissingCount(type.key) > 0)
      .map((type) => this.store.series().find((series) => series.decorTypeKey === type.key)?.decorTypeName
        ?? type.name['zh-TW'] ?? type.name['en'] ?? type.key).join('、');
  }

}
