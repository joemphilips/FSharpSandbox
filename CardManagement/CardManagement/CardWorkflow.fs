namespace CardManagement

(*
    最終的に、これが domain function を composition する部分である。
    実行 Tree の作成を行なっている。
    実際の使用例を見たい場合、 `CardManagement.Infrastructure.CardProgramInterpreter`
    を見ると良い。
*)

module CardWorkflow =
    open CardDomain
    open System
    open CardDomainCommandModels
    open CardManagement.Common
    open CardDomainQueryModels
    open Errors
    open CardProgramBuilder

    let private noneToError (a: 'a option) id =
        match a with
        | Some a -> Good a
        | None ->
            EntityNotFound (sprintf "%s Entity" typeof<'a>.Name, id)
            |> box
            |> RBad.Object
            |> RResult.rbad

    let private tryGetCard cardNumber =
        program {
            let! card = getCardByNumber cardNumber
            let! card = noneToError card cardNumber.Value |> expectDataRelatedError
            return Good card
        }

    let processPayment (currentDate: DateTimeOffset, payment) =
        program {
            // ここでは、 `expectValidationError` と `expectDataRelatedErrors` 関数を使っている。
            // これは異なる error を Error 型に map する。それぞれの 実行ブランチは 同じ型を返す必要があるためである。
            // さらに、 何が起きているのかを online で理解できるようにしてくれる。
            // validation をおこなっているのか。それとも IO を行なっているのかが一目でわかる。
            let! cmd = validateProcessPaymentCommand payment |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let today = currentDate.Date |> DateTimeOffset
            let tomorrow = currentDate.Date.AddDays 1. |> DateTimeOffset
            let! operations = getBalanceOperations (cmd.CardNumber, today, tomorrow)
            let spentToday = BalanceOperation.spentAtDate currentDate cmd.CardNumber operations
            let! (card, op) =
                CardActions.processPayment currentDate spentToday card cmd.PaymentAmount
                |> expectOperationNotAllowedError
            do! saveBalanceOperation op |> expectDataRelatedErrorProgram
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Good
        }

    let setDailyLimit (currentDate: DateTimeOffset, setDailyLimitCommand) =
        program {
            let! cmd = validateSetDailyLimitCommand setDailyLimitCommand |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let! card = CardActions.setDailyLimit currentDate cmd.DailyLimit card |> expectOperationNotAllowedError
            return card |> toCardInfoModel |> Good
        }

    let topUp (currentDate: DateTimeOffset, topUpCmd) =
        program {
            let! cmd = validateTopUpCommand topUpCmd |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let! (card, op) = CardActions.topUp currentDate card cmd.TopUpAmount |> expectOperationNotAllowedError
            do! saveBalanceOperation op |> expectDataRelatedErrorProgram
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Good
        }

    let activateCard activateCmd =
        program {
            let! cmd = validateActivateCardCommand activateCmd |> expectValidationError
            let! res = getCardWithAccountInfo cmd.CardNumber
            let! (card, accInfo) = noneToError res cmd.CardNumber.Value |> expectDataRelatedError
            let card = CardActions.activate accInfo card
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Good
        }

    let deactivateCard deactivateCmd =
        program {
            let! cmd = validateDeactivateCardCommand deactivateCmd |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let card = CardActions.deactivate card
            do! replaceCard card
            return card |> toCardInfoModel |> Good
        }

    let createUser (userid, createUserCommand) =
        program {
            let! userInfo = validateCreateUserCommand userid createUserCommand |> expectValidationError
            do! createNewUser userInfo |> expectDataRelatedErrorProgram
            return { UserInfo = userInfo; Cards = [] } |> toUserModel |> Good
        }

    let createCard cardCommand =
        program {
            let! card = validateCreateCardCommand cardCommand
            let accountInfo = AccountInfo.Default cardCommand.UserId
            do! createNewCard (card, accountInfo) |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Good
        }

    let getCard cardNumber =
        program {
            let! cardNumber = CardNumber.Create "cardNumber" cardNumber
            let! card = getCardByNumber cardNumber
            return card |> Option.map toCardInfoModel |> Good
        }

    let getUser userId =
        simpleProgram {
            let! maybeUser = getUserById userId
            return maybeUser |> Option.map toUserModel
        }
