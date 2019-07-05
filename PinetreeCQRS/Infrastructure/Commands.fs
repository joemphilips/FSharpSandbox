module PinetreeCQRS.Infrastructure.Commands

open System
open PinetreeCQRS.Infrastructure.Events
open PinetreeCQRS.Infrastructure.Types

let (<!>) = RResult.rmap

let createCommand aggregateId (v, causationId, correlationId, processId) payload =
    let commandId = Guid.NewGuid()
    let causationId' =
        match causationId with
        | Some c -> c
        | _ -> CausationId commandId
    let correlationId' =
        match correlationId with
        | Some c -> c
        | _ -> CorrelationId commandId
    {
        AggregateId = aggregateId
        Payload = payload
        CommandId = CommandId commandId
        ProcessId = processId
        CausationId = causationId'
        CorrelationId = correlationId'
        ExpectedVersion = v
    }

let makeCommandHandler<'TState, 'TEvent, 'TCommand when 'TEvent :> IEvent and 'TCommand :> ICommand>
    (aggregate : Aggregate<'TState, 'TCommand, 'TEvent>)
    (load: AggregateId -> RResult<EventEnvelope<'TEvent> list>)
    (commit: EventEnvelope<'TEvent> list -> RResult<EventEnvelope<'TEvent> list>) =
    let handleCommand command : RResult<EventEnvelope<'TEvent> list> =
        let processEvents events = 
            let lastEventNumber = List.fold (fun acc e' -> e'.EventNumber) 0 events
            let e = lastEventNumber

            let v =
                match command.ExpectedVersion with
                | Expected v' -> Some v'
                | Irrelevant -> None
            match e, v with
            | (x, Some(y)) when x > y -> RResult.rmsg("Version mismatch")
            | _ ->
                let eventPayloads = List.map (fun (e: EventEnvelope<'TEvent>) -> e.Payload) events
                let s = List.fold aggregate.ApplyEvent aggregate.Zero eventPayloads
                let result = aggregate.ExecuteCommand s command.Payload
                List.map (fun e -> createEventMetadata e command) <!> result >>= commit
        let id = command.AggregateId
        let loadedEvents = load id
        loadedEvents >>= processEvents
    handleCommand