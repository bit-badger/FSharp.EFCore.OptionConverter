module FSharp.EFCore.OptionConverter.Tests

open Expecto
open Microsoft.EntityFrameworkCore

module Model =

  [<CLIMutable>]
  type TestModel =
    { modelId : string
      anInt : int option
      aString : string option
      }
    with
      static member configureEF (mb : ModelBuilder) =
        mb.Entity<TestModel>().ToTable("test_table") |> ignore
        mb.Entity<TestModel>().HasKey(fun e -> e.modelId :> obj) |> ignore
        mb.Entity<TestModel>().Property(fun e -> e.modelId).HasColumnName "id" |> ignore
        mb.Entity<TestModel>().Property(fun e -> e.anInt).HasColumnName "an_int" |> ignore
        mb.Entity<TestModel>().Property(fun e -> e.aString).HasColumnName "a_string" |> ignore
        mb.Model.FindEntityType(typeof<TestModel>).FindProperty("anInt").SetValueConverter(OptionConverter<int> ())
        mb.Model.FindEntityType(typeof<TestModel>).FindProperty("aString").SetValueConverter(OptionConverter<string> ())

  [<AllowNullLiteral>]
  type TestDbContext (options : DbContextOptions<TestDbContext>) =
    inherit DbContext (options)
    [<DefaultValue>]
    val mutable private tests : DbSet<TestModel>
    member this.TestModels
      with get() = this.tests
       and set v = this.tests <- v
    override __.OnModelCreating modelBuilder =
      base.OnModelCreating modelBuilder
      TestModel.configureEF modelBuilder
  
  let opts = 
    let opts = DbContextOptionsBuilder<TestDbContext> ()
    opts.UseSqlite("Data Source=:memory:") |> ignore
    opts.Options

  let newCtx () =
    let ctx = new TestDbContext (opts)
    ctx.Database.GetDbConnection().Open ()
    use cmd = ctx.Database.GetDbConnection().CreateCommand ()
    cmd.CommandText <- "CREATE TABLE test_table (id text, an_int number, a_string text)"
    cmd.ExecuteNonQuery () |> ignore
    ctx

module Tests =
  
  open Model
  open System.Linq
  
  let withCtx f () =
    use ctx = newCtx ()
    f ctx

  [<Tests>]
  let noneTests =
    testList "None will round-trip successfully" [
      yield! testFixture withCtx [
        "Value types work",
        fun ctx ->
          ctx.Add { modelId = "Test1"; anInt = None; aString = Some "testing" } |> ignore
          ctx.SaveChanges () |> ignore
          let result = ctx.TestModels.AsNoTracking().FirstOrDefault(fun m -> m.modelId = "Test1")
          Expect.isTrue result.anInt.IsNone "The integer should have been None"
        "Reference types work",
        fun ctx ->
          ctx.Add { modelId = "Test2"; anInt = Some 5; aString = None } |> ignore
          ctx.SaveChanges () |> ignore
          let result = ctx.TestModels.AsNoTracking().FirstOrDefault(fun m -> m.modelId = "Test2")
          Expect.isTrue result.aString.IsNone "The string should have been None"
        ]
      ]

  [<Tests>]
  let someTests =
    testList "Some will round-trip successfully" [
      yield! testFixture withCtx [
        "Value types work",
        fun ctx ->
          ctx.Add { modelId = "Test3"; anInt = Some 7; aString = None } |> ignore
          ctx.SaveChanges () |> ignore
          let result = ctx.TestModels.AsNoTracking().FirstOrDefault(fun m -> m.modelId = "Test3")
          Expect.isTrue result.anInt.IsSome "The integer should have been Some"
          Expect.equal (Option.get result.anInt) 7 "The integer value was incorrect" 
        "Reference types work",
        fun ctx ->
          ctx.Add { modelId = "Test4"; anInt = None; aString = Some "hello" } |> ignore
          ctx.SaveChanges () |> ignore
          let result = ctx.TestModels.AsNoTracking().FirstOrDefault(fun m -> m.modelId = "Test4")
          Expect.isTrue result.aString.IsSome "The string should have been Some"
          Expect.equal (Option.get result.aString) "hello" "The string value was incorrect"
        ]
      ]

  [<Tests>]
  let nullTests =
    testList "Optional values are actually stored as null" [
      yield! testFixture withCtx [
        "Optional values are actually stored as null",
        fun ctx ->
          [ { modelId = "ABC"; anInt = None; aString = None }
            { modelId = "DEF"; anInt = Some 9; aString = Some "filled" }
            ]
          |> ctx.TestModels.AddRange
          ctx.SaveChanges () |> ignore
          let conn = ctx.Database.GetDbConnection()
          use cmd = conn.CreateCommand ()
          cmd.CommandText <- "SELECT COUNT(id) FROM test_table WHERE an_int IS NULL"
          let nullInts : int64 = downcast cmd.ExecuteScalar ()
          Expect.equal nullInts 1L "Could not find a row with a null integer"
          cmd.CommandText <- "SELECT COUNT(id) FROM test_table WHERE a_string IS NULL"
          let nullStrings : int64 = downcast cmd.ExecuteScalar ()
          Expect.equal nullStrings 1L "Could not find a row with a null string"
        ]
      ]

[<EntryPoint>]
let main argv =
  runTestsInAssembly defaultConfig argv
  