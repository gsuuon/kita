namespace Kita.Core.Resources

open Kita.Core
open Kita.Core.Providers


type Cloud<'T> =
    abstract member ReportDesiredState : Config -> unit
    abstract member BeginActivation : Config -> unit

open System.Collections.Generic

type CloudQueue<'T>() =
    interface Cloud<Queue<'T>> with
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()

type CloudMap<'K, 'V when 'K : comparison>() =
    interface Cloud<Map<'K, 'V>> with
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()

type CloudZero() =
    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
    static member Instance = CloudZero()
