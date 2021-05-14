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

type IResourceBuilder<'P, 'A when 'P :> Provider > =
    abstract Build : 'P -> 'A

type ResourceBuilderMarker = interface end

type AnotherProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

type ResourceBlockBuilder< ^P when 'P :> Provider>(provider: 'P) = // Just using this here to pin the type, in real use this wouldn't be an argument
    // FIXME think I need to use the inherit trick to make provider public, or this breaks at actual compile time (but doesn't error in checker)
    
    // SRTP
    member inline _.Bind
        (
            builder : ^B when 'B :> ResourceBuilderMarker
                          and 'B : (member Build : 'P -> ^A),
            f
        ) =
        let x = ( ^B : (member Build : 'P -> 'A) (builder, provider) )
        f x

    // Interface
    member inline _.Bind
        (
            builder : IResourceBuilder<'P, 'A>,
            f
        ) =
        let x = builder.Build provider
        f x

    // Record
    member inline _.Bind
        (
            builder : ResourceBuilder<'P, 'A>,
            f
        ) =
        let x = builder.build provider
        f x

    member inline _.Return x = x
    member inline _.Zero () = ()

module MyResources =
    // Record
    type ValueResourceRecord() =
        static member Builder value =
            { build = fun (_p: SomeProvider) ->
                ValueResourceFrontend
                    { new ValueResourceBackend<_> with 
                        member _.Value () = value } }

        static member Builder value =
            { build = fun (_p: OtherProvider) ->
                ValueResourceFrontend
                    { new ValueResourceBackend<_> with 
                        member _.Value () = value } }
        // Can't use static member overload since the type of the provider is in the return type
        // Return type doesn't distinguish methods for overload resolution

    // Interface
    type ValueResource<'A>(value: 'A) =
        interface IResourceBuilder<SomeProvider, ValueResourceFrontend<'A>> with
            member _.Build (_p: SomeProvider) =
                ValueResourceFrontend<'A>
                    { new ValueResourceBackend<_> with 
                        member _.Value () = value }

        interface IResourceBuilder<OtherProvider, ValueResourceFrontend<'A>> with
            member _.Build (_p: OtherProvider) =
                ValueResourceFrontend<'A>
                    { new ValueResourceBackend<_> with 
                        member _.Value () = value }
        // Can't implement interfaces in type extensions, so would need to create an inheriting type
        // which implements the builder for new providers and change all instantiations to that type

    // SRTP
    type ValueResourceSRTP<'A>(value: 'A) =
        interface ResourceBuilderMarker

        member _.Build (_p: OtherProvider) =
            ValueResourceFrontend<'A>
                { new ValueResourceBackend<_> with 
                    member _.Value () = value }

        member _.Build (_p: SomeProvider) =
            ValueResourceFrontend<'A>
                { new ValueResourceBackend<_> with 
                    member _.Value () = value }
        // Doesn't type check correctly with SRTP when there's a generic return type (maybe just due to computation desugar)
        // Return type gets typed to obj, but still will match an incorrect bind when the provider is not supported.
        // This could be a (real) compile-time error (even though no error in checker)? But it'd be a bit confusing to debug
        // May also be just limited to generic return types?

module Scenario =
    open MyResources

    module InterfaceResource =
        let works =
            ResourceBlockBuilder<_>(SomeProvider()) {
                let! _x = ValueResource(0)
                return ()
            }

        let works2 =
            ResourceBlockBuilder<_>(OtherProvider()) {
                let! _x = ValueResource(0)
                return ()
            }

        let breaks =
            ResourceBlockBuilder<_>(AnotherProvider()) {
                let! _x = ValueResource(0)
                return ()
            }

    module RecordResource =
        let works = 
            ResourceBlockBuilder<_>(SomeProvider()) {
                let! _x = ValueResourceRecord.Builder 0 // Can't resolve overload based on return type
                return ()
            }

    module SRTPResource =
        let works = 
            ResourceBlockBuilder<_>(SomeProvider()) {
                let! _x = ValueResourceSRTP(0)
                return ()
            }

        let works2 =
            ResourceBlockBuilder<_>(OtherProvider()) {
                let! _x = ValueResourceSRTP(0)
                return ()
            }

        let breaks =
            ResourceBlockBuilder<_>(AnotherProvider()) {
                let! _x = ValueResourceSRTP(0) // This should be a compile error, not just typed as `obj`
                return ()
            }

