module BlockRewrite.Scenarios.Basic

open BlockRewrite
open BlockRewrite.Providers
open BlockRewrite.Resources

module SimpleScenario =
    let mainProvider name = Block<AProvider>(name)
    
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
    let mainProvider name = Block<AProvider>(name)

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
            Block<BProvider> "inner" {
                let! x = BResource("one")
                return ()
            }

        let blockOuter =
            let bProvider = BProvider()

            mainProvider "outer" {
                let! x = AResource("two")
                nest blockInner bProvider
                return ()
            }

        let go () =
            blockOuter |> attach (AProvider()) |> launchAndRun
