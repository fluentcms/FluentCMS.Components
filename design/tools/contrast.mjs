// Checks the colour tokens of src/FluentCMS.Components/Styles/tokens.css against WCAG 2.2 AA
// in both themes and prints a table. Exits with 1 when a pair fails.
//
//   node design/tools/contrast.mjs
//
// Text needs 4.5:1. Borders of controls, marks and the focus ring need 3:1.
// Disabled colours are exempt (WCAG 1.4.3) and are not listed.

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const file = fileURLToPath(new URL('../../src/FluentCMS.Components/Styles/tokens.css', import.meta.url));
const css = readFileSync(file, 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');

const tokens = new Map();
for (const [, name, value] of css.matchAll(/(--fl-[\w-]+)\s*:\s*([^;]+);/g)) {
  if (!tokens.has(name)) tokens.set(name, value.trim());
}

const TEXT = 4.5;
const MARK = 3;

const surfaces = ['canvas', 'surface', 'surface-subtle', 'surface-muted', 'surface-raised'];
const tones = ['neutral', 'success', 'warning', 'danger', 'info', 'pending'];

// [foreground, background, minimum]. A background may be a list of layers, top first.
const pairs = [
  ...surfaces.map(s => ['text', s, TEXT]),
  ['text', ['hover', 'surface'], TEXT],
  ['text', ['pressed', 'surface'], TEXT],
  ['text', ['hover', 'canvas'], TEXT],
  ['text', 'accent-soft', TEXT],
  ['text', 'accent-soft-hover', TEXT],
  ['text', ['hover', 'surface-subtle'], TEXT],
  ['text', ['pressed', 'surface-subtle'], TEXT],
  ['text', ['pressed', 'canvas'], TEXT],
  ...surfaces.map(s => ['text-muted', s, TEXT]),
  ['text-muted', ['hover', 'surface'], TEXT],
  ['text-muted', ['pressed', 'surface'], TEXT],
  ['text-muted', ['hover', 'canvas'], TEXT],
  ['text-muted', 'accent-soft', TEXT],
  ['text-muted', ['hover', 'surface-raised'], TEXT],
  ['text-muted', ['pressed', 'surface-raised'], TEXT],
  ['text-muted', 'danger-soft', TEXT],
  ...surfaces.map(s => ['text-subtle', s, TEXT]),
  ['text-subtle', ['hover', 'surface'], TEXT],
  ['text-subtle', ['hover', 'canvas'], TEXT],
  ['text-subtle', 'accent-soft', TEXT],
  ['text-inverse', 'surface-inverse', TEXT],
  ['text-inverse-subtle', 'surface-inverse', TEXT],
  ...surfaces.map(s => ['accent-text', s, TEXT]),
  ['accent-text', 'accent-soft', TEXT],
  ['accent-text', 'accent-soft-hover', TEXT],
  ['accent-text', ['hover', 'surface'], TEXT],
  ['on-accent', 'accent', TEXT],
  ['on-accent', 'accent-hover', TEXT],
  ['on-accent', 'accent-pressed', TEXT],
  ...tones.flatMap(t => [
    [`${t}-text`, `${t}-soft`, TEXT],
    [`${t}-text`, 'canvas', TEXT],
    [`${t}-text`, 'surface', TEXT],
    [`${t}-text`, 'surface-subtle', TEXT],
    [`${t}-text`, 'surface-raised', TEXT],
    [`${t}-on-solid`, `${t}-solid`, TEXT],
    [`${t}-solid`, 'canvas', MARK],
    [`${t}-solid`, 'surface', MARK],
    [`${t}-solid`, 'surface-raised', MARK],
  ]),
  ['danger-on-solid', 'danger-solid-hover', TEXT],
  ['danger-on-solid', 'danger-solid-pressed', TEXT],
  ['danger-text', 'danger-border', TEXT],
  ...[1, 2, 3, 4, 5, 6].map(n => ['on-category', `category-${n}`, TEXT]),
  ...surfaces.map(s => ['border-control', s, MARK]),
  ...surfaces.map(s => ['accent', s, MARK]),
  ...surfaces.map(s => ['focus', s, MARK]),
  ['focus', 'accent-soft', MARK],
  ['focus-halo', 'accent', MARK],
  ['focus-halo', 'danger-solid', MARK],
  ['focus-halo', 'surface-inverse', MARK],
];

function splitArgs(text) {
  const parts = [];
  let depth = 0;
  let start = 0;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '(') depth++;
    else if (c === ')') depth--;
    else if (c === ',' && depth === 0) {
      parts.push(text.slice(start, i).trim());
      start = i + 1;
    }
  }
  parts.push(text.slice(start).trim());
  return parts;
}

function call(value, name) {
  return value.startsWith(`${name}(`) && value.endsWith(')') ? value.slice(name.length + 1, -1) : null;
}

const toLinear = c => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
const toGamma = c => (c <= 0.0031308 ? c * 12.92 : 1.055 * c ** (1 / 2.4) - 0.055);

