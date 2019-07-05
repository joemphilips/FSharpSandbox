module PinetreeCQRS.Infrastructure.Types

open System
open RResult

type AggregateId = AggregateId of Guid
type EventId = EventId of Guid
type CommandId = CommandId of Guid
type FailureId = FailureId of Guid
type CausationId = CausationId of Guid
type CorrelationId = CorrelationId of Guid
type ProcessId = ProcessId of Guid
type QueueName = QueueName of string
type Category = Category of string
type AggregateVersion =
    | Expected of int
    | Irrelevant
type EventNumber = int
type IEvent = interface end
type ICommand = interface end

type BasketId = 
    | BasketId of Guid
    static member FromAggregateId (AggregateId aggregateId) = BasketId aggregateId 

type ProductId =
    | ProductId of Guid
    static member FromAggregateId (AggregateId aggregateId) = ProductId aggregateId 
type OrderId = 
    | OrderId of Guid
    static member FromAggregateId (AggregateId aggregateId) = OrderId aggregateId 

type ShippingAddress = ShippingAddress of string

type BasketItem = 
    {
        ProductId: ProductId
        ProductName: string
        Price: decimal
        Quantity: int
    }

type OrderLine =
    {
        ProducctId: ProductId
        ProductName: string
        Price: Decimal
        Quantity: int
    }


type EventEnvelope<'TEvent when 'TEvent :> IEvent> =
    {
        AggregateId: AggregateId
        Payload: 'TEvent
        EventId: EventId
        ProcessId: ProcessId option
        CausationId: CausationId
        CorrelationId: CorrelationId
        EventNumber: EventNumber
    }

type CommandEnvelope<'TCommand when 'TCommand :> ICommand>  =
    {
        AggregateId: AggregateId
        Payload: 'TCommand
        CommandId: CommandId
        ProcessId: ProcessId option
        CausationId: CausationId
        CorrelationId: CorrelationId
        ExpectedVersion: AggregateVersion
    }

type Aggregate<'TState, 'TCommand, 'TEvent> =
    {
        Zero: 'TState
        ApplyEvent: 'TState -> 'TEvent -> 'TState
        ExecuteCommand: 'TState -> 'TCommand -> RResult<'TEvent list> 
    }

type ProcessManager<'TState> =
    {
        Zero: 'TState
        ApplyEvent: 'TState -> EventEnvelope<IEvent> -> 'TState
        ProcessEvent: 'TState -> EventEnvelope<IEvent> -> RResult<(QueueName * CommandEnvelope<ICommand>) list>
    }