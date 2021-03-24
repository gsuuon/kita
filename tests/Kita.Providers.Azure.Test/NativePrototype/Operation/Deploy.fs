module AzureNativePrototype.Deploy

open Kita.Core
open FSharp.Control.Tasks

let generateFunctionsApp (managed: Managed<AzureNative>) = async {
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
    return ()
}
