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

The open question was the base class of the inputs: `InputBase<T>`, or a base of the library's own. A first draft of this record assumed that `InputBase<T>` cannot work without an `EditForm`. That was wrong, and it was found by testing.

### What was tested

On 27 November 2026, with .NET SDK 10.0.401 and the shared framework `Microsoft.AspNetCore.App` 10.0.12, in a scratch web application outside this repository (static server-side rendering, no render mode, requests made over HTTP, including a form post with its antiforgery token). The scratch code is not committed.

| Case | Result |
| --- | --- |
| `InputText` with `@bind-Value`, no `EditForm` | Renders: `<input name="v" value="hello">`. The name is the text of the bound expression. |
| `InputText` with `Value` and `ValueChanged`, no `ValueExpression`, no `EditForm` | Throws `InvalidOperationException`: requires a value for the `ValueExpression` parameter. |
| `InputText` with `Value` only | The same exception. |
| `InputText` with an explicit `name="custom"` attribute | Renders `name="custom"`. |
| `InputText` in an `EditForm` with a `FormName` and `[SupplyParameterFromForm] Model`, bound to `Model.Name`, `Model.Address.City` and `Model.Tags[0]` | Renders `name="Model.Name"`, `name="Model.Address.City"` and `name="Model.Tags[0]"`. |
| The same form, posted with values | Re-renders with every value bound back into the model, flat, nested and indexed. |
| The same form, posted empty, with `DataAnnotationsValidator` and a `[Required]` property | The invalid `InputText` renders `aria-invalid="true"` and `class="invalid"`. |
| A class derived from `InputBase<string>`, no `EditForm` | Renders, with `EditContext` null. Its `NameAttributeValue` is the name from the expression, and `CurrentValueAsString` is the value. |
| The same derived class in an `EditForm`, after an invalid post | Reads its `FieldIdentifier` (field `Name` of `Model`) and the message `Name is required` through `EditContext.GetValidationMessages`. |

Two other things were seen and are worth knowing. `DataAnnotationsValidator` validated the top-level `Name` but not `Address.City` of the nested object, which is how the framework's validator works and not a property of the inputs. The cascaded `EditContext` is optional: nothing in `InputBase<T>` requires it.

## Decision

**Binding.** Every input has `Value`, `ValueChanged` and `ValueExpression`, so `@bind-Value` works.

**Base class.** The inputs that hold one value derive from `InputBase<TValue>`. The tests show that it meets every requirement above, and the framework then owns what the library would otherwise copy and see drift: parsing, the field identity, change notification, the validation state and the `name` that a form post depends on in static rendering. The library adds only what is its own: the markup and classes of the design system, `FlField` wiring, and the parameters of each input.

**Limit: a `ValueExpression` is required.** `InputBase<T>` throws without one, with or without an `EditForm`. Writing `@bind-Value` always supplies it. A one-way use, such as `Value="@x"` with no change callback, must give `ValueExpression="() => x"` too, or use a read-only element. The library does not work around this: the exception names the missing parameter, and the gallery shows the two correct forms. If a case appears where an input must exist with no expression, it is a new record.

**`name`.** An input posts under the name the framework derives from the expression, or under a `name` attribute given by the host, which wins. The library does not invent names, so form model binding with `[SupplyParameterFromForm]` finds the values, as tested.

**`FlField`.** It renders the `<label for>`, the help text, the error and the counter, and wires `aria-describedby` and `aria-invalid` on the control it wraps. The control tells it its id and its state through a small cascading context that `FlField` provides and the inputs read, so the field does not guess at its child. Messages come from the `EditContext` when there is one, through the field identity of the input, and from an `Error` text given as a parameter when there is not. An input used without `FlField` still works and takes its accessible name from `aria-label`. This part is design, not yet tested; the form issues of the plan (C-17 and C-23) build and test it.

**Inputs that are not one value.** `FlRadioGroup`, `FlSegmented` and `FlCheckbox` with a mixed state may need markup or parameters that `InputBase<T>` does not shape. They still derive from it where one value is bound. A case where that does not fit is decided in its own issue, with the fact named.

**Static SSR.** An input renders a native element with its `name` and `value`, and the form posts it. The host receives the values with `[SupplyParameterFromForm]` and form model binding. Checkbox, radio, switch and select follow the rules of HTML (an unchecked box posts nothing), and each documents them.

## Consequences

- The library owns no copy of the framework's form logic. When the framework changes how names, parsing or validation work, the inputs follow with no work.
- The inputs are `InputBase<T>`, so host code and tools that expect that type accept them.
- The public API of every input includes the members of `InputBase<T>`, such as `AdditionalAttributes` and `CurrentValueAsString`. That is accepted: the host's own inputs have them too.
- The library cannot render an input with no `ValueExpression`. This is the cost of the choice, and the one fact that restricts it.
- `InputBase<T>` sets validation classes itself (`valid` and `invalid` were seen in the tests). The stylesheet reads `aria-invalid`, as the design system does, so those classes are left unstyled and do not become a second source of state.
- The nested-object case needs a validator that walks objects. That is the host's choice; the library does not add one.
- No dependency is added: `InputBase<T>`, `EditContext` and `FieldIdentifier` are in `Microsoft.AspNetCore.Components.Web`.

**What would make a base class of the library's own right:**

- A requirement for an input without a `ValueExpression`, for example a bare control driven only by a value and a callback in a filter bar, that the host cannot meet with `ValueExpression="() => x"`.
- A behaviour of `InputBase<T>` that the library cannot work with, for instance its validation classes becoming impossible to ignore, or its parse path unable to express a type the library needs.
- The framework changing `InputBase<T>` to demand an `EditContext` again.

In any of these cases a new record replaces this one. The inputs change their base class and keep their parameters, since the contract of this record is on the parameters.
