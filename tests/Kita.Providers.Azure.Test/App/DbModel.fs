namespace AzureApp.DbModel

open Microsoft.EntityFrameworkCore
open System.ComponentModel.DataAnnotations
open EntityFrameworkCore.FSharp.Extensions


[<CLIMutable>]
type User =
    { [<Key>] Id : int
      name : string
      bio : string
    }

[<CLIMutable>]
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

    override _.OnConfiguring (options) =
        options.UseSqlServer() |> ignore

    override _.OnModelCreating modelBuilder =
        modelBuilder.RegisterOptionTypes()
