module PinetreeShop.Domain.Products.CommandHandler

open PinetreeCQRS.Infrastructure.Types
open PinetreeCQRS.Infrastructure.Events
open System

module Persistance = PinetreeCQRS.Persistance.SqlServer
module Product = PinetreeShop.Domain.Products.ProductAggregate


module private Helpers =
    let load = Persistance.Events.loadAggregateEvents Product.productCategory 0
    let commit = Persistance.Events.commitEvents Product.productCategory
    let dequeue () = Persistance.Commands.dequeueCommands Product.productQueueName

let handler = Product.makeProductCommandHandler Helpers.load Helpers.commit
let processCommandQueue2() = Helpers.dequeue() >>= (fun c -> c |> List.map handler |> List.sequenceRResultA)