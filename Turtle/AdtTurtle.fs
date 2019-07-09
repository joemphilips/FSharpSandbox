namespace FPTurtle

open Common
open System


module AdtTurtle =
    type Turtle = private {
        position: Position
        angle : float<Degrees>
        color: PenColor
        penState: PenState
    }

    module Turtle =
        let Create(initialColor) =
            {
                position = initialPosition
                angle = 0.0<Degrees>
                color = initialColor
                penState = initialPenState
            }

        let move log distance state =
            log (sprintf " Move %0.1f" distance)
            let newpos = calcNewPosition distance state.angle state.position
            if (state.penState = Down) then
                dummyDrawLine log state.position newpos state.color
            {state with position = newpos }

        let penUp log state =
            log "Pen up"
            { state with penState = Up }
        let penDown log state =
            log "Pen down"
            { state with penState = Down }

        let setColor log color state =
            log (sprintf " SetColor %A" color)
            { state with color = color }

module AdtTurtleClient =
    let log msg =
        printfn "%s" msg

    let move = TurtleV1.move log