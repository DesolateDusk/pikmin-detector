import type { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    title: '拾光追蹤｜皮克敏缺項紀錄',
    data: { standalone: true },
    loadComponent: () => import('./view/tracker/tracker.component').then((module) => module.TrackerComponent),
  },
];
