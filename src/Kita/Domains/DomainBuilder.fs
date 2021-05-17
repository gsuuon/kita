namespace Kita.Domains

open Kita.Core

type UserDomain<'U, 'D> =
    abstract get : 'U -> 'D
    abstract set : 'U -> 'D -> 'U

module UserDomain =
    let inline update<'P, 'U, 'D when 'P :> Provider>
        (userDomain: UserDomain<'U, _>)
        (updater: 'D -> 'D)
        (state: BlockBindState<'P, 'U>)
        =
        let domain = userDomain.get state.user

        let updated = updater domain
        let resultUser = userDomain.set state.user updated

        { state with user = resultUser }

type DomainBuilder<'P, 'U, 'D when 'P :> Provider>() =
    member _.Return x = x
    member _.Bind (m, f) = f m
    member _.Delay f = f
    member _.Run f = f()
