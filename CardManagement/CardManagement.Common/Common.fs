namespace CardManagement.Common

[<AutoOpen>]
module Common =
    let inline (|HasLength|) x =
        fun () -> (^a: (member Length: int) x) 
    let inline (|HasCount|) x =
        fun () -> (^a: (member Length: int) x)

    let inline length (HasLength f) = f ()

    let inline isNullOrEmpty arg =
        isNull arg || (length arg) = 0 

    let bindAsync f a =
        async {
            let! a = a
            return! f a
        }