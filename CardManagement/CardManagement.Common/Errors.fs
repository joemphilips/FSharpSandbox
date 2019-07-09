namespace CardManagement.Common

(*
    このモジュールは error handling に関連している。 以下のエラー型を定義する。
    - ValidationError
    - OperationNotAllowedError: ビジネスロジックの関数用
    - 
*)
module Errors =
    open System

    type ValidationError =
        {
            FieldPath: string
            Message: string
        }

    type OperationNotAllowedError =
        {
            Operation: string
            Reason: string
        }

    type DataRelatedError =
        | EntityAlreadyExists of entityName: string * id: string
        | EntityNotFound of entityName: string * id: string
        | EntityIsInUse of entityName: string * id: string
        | UpdateError of entityName: string * id: string * message: string

    type Error =
        | ValidationError of ValidationError
        | OperationNotAllowedError of OperationNotAllowedError
        | DataError of DataRelatedError
        | Bug of exn

    let bug exc = Bug exc |> Error

    let operationNotAllowed operation reason = { Operation = operation; Reason = reason } |> Error

    let notFound name id = EntityNotFound (name, id) |> Error

    let entityInUse name = EntityIsInUse name |> Error


    let expectOperationNotAllowedError result =
        RResult.rbadMap (fun e -> RBadTree.Fork(RBadTree.Leaf(RBad.Object(OperationNotAllowedError)), e)) result

    let expectDataRelatedError result =
        RResult.rbadMap (fun e -> RBadTree.Fork(RBadTree.Leaf(RBad.Object(DataError)), e)) result

    let expectValidationError result =
        RResult.rbadMap (fun e -> RBadTree.Fork(RBadTree.Leaf(RBad.Object(ValidationError)), e)) result
