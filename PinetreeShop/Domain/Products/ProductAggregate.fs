module PinetreeShop.Domain.Products.ProductAggregate

open PinetreeCQRS.Infrastructure.Types
open PinetreeCQRS.Infrastructure.Commands
open PinetreeCQRS.Infrastructure.Events
open System.ComponentModel.DataAnnotations

let productCategory = Category "Product"
let productQueueName = QueueName "Product"

exception ProductException of string

type Command =
    | Create of name: string * price: decimal
    | AddToStock of int
    | RemoveFromStock of int
    | Reserve of int
    | CancelReservation of int
    | PurchaseResurved of int
    interface ICommand

type Event =
    | ProductCreated of name: string * price: decimal
    | ProductQuantityChanged of int
    | ProductReserved of int
    | ProductReservationCanceled of int
    | ProductReservationFailed of int
    | ProductPurchased of int
    interface IEvent

module private Handlers =
    type State =
        {
            Created: bool
            Quantity: int
            Reserved: int
        }
            static member Zero =
                {
                    Created = false
                    Quantity = 0
                    Reserved = 0
                }

    let applyEvent s e =
        match e with
        | ProductCreated(name, price) -> { s with Created = true}
        | ProductQuantityChanged diff -> { s with Quantity = s.Quantity + diff }
        | ProductReserved qty -> { s with Reserved = s.Reserved + qty}
        | ProductReservationCanceled qty -> { s with Reserved = s.Reserved - qty}
        | ProductReservationFailed _ -> s
        | ProductPurchased qty ->
            {
                s with
                    Reserved = s.Reserved - qty
                    Quantity = s.Quantity - qty
            }

    module private Validate =
        let inCase predicate e v =
            let res = predicate v
            match res with
            | false -> Good v
            | true -> RResult.rexn (ProductException e)

        module private Helpers =
            let available s = s.Quantity - s.Reserved
            let notCreated = inCase (fun s' -> s'.Created) "Product already created"
            let positiveQuantity = inCase (fun q' -> q' <= 0) "Quantity must be a positive number"
            let positivePrice = inCase(fun p' -> p' < 0m) "Price must be a positive number"
            let canChangeQuantity = inCase (fun (s', d') -> not (d' < 0 && available s' >= -d' || d' >= 0)) "Not enough available items"
            let enoughReservedItems = inCase (fun (s', q') -> s'.Reserved < q') "Not enough reserved items"
            let created = inCase(fun s' -> not s'.Created) "Product must be created"

        let canCreate (s, p) = Helpers.notCreated s <* Helpers.positivePrice p
        let createdAndPositiveQuantity (s, q) = Helpers.created s <* Helpers.positiveQuantity q
        let createdAndEnoughReservedItems (s, q) = Helpers.created s <* Helpers.positiveQuantity q <* Helpers.enoughReservedItems (s, q)
        let createdAndCanRemoveItems (s, q) = Helpers.created s <* Helpers.positiveQuantity q <* Helpers.canChangeQuantity (s, -q)

    let executeCommand (s: State) command =
        match command with
        | Create(name, price) ->  Validate.canCreate (s, price) *> Good [ ProductCreated(name, price) ]
        | AddToStock qty -> Validate.createdAndPositiveQuantity (s, qty) *> Good [ ProductQuantityChanged qty ]
        | RemoveFromStock qty -> Validate.createdAndCanRemoveItems(s, qty) *> Good [ProductQuantityChanged(-qty)]
        | CancelReservation qty -> Validate.createdAndEnoughReservedItems(s, qty) *> Good [ProductReservationCanceled qty]
        | PurchaseResurved qty -> Validate.createdAndEnoughReservedItems (s, qty) *> Good [ProductPurchased (qty)]
        | Reserve qty ->
            let r = Validate.createdAndCanRemoveItems (s, qty)
            match r with
            | Good (s) -> Good [ProductReserved (qty)]
            | Bad f -> Good [ProductReservationFailed (qty)]

let makeProductCommandHandler =
    makeCommandHandler {
        Zero = Handlers.State.Zero
        ApplyEvent = Handlers.applyEvent
        ExecuteCommand = Handlers.executeCommand
    }