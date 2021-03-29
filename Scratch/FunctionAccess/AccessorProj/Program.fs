open ControllerProj // Execute the operation

open AccessorProj.Mutated

module Container =
    let num = ref 2
    let num1 = ref 1
    let addNums x y = x + y + !num
    
    let addOne = addNums (!num1)

    module Nested =
        let addTwo = addNums 2

        module Deeper =
            let num3 = ref 3
            let addThree = addNums (!num3)
    
 
open System
open System.Reflection

let doThing () =
    let getMemberOfType (parentType: Type) (memberType: Type) =
        printfn "Looking for type: %A" memberType
        parentType.GetMembers()
        |> Array.choose
            (fun mi ->
                if mi.MemberType = MemberTypes.Property then
                    printfn "Found property: %A" (mi :?> PropertyInfo)
                    Some (mi :?> PropertyInfo)
                else
                    None
            )
        |> Array.find
            (fun prop ->
                let staticValue = prop.GetValue(null)
                staticValue.GetType() = memberType
            )

    let getAccessAddressOf v =
        // I need to 
        printfn "processFn nameOf: %s" (nameof v)

        let typ = v.GetType()
        typ.FullName |> printfn "FullName: %A"
        typ.DeclaringType.FullName |> printfn "Declaring: %A"
        // I could use reflection to find the member of the declaring type that has the typ?

        let membr = getMemberOfType typ.DeclaringType typ
        printfn "Member: %A" membr
        let accessor =
            let memberName = membr.Name
            let typeName = membr.DeclaringType.FullName

            let canonTypeName (name: string) =
                let withoutGenerics = name.Split("`").[0]
                withoutGenerics.Replace ("+", ".")

            printfn "Type: %s Member: %s" typeName memberName

            $"{canonTypeName typeName}.{memberName}"

        printfn "Accessor: %s" accessor

        ()

    getAccessAddressOf Container.addOne
    getAccessAddressOf Container.Nested.addTwo
    getAccessAddressOf Container.Nested.Deeper.addThree
        // I want to get "AccessorProj.Container.addOne"

[<EntryPoint>]
let main _argv =
    (* let v = adder 1 *)
    (* printfn "Value with one was: %A" v *)

    doThing()

    0
