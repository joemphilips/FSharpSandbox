namespace CardManagement

(*
    This file contains
*)

module CardDomain =
    open CardManagement.Common
    open System
    open System.Text.RegularExpressions
    open RResult

    let private cardNumberRegex = Regex("^[0-9]{16}$", RegexOptions.Compiled)

    (*
        技術的には、 card number は文字列として表現される。
        しかし、一定の validation rules があり、 これに違反したくはない。
        したがって、C# のように例外を投げるのではなく、 独立の型を作成する。
        コンストラクタを private にして、 factory method を定義する。このメソッドは
        `Result` を返し、 `ValidationError` になる可能性がある。
    *)

    type CardNumber = private CardNumber of string
        with
        member this.Value = match this with CardNumber s -> s
        static member Create fieldName str =
            match str with
            | (null|"") -> RResult.rmsg "card number can't be empty"
            | str ->
                if cardNumberRegex.IsMatch str then
                    CardNumber str |> Good
                else
                    RResult.rmsg "Card number must be a 16 digits string"

    (*
        ここでも、 daily limit は decimal として表現される。 しかし、厳密には `decimal` が必要なわけではない。
        負の値は取れないし、(日付の limit として負の値は適切ではない。)zero になることはできるが、 それは deily limit
        が存在しないことを示すのか、あるいは一切使用することができないことを示すのかわからない。
        Nullable <decimal> を使用しても良いが、使い方が下手だと NullReferenceException を起こしてしまう。
        何れにせよ以下のようにした方が読みやすい。
    *)
    [<Struct>]
    type DailyLimit =
        private
        | Limit of Money
        | Unlimited
        with
        static member ofDecimal dec =
            if dec > 0m then
                Money dec |> Limit
            else
                Unlimited

        member this.ToDecimalOption() =
            match this with
            | Unlimited -> None
            | Limit limit -> Some limit.Value

    (*
        コンストラクタを private にしたので、 直接パターンマッチができなくなる。
        DU の場合、これは致命的なので、ActivePattern を解放する。
        結論から見ると、 get; private set; を定義した場合と同じような感じになる。
    *)

    let (|Limit|Unlimited|) limit =
        match limit with
        | Limit dec -> Limit dec
        | Unlimited -> Unlimited

    type UserId = System.Guid

    type AccountInfo =
        {
            HolderId: UserId
            Balance : Money
            DailyLimit: DailyLimit
        }
        with
        static member Default userId =
            {
                HolderId = userId
                Balance = Money 0m
                DailyLimit = Unlimited
            }

    (*
        これはちょっと重要な奴である。 AccountInfo type は 自分が持っている money の情報を保持しており、
        決済を処理する際に間違いなく必要になる。
        ここで、deactivate されたカードでは絶対に決済処理をしたくないので、この嬢は active でない時には
        保持しないことにする。
    *)
    type CardAccountInfo =
        | Active of AccountInfo
        | Deactivated

    (*
        `DateTime` 型を使用して、 締め切りの日付を 表現しても良いが、 `DateTime` は必要な分より多くの情報を保持している。
        したがって取扱う際に疑問が生じる可能性がある。
        * timezone はどう扱えば良い?
        * 日付だけが欲しくて、 month はどうでも良い時はどうする?
        以下のようにしておくことで、利用者側が簡単になる。
    *)
    type Card =
        {
            CardNumber: CardNumber
            Name: LetterString
            HolderId: UserId
            Expiration : (Month * Year)
            AccountDetails: CardAccountInfo
        }

    type CardDetails =
        {
            Card: Card
            HolderAddress: Address
            HolderId: UserId
            HolderName: LetterString
        }

    type UserInfo =
        {
            Name: LetterString
            Id: UserId
            Address: Address
        }

    type User =
        {
            UserInfo: UserInfo
            Cards: Card list
        }

    [<Struct>]
    type BalanceChange =
        | Increase of increase : MoneyTransaction
        | Decrease of decrease : MoneyTransaction
        with
        member this.ToDecimal() =
            match this with
            | Increase i -> i.Value
            | Decrease d -> -d.Value

    [<Struct>]
    type BalanceOperation =
        {
            CardNumber: CardNumber
            Timestamp: DateTimeOffset
            BalanceChange: BalanceChange
            NewBalance: Money
        }