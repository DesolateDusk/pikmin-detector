import { Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import type { PageId } from '../core/tracker/tracker-data';
import { TrackerStore } from '../core/tracker/tracker.store';
import { NearbyStore } from '../core/tracker/nearby.store';
import { OverviewComponent } from '../view/overview/overview.component';
import { TrackingComponent } from '../view/tracking/tracking.component';
import { NearbyComponent } from '../view/nearby/nearby.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [OverviewComponent, TrackingComponent, NearbyComponent],
  providers: [NearbyStore],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss',
})
export class LayoutComponent {
  readonly store = inject(TrackerStore);
  readonly nearby = inject(NearbyStore);
  readonly showBrand = inject(ActivatedRoute).snapshot.data['showBrand'] === true;
  readonly page = signal<PageId>('overview');
  readonly resetArmed = signal(false);
  readonly notice = signal('');
  private noticeTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.noticeTimer));
  }

  navigate(page: PageId): void {
    this.page.set(page);
    this.resetArmed.set(false);
    window.scrollTo({ top: 0, behavior: 'instant' });
  }

  resetRecords(): void {
    if (!this.resetArmed()) {
      this.resetArmed.set(true);
      return;
    }
    this.store.reset();
    this.nearby.reset();
    this.resetArmed.set(false);
    this.flash('已清除本機辨識紀錄');
  }

  flash(message: string): void {
    this.notice.set(message);
    clearTimeout(this.noticeTimer);
    this.noticeTimer = setTimeout(() => {
      if (this.notice() === message) this.notice.set('');
    }, 3600);
  }
}
