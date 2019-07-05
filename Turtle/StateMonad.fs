namespace FPTurtle

open System
open Common

/// Create a type to wrap a function like
///     oldState -> j(a, newState)
type TurtleStateComputation<'a> =
    TurtleStateComputation of (Turtle.TurtleState -> 'a * Turtle.TurtleState)


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
        Turtle.initialTurtleState

    let move dist =
        toUnitComputation (Turtle.move log dist)

    let turn angle =
        toUnitComputation (Turtle.turn log angle)

    let penDown =
        toUnitComputation (Turtle.penDown log)

    let penUp =
        toUnitComputation (Turtle.penUp log)

    let setColor c =
        toUnitComputation (Turtle.setColor log c)

    let drawTriangle() =
        let t = turtle {
            do! move 100.0
            do! turn 120.0<Degrees>
            do! move 100.0
            do! turn 120.0<Degrees>
            do! move 100.0
        }
        runT t initialTurtleState