import type { Routes } from '@angular/router';

export const remoteRoutes: Routes = [
  {
    path: '',
    title: '拾光追蹤｜皮克敏缺項紀錄',
    data: { standalone: false },
    loadComponent: () => import('./view/tracker/tracker.component').then((module) => module.TrackerComponent),
  },
];
