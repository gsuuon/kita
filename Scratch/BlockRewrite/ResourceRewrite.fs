namespace BlockRewrite.ResourceRewrite

open BlockRewrite
open Providers
open Resources

type SomeProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

type ValueResourceBackend<'a> =
    abstract Value<'a> : unit -> 'a
    
type ValueResourceFrontend<'A>(backend: ValueResourceBackend<'A>) =
    // Run time object
    // Initialized and ready for use
    member _.Value () = backend.Value()

type OtherProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

type ResourceBuilder<'P, 'A when 'P :> Provider> =
    { build : 'P -> 'A }

type AnotherProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

type ResourceBlockBuilder<'P when 'P :> Provider>(provider: 'P) =
    member _.Bind
        (
            builder : ResourceBuilder<'P, 'A>,
            f
        ) =
        let x = builder.Build provider
        f x

    member _.Return x = x
    member _.Zero () = ()

module MyResources =
    let valueResource name value =
        { build = fun (p: AnotherProvider) ->
            ValueResourceFrontend
                { new ValueResourceBackend<_> with 
                    member _.Value () = value } }

module Scenario =
    let works =
        ResourceBlockBuilder<_>(SomeProvider()) {
            let! x = ValueResource("", 0)
            return ()
        }

    let works2 =
        ResourceBlockBuilder<_>(OtherProvider()) {
            let! x = ValueResource("", 0)
            return ()
        }

    let breaks =
        ResourceBlockBuilder<_>(AnotherProvider()) {
            let! x = ValueResource("", 0)
            return ()
        }

module MyResourcesScenario =
    open MyResources

    let works = 
        ResourceBlockBuilder<_>(AnotherProvider()) {
            let! x = valueResource "hey" 0
            return ()
        }
