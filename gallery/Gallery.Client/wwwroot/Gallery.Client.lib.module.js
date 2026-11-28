function register(blazor) {
  blazor.registerCustomEventType('flsamplechange', {
    browserEventName: 'change',
    createEventArgs: event => ({ value: event.target.value }),
  });
}

export { register as afterWebStarted, register as afterStarted };
