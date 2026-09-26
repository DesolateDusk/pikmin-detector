import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, { providers: [provideRouter(routes)] })
  .catch((error: unknown) => console.error('無法載入圖鑑畫面', error));
