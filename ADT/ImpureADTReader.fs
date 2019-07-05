namespace ImpureADTReader

[<AutoOpen>]
module LoggerModule =
    /// A fake logger type.
    type Logger = private { ___: unit}

    [<RequireQualifiedAccess>]
    module Logger =
        let info (_ : string) (_: Logger) = ()
        let warn (_ : string) (_: Logger) = ()
        let error (_ : string) (_: Logger) = ()
        let make () = { ___ = () }

[<AutoOpen>]
module DatabaseConnectionModule =
    type DatabaseConnection = private { ___ : unit }

    [<RequireQualifiedAccess>]
    module DatabaseConnection =
        let getName (_ : DatabaseConnection) = "MyDatabase"
        let getValue(_ : DatabaseConnection) = 10
        let make (_ : string) = { ___ = () }


[<AutoOpen>]
module FrobServiceModule =
    type FrobService = private {
            SanitizationCapacity: int
            FinalizationRate: int
            Logger: Logger
            Database: DatabaseConnection
        }

    module FrobService =
        let private queryFrobData frobService =
            DatabaseConnection.getValue frobService.Database