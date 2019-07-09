namespace CardManagement


(*
    このモジュールでは、command models, validate 済み command, そして validation funcitons を保持する
    C# で通常利用されるパターンでは、例外を投げるのが普通である。
    このアプローチでは、 validation を忘れてしまう可能性が大いにある。
*)
module CardDomainCommandModels =
    open CardManagement.Common
    open CardDomain
    open CardManagement.Common.Errors
    type ActivateCommand = { CardNumber: CardNumber }
    type DeactivateCommand = { CardNumber: CardNumber }

    type SetDailyLimitCommand =
        {
            CardNumber: CardNumber
            DailyLimit: DailyLimit
        }

    type ProcessPaymentCommand =
        {
            CardNumber: CardNumber
            PaymentAmount: MoneyTransaction
        }
    type TopUpCommand =
        { CardNumber: CardNumber
          TopUpAmount: MoneyTransaction }

    [<CLIMutable>]
    type ActivateCardCommandModel =
        { CardNumber: string }

    [<CLIMutable>]
    type DeactivateCardCommandModel =
        { CardNumber: string }

    [<CLIMutable>]
    type SetDailyLimitCardCommandModel =
        { CardNumber: string
          Limit: decimal }

    [<CLIMutable>]
    type ProcessPaymentCommandModel =
        { CardNumber: string
          PaymentAmount: decimal }

    [<CLIMutable>]
    type TopUpCommandModel =
        { CardNumber: string
          TopUpAmount: decimal }

    [<CLIMutable>]
    type CreateAddressCommandModel =
        { Country: string
          City: string
          PostalCode: string
          AddressLine1: string
          AddressLine2: string }

    [<CLIMutable>]
    type CreateUserCommandModel =
        { Name: string
          Address: CreateAddressCommandModel }

    [<CLIMutable>]
    type CreateCardCommandModel =
        { CardNumber : string
          Name: string
          ExpirationMonth: uint16
          ExpirationYear: uint16
          UserId: UserId }

    (*
    This is a brief API description made with just type aliases.
    As you can see, every public function here returns a `Result` with possible `ValidationError`.
    No other error can occur in here.
    *)
    type ValidateActivateCardCommand   = ActivateCardCommandModel      -> RResult<ActivateCommand>
    type ValidateDeactivateCardCommand = DeactivateCardCommandModel    -> RResult<DeactivateCommand>
    type ValidateSetDailyLimitCommand  = SetDailyLimitCardCommandModel -> RResult<SetDailyLimitCommand>
    type ValidateProcessPaymentCommand = ProcessPaymentCommandModel    -> RResult<ProcessPaymentCommand>
    type ValidateTopUpCommand          = TopUpCommandModel             -> RResult<TopUpCommand>
    type ValidateCreateAddressCommand  = CreateAddressCommandModel     -> RResult<Address>
    type ValidateCreateUserCommand     = CreateUserCommandModel        -> RResult<UserInfo>
    type ValidateCreateCardCommand     = CreateCardCommandModel        -> RResult<Card>

    let private validateCardNumber = CardNumber.Create "cardNumber"

    let validateActivateCardCommand : ValidateActivateCardCommand =
        fun cmd ->
            rresult {
                let! number = cmd.CardNumber |> validateCardNumber
                return { ActivateCommand.CardNumber = number }
            }

    let validateDeactivateCardCommand : ValidateDeactivateCardCommand =
        fun cmd ->
            rresult {
                let! number = cmd.CardNumber |> validateCardNumber
                return { DeactivateCommand.CardNumber = number }
            }

    let validateSetDailyLimitCommand : ValidateSetDailyLimitCommand =
        fun cmd ->
            rresult {
                let! number = cmd.CardNumber |> validateCardNumber
                let limit = DailyLimit.ofDecimal cmd.Limit
                return
                    { CardNumber = number
                      DailyLimit = limit }
            }

    let validateProcessPaymentCommand : ValidateProcessPaymentCommand =
        fun cmd ->
            rresult {
                let! number = cmd.CardNumber |> validateCardNumber
                let! amount = cmd.PaymentAmount |> MoneyTransaction.Create
                return
                    { ProcessPaymentCommand.CardNumber = number
                      PaymentAmount = amount }
            }

    let validateTopUpCommand : ValidateTopUpCommand =
        fun cmd ->
        rresult {
            let! number = cmd.CardNumber |> validateCardNumber
            let! amount = cmd.TopUpAmount |> MoneyTransaction.Create
            return
                { TopUpCommand.CardNumber = number
                  TopUpAmount = amount }
        }

    let validateCreateAddressCommand : ValidateCreateAddressCommand =
        fun cmd ->
        rresult {
            let! country = parseCountry cmd.Country
            let! city = LetterString.Create "city" cmd.City
            let! postalCode = PostalCode.Create "postalCode" cmd.PostalCode
            return
                { Address.Country = country
                  City = city
                  PostalCode = postalCode
                  AddressLine1 = cmd.AddressLine1
                  AddressLine2 = cmd.AddressLine2}
        }

    let validateCreateUserCommand userId : ValidateCreateUserCommand =
        fun cmd ->
        rresult {
            let! name = LetterString.Create "name" cmd.Name
            let! address = validateCreateAddressCommand cmd.Address
            return
                { UserInfo.Id = userId
                  Name = name
                  Address = address }
        }

    let validateCreateCardCommand : ValidateCreateCardCommand =
        fun cmd ->
        rresult {
            let! name = LetterString.Create "name" cmd.Name
            let! number = CardNumber.Create "cardNumber" cmd.CardNumber
            let! month = Month.Create "expirationMonth" cmd.ExpirationMonth
            let! year = Year.Create "expirationYear" cmd.ExpirationYear
            return
                { Card.CardNumber = number
                  Name = name
                  HolderId = cmd.UserId
                  Expiration = month,year
                  AccountDetails =
                     AccountInfo.Default cmd.UserId
                     |> Active }
        }
