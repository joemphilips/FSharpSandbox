module AlternativeReaderMonad


/// Reader モナドとは、 state 'S をとって'Tの値を生み出す computaion である。
/// 
/// 通常の reader monad では、 `unit : T -> Reader<S, T>` と bind を持つが、
/// 今回はちょっと違う方法を持ちいる。State は なんrakano Tuple として表され、
/// bind で 結合されるそれぞれの Computaion 内部で使用されるものとする。
type Reader<'S, 'T> = Reader of ('S -> 'T)

/// Unit オペレーションは何も行わない。
let unit v = Reader(fun () -> v)

/// bind オペレーションは、 二つの値を読み出す Reader を作る。
let bind f (Reader(g)) = Reader (fun (a, b) ->
        let v = g a
        let (Reader h) = f v
        h b
    )

