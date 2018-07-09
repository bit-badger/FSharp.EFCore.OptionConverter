/// Module containing the OptionConverter type
module FSharp.EFCore.OptionConverter

open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open Microsoft.FSharp.Linq.RuntimeHelpers
open System
open System.Linq.Expressions

let private fromOption<'T> =
  <@ Func<'T option, 'T>(fun (x : 'T option) -> match x with Some y -> y | None -> Unchecked.defaultof<'T>) @>
  |> LeafExpressionConverter.QuotationToExpression
  |> unbox<Expression<Func<'T option, 'T>>>

let private toOption<'T> =
  <@ Func<'T, 'T option>(fun (x : 'T) -> match box x with null -> None | _ -> Some x) @>
  |> LeafExpressionConverter.QuotationToExpression
  |> unbox<Expression<Func<'T, 'T option>>>
  
/// Conversion between a nullable column and a field/property option
type OptionConverter<'T> () =
  inherit ValueConverter<'T option, 'T> (fromOption, toOption)
