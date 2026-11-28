// Keep each twin in the original rule's layer and nesting, without touching live controls.
const sheetLink = 'link[data-gallery-button-states]';
const processed = new WeakSet();

// The button and card matrices force states on the element itself. The input matrix puts the
// attribute on the native control, since the host's other attributes go there, and the box reads it through :has().
const blocks = {
  '.fl-button': {
    pattern: /^&:(hover|active|focus-visible)$/,
    twin: state => `&[data-force~="${state}"]:where(#button-matrix .gallery-matrix button)`,
  },
  '.fl-card': {
    pattern: /^&\.fl-card--link:(hover|active|focus-visible)$/,
    twin: state => `&.fl-card--link[data-force~="${state}"]:where(#card-states .gallery-matrix a)`,
  },
  '.fl-input': {
    pattern: /^&(?::(hover)|:has\(> \.fl-input__control:(focus-visible)\))$/,
    twin: state => `&:has(> .fl-input__control[data-force~="${state}"]):where(#input-matrix .gallery-matrix .fl-input)`,
  },
};

function addTwins(parent) {
  for (const rule of parent.cssRules) {
    const block = blocks[rule.selectorText];
    if (block) {
      for (let i = 0; i < rule.cssRules.length; i++) {
        const state = rule.cssRules[i];
        const match = block.pattern.exec(state.selectorText);
        if (match) {
          rule.insertRule(`${block.twin(match[1] ?? match[2])} { ${state.style.cssText} }`, ++i);
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
