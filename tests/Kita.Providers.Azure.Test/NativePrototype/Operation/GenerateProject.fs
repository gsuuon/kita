module AzureNativePrototype.GenerateProject

open System
open System.IO
open System.IO.Compression

open System.Collections.Generic
open FSharp.Control.Tasks

open Kita.Core
open Kita.Compile
open Kita.Compile.Domains.Routes
open AzureNativePrototype

[<AutoOpen>]
module Zipper = 
    let transformFilesToArchive
        (path: string)
        transform
        withArchive
        = // NOTE this only reads and transforms top-level files

        if not (Directory.Exists path) then
            failwithf "Can't create archive, this isn't a directory: %s" path

        use mem = new MemoryStream()

        using (new ZipArchive(mem, ZipArchiveMode.Update , true)) <| fun archive ->
            let createAndTransformEntry (relativeFilePath, fileText: string) =
                let file = archive.CreateEntry relativeFilePath
                use entry = file.Open()
                use sw = new StreamWriter(entry)

                let transformed : string = transform relativeFilePath fileText

                sw.Write transformed

            let readFile filePath =
                use fs = File.OpenRead(filePath)
                use sr = new StreamReader(fs)

                let relativePath = Path.GetRelativePath(path, filePath)

                relativePath, sr.ReadToEnd()

            let files = Directory.GetFiles path

            files
            |> Seq.map readFile
            |> Seq.iter createAndTransformEntry

            withArchive archive

    let findAndReplaceLine find replace (fileText: string) =
        let lines = fileText.Split("\r")

        lines
        |> Array.map (fun line -> if find line then replace line else line)
        |> String.concat "\r"
        
    let replaceAppReference (fileText: string) =
        // TODO
        // Find the type with rootblock attribute
        // get fullname of type
        // In proxy project:
        // instantiate the replaced reference
        // call interface method DomainLauncher<RouteState,_>.Launch

        let appLauncherMethod = Reflect.findRoutesEntry "main"
        let appLauncherCallString = Reflect.getCallString appLauncherMethod

        let targetAssign = "let appLauncher = "

        printfn "Replacing appLauncher with: %s" appLauncherCallString

        findAndReplaceLine
        <| fun line -> line.Contains targetAssign
        <| fun line ->
            let indent = (line.Split "let").[0]

            let res = indent + targetAssign + appLauncherCallString
            printfn "Replaced accessor with: %s" res
            res
        <| fileText

    let replaceProjectReference proxyAppPath (fileText: string) =
        // FIXME we're assuming we've executed via `dotnet run` from project root directory
        // The goal here is to add all the references required for app to the proxy project
        // the easiest way I see is to just add a project reference to the current executing project
        // we do that by assuming we're running from `bin/<config>/<tfm>/` and going up, and looking
        // for any .fsproj
        // I could also add all references of the app's assembly
        // and recursively all their references
        // Not sure how I feel about that. I'd rather let msbuild sort out resolving any reference
        // conflicts. Could I use paket for this?

        (* let appAssemblyPath = app.GetType().Assembly.Location *)
        (* if appAssemblyPath = "" then *)
        (*     failwith "app can't be defined from a byte[] loaded assembly" *)
        (* printfn "App assembly path: %s" appAssemblyPath *)

        let replaceAppProjectReference text =
            let projDir =
                let parent = Directory.GetParent System.AppDomain.CurrentDomain.BaseDirectory
                parent.Parent.Parent.Parent.FullName

            printfn "Project directory: %s" projDir

            let fsProjPath =
                match Directory.GetFiles projDir
                    |> Array.tryFind (fun fn -> fn.EndsWith ".fsproj") with
                | Some fsProjFile ->
                    fsProjFile
                | None ->
                    failwithf "Couldn't find a project file in %s" projDir


            printfn "Adding project reference to: %s" fsProjPath

            findAndReplaceLine
            <| fun line -> line.Contains "<!-- Kita_AppProjectReference"
            <| fun line ->
                let indent = (line.Split "<").[0]
                $"""{indent}<ProjectReference Include="{fsProjPath}" />"""

            <| text

        let replaceRelativeProjectReferences text =
            findAndReplaceLine
            <| fun line -> line.Contains "<ProjectReference"
            <| fun line ->
                let indent = (line.Split "<").[0]
                let matches = System.Text.RegularExpressions.Regex.Match(line, "Include=\"(.+)\"")

                if matches.Success then
                    let relativeReference = matches.Groups.[1].Value

                    if Path.IsPathRooted relativeReference then
                        line
                    else
                        let path = Path.GetFullPath(Path.Join(proxyAppPath, relativeReference))

                        $"""{indent}<ProjectReference Include="{path}" />"""
                else
                    line

            <| text
            
        fileText
        |> replaceAppProjectReference
        |> replaceRelativeProjectReferences

    let replaceLocalSettings conString fileText =
        let replaceTarget = "<Kita_ConnectionString>"

        findAndReplaceLine
        <| fun line -> line.Contains replaceTarget
        <| fun line -> line.Replace(replaceTarget, conString)
        <| fileText

