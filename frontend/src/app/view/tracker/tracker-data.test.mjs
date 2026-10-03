import assert from 'node:assert/strict';
import test from 'node:test';
import { mergeRecognition } from './tracker-data.ts';

const cafe = (status) => ({
  decorTypeKey: 'cafe', decorTypeName: '咖啡廳', costumes: [{
    costumeTypeKey: 'coffee_cup', costumeTypeName: '咖啡杯',
    availableTypes: [{ pikminType: 'red', status }, { pikminType: 'blue', status: 'missing' }],
  }],
});

test('重複辨識只覆蓋本張格位並保留其他款式與系列', () => {
  const current = [cafe('missing'), {
    decorTypeKey: 'park', decorTypeName: '公園', costumes: [{
      costumeTypeKey: 'clover', costumeTypeName: '三葉草',
      availableTypes: [{ pikminType: 'ice', status: 'missing' }],
    }],
  }];
  const update = [{ ...cafe('collected'), costumes: [{
    ...cafe('collected').costumes[0], availableTypes: [{ pikminType: 'red', status: 'collected' }],
  }] }];

  const result = mergeRecognition(current, update);

  assert.deepEqual(result[0].costumes[0].availableTypes, [
    { pikminType: 'red', status: 'collected' }, { pikminType: 'blue', status: 'missing' },
  ]);
  assert.deepEqual(result[1], current[1]);
  assert.equal(current[0].costumes[0].availableTypes[0].status, 'missing');
});

test('新的完整格位可加入現有系列', () => {
  const result = mergeRecognition([cafe('missing')], [{
    decorTypeKey: 'cafe', decorTypeName: '咖啡廳', costumes: [{
      costumeTypeKey: 'rare_coffee_cup', costumeTypeName: '稀有咖啡杯',
      availableTypes: [{ pikminType: 'yellow', status: 'collected' }],
    }],
  }]);

  assert.equal(result[0].costumes.length, 2);
  assert.equal(result[0].costumes[1].availableTypes[0].status, 'collected');
});
