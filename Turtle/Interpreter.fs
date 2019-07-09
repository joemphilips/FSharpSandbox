namespace FPTurtle

open System
open Common

// https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/13-Interpreter-v1.fsx
module InterpreterV1 =
    open TurtleV2
    type TurtleCommand =
        | Move of Distance
        | Turn of Angle
        | PenUp
        | PenDown
        | SetColor of PenColor

    type TurtleResponse =
        | Moved of MoveResponse
        | Turned
// https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/13-Interpreter-v2.fsx
module InterpreterV2 =
    open TurtleV2
    type TurtleInstruction<'Tnext> =
        | Move of Distance * (MoveResponse -> 'Tnext)
