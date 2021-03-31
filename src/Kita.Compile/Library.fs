namespace Kita.Compile
 
open System
open System.Reflection

module Reflect =
    /// Finds the PropertyInfo of the member of parentType which has
    /// type of memberType. Throws if not found.
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

    /// Get static access path of a value. Throws if not a public static.
    let getStaticAccessPath v =
        let typ = v.GetType()

        if typ.DeclaringType = null then
            failwithf "Declaring type was null for type: %A" typ

        let propInfo = getMemberOfType typ.DeclaringType typ

        if not (canReadStatic propInfo) then
            failwith "The value needs to be a public static property"

        let accessor =
            let canonTypeName (name: string) =
                let withoutGenerics = name.Split("`").[0]
                withoutGenerics.Replace ("+", ".")

            $"{canonTypeName propInfo.DeclaringType.FullName}.{propInfo.Name}"

        accessor
