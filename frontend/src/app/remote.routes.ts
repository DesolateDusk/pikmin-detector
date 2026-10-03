import type { Routes } from '@angular/router';

export const remoteRoutes: Routes = [
  {
    path: '',
    title: '拾光追蹤｜皮克敏缺項紀錄',
    data: { showBrand: false },
    loadComponent: () => import('./layout/layout.component').then((module) => module.LayoutComponent),
  },
];
