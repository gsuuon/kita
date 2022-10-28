namespace Kita.Resources.Collections

open Kita.Core


module UsageSketch =
    [<CLIMutable>]
    type Blog = {
        [<Key>] Id: int
        Url: string
    }

    [<CLIMutable>]
    type Post = {
        [<Key>] Id: int
        title: string
        BlogId: int
        Blog: Blog
    }

    type ConStringConfig =
        { serverName : string
          database : string }

    type AzureDbContext(cfg: ConStringConfig) =
        inherit DbContext()

        override _.OnModelCreating builder =
            builder.RegisterOptionTypes() // enables option values for all entities

        override _.OnConfiguring(options: DbContextOptionsBuilder) : unit =
            let conString =
                $"Server=tcp:{cfg.serverName}.database.windows.net,1433;Initial Catalog={cfg.database};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;"

            options.UseSqlServer(conString) |> ignore
            options.AddInterceptors(new AzureConnectionInterceptor()) |> ignore
                // todo pull in azureconnectioninterceptor


    type BloggingContext() =
        inherit DbContext()
        // NOTE DbContext is not threadsafe
        // meant to be created per request / thread
        // how does this work in terms of changing providers?

        [<DefaultValue>] val mutable posts : DbSet<Post>
        member this.Posts with get() = this.posts and set v = this.posts <- v
            // NOTE why is this not member val?

        [<DefaultValue>] val mutable blogs : DbSet<Blog>
        member this.Blogs with get() = this.blogs and set v = this.blogs <- v
   

    let blockUsageSketch =
        Block<Azure, unit> "main" {
            let! bloggingDb = AzureDatabaseSQL<BloggingContext>("mainsqlserver")

            do! routes {
                get "hey" <| fun req -> async {
                    use blogCtx = bloggingDb.GetContext()

                    let posts =
                        query {
                            for post in blogCtx.Posts do
                                let blog = post.Blog
                                select { post with Blog = blog }
                        }
                        |> Seq.toList

                    return ok <| String.concat "\n" posts
                }
                    
            }
        }

