module BlockRewrite.Scenarios.Basic

open BlockRewrite
open BlockRewrite.Providers
open BlockRewrite.Resources

module InitializationMethods =  
    let inline block name = Block<_, unit>(name)
    type BuiltBlock<'P when 'P :> Provider> =
        BindState<'P, unit> -> AttachedBlock

    let mainProvider = AProvider()

    let okLooseBlock = 
        // Okay loose as AResource uniquely defines a provider
        block "okLoose" {
            let! x = AResource("two")
            return ()
        }

    let annotatedLooseBlock : BuiltBlock<AProvider> =
        // If the resource supports multiple providers,
        // then we need an annotation
        block "loose" {
            let! x = ABResource("three")
            return ()
        }
        // We could alias AProvider to another type that represents
        // the 'main' provider type, or however we want to structure it
        // so that we still have just a one line change if we want to
        // swap out a group of blocks to another provider
        // e.g.
        // type MainProvider = AProvider
        // let blockA : BuiltBlock<MainProvider> =
        // let blockB : BuiltBlock<MainProvider> =

    let immediatelyAttachLooseBlock =
        // Or to immediately call attach
        block "loose" {
            let! x = ABResource("three")
            return ()
        }
        |> attach mainProvider



module SimpleScenario =
    let mainProvider name = Block<AProvider, unit>(name)
    
    let leafBlock =
        mainProvider "leaf" {
            let! x = ABResource()
            return ()
        }

    let rootBlock =
        mainProvider "root" {
            let! x = ABResource()
            x.DoABThing()
            let! y = AResource()
            return ()
        }

    let go () =
        let aProvider = AProvider()

        printfn "Root"
        let attachedRoot = rootBlock |> attach aProvider
        attachedRoot.launch()
        attachedRoot.run()

        printfn "Leaf"
        let attachedLeaf = leafBlock |> attach aProvider 
        attachedLeaf.launch()
        attachedLeaf.run()
        ()

module NestedScenario =
    let mainProvider name = Block<AProvider, unit>(name)

    module SameProviderScenario =
        let blockInner =
            mainProvider "inner" {
                let! x = AResource("three")
                return ()
            }

        let blockOuter =
            mainProvider "outer" {
                let! x = ABResource("one")
                printfn "hi"
                child blockInner
                let! y = ABResource("two")
                printfn "bye"
                return ()
            }

        let go () =
            blockOuter |> attach (AProvider()) |> launchAndRun

    module SameProviderResourcePassScenario =
        let blockInner (resource: ABResource) =
            mainProvider "inner" {
                let! x = AResource("three")
                printfn "Got resource: %A" resource
                return ()
            }

        let blockOuter =
            mainProvider "outer" {
                let! x = ABResource("one")
                let x' = x
                child (blockInner x')
                let! y = AResource("two")
                return ()
            }

        let go () =
            blockOuter |> attach (AProvider()) |> launchAndRun

    module DifferentProvidersScenario =
        let blockInner =
            Block<BProvider, unit> "inner" {
                let! x = BResource("one")
                return ()
            }

        let blockInnerSame =
            Block<AProvider, unit> "innerSame" {
                let! x = AResource("one")
                return ()
            }

        let blockOuter =
            let bProvider = BProvider()

            mainProvider "outer" {
                let! x = AResource("two")
                nest blockInner bProvider
                child blockInnerSame
            }

        let go () =
            blockOuter |> attach (AProvider()) |> launchAndRun
