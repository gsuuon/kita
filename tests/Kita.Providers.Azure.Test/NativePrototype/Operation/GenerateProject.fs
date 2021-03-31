module AzureNativePrototype.GenerateProject

open System.IO
open System.Collections.Generic
open FSharp.Control.Tasks

open Kita.Core
open Kita.Compile
open AzureNativePrototype

[<AutoOpen>]
module Zipper = 
    open System.IO
    open System.IO.Compression

    let inMemoryArchive
        (path: string)
        transform
        =
        if not (Directory.Exists path) then
            failwithf "Can't create archive, this isn't a directory: %s" path

        use mem = new MemoryStream()

        using (new ZipArchive(mem, ZipArchiveMode.Create, true)) <| fun archive ->
            let createAndTransformEntry (relativeFilePath, fileText: string) =
                let file = archive.CreateEntry relativeFilePath
                use entry = file.Open()
                use sw = new StreamWriter(entry)

                let transformed : string = transform relativeFilePath fileText
                printfn "Added entry: %s\n%s" relativeFilePath transformed

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

        mem.ToArray()

    let findAndReplaceLine find replace (fileText: string) =
        let lines = fileText.Split("\r")

        lines
        |> Array.map (fun line -> if find line then replace line else line)
        |> String.concat "\r"
        
    let replaceAppReference app (fileText: string) =
        let targetAssign = "let app = "

        let accessor = Reflect.getStaticAccessPath app

        findAndReplaceLine
        <| fun line -> line.StartsWith targetAssign
        <| fun _ -> targetAssign + accessor
        <| fileText

    let setKitaEnv conString =
        // set the value in local.settings.json
        // remember that deploying this requires that
        // local.settings.json is included in publish
        // and that i need to see local.settings should overwrite
        // is there a better way to do this than setting it in local.settings?
        ()

let generateFunctionsAppZip
    (proxyAppPath: string)
    (replaceFileName: string)
    (app: Managed<_> -> Managed<_>)
        // We don't actually care what provider is here I think
    = task {

    let archiveBytes =
        inMemoryArchive
        <| proxyAppPath
        <| fun relativePath fileText ->
            if relativePath = replaceFileName then
                replaceAppReference app fileText
            // TODO replace environment settings file?
            else
                fileText

    use zipFile = File.Open("archiveBytes.zip", FileMode.OpenOrCreate)
    zipFile.Write (System.ReadOnlyMemory(archiveBytes).Span)

    printfn "Wrote zipfile %s" zipFile.Name

    return archiveBytes
}
