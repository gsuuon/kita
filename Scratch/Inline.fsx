type MyType(name: string) =
    member val Name = name

    member inline x.PrintNameMember = printfn "%A" x.Name
// Works

(* member inline _.PrintName = *)
(*     printfn "%A" name *)
// error FS1113: The value 'PrintName' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible

let t = MyType("hi")

printfn "%A" t.Name

/// But with SRTP, we get:

(* type Foo< ^T when ^T :> obj>(name: string) = *)
(*     member val Name = name *)
// error FS0670: This code is not sufficiently generic. The type variable  ^T could not be generalized because it would escape its scope.

type FooBase(name: string) =
    member val Name = name

type Foo2< ^T when ^T :> obj>(name: string) =
    inherit FooBase(name)

    member inline x.DoThing() = printfn "%A" x.Name
// Works
