// Reference screens: the behaviour that the script of the library will own.
// It is not part of the library. First the frame, which every screen has,
// then the components that need a script to be live. What belongs to one
// screen alone is in a script of that page.
(() => {
  const shell = document.querySelector('.fl-app-shell');
  const sidebar = shell.querySelector('.fl-navigation');
  const drawer = shell.querySelector('.fl-app-shell__drawer');
  const icons = '../icons/lucide.svg';

  const listen = (type, selector, handler) => document.addEventListener(type, event => {
    const target = event.target instanceof Element && event.target.closest(selector);
    if (target) handler(target, event);
  });

  // ---- The frame ----

  // The navigation a second time, in the drawer. Its ids, and what points at
  // them, get a suffix.
  const copy = sidebar.cloneNode(true);
  for (const node of copy.querySelectorAll('*')) {
    for (const name of ['id', 'aria-labelledby', 'popovertarget']) {
      if (node.hasAttribute(name)) node.setAttribute(name, node.getAttribute(name) + '-drawer');
    }
  }
  copy.querySelector('[data-screen="collapse"]').remove();
  drawer.prepend(copy);
  matchMedia('(min-width: 48rem)').addEventListener('change', event => {
    if (event.matches && drawer.open) drawer.close();
  });

  // The sidebar as a rail. The name of the button says what it does next;
  // aria-expanded would draw it pressed while the sidebar is open. A native
  // title stands for the tooltip of a link.
  function collapse(on) {
    sidebar.classList.toggle('fl-navigation--collapsed', on);
    shell.classList.toggle('fl-app-shell--collapsed', on);
    sidebar.querySelector('[data-screen="collapse"]').setAttribute('aria-label', on ? 'Expand sidebar' : 'Collapse sidebar');
    for (const link of sidebar.querySelectorAll('.fl-navigation__link')) {
      if (on) link.title = link.querySelector('.fl-navigation__label').textContent;
      else link.removeAttribute('title');
    }
  }

  // The button of a menu that is open: the last one that was pressed.
  let invoker = null;

  document.addEventListener('click', event => {
    const target = event.target;
    invoker = target.closest('[popovertarget]') || invoker;

    const toggle = target.closest('[data-screen="collapse"]');
    if (toggle) collapse(!shell.classList.contains('fl-app-shell--collapsed'));

    // A group that is opened in the rail opens the sidebar.
    const summary = target.closest('.fl-navigation--collapsed summary');
    if (summary) {
      event.preventDefault();
      collapse(false);
      summary.parentElement.open = true;
    }

    // Buttons with command and commandfor, for a browser without them.
    const command = target.closest('button[commandfor]');
    if (command && !('commandForElement' in HTMLButtonElement.prototype)) {
      const dialog = document.getElementById(command.getAttribute('commandfor'));
      if (command.getAttribute('command') === 'show-modal') dialog.showModal();
      else dialog.close();
    }

    // An item of a menu: a choice is marked, an action closes the menu.
    const item = target.closest('.fl-menu[popover] .fl-menu__item');
    const role = item && item.getAttribute('role');
    if (role === 'menuitemradio') {
      for (const other of item.parentElement.querySelectorAll('[role="menuitemradio"]')) other.setAttribute('aria-checked', String(other === item));
    } else if (role === 'menuitemcheckbox') {
      item.setAttribute('aria-checked', String(item.getAttribute('aria-checked') !== 'true'));
    } else if (item) {
      item.closest('.fl-menu').hidePopover();
    }
  }, true);

  // A menu or a popover: the state of its button. Menu: focus on the first
  // item, and the arrow keys.
  document.addEventListener('toggle', event => {
    const popover = event.target;
    if (!(popover instanceof HTMLElement) || !popover.matches('.fl-menu[popover], .fl-popover[popover]')) return;
    const open = event.newState === 'open';
    for (const button of document.querySelectorAll(`[popovertarget="${popover.id}"]`)) button.setAttribute('aria-expanded', String(open && button === invoker));
    if (open && popover.matches('.fl-menu')) popover.querySelector('.fl-menu__item').focus();
  }, true);

  document.addEventListener('keydown', event => {
    const menu = event.target.closest('.fl-menu[popover]');
    if (!menu) return;
    const items = [...menu.querySelectorAll('.fl-menu__item')];
    const at = items.indexOf(document.activeElement);
    const to = { ArrowDown: at + 1, ArrowUp: at - 1, Home: 0, End: items.length - 1 }[event.key];
    if (to === undefined) return;
    event.preventDefault();
    items[(to + items.length) % items.length].focus();
  });

  // Tooltip, for a browser without interestfor: hover and keyboard focus.
  if (!('interestForElement' in HTMLButtonElement.prototype)) {
    for (const control of document.querySelectorAll('[interestfor]')) {
      const tooltip = document.getElementById(control.getAttribute('interestfor'));
      const show = () => tooltip.matches(':popover-open') || tooltip.showPopover({ source: control });
      const hide = () => tooltip.matches(':popover-open') && tooltip.hidePopover();
      control.addEventListener('pointerenter', show);
      control.addEventListener('pointerleave', hide);
      control.addEventListener('focus', () => control.matches(':focus-visible') && show());
      control.addEventListener('blur', hide);
      control.addEventListener('click', hide);
    }
  }

  // ---- Components ----

  // Number input: a step button calls the step of its input.
  listen('click', '.fl-number-input__step', step => {
    const box = step.parentElement;
    const input = box.querySelector('input');
    const [down, up] = box.querySelectorAll('.fl-number-input__step');
    if (step === up) input.stepUp();
    else input.stepDown();
    down.disabled = input.min !== '' && Number(input.value) <= Number(input.min);
    up.disabled = input.max !== '' && Number(input.value) >= Number(input.max);
    input.dispatchEvent(new Event('input', { bubbles: true }));
  });

  // File drop zone: the look while files are held over it, and the list.
  for (const type of ['dragenter', 'dragover', 'dragleave', 'drop']) {
    listen(type, '[data-drop-zone] > .fl-file-drop-zone__area', area => {
      area.toggleAttribute('data-fl-dragover', type === 'dragenter' || type === 'dragover');
    });
  }
  listen('change', '[data-drop-zone] .fl-file-drop-zone__input', input => {
    const list = input.closest('[data-drop-zone]').querySelector('.fl-file-drop-zone__list');
    if (!input.multiple) list.replaceChildren();
    for (const file of input.files) {
      const size = file.size < 1048576 ? Math.ceil(file.size / 1024) + ' KB' : (file.size / 1048576).toFixed(1) + ' MB';
      const row = document.createElement('li');
      row.className = 'fl-file-drop-zone__file fl-file-drop-zone__file--done';
      row.innerHTML = `<svg class="fl-icon fl-file-drop-zone__status" aria-hidden="true"><use href="${icons}#circle-check"/></svg><span class="fl-file-drop-zone__name"></span><span class="fl-file-drop-zone__meta"></span><span class="fl-file-drop-zone__actions"><button class="fl-button fl-button--subtle fl-button--icon fl-button--s" type="button" data-remove><svg class="fl-icon" aria-hidden="true"><use href="${icons}#trash-2"/></svg></button></span>`;
      row.children[1].textContent = file.name;
      row.children[2].textContent = size + ' · Added';
      row.querySelector('button').setAttribute('aria-label', 'Remove ' + file.name);
      list.append(row);
    }
  });
  listen('click', '[data-drop-zone] [data-remove]', button => {
    const zone = button.closest('[data-drop-zone]');
    button.closest('li').remove();
    zone.querySelector('.fl-file-drop-zone__input').focus();
  });

  // Combobox: opens the list, filters it, moves through it and chooses.
  const parts = root => ({
    input: root.querySelector('.fl-combobox__input'),
    popover: root.querySelector('.fl-combobox__popover'),
    options: [...root.querySelectorAll('.fl-combobox__option')],
    status: root.querySelector('.fl-combobox__status'),
    multiple: root.hasAttribute('data-multiple'),
  });
  const label = option => option.querySelector('.fl-combobox__option-label').textContent;
  const chosen = root => parts(root).options.filter(option => option.getAttribute('aria-selected') === 'true');

  function activate(root, option) {
    const { input, options } = parts(root);
    for (const o of options) o.toggleAttribute('data-fl-active', o === option);
    if (option) {
      input.setAttribute('aria-activedescendant', option.id);
      option.scrollIntoView({ block: 'nearest' });
    } else {
      input.removeAttribute('aria-activedescendant');
    }
  }

  function filter(root, text) {
    const { options, status } = parts(root);
    for (const option of options) option.hidden = !label(option).toLowerCase().includes(text.trim().toLowerCase());
    status.hidden = options.some(option => !option.hidden);
  }

  function open(root, show) {
    const { input, popover } = parts(root);
    if (popover.matches(':popover-open') !== show) popover.togglePopover(show);
    input.setAttribute('aria-expanded', String(show));
    if (!show) activate(root, null);
  }

  function tags(root) {
    const { input } = parts(root);
    for (const old of root.querySelectorAll('.fl-combobox__tag')) old.remove();
    for (const option of chosen(root)) {
      const tag = document.createElement('span');
      tag.className = 'fl-combobox__tag';
      tag.innerHTML = `<span class="fl-combobox__tag-label"></span><button class="fl-combobox__remove" type="button"><svg class="fl-icon fl-icon--s" aria-hidden="true"><use href="${icons}#x"/></svg></button>`;
      tag.firstChild.textContent = label(option);
      tag.lastChild.setAttribute('aria-label', 'Remove ' + label(option));
      tag.lastChild.dataset.option = option.id;
      input.before(tag);
    }
  }

  function choose(root, option) {
    const { input, options, multiple } = parts(root);
    if (option.getAttribute('aria-disabled') === 'true') return;
    if (multiple) {
      option.setAttribute('aria-selected', String(option.getAttribute('aria-selected') !== 'true'));
      input.value = '';
      filter(root, '');
      tags(root);
    } else {
      for (const o of options) o.setAttribute('aria-selected', String(o === option));
      input.value = label(option);
      open(root, false);
    }
  }

  listen('pointerdown', '[data-combobox] .fl-combobox__popover', (popover, event) => event.preventDefault());
  listen('click', '[data-combobox] .fl-combobox__option', option => choose(option.closest('[data-combobox]'), option));
  listen('click', '[data-combobox] .fl-combobox__box', (box, event) => {
    const root = box.parentElement;
    const { input, options } = parts(root);
    const remove = event.target.closest('.fl-combobox__remove');
    const action = event.target.closest('.fl-combobox__action');
    if (remove) {
      (document.getElementById(remove.dataset.option) || options.find(o => 'Remove ' + label(o) === remove.getAttribute('aria-label'))).setAttribute('aria-selected', 'false');
      tags(root);
    } else if (action && action.getAttribute('aria-label') === 'Clear') {
      for (const o of options) o.setAttribute('aria-selected', 'false');
      input.value = '';
    } else {
      filter(root, '');
      open(root, !(action && input.getAttribute('aria-expanded') === 'true'));
    }
    input.focus();
  });
  listen('input', '[data-combobox] .fl-combobox__input', input => {
    const root = input.closest('[data-combobox]');
    filter(root, input.value);
    open(root, true);
    activate(root, parts(root).options.find(option => !option.hidden && option.getAttribute('aria-disabled') !== 'true') || null);
  });
  listen('keydown', '[data-combobox] .fl-combobox__input', (input, event) => {
    const root = input.closest('[data-combobox]');
    const { options, multiple } = parts(root);
    const shown = options.filter(option => !option.hidden && option.getAttribute('aria-disabled') !== 'true');
    const active = shown.indexOf(root.querySelector('[data-fl-active]'));
    const expanded = input.getAttribute('aria-expanded') === 'true';
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      if (!expanded) {
        filter(root, '');
        open(root, true);
      }
      const list = expanded ? shown : options.filter(option => option.getAttribute('aria-disabled') !== 'true');
      const step = event.key === 'ArrowDown' ? 1 : -1;
      activate(root, list[(active + step + list.length) % list.length] || list[0] || null);
    } else if (event.key === 'Enter' && expanded && active >= 0) {
      event.preventDefault();
      choose(root, shown[active]);
    } else if (event.key === 'Escape') {
      if (expanded) open(root, false);
      else input.value = '';
    } else if (event.key === 'Backspace' && multiple && input.value === '' && chosen(root).length) {
      chosen(root).at(-1).setAttribute('aria-selected', 'false');
      tags(root);
    }
  });
  listen('focusout', '[data-combobox]', (root, event) => {
    if (root.contains(event.relatedTarget)) return;
    const { input, multiple } = parts(root);
    open(root, false);
    input.value = multiple ? '' : chosen(root).map(label)[0] || '';
  });

  // Sortable list: a button with data-move moves its item by one place,
  // wherever it is in the item; the handle lifts the item, the arrow keys
  // move it, and Escape puts it back.
  const sortableItems = list => [...list.children].filter(child => child.matches('.fl-sortable-list__item'));

  function place(list, item, to, say = true) {
    const items = sortableItems(list);
    if (to < 0 || to >= items.length) return;
    const at = items.indexOf(item);
    list.insertBefore(item, to > at ? items[to].nextSibling : items[to]);
    sortableItems(list).forEach((each, i, all) => {
      for (const up of each.querySelectorAll('[data-move="up"]')) up.disabled = i === 0;
      for (const down of each.querySelectorAll('[data-move="down"]')) down.disabled = i === all.length - 1;
    });
    const status = list.parentElement.querySelector('[data-screen="order"]');
    if (say && status) status.textContent = `${item.dataset.name}, place ${to + 1} of ${items.length}`;
  }

  listen('click', '.fl-sortable-list__handle, .fl-sortable-list__item [data-move]', button => {
    const item = button.closest('.fl-sortable-list__item');
    const list = item.parentElement;
    const at = sortableItems(list).indexOf(item);
    if (button.matches('.fl-sortable-list__handle')) {
      const lifted = button.getAttribute('aria-pressed') !== 'true';
      button.setAttribute('aria-pressed', String(lifted));
      if (lifted) item.dataset.from = at;
      return;
    }
    place(list, item, at + (button.dataset.move === 'up' ? -1 : 1));
    // From a menu, which has closed, the focus goes back to its button.
    const menu = button.closest('.fl-menu[popover]');
    (menu ? item.querySelector(`[popovertarget="${menu.id}"]`) : button.disabled ? item.querySelector('[data-move]:not(:disabled)') : button).focus();
  });

  listen('keydown', '.fl-sortable-list__handle[aria-pressed="true"]', (handle, event) => {
    const item = handle.closest('.fl-sortable-list__item');
    const list = item.parentElement;
    const at = sortableItems(list).indexOf(item);
    if (event.key === 'ArrowUp') place(list, item, at - 1);
    else if (event.key === 'ArrowDown') place(list, item, at + 1);
    else if (event.key === 'Escape') {
      place(list, item, Number(item.dataset.from), false);
      handle.setAttribute('aria-pressed', 'false');
    } else return;
    handle.focus();
    event.preventDefault();
  });

  // Toast: a copy of a <template>, added to the region and taken away after
  // five seconds unless it is an error. The time does not run while it is
  // pointed at or holds focus. A button with data-toast shows one; so does a
  // form with data-toast when it is submitted and nothing stopped it.
  const region = document.querySelector('.fl-toast-region');

  function toast(template) {
    const node = document.getElementById(template).content.firstElementChild.cloneNode(true);
    let timer = 0;
    const close = () => {
      clearTimeout(timer);
      node.remove();
      if (!region.childElementCount && region.matches(':popover-open')) region.hidePopover();
    };
    const start = () => {
      if (node.getAttribute('role') !== 'alert') timer = setTimeout(close, 5000);
    };
    for (const button of node.querySelectorAll('button')) button.addEventListener('click', close);
    node.addEventListener('pointerenter', () => clearTimeout(timer));
    node.addEventListener('focusin', () => clearTimeout(timer));
    node.addEventListener('pointerleave', start);
    node.addEventListener('focusout', start);
    region.append(node);
    if (!region.matches(':popover-open')) region.showPopover();
    start();
  }

  listen('click', 'button[data-toast]', button => toast(button.dataset.toast));
  listen('submit', 'form[data-toast]', (form, event) => {
    if (event.defaultPrevented) return;
    event.preventDefault();
    toast(form.dataset.toast);
  });

  // A dialog that opens later would lie over the region: the region is shown
  // again, which puts it back on top.
  document.addEventListener('toggle', event => {
    if (!region || !(event.target instanceof HTMLDialogElement) || event.newState !== 'open' || !region.matches(':popover-open')) return;
    region.hidePopover();
    region.showPopover();
  }, true);
})();
