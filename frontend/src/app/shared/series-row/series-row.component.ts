import { Component, input, output } from '@angular/core';
import type { RecognizedSeriesResponse } from '../../core/tracker/tracker-data';

@Component({
  selector: 'app-series-row',
  standalone: true,
  templateUrl: './series-row.component.html',
  styleUrl: './series-row.component.scss',
})
export class SeriesRowComponent {
  readonly series = input.required<RecognizedSeriesResponse>();
  readonly missingCount = input.required<number>();
  readonly selected = input(false);
  readonly showArrow = input(false);
  readonly selectSeries = output<void>();
}
