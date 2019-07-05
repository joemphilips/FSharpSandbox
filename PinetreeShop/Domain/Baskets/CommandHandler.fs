module PinetreeShop.Domain.Baskets.CommandHandler

open PinetreeCQRS.Infrastructure.Types
open PinetreeCQRS.Infrastructure.Events

module Persistance = PinetreeCQRS.Persistance.SqlServer
module Basket = PinetreeShop.Domain.Baskets.BasketAggregate

module private Helpers =
    let load = Persistance.Events.loadAggregateEvents Basket.basketCategory 0
    let commit = Persistance.Events.commitEvents Basket.basketCategory
    let dequeue () = Persistance.Commands.dequeueCommands Basket.basketQueueNname

let handler = Basket.makeBasketCommandHandler Helpers.load Helpers.commit
let processCommandQueue() = Helpers.dequeue() >>= (fun c -> c  |> Seq.toList |> List.map handler |> List.sequenceRResultA)