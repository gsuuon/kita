namespace AzureApp.DbModel

open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions


type User =
    { [<Key>] Id : int
      name : string
      permissions : string list
    }

type Room =
    { [<Key>] Id : int
      name : string
      users : User list
    }


type ApplicationDbContext() =
    inherit DbContext()

    [<DefaultValue>] val mutable users : DbSet<User>
    member this.Users with get() = this.users and set v = this.users <- v

    [<DefaultValue>] val mutable rooms : DbSet<Room>
    member this.Rooms with get() = this.rooms and set v = this.rooms <- v
