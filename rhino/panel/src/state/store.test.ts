import assert from 'node:assert/strict';
import test from 'node:test';
import { Store } from './store.ts';

test('an identical notice bumps the toast already showing instead of stacking', () => {
  const store = new Store();

  store.apply({ type: 'notice', level: 'warn', text: 'Nothing selected.' });
  store.apply({ type: 'notice', level: 'warn', text: 'Nothing selected.' });
  store.apply({ type: 'notice', level: 'warn', text: 'Nothing selected.' });

  assert.equal(store.notices().length, 1);
  assert.equal(store.notices()[0]?.repeats(), 2);
});

test('a different text or level is its own notice', () => {
  const store = new Store();

  store.apply({ type: 'notice', level: 'info', text: 'Saved.' });
  store.apply({ type: 'notice', level: 'info', text: 'Saved to disk.' });
  store.apply({ type: 'notice', level: 'error', text: 'Saved.' });

  assert.deepEqual(
    store.notices().map((notice) => `${notice.level}:${notice.text}`),
    ['info:Saved.', 'info:Saved to disk.', 'error:Saved.'],
  );
});

test('the same text shows again once the earlier one is gone', () => {
  const store = new Store();

  store.apply({ type: 'notice', level: 'info', text: 'Reverted that turn.' });
  const first = store.notices()[0];
  assert.ok(first);
  store.dismissNotice(first.id);
  store.apply({ type: 'notice', level: 'info', text: 'Reverted that turn.' });

  assert.equal(store.notices().length, 1);
  assert.notEqual(store.notices()[0]?.id, first.id);
});
