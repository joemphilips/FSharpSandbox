module PinetreeCQRS.Persistance.SqlServer
open System
open FSharp.Data.Sql
open PinetreeCQRS.Infrastructure.Types


exception DataAccessException of exn

module private DataAccess =
    [<Literal>]
    let  private conString = "EventStore"
    // type dbSchema = SqlDataProvider< ConnectionStringName="EventStore", UseOptionTypes=true >
    //type dbSchema2 = SqlCommandProvider<"SELECT GETDATE() AS Now", >

    let ctx = ()

    let entityToEvent<'TEvent when 'TEvent :> IEvent> (e): EventEnvelope<'TEvent> =
        failwith ""

    let entityToCommand<'TCommand when 'TCommand :> ICommand> (c): CommandEnvelope<'TCommand> =
        failwith ""

    let processIdToGuid pid =
        failwith ""

    let eventToEntity<'TEvent when 'TEvent :> IEvent> (Category c) (e: EventEnvelope<'TEvent>) =
        failwith ""

    let commandToEntity<'TCommand when 'TCommand :> ICommand>  (QueueName qName) (c: CommandEnvelope<'TCommand>) =
        failwith ""

    let commitEvents category e =
        let entities = List.map (eventToEntity category) e
        failwith ""

    let loadEvents n =
        failwith ""

    let loadTypeEvents (Category c) (fromNumber: int32) =
        failwith ""

    let loadAggregateEvents (c) (fromNumber) (AggregateId aId) =
        failwith ""

    let loadProcessEvents fromNumber toNumber (ProcessId pId) =
        failwith ""

    let queueCommands (c) =
        failwith ""

    let dequeueCommands (QueueName qName) =
        failwith ""

module Events =
    let commitEvents<'TEvent when 'TEvent :> IEvent> category (events: EventEnvelope<'TEvent> list) : RResult<EventEnvelope<'TEvent> list> =
        try
            DataAccess.commitEvents  category events |> RResult.Good
        with
            ex -> RResult.rexn (DataAccessException ex)

    let loadAllEvents n : RResult<EventEnvelope<IEvent> list> =
        try
            DataAccess.loadEvents n
            |> Seq.toList
            |> List.map DataAccess.entityToEvent
            |> Good
        with
            ex -> RResult.rexn (DataAccessException ex)

    let loadTypeEvents c (n: int32) : RResult<EventEnvelope<'TEvent> list> =
        try
            DataAccess.loadTypeEvents c n
            |> Seq.toList
            |> List.map DataAccess.entityToEvent
            |> Good
        with
            ex -> RResult.rexn (DataAccessException ex)

    let loadAggregateEvents<'TEvent when 'TEvent :> IEvent> category number aggregateId : RResult<EventEnvelope<'TEvent>list> =
        try
            DataAccess.loadAggregateEvents category number aggregateId
            |> Seq.toList
            |> List.map DataAccess.entityToEvent
            |> Good
        with ex -> RResult.rexn (DataAccessException ex )

    let loadProcessEvents fromNumber toNumber processId: RResult<EventEnvelope<IEvent> list> =
        try
            DataAccess.loadProcessEvents fromNumber toNumber processId
            |> Seq.toList
            |> List.map DataAccess.entityToEvent
            |> Good
        with
            ex -> RResult.rexn (DataAccessException ex)

module Commands =
    let queryCommands<'TCommand when 'TCommand :> ICommand> (commands: (QueueName * CommandEnvelope<'TCommand>) list) : RResult<CommandEnvelope<'TCommand> list> =
        try
            DataAccess.queueCommands commands |> Good
        with
            ex -> RResult.rexn (DataAccessException ex)

    let dequeueCommands<'TCommand when 'TCommand :> ICommand> (queueName: QueueName): RResult<CommandEnvelope<'TCommand> list> =
        try
            DataAccess.dequeueCommands queueName
            |> List.map DataAccess.entityToCommand
            |> Good
        with
            ex -> RResult.rexn (DataAccessException ex)