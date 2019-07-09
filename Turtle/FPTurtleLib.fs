namespace FPTurtle

module TurtleV1 =
    type TurtleState = {
        position: Position
        angle: float<Degrees>
        color: PenColor
        penState: PenState
    }

    let initialTurtleState = {
        position = initialPosition
        angle = 0.0<Degrees>
        color = initialColor
        penState = initialPenState
    }

    // It is important in here that "state" is a last param of these functions.
    let move log distance state =
        log (sprintf "Move %0.1f" distance)
        let newPosition = calcNewPosition distance state.angle state.position
        { state with position = newPosition}

    let turn log angle state =
        log (sprintf "Turn %0.1f" angle)
        let newAngle = (state.angle + angle) % 360.0<Degrees>
        { state with angle = newAngle}

    let penUp log state =
        log "Pen up"
        {state with penState = Up}

    let penDown log state =
        log "Pen down"
        {state with penState = Down}

    let setColor log color state =
        log (sprintf "SetColor %A" color)
        { state with color = color}