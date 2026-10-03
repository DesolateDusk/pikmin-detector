import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-heading',
  standalone: true,
  templateUrl: './page-heading.component.html',
  styleUrl: './page-heading.component.scss',
})
export class PageHeadingComponent {
  readonly headingId = input.required<string>();
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly note = input('');
}
