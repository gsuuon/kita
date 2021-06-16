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

    // TODO re-evaluate how cross-project interaction works
    type RootBlockAttribute(rootName: string) =
        inherit Attribute()
        member val RootName = rootName

    let findMethod eval =
        Assembly.GetEntryAssembly().GetTypes()
        |> Array.pick (fun typ ->

            typ.GetMethods()
            |> Array.tryFind (fun mi -> eval mi) )

    let findType eval = 
        Assembly.GetEntryAssembly().GetTypes()
        |> Array.tryFind (fun typ -> eval typ )
        
    let findRootBlockFor rootName =
        findType <| fun typ -> 
            typ.GetCustomAttributes()
            |> Seq.exists (fun attr ->

                if attr.GetType() = typeof<RootBlockAttribute> then
                    let rootBlock = attr :?> RootBlockAttribute

                    rootBlock.RootName = rootName
                else
                    false

                 )

    let getCallString (mi: MethodInfo) =
        if not mi.IsStatic then
            failwith "Trying to generate a call string for a non-static method, but we're expecting a static method. An attribute may be misplaced."
        let typ = mi.DeclaringType
        if typ.IsGenericType then
            failwith "Trying to generate a call string for a generic type's method. The method should be on a non-generic class"

        typ.FullName
        |> fun s -> s.Replace("+", ".")
        |> fun typName -> typName + "." + mi.Name

    let getConstructString (typ: Type) =
        if typ.IsGenericType then
            failwith "Trying to generate a construct string for a generic type."

        let hasParameterlessPublicCtor =
            typ.GetConstructors()
            |> Array.exists
                (fun ctor ->
                    let pars = ctor.GetParameters()
                    if pars.Length > 0 then false
                    elif ctor.IsPrivate then false
                    else true
                )
        if not hasParameterlessPublicCtor then
            failwith "Trying to generate a construct string for a type missing a parameterless public constructor"

        typ.FullName
        |> fun s -> s.Replace("+", ".")
        |> fun s -> s + "()"
