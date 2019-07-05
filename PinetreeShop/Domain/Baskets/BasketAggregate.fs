module PinetreeShop.Domain.Baskets.BasketAggregate

open PinetreeCQRS.Infrastructure.Types
open PinetreeCQRS.Infrastructure.Commands
open PinetreeCQRS.Infrastructure.Events
open System


let basketCategory = Category "Basket"
let basketQueueNname = QueueName "Basket"

type BasketError =
    | ValidationError of string

exception BasketException of BasketError

type BasketState =
    | NotCreated
    | Pending
    | Canceled
    | CheckedOut

type Command =
    | Create
    | AddItem of BasketItem
    | RemoveItem of ProductId * int
    | Cancel
    | CheckOut of ShippingAddress
    with interface ICommand

type Event =
    | BasketCreated
    | BasketItemAdded of BasketItem
    | BasketItemRemoved of ProductId * int
    | BasketCanceled
    | BasketCheckOut of ShippingAddress * BasketItem list
    with interface IEvent

type State = {
    BasketState : BasketState
    Items: Map<ProductId, BasketItem>
}
    with
    static member Zero =
        {
            BasketState = NotCreated
            Items = Map.empty
        }

module private Handlers =
    let addItem currentItems (item: BasketItem) : Map<ProductId, BasketItem> = 
        let currentItem = Map.tryFind (item.ProductId) currentItems
        let newItem =
            match currentItem with
            | None -> item
            | Some i -> { i with Quantity = i.Quantity + item.Quantity }

        Map.add newItem.ProductId newItem currentItems

    let removeItem currentItems productId quantity : Map<ProductId, BasketItem> =
        let currentItem = Map.tryFind productId currentItems
        match currentItem with
        | None -> currentItems
        | Some i ->
            if (i.Quantity - quantity) <= 0 then
                Map.remove productId currentItems
            else
                Map.add productId { i with Quantity = i.Quantity - quantity } currentItems

    let applyEvent state event =
        match event with
        | BasketCreated -> { state with BasketState = Pending }
        | BasketItemAdded i -> { state with Items = addItem state.Items i }
        | BasketItemRemoved (productId, qty) -> { state with Items = removeItem state.Items productId qty }
        | BasketCanceled -> { state with BasketState = Canceled }
        | BasketCheckOut _ -> { state with BasketState = CheckedOut }

    module private Validate =
        let inCase predicate e value =
            let result = predicate value
            match result with
            | false -> Good value
            | true -> RResult.rexn (BasketException (ValidationError e))

        module private Helpers =
            let notEmptyShippingAddress (ShippingAddress sa) =
                inCase (fun sa -> String.IsNullOrWhiteSpace(sa)) "Shipping Address Cannot be empty" sa

            let isBasketState s os =
                inCase (fun s -> s.BasketState <> os) (sprintf "Wronng basket State %A" s.BasketState) s

            let notBasketState s os =
                inCase (fun s -> s.BasketState = os) (sprintf "Wronng basket State %A" s.BasketState) s

            let hasItems ol = inCase (fun ol -> ol = Map.empty) "No items" ol
            let canCreate s = isBasketState s NotCreated
            let created s = notBasketState s NotCreated

        let canCreate s = Helpers.canCreate s
        let canCheckOut s sa = Helpers.isBasketState s Pending <* Helpers.hasItems s.Items <* Helpers.notEmptyShippingAddress sa
        let canCancel s = Helpers.isBasketState s Pending
        let canAddItem s = Helpers.isBasketState s Pending
        let canRemoveItem s = Helpers.isBasketState s Pending

    let executeCommand (state: State) command: RResult<Event list> =
        match command with
        | Create -> Validate.canCreate state *> Good [ BasketCreated ]
        | AddItem i -> Validate.canAddItem state *> Good [BasketItemAdded i]
        | RemoveItem (productId, qty) ->
            let removeItems =
                let item = Map.tryFind productId state.Items
                match item with
                | Some i ->
                    if (i.Quantity <= qty) then
                        [BasketItemRemoved(productId, i.Quantity)]
                    else
                        [BasketItemRemoved(productId, qty)]
                | None -> []
            Validate.canRemoveItem state *> Good removeItems
        | Cancel -> Validate.canCancel state *> Good [BasketCanceled]
        | CheckOut sa ->
            let items = Map.toList state.Items |> List.map( fun (k, v) -> v)
            Validate.canCheckOut state sa *> Good [BasketCheckOut (sa, items)]

let makeBasketCommandHandler =
    makeCommandHandler { Zero = State.Zero; ApplyEvent = Handlers.applyEvent; ExecuteCommand = Handlers.executeCommand}

let loadBasket e = Seq.fold Handlers.applyEvent State.Zero e