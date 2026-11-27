// The script of the design system pages. It is not part of the library.
//
// - theme, density and direction switches, kept in localStorage and settable by
//   the query string: ?theme=dark&density=compact&dir=rtl
// - the header and the list of sections
// - forced states: data-force="hover" (or active, focus-visible, focus-within)
//   on an element shows the rule of that pseudo-class on a static page
// - .docs-modes: repeats its content in the other theme, density and direction
// - specimens of tokens, measured contrast, the icon grid

(() => {
  const root = document.documentElement;
  const base = new URL('..', document.currentScript.src);

  const pages = [
    ['index.html', 'Tokens'],
    ['foundations.html', 'Foundations'],
    ['forms.html', 'Forms'],
    ['actions-overlays.html', 'Actions and overlays'],
    ['data.html', 'Data'],
    ['layout-navigation.html', 'Layout and navigation'],
    ['screens/index.html', 'Screens', true],
  ];

  const options = {
    theme: ['auto', 'light', 'dark'],
    density: ['comfortable', 'compact'],
    dir: ['ltr', 'rtl'],
  };
  const labels = { auto: 'Auto', light: 'Light', dark: 'Dark', comfortable: 'Comfortable', compact: 'Compact', ltr: 'LTR', rtl: 'RTL' };

  // Settings

  const settings = { theme: 'auto', density: 'comfortable', dir: 'ltr' };
  try {
    Object.assign(settings, JSON.parse(localStorage.getItem('fl-docs') || '{}'));
  } catch { /* storage may be blocked */ }
  const query = new URLSearchParams(location.search);
  for (const key of Object.keys(options)) {
    if (options[key].includes(query.get(key))) settings[key] = query.get(key);
    if (!options[key].includes(settings[key])) settings[key] = options[key][0];
  }

  const dark = matchMedia('(prefers-color-scheme: dark)');
  const isDark = () => settings.theme === 'dark' || (settings.theme === 'auto' && dark.matches);

  function apply() {
    if (settings.theme === 'auto') delete root.dataset.flTheme;
    else root.dataset.flTheme = settings.theme;
    root.dataset.flDensity = settings.density;
    root.dir = settings.dir;
    try {
      localStorage.setItem('fl-docs', JSON.stringify(settings));
    } catch { /* storage may be blocked */ }
    for (const button of document.querySelectorAll('.docs-switch button')) {
      button.setAttribute('aria-pressed', String(settings[button.dataset.key] === button.value));
    }
    refresh();
  }

  // Forced states: every rule with a state pseudo-class gets a twin that
  // matches data-force instead, so the library's CSS carries no docs class.

  const STATE = /:(hover|active|focus-visible|focus-within)(?![\w-])/;

  function forceStates(rules, parent) {
    for (let i = 0; i < rules.length; i++) {
      const rule = rules[i];
      if (rule.selectorText && STATE.test(rule.selectorText)) {
        const selector = rule.selectorText.replace(new RegExp(STATE, 'g'), (_, state) => `[data-force~="${state}"]`);
        const text = rule.cssText;
        // A state after a pseudo-element has no twin: the selector is not valid.
        try {
          parent.insertRule(selector + text.slice(text.indexOf('{')), i + 1);
          i++;
        } catch { /* the rule keeps its real state only */ }
      }
      if (rule.cssRules && rule.cssRules.length) forceStates(rule.cssRules, rule);
    }
  }

  function forceAll() {
    for (const sheet of document.styleSheets) {
      if (!sheet.href || !sheet.href.startsWith(new URL('css/', base).href)) continue;
      try {
        forceStates(sheet.cssRules, sheet);
      } catch (error) {
        console.error('docs: forced states failed for', sheet.href, error);
      }
    }
  }

  // Colours

  const canvas = document.createElement('canvas').getContext('2d', { willReadFrequently: true });

  function rgba(color) {
    canvas.clearRect(0, 0, 1, 1);
    canvas.fillStyle = '#000';
    canvas.fillStyle = color;
    canvas.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = canvas.getImageData(0, 0, 1, 1).data;
    return a === 0 ? [0, 0, 0, 0] : [r, g, b, a / 255];
  }

  const hex = ([r, g, b, a]) =>
    '#' + [r, g, b].map(c => Math.round(c).toString(16).padStart(2, '0')).join('').toUpperCase() + (a < 0.999 ? ` ${Math.round(a * 100)}%` : '');

  const over = (top, bottom) => [...[0, 1, 2].map(i => top[i] * top[3] + bottom[i] * (1 - top[3])), 1];

  function backgroundOf(element) {
    const layers = [];
    for (let e = element; e; e = e.parentElement) {
      const color = rgba(getComputedStyle(e).backgroundColor);
      if (color[3] > 0) layers.push(color);
      if (color[3] > 0.999) break;
    }
    return layers.reduceRight((below, layer) => over(layer, below), [255, 255, 255, 1]);
  }

  function luminance(color) {
    const [r, g, b] = color.slice(0, 3).map(c => {
      c /= 255;
      return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
  }

  function refresh() {
    for (const swatch of document.querySelectorAll('.docs-swatch[data-token]')) {
      const chip = swatch.querySelector('i');
      chip.style.backgroundColor = `var(${swatch.dataset.token})`;
      swatch.querySelector('small').textContent = hex(rgba(getComputedStyle(chip).backgroundColor));
    }
    for (const sample of document.querySelectorAll('[data-contrast]')) {
      const back = backgroundOf(sample);
      const front = over(rgba(getComputedStyle(sample).color), back);
      const [hi, lo] = [luminance(front), luminance(back)].sort((a, b) => b - a);
      sample.dataset.ratio = ((hi + 0.05) / (lo + 0.05)).toFixed(2) + ':1';
    }
    for (const frame of document.querySelectorAll('.docs-mode')) {
      const mode = frame.dataset.mode;
      let caption;
      if (mode === 'theme') {
        frame.dataset.flTheme = isDark() ? 'light' : 'dark';
        caption = isDark() ? 'Light' : 'Dark';
      } else if (mode === 'density') {
        frame.dataset.flDensity = settings.density === 'compact' ? 'comfortable' : 'compact';
        caption = settings.density === 'compact' ? 'Comfortable' : 'Compact';
      } else {
        frame.dir = settings.dir === 'rtl' ? 'ltr' : 'rtl';
        caption = settings.dir === 'rtl' ? 'Left to right' : 'Right to left';
      }
      frame.firstElementChild.textContent = caption;
    }
  }

  // Building blocks

  function element(tag, attributes = {}, ...children) {
    const e = document.createElement(tag);
    for (const [name, value] of Object.entries(attributes)) e.setAttribute(name, value);
    e.append(...children);
    return e;
  }

  function header() {
    const current = location.pathname.replace(/\/$/, '/index.html');
    const nav = element('nav', { class: 'docs-nav', 'aria-label': 'Pages' });
    for (const [path, title, planned] of pages) {
      const url = new URL(path, base);
      const here = url.pathname === current;
      nav.append(planned && !here
        ? element('span', { title: 'Not drawn yet' }, title)
        : element('a', here ? { href: url.href, 'aria-current': 'page' } : { href: url.href }, title));
    }
    const switches = element('div', { class: 'docs-switches' });
    for (const [key, values] of Object.entries(options)) {
      const group = element('div', { class: 'docs-switch', role: 'group', 'aria-label': key });
      for (const value of values) {
        const button = element('button', { type: 'button', value, 'data-key': key }, labels[value]);
        button.addEventListener('click', () => {
          settings[key] = value;
          apply();
        });
        group.append(button);
      }
      switches.append(group);
    }
    const brand = element('a', { class: 'docs-brand', href: new URL('index.html', base).href }, 'FluentCMS Components', element('small', {}, 'Design system'));
    document.body.prepend(element('header', { class: 'docs-header' }, brand, nav, switches));
  }

  function contents() {
    const main = document.querySelector('.docs-main');
    if (!main || !main.parentElement.classList.contains('docs-layout')) return;
    const toc = element('nav', { class: 'docs-toc', 'aria-label': 'On this page' }, element('strong', {}, 'On this page'));
    for (const section of main.querySelectorAll('.docs-section[id]')) {
      const heading = section.querySelector('h2');
      if (heading) toc.append(element('a', { href: `#${section.id}` }, heading.textContent));
    }
    main.before(toc);
  }

  // Attributes that hold ids, which a copy must not share with its source.
  const REFERENCES = ['for', 'aria-describedby', 'aria-labelledby', 'aria-controls', 'aria-owns', 'aria-activedescendant', 'popovertarget', 'commandfor', 'interestfor', 'list', 'form', 'name'];

  function copy(source, suffix) {
    const clone = source.cloneNode(true);
    const ids = new Set([...clone.querySelectorAll('[id]')].map(e => e.id));
    for (const e of clone.querySelectorAll('*')) {
      if (e.id) e.id += suffix;
      for (const name of REFERENCES) {
        if (!e.hasAttribute(name)) continue;
        const value = e.getAttribute(name).split(/\s+/).map(v => (ids.has(v) || name === 'name' ? v + suffix : v)).join(' ');
        e.setAttribute(name, value);
      }
      const href = e.getAttribute('href');
      if (href && href.startsWith('#') && ids.has(href.slice(1))) e.setAttribute('href', href + suffix);
    }
    return clone;
  }

  function modes() {
    for (const [index, block] of [...document.querySelectorAll('.docs-modes')].entries()) {
      const column = block.classList.contains('docs-demo--column');
      block.classList.remove('docs-demo--column');
      const template = element('div');
      template.append(...block.childNodes);
      for (const mode of ['theme', 'density', 'dir']) {
        const body = copy(template, `-${mode}${index}`);
        if (column) body.className = 'docs-demo--column';
        block.append(element('figure', { class: 'docs-mode fl-root', 'data-mode': mode }, element('figcaption'), body));
      }
    }
  }

  async function icons() {
    for (const grid of document.querySelectorAll('[data-docs-icons]')) {
      const href = new URL(grid.dataset.docsIcons, base).href;
      const sprite = new DOMParser().parseFromString(await (await fetch(href)).text(), 'image/svg+xml');
      for (const symbol of sprite.querySelectorAll('symbol')) {
        const figure = element('figure', {}, '', element('figcaption', {}, symbol.id));
        figure.insertAdjacentHTML('afterbegin', `<svg class="fl-icon fl-icon--l" aria-hidden="true"><use href="${href}#${symbol.id}"/></svg>`);
        grid.append(figure);
      }
    }
  }

  // Start. The settings go on <html> before the first paint.

  apply();

  document.addEventListener('DOMContentLoaded', () => {
    forceAll();
    header();
    contents();
    modes();
    for (const input of document.querySelectorAll('[data-indeterminate]')) input.indeterminate = true;
    apply();
    icons();
    dark.addEventListener('change', refresh);
    addEventListener('load', refresh);
  });
})();
