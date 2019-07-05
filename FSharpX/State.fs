namespace CustomFSharpX

module State =
    open System
    type State<'T, 'State> = 'State -> 'T * 'State

    let getState = fun s -> (s, s)
    let putState s = fun _ -> (), s

    let eval m s = m s |> fst
    let exec m s = m s |> snd

    let empty = fun s -> ((), s)
    let bind k m = fun s -> let (a, s') = m s in (k a) s'

    type StateBbuilder() =
        member this.Return (a) : State<'T, 'State> = fun s -> (a, s)
        member this.ReturnFrom(m: State<_,_>) = m
        member this.Bind (m: State<_,_>, k: 'T -> State<'U, 'State>) = bind k m