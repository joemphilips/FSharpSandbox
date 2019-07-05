namespace CustomFSharpPlus

[<Struct>]
type State<'s, 't> = State of ('s -> ('t * 's))

[<RequireQualifiedAccess>]
module State =
    let run (State x) = x

    let map f (State m) = State (fun s -> let (a: 'T, s') = m s in (f a, s'))
    let bind f (State m) = State (fun s -> let (a: 'T, s') = m s in run (f a) s')
    let apply (State f) (State x) = State (fun s -> let (f', s1) = f s in let (x': 'T, s2) = x s1 in (f' x', s2))

    let eval (State sa) (s: 's) = fst (sa s)
    let exec (State sa) s = snd (sa s)

    let get = State (fun s -> (s, s))
    let put s = State (fun _ -> (), s)

type State<'s, 't> with
    static member Map (x, f: 'T -> _) = State.map f x
    static member Return a = State (fun s -> a, s)
    static member (>>=) (x, f : 'T -> _) = State.bind f x
    static member (<*>) (f: State<'S, 'T -> 'U>, x: State<'S, 'T>) = State.apply f x: State<'S, 'U>
    static member get_Get () = State.get
    static member Put x = State.put x

type StateT<'s, '``monad<'t * 's>``> = StateT of ('s -> '``monad<'t * 's>``)

module StateT =
    let run (StateT x) = x: 'S -> '``Monad<'T * 'S>``