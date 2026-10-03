import type { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    title: '拾光追蹤｜皮克敏缺項紀錄',
    data: { showBrand: true },
    loadComponent: () => import('./layout/layout.component').then((module) => module.LayoutComponent),
  },
];
