﻿module FsLogic.Test.UnificationTest

type Marker = class end

open Fuchu
open Swensen.Unquote
open FsLogic.Goal
open FsLogic.Substitution

#nowarn "25"

let ``unifying two identical variables should succeed and not extend subst``() =
    let a = newVar()
    unify a a Map.empty =! Some Map.empty

let ``unifying two different variables should succeed and extend subst``() =
    let (Var aName) as a = newVar()
    let (Var bName) as b = newVar()

    let expected = [ (aName, b) ] |> Map.ofList
    unify a b Map.empty =! Some expected

    let expected = [ (bName, a) ] |> Map.ofList
    unify b a Map.empty =! Some expected

let internal zero = Atom 0
let internal one = Atom 1

let ``unifying variable with value should succeed and extend subst``() =
    let (Var aName) as a = newVar()

    let expected = [ (aName, zero) ] |> Map.ofList
    unify a zero Map.empty =! Some expected
    unify zero a Map.empty =! Some expected

let ``unifying different values should fail``() =
    unify one zero Map.empty =! None

let ``unifying same values should succeed without extending substitution``() =
    unify zero zero Map.empty =! Some Map.empty

let internal nil = nil<obj>.Uni
let internal cons x xs = (cons { Uni = x } { Uni = xs }).Uni
let internal list2 (x,y) = cons x (cons y nil)

let ``unifying head of list with variable should extend substitution``() =
    let (Var aName) as a = newVar()
    let expected = [ (aName, zero ) ] |> Map.ofList
    unify (cons a nil) (cons zero nil) Map.empty =! Some expected
    unify (cons zero nil) (cons a nil) Map.empty =! Some expected

let ``unifying tail of list with variable should extend substitution``() =
    let (Var aName) as a = newVar()
    let expected = [ (aName, list2 (Atom 1,Atom 2)) ] |> Map.ofList
    unify (cons (Atom 3) a) (cons (Atom 3) (list2 (Atom 1, Atom 2))) Map.empty =! Some expected
    unify (cons (Atom 3) (list2 (Atom 1, Atom 2))) (cons (Atom 3) a) Map.empty =! Some expected

let ``unifying element of tuple with variable should extend substitution``() =
    let (Var aName) as a = newVar()
    let expected = [ (aName, zero) ] |> Map.ofList
    unify (list2 (a,Atom 1)) (list2 (Atom 0, Atom 1)) Map.empty =! Some expected
    unify (list2 (Atom 0, Atom 1)) (list2 (a,Atom 1)) Map.empty =! Some expected


