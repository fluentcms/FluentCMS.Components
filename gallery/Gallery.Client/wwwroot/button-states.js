// Keep each twin in the original rule's layer and nesting, without touching live controls.
const sheetLink = 'link[data-gallery-button-states]';
const processed = new WeakSet();

function addTwins(parent) {
  for (const rule of parent.cssRules) {
    if (rule.selectorText === '.fl-button') {
      for (let i = 0; i < rule.cssRules.length; i++) {
        const state = rule.cssRules[i];
        const match = /^&:(hover|active|focus-visible)$/.exec(state.selectorText);
        if (match) {
          const selector = `&[data-force~="${match[1]}"]:where(#button-matrix .gallery-matrix button)`;
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
