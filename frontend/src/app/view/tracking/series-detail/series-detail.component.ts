import { Component, input, output } from '@angular/core';
import type { ColorOption, CollectionStatus, PikminColor, RecognizedCostumeResponse, RecognizedSeriesResponse } from '../../../core/tracker/tracker-data';

@Component({
  selector: 'app-series-detail',
  standalone: true,
  templateUrl: './series-detail.component.html',
  styleUrl: './series-detail.component.scss',
})
export class SeriesDetailComponent {
  readonly series = input<RecognizedSeriesResponse | null>(null);
  readonly selectedCostume = input<RecognizedCostumeResponse | null>(null);
  readonly colors = input<readonly ColorOption[]>([]);
  readonly selectedColor = input<ColorOption | null>(null);
  readonly missingCount = input(0);
  readonly costumeSelected = output<string>();
  readonly colorSelected = output<PikminColor>();
  readonly statusChanged = output<CollectionStatus>();

  status(costumeKey: string, color: PikminColor): CollectionStatus | null {
    return this.series()?.costumes.find(item => item.costumeTypeKey === costumeKey)?.availableTypes.find(item => item.pikminType === color)?.status ?? null;
  }
}
