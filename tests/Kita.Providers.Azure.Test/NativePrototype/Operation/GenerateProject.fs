module AzureNativePrototype.GenerateProject

open System.IO
open System.Collections.Generic
open FSharp.Control.Tasks

open Kita.Core
open Kita.Compile
open AzureNativePrototype

(*
- [ ] Modify files in memory, use in-memory files to zip
*)

let findAndReplaceLine (filePath: string) find replace =

    let findNReplaceAsLines () = 
        use sr = new StreamReader(File.Open(filePath, FileMode.Open))
        let lines = Queue()

        let rec readLinesReplaceInMemory () =
            if sr.EndOfStream then () else

            let line = sr.ReadLine()

            if find line then
                let replaced : string = replace line
                lines.Enqueue replaced
            else
                lines.Enqueue line

            readLinesReplaceInMemory()

        readLinesReplaceInMemory()

        lines

    let writeToFile (lines: string seq) = 
        use sw = new StreamWriter(filePath, false)
        lines |> Seq.iter (fun line -> sw.WriteLine line)

    findNReplaceAsLines() |> writeToFile

let setAppReference app =
    let proxyAppPath = "../FunctionApp/ProxyApp"
        // I think pwd would be NativePrototype/App/
    let replaceFileName = "AutoReplacedReference.fs"
    let targetAssign = "let app = "

    let filePath = Path.Join([|proxyAppPath; replaceFileName|])

    let accessor = Reflect.getStaticAccessPath app

    printfn "Setting accessor: %s" accessor

    findAndReplaceLine
    <| filePath
    <| fun line -> line.StartsWith targetAssign
    <| fun _ -> targetAssign + accessor

    printfn "Set accessor in %s" filePath

let setKitaEnv conString =
    // set the value in local.settings.json
    // remember that deploying this requires that
    // local.settings.json is included in publish
    // and that i need to see local.settings should overwrite
    // is there a better way to do this than setting it in local.settings?
    ()

let generateFunctionsAppZip (app: Managed<_> -> Managed<_>) = task {
        // We don't actually care what provider is here I think
    // Generate a whole project? Or use template?
        // Generate all files
            // generate fsproj
                // Compile include generated function wrappers file
                    // * all in one file
                // Compile include configuration file
                // Reference 
        // Take fsproj with startup file as inputs
            // generate fsproj file
            // do with that what we need
            // pro: can configure how we want
            // pro: could add other stuff into the project if desired
            // con: user would have to create an fsproj
                // could we just have a default?
                // sure - i could have it in this project
                // include it as a resource
    // generate wrapper functions
    // generate webhostconfiguration
    // generate function.jsons
    // zip folder
    // zip files can be hosted in blob storage

    setAppReference app

    return [| 0x0uy |]
}

