[<AutoOpen>]
module ApplciativeOptParser

// refs: https://github.com/fsharp/fslang-suggestions/issues/579#issuecomment-309764738

let readInt (s: string) =
    match System.Int32.TryParse s with
    | true, i -> Some i
    | _ -> None

type Opt<'a> = Opt of name:string * defaultValue: 'a option * read: (string -> 'a option)
    with
        static member Name<'a> (Opt(n,_,_) : Opt<'a>) = n
        static member Read<'a> (Opt(_,_,r) : Opt<'a>) = r
        static member Default<'a> (Opt(_,d,_) : Opt<'a>) = d
        static member Map (f: 'a -> 'b) (a : Opt<'a>) =
            let (Opt(n, d, r)) = a in Opt(n, Option.map f d, r >> Option.map f)

type OptAp<'a> =
    | PureOpt of 'a
    | ApOpt of (Opt< obj -> 'a>) * OptAp<obj>

    with
        static member Map<'a, 'b> (f: 'a -> 'b) (a: OptAp<'a>): OptAp<'b> =
            match a with
            | PureOpt a -> PureOpt (f a)
            | ApOpt (x, y) -> ApOpt(Opt.Map(fun g -> g >> f) x, y)

        static member Size<'a> (a: OptAp<'a>) : int =
            match a with
            | PureOpt _ -> 1
            | ApOpt(a, b) -> 1 + OptAp.Size b