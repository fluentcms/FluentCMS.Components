const supports = {
  popover: 'popover' in HTMLElement.prototype,
  mixed: 'indeterminate' in HTMLInputElement.prototype,
  steps: 'stepUp' in HTMLInputElement.prototype && 'stepDown' in HTMLInputElement.prototype,
};

const mixedAttribute = 'data-fl-indeterminate';
const checkbox = 'input.fl-checkbox__control[type="checkbox"]';
const numberBox = '.fl-number-input';
const number = 'input.fl-number-input__control[type="number"]';
const stepButton = 'button.fl-number-input__step';
const panel = ':is(.fl-popover, .fl-menu)[popover][id]';
const invoker = 'button[popovertarget]:not([popovertargetaction="hide" i])';
const needsSetup = `${checkbox}[${mixedAttribute}], ${numberBox}, ${panel}, ${invoker}`;

function syncInvokers(id, open) {
  for (const button of document.querySelectorAll(`${invoker}[popovertarget="${CSS.escape(id)}"]`)) {
    button.setAttribute('aria-expanded', String(open));
  }
}

function onToggle({ target, newState }) {
  if (target.matches?.(panel)) syncInvokers(target.id, newState === 'open');
}

function numericValue(input) {
  return input.value === '' ? null : input.valueAsNumber;
}

function syncSteps(box) {
  const input = box.querySelector(number);
  const [down, up] = box.querySelectorAll(stepButton);
  if (!input || !down || !up) return;

  const locked = input.disabled || input.readOnly || input.step.toLowerCase() === 'any';
  // Let the browser resolve the step grid (including decimal steps and an off-grid max).
  let probe;
  const atLimit = direction => {
    probe ??= input.cloneNode();
    probe.value = input.value;
    probe[direction]();
    return numericValue(probe) === numericValue(input);
  };
  down.toggleAttribute('disabled', locked || (input.min !== '' && atLimit('stepDown')));
  up.toggleAttribute('disabled', locked || (input.max !== '' && atLimit('stepUp')));
}

function onStep({ target }) {
  const button = target.closest?.(stepButton);
  const box = button?.closest(numberBox);
  const input = box?.querySelector(number);
  if (!input || button.disabled || input.matches(':disabled, [readonly]') || input.step.toLowerCase() === 'any') return;

  const [down, up] = box.querySelectorAll(stepButton);
  if (!down || !up) return;
  const before = numericValue(input);
  if (button === down) input.stepDown();
  else if (button === up) input.stepUp();
  if (numericValue(input) !== before) {
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
  } else {
    syncSteps(box);
  }
}

function syncNumber(input) {
  const box = input.matches?.(number) && input.closest(numberBox);
  if (box) syncSteps(box);
}

function onReset(event) {
  if (!(event.target instanceof HTMLFormElement)) return;
  // The cancelable reset event precedes the browser's restoration of the form values.
  setTimeout(() => {
    if (!event.defaultPrevented) {
      for (const control of event.target.elements) syncNumber(control);
    }
  }, 0);
}

function apply(element, changedPanels) {
  if (supports.mixed && element.matches(checkbox)) element.indeterminate = element.hasAttribute(mixedAttribute);
  if (supports.steps && element.matches(numberBox)) syncSteps(element);
  if (supports.popover && element.matches(panel)) changedPanels?.add(element.id);
  if (supports.popover && element.matches(invoker)) {
    const target = element.popoverTargetElement;
    if (target?.matches(panel)) element.setAttribute('aria-expanded', String(target.matches(':popover-open')));
  }
}

function visit(root, selector, action) {
  if (!(root instanceof Element)) return;
  if (root.matches(selector)) action(root);
  if (root.firstElementChild) {
    for (const element of root.querySelectorAll(selector)) action(element);
  }
}

function onMutations(records) {
  const changedPanels = new Set();
  for (const { type, target, addedNodes, removedNodes, attributeName } of records) {
    if (type === 'attributes') {
      if (attributeName === mixedAttribute) apply(target);
      else if (supports.steps) syncNumber(target);
    } else {
      if (supports.steps && target.matches?.(numberBox)) syncSteps(target);
      for (const node of addedNodes) {
        visit(node, needsSetup, el => apply(el, changedPanels));
      }
      if (supports.popover) {
        for (const node of removedNodes) visit(node, panel, el => changedPanels.add(el.id));
      }
    }
  }
  for (const id of changedPanels) {
    syncInvokers(id, document.getElementById(id)?.matches(`${panel}:popover-open`) ?? false);
  }
}

let started = false;
function start() {
  if (started) return;
  started = true;
  if (supports.popover) document.addEventListener('toggle', onToggle, true);
  if (supports.steps) {
    document.addEventListener('click', onStep);
    document.addEventListener('reset', onReset, true);
    for (const type of ['input', 'change']) {
      document.addEventListener(type, ({ target }) => syncNumber(target));
    }
  }
  visit(document.documentElement, needsSetup, apply);
  new MutationObserver(onMutations).observe(document.documentElement, {
    subtree: true, childList: true, attributes: true,
    attributeFilter: [mixedAttribute, 'data-fl-value', 'min', 'max', 'step', 'value', 'readonly', 'disabled'],
  });
}

export { start as afterWebStarted, start as afterStarted };
start();
