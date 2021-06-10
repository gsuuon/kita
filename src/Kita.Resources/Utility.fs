namespace Kita.Resources.Utility

type Serializer<'SerializationFormat> =
    abstract Serialize : 'A -> 'SerializationFormat
    abstract Deserialize : 'SerializationFormat -> 'A