function toOklab([r, g, b]) {
  const [lr, lg, lb] = [r, g, b].map(toLinear);
  const l = Math.cbrt(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
  const m = Math.cbrt(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
  const s = Math.cbrt(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
  return [
    0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
    1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
    0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s,
  ];
}

function fromOklab([L, a, b]) {
  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3;
  return [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ].map(c => Math.min(1, Math.max(0, toGamma(c))));
}

function mix(args, theme) {
  const [space, ...colors] = splitArgs(args);
  if (colors.length !== 2 || !/^in (oklab|srgb)$/.test(space)) throw new Error(`Unsupported color-mix(${args})`);
  const parsed = colors.map(part => {
    const m = part.match(/^(.*?)\s+([\d.]+)%$/s);
    return m ? { color: resolve(m[1], theme), p: Number(m[2]) } : { color: resolve(part, theme), p: null };
  });
  let [p1, p2] = parsed.map(x => x.p);
  if (p1 === null && p2 === null) p1 = p2 = 50;
  else if (p1 === null) p1 = 100 - p2;
  else if (p2 === null) p2 = 100 - p1;
  const w = p2 / (p1 + p2);
  const [a, b] = parsed.map(x => x.color);
  if (a[3] !== 1 || b[3] !== 1) throw new Error(`color-mix with alpha is not supported: ${args}`);
  const lerp = (x, y) => x.map((v, i) => v + (y[i] - v) * w);
  const rgb = space === 'in oklab' ? fromOklab(lerp(toOklab(a), toOklab(b))) : lerp(a.slice(0, 3), b.slice(0, 3));
  return [...rgb, 1];
}

// Returns [r, g, b, a] in 0..1 for the theme, 0 for light and 1 for dark.
function resolve(value, theme) {
  value = value.trim();
  let inner;
  if ((inner = call(value, 'var')) !== null) {
    const name = splitArgs(inner)[0];
    if (!tokens.has(name)) throw new Error(`Unknown token ${name}`);
    return resolve(tokens.get(name), theme);
  }
  if ((inner = call(value, 'light-dark')) !== null) return resolve(splitArgs(inner)[theme], theme);
  if ((inner = call(value, 'color-mix')) !== null) return mix(inner, theme);
  if ((inner = call(value, 'rgb')) !== null) {
    const m = inner.match(/^([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?$/);
    if (!m) throw new Error(`Unsupported rgb(${inner})`);
    return [m[1] / 255, m[2] / 255, m[3] / 255, m[4] === undefined ? 1 : Number(m[4])];
  }
  const hex = value.match(/^#([0-9a-f]{6})$/i);
  if (hex) return [0, 2, 4].map(i => parseInt(hex[1].slice(i, i + 2), 16) / 255).concat(1);
  throw new Error(`Unsupported colour: ${value}`);
}

const over = (top, bottom) => [...top.slice(0, 3).map((c, i) => c * top[3] + bottom[i] * (1 - top[3])), 1];

function flatten(layers, theme) {
  const list = (Array.isArray(layers) ? layers : [layers]).map(n => resolve(`var(--fl-color-${n})`, theme));
  const base = list.pop();
  if (base[3] !== 1) throw new Error(`The lowest layer must be opaque: ${layers}`);
  return list.reduceRight((below, layer) => over(layer, below), base);
}

function luminance([r, g, b]) {
  return 0.2126 * toLinear(r) + 0.7152 * toLinear(g) + 0.0722 * toLinear(b);
}

function contrast(fg, bg, theme) {
  const back = flatten(bg, theme);
  const front = over(resolve(`var(--fl-color-${fg})`, theme), back);
  const [hi, lo] = [luminance(front), luminance(back)].sort((a, b) => b - a);
  return (hi + 0.05) / (lo + 0.05);
}

const label = bg => (Array.isArray(bg) ? `${bg[0]} on ${bg[1]}` : bg);
const rows = pairs.map(([fg, bg, min]) => {
  const light = contrast(fg, bg, 0);
  const dark = contrast(fg, bg, 1);
  return { fg, bg: label(bg), min, light, dark, ok: light >= min && dark >= min };
});

const w1 = Math.max(...rows.map(r => r.fg.length), 10);
const w2 = Math.max(...rows.map(r => r.bg.length), 10);
const line = (a, b, c, d, e, f) => `${a.padEnd(w1)}  ${b.padEnd(w2)}  ${c.padStart(6)}  ${d.padStart(6)}  ${e.padStart(4)}  ${f}`;
console.log(line('Foreground', 'Background', 'Light', 'Dark', 'Min', 'Result'));
console.log(line('-'.repeat(w1), '-'.repeat(w2), '------', '------', '----', '------'));
for (const r of rows) {
  console.log(line(r.fg, r.bg, r.light.toFixed(2), r.dark.toFixed(2), String(r.min), r.ok ? 'pass' : 'FAIL'));
}

const failed = rows.filter(r => !r.ok);
console.log(`\n${rows.length} pairs in two themes, ${failed.length} failed.`);
process.exit(failed.length ? 1 : 0);
