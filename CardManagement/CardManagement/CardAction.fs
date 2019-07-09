namespace CardManagement

(*
    このモジュールは business logic のみを保有している。 data access layer については何も知らない。
*)

module CardActions =
    open System
    open CardDomain
    open CardManagement.Common
    open CardManagement.Common.Errors

    let private isExpired (currentDate: DateTimeOffset) (month: Month, year: Year) =
        (int year.Value, month.ToNumber() |> int) < (currentDate.Year, currentDate.Month)

    let private setDailyLimitNotAllowed = RResult.rmsg

    let private processPaymentNotAllowed = RResult.rmsg

    let private cardExpiredMsg (cardNumber: CardNumber) =
        sprintf "Card %s is expired" cardNumber.Value
 
    let private cardDeactivatedMsg ( cardNumber: CardNumber) =
        sprintf "Card %s is deactivated" cardNumber.Value

    let isCardExpired (currentDate: DateTimeOffset) card =
        isExpired currentDate card.Expiration

    let deactivate card =
        match card.AccountDetails with
        | Deactivated -> card
        | Active _ -> { card with AccountDetails = Deactivated }

    let activate (cardAccountInfo: AccountInfo) card =
        match card.AccountDetails with
        | Active _ -> card
        | Deactivated -> { card with AccountDetails = Active cardAccountInfo }

    let setDailyLimit (currentDate: DateTimeOffset) limit card =
        if isCardExpired currentDate card then
            cardExpiredMsg card.CardNumber |> setDailyLimitNotAllowed
        else
        match card.AccountDetails with
        | Deactivated -> cardDeactivatedMsg card.CardNumber |> setDailyLimitNotAllowed
        | Active accInfo -> { card with AccountDetails = Active { accInfo with DailyLimit = limit } } |> Good


    let processPayment (currentDate: DateTimeOffset) (spentToday: Money) card (paymentAmount: MoneyTransaction) =
        if isCardExpired currentDate card then
            cardExpiredMsg card.CardNumber |> processPaymentNotAllowed 
        else
            match card.AccountDetails with
            | Deactivated -> cardDeactivatedMsg card.CardNumber |> processPaymentNotAllowed
            | Active accInfo ->
                if paymentAmount.Value > accInfo.Balance.Value then
                    sprintf "Insufficient funds on card %s" card.CardNumber.Value
                    |> processPaymentNotAllowed
                else
                    match accInfo.DailyLimit with
                    | Limit l when l < spentToday + paymentAmount ->
                        sprintf "Daily limit is exceeded for card %s with daily limit %M. Today was spent %M"
                            card.CardNumber.Value l.Value spentToday.Value
                        |> processPaymentNotAllowed
                    | Limit _ | Unlimited ->
                        let newBalance = accInfo.Balance - paymentAmount
                        let updatedCard = { card with AccountDetails = Active { accInfo with Balance = newBalance } }
                        let balanceOperation =
                            { Timestamp = currentDate
                              CardNumber = card.CardNumber
                              NewBalance = newBalance
                              BalanceChange = Decrease paymentAmount }
                        Good (updatedCard, balanceOperation)

    let topUp (currentDate) card (topUp: MoneyTransaction) =
        if isCardExpired currentDate card then
            cardExpiredMsg card.CardNumber |> RResult.rmsg
        else
            match card.AccountDetails with
            | Deactivated -> cardDeactivatedMsg card.CardNumber |> RResult.rmsg
            | Active accInfo ->
                let newBalance = accInfo.Balance + topUp
                let updatedCard = { card with AccountDetails = Active { accInfo with Balance = newBalance } }
                let balanceOperation =
                    { Timestamp = currentDate
                      NewBalance = newBalance
                      CardNumber = card.CardNumber
                      BalanceChange = Increase topUp }
                Good (updatedCard, balanceOperation)
