namespace FPTurtle

open System
open Common
open TurtleV1

/// Create a type to wrap a function like
///     oldState -> j(a, newState)
type TurtleStateComputation<'a> =
    TurtleStateComputation of (TurtleV1.TurtleState -> 'a * TurtleV1.TurtleState)


module TurtleStateComputation =
    let runT turtle state =
        let (TurtleStateComputation innerFn) = turtle
        innerFn state

    let returnT x =
        let innerFn state =
            x, state
        TurtleStateComputation innerFn


    let bindT f xT =
        let innerFn state =
            let x, state2 = runT xT state
            runT (f x) state2
        TurtleStateComputation innerFn

    let mapT f =
        bindT (f >> returnT)
        
    let toComputation f =
        let innerFn state =
            let (result, newState) = f state
            (result, newState)
        TurtleStateComputation innerFn

    let toUnitComputation f =
        let f2 state =
            (), f state
        toComputation f2

    type TurtleBuilder() =
        member this.Return(x) = returnT x
        member this.Bind(x, f) = bindT f x

    let turtle = TurtleBuilder()

module TurtleComputationClient =
    open TurtleStateComputation

    let log msg = printfn "%s" msg

    let initialTurtleState =
        TurtleV1.initialTurtleState

    let move dist =
        toUnitComputation (TurtleV1.move log dist)

    let turn angle =
        toUnitComputation (TurtleV1.turn log angle)

    let penDown =
        toUnitComputation (TurtleV1.penDown log)

    let penUp =
        toUnitComputation (TurtleV1.penUp log)

    let setColor c =
        toUnitComputation (TurtleV1.setColor log c)

    let drawTriangle() =
        let t = turtle {
            do! move 100.0
            do! turn 120.0<Degrees>
            do! move 100.0
            do! turn 120.0<Degrees>
            do! move 100.0
        }
        runT t initialTurtleState