namespace CardManagement.Data

module CardDomainEntities =
    open System
    open FSharp.Data.Sql
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq.RuntimeHelpers

    type UserId = Guid

    let [<Literal>] dbVendor = FSharp.Data.Sql.Common.DatabaseProviderTypes.POSTGRESQL
    let [<Literal>] connString =
        "Host=localhost;" +
        "Port=5432;" +
        "Database=PinetreeCqrsDB;" + 
        "Username=root;" + 
        "Password=root"

    let [<Literal>] resPath = @"/Users/miyamotojou/.nuget/packages/npgsql/4.0.7/lib/netstandard2.0/"

    type sqlProvider = 
        SqlDataProvider<
            dbVendor,
            connString,
            "",
            resPath,
            1000,
            true>

    (*
        ここでは、データを DB に保持するための entities を定義している。
        単純な構造を使用しているので、 JSON で表現することができる。
        全ての entity は別々の Identifier を持っている。 User の場合は UserId = Guid である。
        しかし、エラーメッセージの場合は統一的な方法で扱いたい。
        そのような場合は、 EntityId というのを使用している。
    *)

    [<CLIMutable>]
    type AddressEntity =
        {
            Country : string
            City: string
            PostalCode: string
            AddressLine1: string
            AddressLine2: string
        }
        with
        member this.EntityId = sprintf "%A" this

    type CardEntity = {
        CardNumber: string
        Name: string
        IsActive: bool
        ExpirationMonth: uint16
        ExpirationYear: uint16
        UserId: UserId
    }
    with
    member this.EntityId = this.CardNumber.ToString()
    // この Id comparer quoation をここでは使用する。 (C# の expression の F# Alternative である。)
    // entity を Id で update するために使用する。
    // 異なる entity の場合、 identifier は異なる名前と type を持つためである。
    member this.IdComparer = <@ System.Func<_, _> ( fun c -> c.CardNumber = this.CardNumber ) @>
