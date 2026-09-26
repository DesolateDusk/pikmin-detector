import { initFederation } from '@angular-architects/native-federation';

initFederation()
  .then(() => import('./bootstrap'))
  .catch((error: unknown) => console.error('無法啟動圖鑑網站', error));
