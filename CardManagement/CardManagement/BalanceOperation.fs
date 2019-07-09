namespace CardManagement

module BalanceOperation =
    open CardDomain
    open System
    open CardManagement.Common

    let isDecrease change =
        match change with
        | Increase _ -> false
        | Decrease _ -> true

    let spentAtDate (date: DateTimeOffset) cardNumber operations =
        let date = date.Date
        let operationFilter {CardNumber = n; BalanceChange = bc ; Timestamp = ts} =
            isDecrease bc && n = cardNumber && ts.Date = date

        let spendings = List.filter operationFilter operations
        List.sumBy  (fun s -> -s.BalanceChange.ToDecimal()) spendings |> Money