namespace FPTurtle
open System.Numerics

type TurtleId = System.Guid

type TurtleCommandAction =
    | Move of Distance
    | Turn of Angle
    | PenUp
    | PenDown
    | SetColor of PenColor

type TurtleCommand = {
    turtleId: TurtleId
    action: TurtleCommandAction
}


// Define events
type StateChangedEvent =
    | Moved of Distance
    | Turned of Angle
    | PenWentUp
    | PenWentDown
    | ColorChanged of PenColor

type MovedEvent = {
    startPos: Position
    endPos: Position
    penColor : PenColor option
}

type TurtleEvent =
    | StateChangedEvent of StateChangedEvent
    | MovedEvent of MovedEvent

module CommandHandler =
    let applyEvent log oldState e =
        match e with
        | Moved d ->
            TurtleV1.move log d oldState
        | Turned angle ->
            TurtleV1.turn log angle oldState
        | PenWentUp ->
            TurtleV1.penUp log oldState
        | PenWentDown ->
            TurtleV1.penDown log oldState
        | ColorChanged c ->
            TurtleV1.setColor log c oldState

    let eventsFromCommand log command stateBeforeCommand =
        let stateChangedEvent = 
            match command.action with
            | Move d -> Moved d
            | Turn angle -> Turned angle
            | PenUp -> PenWentUp
            | PenDown -> PenWentDown
            | SetColor c -> ColorChanged c

        let stateAfterCommand =
            applyEvent log stateBeforeCommand stateChangedEvent

        let startPos = stateBeforeCommand.position
        let endPos = stateAfterCommand.position
        let penColor =
            if stateBeforeCommand.penState = Down then
                Some stateBeforeCommand.color
            else
                None

        let movedEvent = {
            startPos = startPos
            endPos = endPos
            penColor = penColor
        }
        if startPos <> endPos then
            [ StateChangedEvent stateChangedEvent; MovedEvent movedEvent]
        else
            [ StateChangedEvent stateChangedEvent]

    // StateChangedEvents を 特定の turtle id に関して全て取得するためのもの
    type GetStateChangedEventsForId =
        TurtleId -> StateChangedEvent list

    // 特定の event を保存する。
    type SaveTurtleEvent =
        TurtleId -> TurtleEvent -> unit

    let commandHandler
        (log: string -> unit)
        (getEvents: GetStateChangedEventsForId)
        (saveEvent: SaveTurtleEvent)
        (command: TurtleCommand) =
        // get historical events for this turtle id
        let eventHistory =
            getEvents command.turtleId

        // recreate events
        let stateBeforeCommand =
            let nolog = ignore /// no logging when recreating state
            eventHistory |> List.fold (applyEvent nolog) TurtleV1.initialTurtleState

        // construct the events from the command and the stateBeforeCommand
        // Do use the supplied logger for this bit.
        let events = eventsFromCommand log command stateBeforeCommand

        // events を event store に保存
        events |> List.iter (saveEvent command.turtleId)