// Keep each twin in the original rule's layer and nesting, without touching live controls.
const sheetLink = 'link[data-gallery-button-states]';
const processed = new WeakSet();

function addTwins(parent) {
  for (const rule of parent.cssRules) {
    const forced = rule.selectorText === '.fl-button'
      ? { state: /^&:(hover|active|focus-visible)$/, prefix: '&', scope: '#button-matrix .gallery-matrix button' }
      : rule.selectorText === '.fl-card'
        ? { state: /^&\.fl-card--link:(hover|active|focus-visible)$/, prefix: '&.fl-card--link', scope: '#card-states .gallery-matrix a' }
        : null;
    if (forced) {
      for (let i = 0; i < rule.cssRules.length; i++) {
        const state = rule.cssRules[i];
        const match = forced.state.exec(state.selectorText);
        if (match) {
          const selector = `${forced.prefix}[data-force~="${match[1]}"]:where(${forced.scope})`;
          rule.insertRule(`${selector} { ${state.style.cssText} }`, ++i);
        }
      }
    } else if (!rule.selectorText && rule.cssRules) {
      addTwins(rule);
    }
  }
}

function prepare(link) {
  const sheet = link.sheet;
  if (!sheet?.cssRules.length || processed.has(sheet)) return;
  addTwins(sheet);
  processed.add(sheet);
}

// Selectors survive the interactive render; a replaced stylesheet gets its own twins on load.
document.addEventListener('load', event => {
  if (event.target.matches?.(sheetLink)) prepare(event.target);
}, true);
document.querySelectorAll(sheetLink).forEach(prepare);
