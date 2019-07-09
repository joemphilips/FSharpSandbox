namespace CardManagement

module CardProgramBuilder =
    open CardDomain
    open System
    open CardManagement.Common
    open Errors

    (*
        このモジュールは ちょっと説明が必要。
        ここまでで、 validtion のための関数、 logic, model mappingを色々作ってきたが、
        これらは全て純粋関数であった。したがって combine するのはそんなに難しいことではない。
        しかし、 DB とやり取りする必要があり、他の layer にある関数とも合わせる必要がある。
        いずれもここからは参照できないが、それらの output に基づいた business decision を行う
        必要があるので、 emulate (もしくは inject と言っても良い) を行う必要がある。
        OOP の場合、 DI frameworks で行うが、 最終的なゴールは実行時エラーをできる限り減らすことにある。
        Classic IoC container では逆の方向に向かってしまう。
        最初のチョイスとしては、問題ないので、そのコードを obsolete-dependency-managing というブランチ
        に移行させた。 CardPipelines.fs というファイルを見て欲しい。
        ここでは interpretor pattern で同じことを行う。
        AST の構築と、実際の実行を二つに分けるのがミソである。以下のようなインストラクションを用意する。
        * input card number を validate する。もし valid ならば
        * card をその number で取得する。存在するならb
        * Activate し、
        * 結果を保存し、
        * model にマップして返す。

        では、この tree はどの db が使用させるのか知らない。どのライブラリを使うのかも知らない。
        sync call なのか async call なのかも気にしない。
        知っているのは operation の名前、input の型と返り値の型だけである。
    *)

    type Program<'a> =
        | GetCard of CardNumber * (Card option -> Program<'a>)
        | GetCardWithAccountInfo of CardNumber * ((Card * AccountInfo) option -> Program<'a>)
        | CreateCard of (Card * AccountInfo) * (RResult<unit> -> Program<'a>)
        | ReplaceCard of Card * (RResult<unit> -> Program<'a>)
        | GetUser of UserId * (User option -> Program<'a>)
        | CreateUser of UserInfo * (RResult<unit> -> Program<'a>)
        | GetBalanceOperations of (CardNumber * DateTimeOffset * DateTimeOffset) * (BalanceOperation list -> Program<'a>)
        | SaveBalanceOperation of (BalanceOperation) * (RResult<unit> -> Program<'a>)
        | Stop of 'a

    let rec bind f instruction =
        match instruction with
        | GetCard (x, next) -> GetCard (x, (next >> bind f))
        | GetCardWithAccountInfo (x, next) -> GetCardWithAccountInfo (x, (next >> bind f))
        | CreateCard (x, next) -> CreateCard (x, (next >> bind f))
        | ReplaceCard (x, next) -> ReplaceCard (x, (next >> bind f))
        | GetUser (x, next) -> GetUser (x,(next >> bind f))
        | CreateUser (x, next) -> CreateUser (x,(next >> bind f))
        | GetBalanceOperations (x, next) -> GetBalanceOperations (x,(next >> bind f))
        | SaveBalanceOperation (x, next) -> SaveBalanceOperation (x,(next >> bind f))
        | Stop x -> f x

    // basic function たちを定義。これらは expression tree builder で使うことで、 dependency call を表現せよ。
    let rec stop x = Stop x
    let getCardByNumber n = GetCard (n, stop)
    let getCardWithAccountInfo n = GetCardWithAccountInfo (n, stop)
    let createNewCard (c, acc) = CreateCard ((c, acc), stop)
    let replaceCard card = ReplaceCard (card, stop)
    let getUserById id = GetUser (id, stop)
    let createNewUser user = CreateUser (user, stop)
    let getBalanceOperations (number, fromDate, toDate) = GetBalanceOperations ((number, fromDate, toDate), stop)
    let saveBalanceOperation op = SaveBalanceOperation (op, stop)

    // これらは CE のための Builder である。 CE を使うことによって、 execution tree の利用が簡単になる。
    type SimpleProgramBuilder() =
        member __.Bind (x, f) = bind f x
        member __.Return x = Stop x
        member __.Zero () = Stop ()
        member __.ReturnFrom x = x

    type ProgramBuilder() =
        member __.Bind (x,f) = bind f x
        member this.Bind  (x, f) =
            match x with
            | Good x -> this.ReturnFrom (f x)
            | Bad e -> this.Return (Bad e)
        member this.Bind ((x: Program<RResult<_>>), f) =
            let f x =
                match x with
                | Good x -> this.ReturnFrom (f x)
                | Bad e -> this.Return(Bad e)
            this.Bind(x, f)
        member this.Return x = Stop x
        member this.Zero () = Stop ()
        member tihs.ReturnFrom x = x

    let program = ProgramBuilder ()
    let simpleProgram = SimpleProgramBuilder()

    let expectDataRelatedErrorProgram (prog: Program<RResult<'a>>) =
        program {
            let! res = prog
            return expectDataRelatedError res
        }