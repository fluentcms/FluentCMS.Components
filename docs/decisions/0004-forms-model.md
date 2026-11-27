# 0004. Forms model

Status: accepted, 27 November 2026.

## Context

A back-office library is mostly forms. Blazor offers `EditForm`, an `EditContext` that tracks changes and validation messages, and a family of `Input*` components derived from `InputBase<T>`. Section 4.5 of the [implementation plan](../planning/implementation-plan.md) asks that the library's inputs:

- bind with `@bind-Value`;
- work inside an `EditForm` and show its validation messages;
- work without one;
- post as ordinary form fields with stable names in static server-side rendering, where no .NET code runs in the browser;
- be wrapped by `FlField`, which owns the label, the help text, the error, the required mark and the character counter.

A form validation framework is not part of the library (section 3 of the plan). Validation stays with `DataAnnotationsValidator` or whatever the host chooses.

The open question is the base class of the inputs. `InputBase<T>` handles parsing, the `EditContext`, field identity and change notification. But its `SetParametersAsync` throws when there is no cascaded `EditContext`, and when `ValueExpression` is not set. The cascading parameter is private, so a derived class cannot supply a substitute. Deriving from `InputBase<T>` therefore cannot meet "work without an `EditForm`".

## Decision

**Binding.** Every input has `Value`, `ValueChanged` and `ValueExpression`, so `@bind-Value` works. `ValueExpression` is optional.

**Base class.** The inputs derive from a base class of the library's own, `FlInputBase<TValue>`, and not from `InputBase<TValue>`. The base class:

- takes an optional cascading `EditContext`;
- when there is a context and a `ValueExpression`, builds the `FieldIdentifier`, notifies the context of each change, follows its validation state and reads the messages of its field;
- when there is no context, works as a plain bound input with no validation;
- parses and formats values in one place, with a `TryParseValueFromString` that each input overrides, as `InputBase<T>` does, so that culture handling is the same;
- sets the `name` attribute from a `Name` parameter, and otherwise from the field name of `ValueExpression`, so a post in static SSR carries stable names;
- sets `id`, `disabled`, `readonly`, `required` and `aria-invalid` from its own state;
- exposes what `FlField` needs through a cascading field context: the id of the control, whether it is required or invalid, and the messages.

**`FlField`.** It renders the `<label for>`, the help text, the error and the counter, and wires `aria-describedby` and `aria-invalid` on the control it wraps through the field context, not by guessing at its child. It shows the validation messages of the `EditContext` for its field when there is one, and an error text given as a parameter when there is not. A control used without `FlField` still works; it takes its accessible name from `aria-label`.

**Static SSR.** An input renders `<input name="…" value="…">` and the form posts it. The host receives the values with `[SupplyParameterFromForm]` and form model binding, or reads the request's form. The library renders no hidden fields of its own beyond those the framework adds. Checkbox, radio, switch and select follow the rules of HTML (an unchecked box posts nothing), and each documents them.

**Interactive modes.** The input renders the same markup and updates .NET state when the browser reports a change.

## Consequences

- The inputs work in all three render modes, with and without an `EditForm`, which `InputBase<T>` cannot give.
- The library owns a base class and its upkeep. When the framework changes how field names, parsing or validation notification work, the base follows. The form issues of the plan (C-17 and C-23) include a test of the posted names in static SSR against form model binding, to keep the base honest.
- A host cannot pass a `FlTextInput` where code expects an `InputBase<string>`. Such code is rare, and the `EditContext`, not the type, is the contract with validation.
- The base class is public API, named with the `Fl` prefix and tracked by the public API analyzer. It cannot be internal, since `FlField` and the inputs need a common type.
- No dependency is added: `EditContext`, `FieldIdentifier` and the rest are in `Microsoft.AspNetCore.Components.Web`.

**What would make `InputBase<T>` the better choice:**

- The library decides that every input requires an `EditForm`. That ends the promise to work without one, and makes a bare input in a toolbar or a filter bar impossible.
- The framework makes the `EditContext` optional in `InputBase<T>`, or offers a way to supply one.
- Keeping our own base costs more than it is worth, for example because the framework's form naming becomes hard to match from outside.

In any of these cases a new record replaces this one. The inputs change their base class and keep their parameters, since the contract of this record is on the parameters.
