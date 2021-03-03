## Thought heap
Can I use `[<Literal>]` to make primitives available to compiler as metadata?

## Overload + SRTP
Using overloaded Deploy + SRTP means no way simple way to extend with additional providers externally, at least until this PR is in:
https://github.com/dotnet/fsharp/pull/6805

Workaround:
Provider config can be inherited in a straightforward way and plugged in.
Inherit from the cloud classes, implement custom provider Deploy override, and replace in all infra usages.

(The extra step here is that all usages of the class must be replaced, whereas when extensions are visible simply loading the extensions methods into scope should suffice, assuming overload resolution works too)
