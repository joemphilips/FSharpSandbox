module MonadicTrampoline

type TrampValue<'T> =
    | DelayValue of Delay<'T>
    | ReturnValue of Return<'T>
    | BindValue of IBind<'T>
and ITramp<'T> =
    abstract member Value : TrampValue<'T>
    abstract member Run : unit -> 'T

and Delay<'T>(f: unit -> ITramp<'T>) =
    member self.Func = f
    interface ITramp<'T> with
        member self.Value = DelayValue self
        member self.Run () = (f ()).Run()

and Return<'T> (x: 'T) =
    member self.Value = x
    interface ITramp<'T> with
        member self.Value = ReturnValue self
        member self.Run() = x

and IBind<'T> =
    abstract Bind<'R> : ('T -> ITramp<'R>) -> ITramp<'R> 

and Bind<'T, 'R>(tramp: ITramp<'T>, f : ('T -> ITramp<'R>)) =
    interface IBind<'R> with
        member self.Bind<'K>(f' : 'R -> ITramp<'K>): ITramp<'K> =
            new Bind<'T, 'K>(tramp, fun t -> new Bind<'R, 'K>(f t, (fun r -> f' r)) :> _) :> _

    interface ITramp<'R> with
        member self.Value = BindValue self
        member self.Run() =
            match tramp.Value with
            | BindValue b -> b.Bind(f).Run()
            | ReturnValue r -> (f r.Value).Run()
            | DelayValue d -> (new Bind<'T, 'R>(d.Func(), f) :> ITramp<'R>).Run()
