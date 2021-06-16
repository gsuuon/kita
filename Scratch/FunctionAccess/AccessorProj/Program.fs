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
    /// Finds the PropertyInfo of the member of parentType which has type of memberType
    /// Throws if not found
    let getMemberOfType (parentType: Type) (memberType: Type) =
        parentType.GetMembers()
        |> Array.choose
            (fun mi ->
                if mi.MemberType = MemberTypes.Property then
                    Some (mi :?> PropertyInfo)
                else
                    None
            )
        |> Array.find
            (fun prop ->
                let staticValue = prop.GetValue(null)
                staticValue.GetType() = memberType
            )

    let canReadStatic (propertyInfo: PropertyInfo) =
        propertyInfo.CanRead &&
            let getMethod = propertyInfo.GetGetMethod()
            getMethod.IsStatic

    let getStaticAccessPath v =
        let typ = v.GetType()

        let propInfo = getMemberOfType typ.DeclaringType typ

        if not (canReadStatic propInfo) then
            failwith "The value needs to be a public static property"

        let accessor =
            let canonTypeName (name: string) =
                let withoutGenerics = name.Split("`").[0]
                withoutGenerics.Replace ("+", ".")

            $"{canonTypeName propInfo.DeclaringType.FullName}.{propInfo.Name}"

        accessor

    let getAndReport x =
        let accessor = getStaticAccessPath x
        printfn "Value: %A\nPath: %s" x accessor

    getAndReport Container.addOne
    getAndReport Container.Nested.addTwo
    getAndReport Container.Nested.Deeper.addThree

[<EntryPoint>]
let main _argv =
    doThing()

    0
