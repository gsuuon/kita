## Errors


### Provider / resource mismatch is a compile error
Not all resources have to be implemented for every provider. If a resource is not implemented for the provider context, a compile-time error will be shown: No overloads match for method 'Bind'.

```
let app = infra'<Local> "myapp" {
        let! m = DoesntWorkWithLocalResource() // [41]: No overloads match
        ...
    }
```

If you have a root `infra'<SomeProvider>`, changing the provider type will cause compile errors in all places where the resource doesn't support the new provider.
