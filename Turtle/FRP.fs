namespace FPTurtle
open System.Collections.Generic
open FPTurtle.CommandHandler
open System

type EventStore() =
    let eventDict = Dictionary<System.Guid, obj list>()
    let saveEvent = new Event<System.Guid * obj>()

    member this.SaveEvent = saveEvent.Publish

    member this.Save(eventId, event) =
        match eventDict.TryGetValue eventId with
        | true, eventList ->
            let newList = event :: eventList
            eventDict.[eventId] <- newList
        | false, _ ->
            let newList = [event]
            eventDict.[eventId] <- newList
        saveEvent.Trigger(eventId, event)

    member this.Get<'a>(eventId) =
        match eventDict.TryGetValue eventId with
        | true, eventList ->
            eventList
            |> Seq.cast<'a>  |> Seq.toList
            |> List.rev
        | false, _ -> []

    member this.Clear(eventId) =
        eventDict.[eventId] <- []

module EventProcessors =
    let turtleFilter ev =
        match box ev with
        | :? TurtleEvent as tev -> Some tev
        | _ -> None

    let moveFilter = function
        | MovedEvent ev -> Some ev
        | _ -> None

    let stateChangedEventFilter = function
        | StateChangedEvent ev -> Some ev
        | _ -> None

    let physicalTutleProcessor (eventStream: IObservable<Guid * obj>) =
        let subscriberFn (ev: MovedEvent) =
            let colorText =
                match ev.penColor with
                | Some c -> sprintf "line of color %A" c
                | None -> "no line"
            printfn "[turtle] ] Moved from (%0.2f, %0.2f) to (%0.2f, %0.2f) with %s"
                ev.startPos.x ev.startPos.y ev.endPos.x ev.endPos.y colorText

        eventStream
        |> Observable.choose (fun (id, ev) -> turtleFilter ev)
        |> Observable.choose (moveFilter)
        |> Observable.subscribe subscriberFn

    /// Draw lines on a graphics device
    let graphicsProcessor (eventStream:IObservable<Guid*obj>) =

    // the function that handles the input from the observable
        let subscriberFn (ev:MovedEvent) =
            match ev.penColor with
            | Some color -> 
                printfn "[graphics]: Draw line from (%0.2f,%0.2f) to (%0.2f,%0.2f) with color %A" 
                    ev.startPos.x ev.startPos.y ev.endPos.x ev.endPos.y color
            | None -> 
                ()  // do nothing

        // start with all events
        eventStream
        // filter the stream on just TurtleEvents
        |> Observable.choose (function (id,ev) -> turtleFilter ev)
        // filter on just MovedEvents
        |> Observable.choose moveFilter
        // handle these
        |> Observable.subscribe subscriberFn 

    /// Listen for "moved" events and aggregate them to keep
    /// track of the total ink used
    let inkUsedProcessor (eventStream:IObservable<Guid*obj>) =

        // Accumulate the total distance moved so far when a new event happens
        let accumulate distanceSoFar (ev:StateChangedEvent) =
            match ev with
            | Moved dist -> 
                distanceSoFar + dist 
            | _ -> 
                distanceSoFar 

        // the function that handles the input from the observable
        let subscriberFn distanceSoFar  =
            printfn "[ink used]: %0.2f" distanceSoFar  

        // start with all events
        eventStream
        // filter the stream on just TurtleEvents
        |> Observable.choose (function (id,ev) -> turtleFilter ev)
        // filter on just StateChangedEvent
        |> Observable.choose stateChangedEventFilter
        // accumulate total distance
        |> Observable.scan accumulate 0.0
        // handle these
        |> Observable.subscribe subscriberFn 


// using processors
module CommandHandlerClient =
    open CommandHandler

    let eventStore = EventStore()
    let makeCommandHandler =
        let logger = ignore
        let getEvents id =
            eventStore.Get<TurtleEvent>(id)
        let getStateChangedEvents id =
            getEvents id
            |> List.choose (function StateChangedEvent ev -> Some ev | _ -> None)
        let saveEvent id ev =
            eventStore.Save(id, ev)
        commandHandler logger getStateChangedEvents saveEvent

    let turtleId = System.Guid.NewGuid()
    let move dist = { turtleId = turtleId; action = Move dist }
    let turn angle = { turtleId = turtleId; action = Turn angle }
    let penDown = { turtleId = turtleId; action = PenDown }
    let penUp = { turtleId = turtleId ; action = PenUp }
    let setColor c = { turtleId = turtleId; action= SetColor c}
    
    let drawTriangle() =
        eventStore.Clear turtleId

        // create an event stream from an IEvent
        let eventStream = eventStore.SaveEvent :> IObservable<Guid * obj>
        use physicalTutleProcessor = EventProcessors.physicalTutleProcessor eventStream
        use graphicsProcessor = EventProcessors.graphicsProcessor eventStream
        use inkUsedProcessor = EventProcessors.inkUsedProcessor eventStream

        let handler = makeCommandHandler
        handler (move 100.0)
        handler (turn 120.0<Degrees>)
        handler (move 100.0)
        handler (turn 120.0<Degrees>)