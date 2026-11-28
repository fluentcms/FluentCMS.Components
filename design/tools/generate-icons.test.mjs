import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { generateIcons } from './generate-icons.mjs';

const sprite = (...ids) => `<svg xmlns="http://www.w3.org/2000/svg">\n${ids.map(id =>
  `<symbol id="${id}" viewBox="0 0 24 24"><path d="M0 0" /></symbol>`).join('\n')}\n</svg>\n`;

test('PascalCase distinguishes keywords and prefixes a leading digit', () => {
  const { library, gallery } = generateIcons(sprite('triangle-alert', 'class', '3d-box'));
  for (const name of ['TriangleAlert', 'Class', 'Icon3dBox']) {
    assert.ok(library.includes(`FlIconDefinition ${name} =>`));
    assert.ok(gallery.includes(`FlIcons.${name}`));
  }
});

test('rejects name collisions and unexpected sprite records rather than dropping a drawing', () => {
  assert.throws(() => generateIcons(sprite('test-1', 'test1')), /duplicate/i);
  assert.throws(() => generateIcons(sprite('test').replace('0 0 24 24', '0 0 20 20')), /symbol/i);
});

test('both committed outputs are current', () => {
  execFileSync(process.execPath, [fileURLToPath(new URL('./generate-icons.mjs', import.meta.url)), '--check']);
});
