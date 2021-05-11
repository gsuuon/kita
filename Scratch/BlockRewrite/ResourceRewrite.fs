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

// REMOVE
type OtherProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

type ResourceBuilder = interface end

type ValueResourceBuilder<'A>(name: string, value: 'A) =
    member val Value = value
    member _.Build (p: SomeProvider) =
        ValueResourceFrontend
            { new ValueResourceBackend<_> with
                member _.Value () = value }

// REMOVE
    member _.Build (p: OtherProvider) =
        ValueResourceFrontend
            { new ValueResourceBackend<_> with 
                member _.Value () = value }

    member _.Start (p: SomeProvider) = ()
    member _.Start (p: OtherProvider) = ()

    interface ResourceBuilder

// Adding support in ValueResource for some other provider
(* type OtherProvider() = *)
(*     interface Provider with *)
(*         member _.Launch () = () *)
(*         member _.Run () = () *)

type AnotherProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

(* type ValueResourceBuilder<'A> with *)
(*     member x.Build (p: OtherProvider) = *)
(*         ValueResourceFrontend *)
(*             { new ValueResourceBackend<_> with *) 
(*                 member _.Value () = x.Value } *)

module TinyRepro =
    type Resource() =
        member _.Build (p: SomeProvider) =
            "hi"

        member _.Build (p: OtherProvider) =
            0

    module Ops =
        let inline build
            (builder: 'R when 'R : (member Build : 'P -> 'A ))
            (provider : 'P)
            =
            ( ^R : (member Build : 'P -> 'A) (builder, provider))

    type ResourceBuilderBlock< ^P when ^P :> Provider>(name, provider) =
    // Returns 'A as obj when 'P doesn't match
        member inline _.Bind
            (
                builder : ^R when 'R : (member Build : 'P -> 'A ),
                f
            ) =

            let x = Ops.build builder provider

            f x

    // Correctly errors Return type is unit
        (* member inline _.Bind *) 
        (*     ( *)
        (*         builder : ^R when ^R : (member Start : ^P -> unit ), *)
        (*         f *)
        (*     ) = *)
        (*     let x = ( ^R : (member Start : ^P -> unit) (builder, provider)) *)

        (*     f x *)

        member inline _.Zero = ()
        member inline _.Return x = x

    let worksBlock =
        ResourceBuilderBlock<_>("", SomeProvider()) {
            let! valResource = Resource()
            return ()
        }

    let worksBlock2 =
        ResourceBuilderBlock<_>("", OtherProvider()) {
            let! valResource = Resource()
            return ()
        }

    let breaksBlock =
        ResourceBuilderBlock<_>("", AnotherProvider()) {
            let! valResource = Resource()
            return ()
        }

module Scenario = 
    open TinyRepro

    let worksBlock =
        ResourceBuilderBlock<_>("", SomeProvider()) {
            let! valResource = ValueResourceBuilder("", 0)
            return ()
        }

    let breaksBlock =
        ResourceBuilderBlock<_>("", AnotherProvider()) {
            let! valResource = ValueResourceBuilder("", 0)
            return ()
        }

    let block name = ResourceBuilderBlock<_>(name, SomeProvider())

    let mainBlock =
        block "main" {
            let! aVal = ValueResourceBuilder("foo", 0)
                // ValueResourceBuilder doesn't have a
                // Build member which takes AnotherProvider
                // SRTP should fail here
                //
                // Type extension confuses SRTP. If I comment out the
                // extension, then the compile error is in the bind
                // overload failing to match
                // 
                // Related to the issue on type extensions being
                // visible to srtp constraints
                // https://github.com/dotnet/fsharp/pull/6805

                // Is this matching another bind?
                // but if I remove the bind overload in ResourceBuilderBlock
                // this doesnt' resolve

                // Okay, this has nothing to do with type extensions
                // since its breaking below without them

            (* let x = aVal.Value() *)
                // FIXME
                // this should be breaking at `let! aVal`
                // not here
            (* printfn "Got %A" x *)
            return ()
        }


module QuickRepro =
    let inline build< ^T, 'P, 'A when ^T : ( member Build : 'P -> 'A)>
        (builder: 'T)
        (provider: 'P) : 'A
        =
        ( ^T : ( member Build : 'P -> 'A ) (builder, provider) )

    type Builder< ^P, 'A>() =
        member inline _.Build< ^T when ^T : ( member Build : 'P -> 'A)>
            (
                builder: ^T when ^T : ( member Build : 'P -> 'A),
                provider: 'P
            ) =
            ( ^T : ( member Build : 'P -> 'A ) (builder, provider) )


    let x = build<_, SomeProvider, _> (ValueResourceBuilder("hi", 0))
    let y = build<_, OtherProvider, _> (ValueResourceBuilder("hi", 0))
    let z = build<_, AnotherProvider, _> (ValueResourceBuilder("hi", 0))

    let x' = Builder<SomeProvider, _>().Build (ValueResourceBuilder("", 0), SomeProvider())
    let y' = Builder<OtherProvider, _>().Build (ValueResourceBuilder("", 0), OtherProvider())
    let z' = Builder<AnotherProvider, _>().Build (ValueResourceBuilder("", 0), AnotherProvider())
    // Correctly fails here

module SimpleReproExtensionTrait =
    type Foo() =
        member x.Call (_x: int) = ()

    let inline call (usable: ^T when ^T : (member Call : int -> unit) ) =
        ( ^T : (member Call : int -> unit) (usable, 0) )

    let x = call (Foo())

    type Foo with
        member x.Call (_x: string) = ()
        
    let y = call (Foo())
