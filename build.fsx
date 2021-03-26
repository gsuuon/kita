#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Trace
nuget Fake.Core.Target //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core
open Fake.DotNet

// Common configuration
type Cfg() =
    static member quiet (defaults: DotNet.Options) =
        { defaults with
            Verbosity = Some DotNet.Verbosity.Quiet }

    static member quiet (defaults: DotNet.BuildOptions) =
        { defaults with
            MSBuildParams =
                { defaults.MSBuildParams with
                    Verbosity = Some Quiet }
            NoLogo = true }

let quietListener =
    { new ITraceListener with
        member _.Write _ = ()
    }

CoreTracing.setTraceListeners [quietListener]

// Targets
Target.create "Clean" <| fun _ ->
    !! "src/**/*.fsproj"
    |> Seq.iter
        (DotNet.exec Cfg.quiet "clean" >> ignore)

Target.create "Build" <| fun _ ->
    !! "src/**/*.fsproj"
    |> Seq.iter
        (DotNet.build Cfg.quiet)

Target.create "BuildTests" <| fun _ ->
    !! "tests/**/*.fsproj"
    |> Seq.iter
        (DotNet.build Cfg.quiet)
    

Target.runOrDefaultWithArguments "Build"
