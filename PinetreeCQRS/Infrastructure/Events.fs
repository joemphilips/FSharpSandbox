module PinetreeCQRS.Infrastructure.Events

open RResult
open System
open Types

let createEvent aggregateid (causationId, processId, correlationId) payload =
    {
        AggregateId = aggregateid
        Payload = payload
        EventId = Guid.NewGuid() |> EventId
        ProcessId = processId
        CausationId = causationId
        CorrelationId = correlationId
        EventNumber = 0
    }

let createEventMetadata payload command = 
    let (CommandId cmdGuid) = command.CommandId
    { AggregateId = command.AggregateId
      Payload = payload
      EventId = Guid.NewGuid() |> EventId
      ProcessId = command.ProcessId
      CausationId = CausationId cmdGuid
      CorrelationId = command.CorrelationId
      EventNumber = 0 }

let makeEventProcessor (processManager: ProcessManager<'TState>)
    (load: ProcessId -> RResult<EventEnvelope<IEvent> list>)
    (enqueue: (QueueName * CommandEnvelope<ICommand>) list -> RResult<CommandEnvelope<ICommand> list> ) =
    let handleEvent event =
        let processEvents events = 
            let state = List.fold processManager.ApplyEvent processManager.Zero events
            let result = processManager.ProcessEvent state event
            result >>= enqueue
        
        match event.ProcessId with
        | Some pid ->
            let loadedEvents = load pid
            loadedEvents >>= processEvents
        | _ -> (List<CommandEnvelope<ICommand>>.Empty) |> Good

    handleEvent