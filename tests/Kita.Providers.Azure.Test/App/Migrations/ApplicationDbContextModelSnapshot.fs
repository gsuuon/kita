﻿// <auto-generated />
namespace App.Migrations

open System
open AzureApp.DbModel
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Metadata
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

[<DbContext(typeof<ApplicationDbContext>)>]
type ApplicationDbContextModelSnapshot() =
    inherit ModelSnapshot()

    override this.BuildModel(modelBuilder: ModelBuilder) =
        modelBuilder

            .UseIdentityColumns().HasAnnotation("Relational:MaxIdentifierLength", 128)
            .HasAnnotation("ProductVersion", "5.0.7")
            |> ignore

        modelBuilder.Entity("AzureApp.DbModel.Room", (fun b ->

            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn() |> ignore
            b.Property<string>("name")
                .HasColumnType("nvarchar(max)") |> ignore

            b.HasKey("Id") |> ignore

            b.ToTable("Rooms") |> ignore

        )) |> ignore

        modelBuilder.Entity("AzureApp.DbModel.User", (fun b ->

            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn() |> ignore
            b.Property<Nullable<int>>("RoomId")
                .HasColumnType("int") |> ignore
            b.Property<string>("bio")
                .HasColumnType("nvarchar(max)") |> ignore
            b.Property<string>("name")
                .HasColumnType("nvarchar(max)") |> ignore

            b.HasKey("Id") |> ignore


            b.HasIndex("RoomId") |> ignore

            b.ToTable("Users") |> ignore

        )) |> ignore

        modelBuilder.Entity("AzureApp.DbModel.User", (fun b ->
            b.HasOne("AzureApp.DbModel.Room",null)
                .WithMany("users")
                .HasForeignKey("RoomId") |> ignore
        )) |> ignore