[<AutoOpen>]
module Builder = 
    open System.Diagnostics

    let publishDotnetToArchive directory =
        let startInfo = ProcessStartInfo()
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.WorkingDirectory <- directory
        startInfo.FileName <- "dotnet"

        let outpath = "published"

        let configuration =
            #if DEBUG
            "Debug"
            #else
            "Release"
            #endif

        let specifyOutput = " -o " + outpath

        startInfo.Arguments <- "publish --nologo -c:" + configuration + specifyOutput

        printfn "Publishing in config: %s" configuration

        let proc = Process.Start startInfo

        let lines = Queue()

        async {
            while not proc.HasExited do
                let! line =
                    proc.StandardOutput.ReadLineAsync() |> Async.AwaitTask
                lines.Enqueue line
        } |> Async.Start

        proc.WaitForExit()
        if proc.ExitCode <> 0 then
            let errs = proc.StandardError.ReadToEnd()
            failwithf
                "Publish failed!\n%s\n%s"
                    errs
                    (lines |> String.concat "\n")

        printfn "Finished publish"

        let publishedZipPath = "published.zip"

        if File.Exists publishedZipPath then
            File.Delete publishedZipPath

        ZipFile.CreateFromDirectory(Path.Join(directory, outpath), publishedZipPath)

        printfn "Wrote zipfile %s" publishedZipPath

        File.ReadAllBytes(publishedZipPath)

let rec generateFunctionsAppZip
    (proxyAppPath: string)
    conString
    = task {

    try
        let proxyProjPath = Path.Join(".kita","Proxy")
        printfn "Generating from template at %s" proxyProjPath

        transformFilesToArchive
        <| proxyAppPath
        <| fun relativePath fileText ->
            if relativePath = "AutoReplacedReference.fs" then
                replaceAppReference fileText
            else if relativePath.EndsWith ".fsproj" then
                replaceProjectReference proxyAppPath fileText
            // TODO generate app-namespaced connection string env variable
            else if relativePath.EndsWith "local.settings.json" then
                replaceLocalSettings conString fileText
            else
                fileText
        <| fun archive ->
            // NOTE I'd massively prefer to do all this in-memory and avoid
            // a lot of potential file access exceptions. All of this is
            // transient noise and doesn't need to be persisted.
            // I couldn't find any way to call dotnet build on in-memory files
            // or with a zip archive
            if Directory.Exists proxyProjPath then
                Directory.Delete(proxyProjPath, true)

            Directory.CreateDirectory proxyProjPath |> ignore

            archive.ExtractToDirectory proxyProjPath

        let archiveBytes = publishDotnetToArchive proxyProjPath

        return archiveBytes
    with
    | :? IOException as e->
        // If a file is in use, give the user a chance to handle it
        printfn "IO Exception:\n%A" e
        printfn "[r]etry | [a]bort"

        let rec readKey timeLeft =
            let interval = 100
            if Console.KeyAvailable then
                Some <| Console.ReadKey().KeyChar
            else if timeLeft < 0 then
                None
            else
                System.Threading.Thread.Sleep interval
                readKey (timeLeft - interval)

        match readKey 10000 with
        | Some 'r' ->
            return! generateFunctionsAppZip proxyAppPath conString
        | None
        | Some 'a'
        | Some _ ->
            printfn "Aborted!"
            return raise e

    }
