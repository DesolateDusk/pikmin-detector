import '@angular/compiler';
import { createEnvironmentInjector, runInInjectionContext } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import assert from 'node:assert/strict';
import { after, test } from 'node:test';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { resolve, sep } from 'node:path';
import ts from 'typescript';

// Compile the actual services for Node without adding another test runner.
const cacheRoot = fileURLToPath(new URL('../../../../.angular/', import.meta.url));
mkdirSync(cacheRoot, { recursive: true });
const compiledRoot = mkdtempSync(resolve(cacheRoot, 'tracker-tests-'));
for (const name of ['tracker-data', 'tracker-api', 'tracker.store', 'nearby.store']) {
  const source = readFileSync(new URL(`./${name}.ts`, import.meta.url), 'utf8')
    .replace(/from '\.\/([^']+)'/g, "from './$1.mjs'")
    .replace("from '../../../environments/environment'", "from './environment.mjs'");
  const compiled = ts.transpileModule(source, { compilerOptions: {
    target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022, experimentalDecorators: true,
  }});
  writeFileSync(resolve(compiledRoot, `${name}.mjs`), compiled.outputText);
}
writeFileSync(resolve(compiledRoot, 'environment.mjs'), "export const environment = { apiBaseUrl: 'https://api.example/' };");
after(() => {
  if (!resolve(compiledRoot).startsWith(resolve(cacheRoot) + sep)) throw new Error('Invalid test output path');
  rmSync(compiledRoot, { recursive: true });
});
const { TrackerApi } = await import(pathToFileURL(resolve(compiledRoot, 'tracker-api.mjs')));
const { TrackerStore } = await import(pathToFileURL(resolve(compiledRoot, 'tracker.store.mjs')));
const { NearbyStore } = await import(pathToFileURL(resolve(compiledRoot, 'nearby.store.mjs')));

const series = () => ({
  decorTypeKey: 'cafe', decorTypeName: '咖啡廳', costumes: [{
    costumeTypeKey: 'coffee_cup', costumeTypeName: '咖啡杯', availableTypes: [
      { pikminType: 'red', status: 'missing' }, { pikminType: 'blue', status: 'collected' },
    ],
  }],
});

function createApi(t, client) {
  const injector = createEnvironmentInjector([{ provide: HttpClient, useValue: client }, TrackerApi]);
  t.after(() => injector.destroy());
  return runInInjectionContext(injector, () => injector.get(TrackerApi));
}

test('辨識服務透過 DI 上傳圖片並拒絕錯誤資料', async t => {
  let response = { series: [series()] };
  const api = createApi(t, { post(url, body) {
    assert.equal(url, 'https://api.example/api/recognitions');
    assert.equal(body.get('image').name, 'collection.png');
    return of(response);
  }});
  const image = new File(['sample'], 'collection.png', { type: 'image/png' });
  assert.deepEqual(await api.recognizeImage(image), response);
  response = { series: [null] };
  await assert.rejects(api.recognizeImage(image), /回傳格式不正確/);
});

test('附近服務查詢唯一系列、合併重複地點並依距離排列', async t => {
  const requested = [];
  const api = createApi(t, { get(url, { params }) {
    assert.equal(url, 'https://api.example/api/spots/nearby');
    assert.equal(params.get('latitude'), '25');
    assert.equal(params.get('longitude'), '121');
    assert.equal(params.get('radiusMeters'), '5000');
    requested.push(params.get('decorTypeKey'));
    return of(params.get('decorTypeKey') === 'cafe'
      ? [{ id: 'far', distanceMeters: 200 }, { id: 'near', distanceMeters: 10 }]
      : [{ id: 'near', distanceMeters: 10 }]);
  }});
  assert.deepEqual((await api.fetchNearbySpots(25, 121, ['cafe', 'park', 'cafe'])).map(spot => spot.id), ['near', 'far']);
  assert.deepEqual(requested, ['cafe', 'park']);
});

test('服務保留 ProblemDetails 的訊息並處理無 detail 的錯誤', async t => {
  let detail = { detail: 'Use a portrait image.' };
  const api = createApi(t, { post() {
    return throwError(() => new HttpErrorResponse({ status: 422, error: detail }));
  }});
  const image = new File(['sample'], 'collection.png');
  await assert.rejects(api.recognizeImage(image), /Use a portrait image/);
  detail = {};
  await assert.rejects(api.recognizeImage(image), /API 回應錯誤 \(422\)/);
});

test('收集狀態只更新指定格位、持久化並在重新建立服務後保留', t => {
  const previous = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
  let stored = '[]';
  Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: {
    getItem: () => stored, setItem: (_key, value) => { stored = value; },
  }});
  t.after(() => {
    if (previous) Object.defineProperty(globalThis, 'localStorage', previous);
    else delete globalThis.localStorage;
  });
  const store = new TrackerStore();
  store.applyRecognition([series()]);
  store.setStatus('cafe', 'coffee_cup', 'red', 'collected');
  assert.equal(store.missingCount(), 0);
  assert.equal(store.collectedCount(), 2);
  const restored = new TrackerStore();
  assert.equal(restored.status('cafe', 'coffee_cup', 'red'), 'collected');
  restored.setStatus('cafe', 'coffee_cup', 'ice', 'missing');
  assert.equal(restored.collectedCount(), 2);
  restored.reset();
  assert.deepEqual(JSON.parse(stored), []);
});

test('附近查詢重設後不讓舊回應重新填回結果', async t => {
  const previous = Object.getOwnPropertyDescriptor(globalThis, 'navigator');
  Object.defineProperty(globalThis, 'navigator', { configurable: true, value: {
    geolocation: { getCurrentPosition: success => success({ coords: { latitude: 25, longitude: 121 } }) },
  }});
  t.after(() => {
    if (previous) Object.defineProperty(globalThis, 'navigator', previous);
    else delete globalThis.navigator;
  });
  let finish;
  let requested;
  const started = new Promise(resolve => { requested = resolve; });
  const injector = createEnvironmentInjector([TrackerStore, NearbyStore, { provide: TrackerApi, useValue: {
    fetchNearbySpots() {
      requested();
      return new Promise(resolve => { finish = resolve; });
    },
  }}]);
  t.after(() => injector.destroy());
  injector.get(TrackerStore).applyRecognition([series()]);
  const nearby = injector.get(NearbyStore);
  const pending = nearby.findNearby();
  await started;
  assert.equal(nearby.loading(), true);
  nearby.reset();
  finish([{ id: 'obsolete', decorTypes: [{ key: 'cafe' }] }]);
  await pending;
  assert.deepEqual(nearby.spots(), []);
  assert.equal(nearby.requested(), false);
  assert.equal(nearby.loading(), false);
  assert.equal(nearby.message(), '');
});
