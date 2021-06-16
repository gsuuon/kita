namespace Kita.Domains

open Kita.Core

type UserDomain<'U, 'D> =
    abstract get : 'U -> 'D
    abstract set : 'U -> 'D -> 'U

module UserDomain =
    let update<'P, 'U, 'D when 'P :> Provider>
        (userDomain: UserDomain<'U, _>)
        (updater: 'D -> 'D)
        (state: BlockBindState<'P, 'U>)
        =
        let domain = userDomain.get state.user

        let updated = updater domain
        let resultUser = userDomain.set state.user updated

        { state with user = resultUser }

type DomainRunner<'P,'D, 'a when 'P :> Provider> =
    DomainRunner of (BlockBindState<'P,'D> -> 'a * BlockBindState<'P,'D>)

type DomainBuilder<'U, 'D>
    (userDomain: UserDomain<'U, 'D>)
    =
    member val UserDomain = userDomain

    member _.Yield x = x
    member _.Delay f = f
    member _.Bind (DomainRunner r, f) = fun s -> r s |> f
    member _.Return x = DomainRunner <| fun s -> x, s
    member _.Run f =
        fun s ->
            let (DomainRunner r) = f()
            let (_, s') = r s
            s'

type DomainLauncher<'Domain, 'T> =
    abstract Launch : ('Domain -> 'T) -> 'T
