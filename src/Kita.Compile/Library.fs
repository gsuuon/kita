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
                    // FIXME potential side effects
                staticValue.GetType() = memberType
            )

    let canReadStatic (propertyInfo: PropertyInfo) =
        propertyInfo.CanRead &&
            let getMethod = propertyInfo.GetGetMethod()
            getMethod.IsStatic

    /// Get static access path of a value. Throws if not a public static.
    // NOTE This is broken if the block is actually the return value of a function
    let getStaticAccessPath v =
        let typ = v.GetType()

        if typ.DeclaringType = null then
            failwithf "Declaring type was null for type: %A" typ

        let tryGetMemberType (mi: MemberInfo) =
            match mi.MemberType with
            | MemberTypes.Property ->
                Some <| (mi :?> PropertyInfo).PropertyType
            | MemberTypes.Field ->
                Some <| (mi :?> FieldInfo).FieldType
            | MemberTypes.Method ->
                Some <| (mi :?> MethodInfo).ReturnType
            | _ ->
                None

        let propInfo =
            try getMemberOfType typ.DeclaringType typ
            with e ->
                let members =
                    typ.DeclaringType.GetMembers()
                    |> Array.map (fun x -> sprintf "%s - %A" x.Name (tryGetMemberType x))

                failwithf
                    "Couldn't find member of type %A from %A\nKnown members:%A"
                        typ
                        typ.DeclaringType
                        (members |> String.concat "\n")
            
        if not (canReadStatic propInfo) then
            failwith "The value needs to be a public static property"

        let accessor =
            let canonTypeName (name: string) =
                let withoutGenerics = name.Split("`").[0]
                withoutGenerics.Replace ("+", ".")

            $"{canonTypeName propInfo.DeclaringType.FullName}.{propInfo.Name}"

        accessor
