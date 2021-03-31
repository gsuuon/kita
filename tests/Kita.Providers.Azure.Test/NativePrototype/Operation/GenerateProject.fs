module AzureNativePrototype.GenerateProject

open System.IO
open System.IO.Compression

open System.Collections.Generic
open FSharp.Control.Tasks

open Kita.Core
open Kita.Compile
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
        
    let replaceAppReference app (fileText: string) =
        let targetAssign = "let app = "

        let accessor = Reflect.getStaticAccessPath app
        printfn "Replacing accessor with: %s" accessor

        findAndReplaceLine
        <| fun line -> line.Contains targetAssign
        <| fun line ->
            let indent = (line.Split "let").[0]

            let res = indent + targetAssign + accessor
            printfn "Replaced accessor with: %s" res
            res
        <| fileText

    let replaceProjectReference proxyAppPath app (fileText: string) =
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

let generateFunctionsAppZip
    (proxyAppPath: string)
    (app: Managed<_> -> Managed<_>)
        // We don't actually care what provider is here I think
    = task {
    let proxyProjPath = Path.Join(".kita","Proxy")

    transformFilesToArchive
    <| proxyAppPath
    <| fun relativePath fileText ->
        if relativePath = "AutoReplacedReference.fs" then
            replaceAppReference app fileText
        else if relativePath.EndsWith ".fsproj" then
            replaceProjectReference proxyAppPath app fileText
        // TODO generate app-namespaced connection string env variable
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
}
