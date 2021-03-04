type Config(name: string) =
    member val name = name
    member _.Initialize() = printfn "Base config initialize"

module Default =
    type Local() =
        inherit Config("Local")
        member _.Initialize() = printfn "Local config initialize"

    type Az() =
        inherit Config("Azure.Default")
        member _.Initialize() = printfn "Az config initialize"

    type Gcp() =
        inherit Config("Gcp.Default")
        member _.Initialize() = printfn "Gcp config initialize"

    type Aws() =
        inherit Config("Aws.Default")
        member _.Initialize() = printfn "Aws config initialize"

module ScratchResource =
    open Default

    type ResourceBase() =
        class
        end

    type ResourceClass() =
        inherit ResourceBase()

        member _.Deploy(gcp: Aws) =
            printfn "Resource deploy"
            ()

        member _.Deploy(gcp: Gcp) =
            printfn "Resource deploy"
            ()

    let inline deployStaticVerify< ^C, ^R when ^R: (member Deploy : ^C -> unit) and ^C :> Config>
        (
            resource: ^R,
            config: ^C
        ) =
        (^R: (member Deploy : ^C -> unit) (resource, config))


open ScratchResource
open Default

deployStaticVerify (ResourceClass(), Aws())
deployStaticVerify (ResourceClass(), Gcp())
deployStaticVerify (ResourceClass(), Local())
