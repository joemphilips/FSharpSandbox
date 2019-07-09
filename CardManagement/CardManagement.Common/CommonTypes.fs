namespace CardManagement.Common

[<AutoOpen>]
module CommonTypes =
    open System.Text.RegularExpressions
    open CardManagement.Common.Errors

    let cardNumberRegex = Regex("^[0-9]{16}$", RegexOptions.Compiled)
    let lettersRegex = Regex("^[\w]+[\w ]+[\w]+$", RegexOptions.Compiled)

    let postalCodeRegex = Regex("^[0-9]{5,6}$", RegexOptions.Compiled)

    type Month =
        | January | February | March | April | May | June | July | August | September | October | November | December
        with
        member this.ToNumber() =
            match this with
            | January -> 1us
            | February -> 2us
            | March -> 3us
            | April -> 4us
            | May -> 5us
            | June -> 6us
            | July -> 7us
            | August -> 8us
            | September -> 9us
            | October -> 10us
            | November -> 11us
            | December -> 12us
        static member Create field n =
            match n with
            | 1us -> January |> Good
            | 2us -> February |> Good
            | 3us -> March |> Good
            | 4us -> April |> Good
            | 5us -> May |> Good
            | 6us -> June |> Good
            | 7us -> July |> Good
            | 8us -> August |> Good
            | 9us -> September |> Good
            | 10us -> October |> Good
            | 11us -> November |> Good
            | 12us -> December |> Good
            | _ -> RResult.rmsg "Number must be from 1 to 12"

    [<Struct>]
    type Year = private Year of uint16
        with
            member this.Value = let (Year year) = this in year
            static member Create field year =
                if year >= 2019us && year <= 2050us then
                    Year year |> Good
                else
                    RResult.rmsg (sprintf "Year must be between 2019 and 2050. it was %A" year)

    type LetterString = private LetterString of string
        with
        member this.Value = let (LetterString str) = this in str
        static member Create field str =
            match str with
            | (""|null) -> RResult.rmsg (sprintf "string must contain letters %A" str)
            | str ->
                if lettersRegex.IsMatch str then
                    LetterString str |> Good
                else
                    RResult.rmsg (sprintf "string must contain only letters %A" str)
    [<Struct>]
    type MoneyTransaction = private MoneyTransaction of decimal
        with
        member this.Value = let (MoneyTransaction v) = this in v
        static member Create amount =
            if amount > 0M then
                MoneyTransaction amount |> Good
            else
                RResult.rmsg "Transaction amount must be positive"

    [<Struct>]
    type Money = Money of decimal
        with
        member this.Value = match this with Money money -> money
        static member (+) (Money left, Money right) = left + right |> Money
        static member (-) (Money left, Money right) = left - right |> Money
        static member (+) (Money money, MoneyTransaction tran) = money + tran |> Money
        static member (-) (Money money, MoneyTransaction tran) = money - tran |> Money

    type PostalCode = private PostalCode of string
        with
        member this.Value = match this with PostalCode code -> code
        static member Create field str =
            match str with
            | (""|null) -> RResult.rmsg "Postal code can't be empty"
            | str ->
                if postalCodeRegex.IsMatch(str) |> not
                    then RResult.rmsg "postal code must contain 5 or 6 digits and nothing else"
                else PostalCode str |> Good

    type Address = {
        Country :Country
        City: LetterString
        PostalCode: PostalCode
        AddressLine1: string
        AddressLine2: string
    }

    type nil<'a when 'a: struct and 'a: (new: unit-> 'a) and 'a:> System.ValueType> = System.Nullable<'a>
