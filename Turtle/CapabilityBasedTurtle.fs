namespace FPTurtle

open System
open Common

module CapBasedTurtle =
    type Log = string -> unit

    type private TurtleState = {
        position: Position
        angle : float<Degrees>
        color: PenColor
        penState: PenState

        canMove: bool // new!
        availableInk: Set<PenColor> // new!
        logger: Log // new!
    }

    type MoveResponse =
        | MoveOk
        | HitABarrier

    type SetColorResponse =
        | ColorOk
        | OutOfInk

    type TurtleFunctions = {
        move: MoveFn option
        turn: TurnFn
        penUp : PenUpDownFn
        penDown: PenUpDownFn
        setBlack: SetColorFn option
        setBlue: SetColorFn option
        setRed: SetColorFn option
    }
    and MoveFn = Distance -> MoveResponse * TurtleFunctions
    and TurnFn = Angle -> TurtleFunctions
    and PenUpDownFn = unit -> TurtleFunctions
    and SetColorFn = unit -> (SetColorResponse * TurtleFunctions)