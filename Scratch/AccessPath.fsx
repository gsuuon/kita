type Block =
    abstract member Name : string

type AttachedBlock =
    abstract member Block : Block
    abstract member AccessPath : string

let inline make name =
    { new AttachedBlock with
        member this.AccessPath = 
            getStaticAccessPath this
        member this.Block =
            { new Block with
                member _.Name = name } }

type Builder() =
    member inline _.Make name = make name
/////

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

    if typ.DeclaringType = null then
        failwithf "Declaring type was null for type: %A" typ

    let propInfo =
        try getMemberOfType typ.DeclaringType typ
        with e ->
            failwithf
                "Couldn't find member of type\nParent:%A Type:%A"
                    typ.DeclaringType
                    typ
        
    if not (canReadStatic propInfo) then
        failwith "The value needs to be a public static property"

    let accessor =
        let canonTypeName (name: string) =
            let withoutGenerics = name.Split("`").[0]
            withoutGenerics.Replace ("+", ".")

        $"{canonTypeName propInfo.DeclaringType.FullName}.{propInfo.Name}"

    accessor

module MyMod =
    let hi = make "hi"

MyMod.hi.AccessPath

module MyMod2 =
    let builder = Builder()
    let hey = builder.Make "hey"

MyMod2.hey.AccessPath
