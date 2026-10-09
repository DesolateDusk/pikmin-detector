import { constants, copyFileSync, mkdirSync, writeFileSync } from 'node:fs';

const apiBaseUrl = (process.env.API_BASE_URL ?? '').trim().replace(/\/+$/, '');
const localFile = new URL('../src/environments/environment.ts', import.meta.url);
mkdirSync(new URL('./', localFile), { recursive: true });
// Angular fileReplacements 需要原檔存在；保留開發者既有的本機設定。
try {
  copyFileSync(new URL('../src/environments/environment.example.ts', import.meta.url),
    localFile, constants.COPYFILE_EXCL);
} catch (error) {
  if (error.code !== 'EEXIST') throw error;
}
writeFileSync(new URL('../src/environments/environment.production.ts', import.meta.url),
  `// 建置時依 API_BASE_URL 產生；此網址會公開在前端。\nexport const environment = {\n  apiBaseUrl: ${JSON.stringify(apiBaseUrl)},\n};\n`);
