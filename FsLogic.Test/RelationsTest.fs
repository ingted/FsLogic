﻿
module FsLogic.Test.RelationsTest

type Marker = class end

open FsLogic
open FsLogic.Substitution
open FsLogic.Goal
open FsLogic.Relations
open Swensen.Unquote

let ``should unify with int``() =
    let res = run -1 (fun q ->  q *=* 1Z)
    res =! [ Det 1 ]

let ``should unify with var unified with int``() =
    let goal q =
        let x = fresh()
        x *=* 1Z &&& q *=* x
    let res = run -1 goal
    res =! [ Det 1 ]

let ``should unify with var unified with int 2``() =
    let res =
        run -1 (fun q ->
            let y = fresh()
            y *=* q &&& 3Z *=* y)
    res =! [ Det 3 ]

let ``should unify list of vars``() =
    let res =
        run -1 (fun q ->
            let (x,y,z) = fresh()
            q *=* ofList [x; y; z; x]
            ||| q *=* ofList [z; y; x; z])
    let expected = Half [ Free -3; Free -1; Free -2; Free -3 ]
    res =! [ expected; expected ]

let ``should unify list of vars (2)``() =
    let res =
        run -1 (fun q ->
            let x,y = fresh()
            ofList [x; y] *=* q
            ||| ofList [y; y] *=* q)
    res.Length =! 2
    let expected0 = Half [Free  0; Free -1]
    let expected1 = Half [Free -1; Free -1]
    res =! [ expected0; expected1 ]

let ``disequality constraint``() =
    let res = run -1 (fun q ->
        all [ q *=* 5Z
              q *<>* 5Z ])
    res.Length =! 0

let ``disequality constraint 2``() =
    let res = run -1 (fun q ->
        let x = fresh()
        all [ q *=* x
              q *<>* 6Z ])
    res.Length =! 1

let infinite() =
    let res = run 7 (fun q ->
                let rec loop() =
                    conde [ [ ~~false *=* q ]
                            [ q *=* ~~true  ]
                            [ recurse loop  ]
                        ]
                loop())
    res =! ([ false; true; false; true; false; true; false] |> List.map (box >> Det))

let anyoTest() =
    let res = run 5 (fun q -> anyo (~~false *=* q) ||| ~~true *=* q)
    res =! ([true; false; false; false; false] |> List.map (box >> Det))

let anyoTest2() =
    let res = run 5 (fun q ->
        anyo (1Z *=* q
              ||| 2Z *=* q
              ||| 3Z *=* q))
    res =! ([1; 3; 1; 2; 3] |> List.map (box >> Det))

let alwaysoTest() =
    let res = run 5 (fun x ->
        (~~true *=* x ||| ~~false *=* x)
        &&& alwayso
        &&& ~~false *=* x)
    res =! ([false; false; false; false; false] |> List.map (box >> Det))

let neveroTest() =
    let res = run 3 (fun q -> //more than 3 will diverge...
        1Z *=* q
        ||| nevero
        ||| 2Z *=* q
        ||| nevero
        ||| 3Z *=* q)
    res =! ([1; 3; 2] |> List.map (box >> Det))

let ``conso finds correct head``() =
    let res = run -1 (fun q ->
        conso q ~~[1Z; 2Z; 3Z] ~~[0Z; 1Z; 2Z; 3Z]
    )
    res =! [ Det 0 ]

let ``conso finds correct tail``() =
    let res = run -1 (fun q ->
        conso 0Z q ~~[0Z;1Z;2Z;3Z]
    )
    res =! [ Det [1;2;3] ]

let ``conso finds correct tail if it is empty list``() =
    let res = run -1 (fun q ->
        conso 0Z q (cons 0Z nil)
    )
    res =! [ Det List.empty<int> ]

let ``conso finds correct result``() =
    let res = run -1 (fun q ->
        conso 0Z ~~[1Z;2Z;3Z] q
    )
    res =! [ Det [0;1;2;3] ]

let ``conso finds correct combination of head and tail``() =
    let res = run -1 (fun q ->
        let h,t = fresh()
        conso h t ~~[1Z;2Z;3Z]
        &&& ~~(h,t) *=* q
    )
    res =! [ Det (1,[2;3]) ]

let ``appendo finds correct prefix``() =
    let res = run -1 (fun q -> appendo q ~~[5Z; 4Z] ~~[2Z; 3Z; 5Z; 4Z])
    res =! [ Det [2; 3] ]

let ``appendo finds correct postfix``() =
    let res = run -1 (fun q -> appendo ~~[3Z; 5Z] q ~~[3Z; 5Z; 4Z; 3Z])
    res =! [ Det [4; 3] ]

let ``appendo finds empty postfix``() =
    let res = run -1 (fun q -> appendo ~~[3Z; 5Z] q ~~[3Z; 5Z])
    res =! [ Det List.empty<int> ] //can't use [] because then won't compare equals, type will be 'a list not int list.

let ``appendo finds correct number of prefix and postfix combinations``() =
    let res = run -1 (fun q ->
        let l,s = fresh()
        appendo l s ~~[1Z; 2Z; 3Z]
        &&& ~~(l, s) *=* q)
    res =! ([ [], [1;2;3]
              [1], [2;3]
              [1;2], [3]
              [1;2;3], []
            ] |> List.map (box >> Det))

let ``removeo removes first occurence of elements from list``() =
    let res = run -1 (fun q -> removeo 2Z ~~[1;2;3;4] q)
    res =! [ Det [1;3;4] ]

let ``removeo removes element from singleton list``() =
    let res = run -1 (fun q -> removeo 2Z ~~[2] q)
    res =! [ Det List.empty<int> ]

let projectTest() =
    let res = run -1 (fun q ->
        let x = fresh()
        5Z *=* x
        &&& (project x (fun xv -> let prod = xv * xv in ~~prod *=* q)))
    [ Det 25 ] =! res

let copyTermTest() =
    let g = run -1 (fun q ->
        let w,x,y,z = fresh()
        ~~(~~"a", x, 5Z, y, x) *=* w
        &&& copyTerm w z
        &&&  ~~(w, z) *=* q)
    g =! [Half
             [Half [Det "a"; Free -2; Det 5; Free -1; Free -2];
              Half [Det "a"; Free -5; Det 5; Free -4; Free -5]]]

let ``conda commits to the first clause if its head succeeds``() =
    let res = run -1 (fun q ->
        conda [ [ ~~"olive" *=* q ]
                [ ~~"oil" *=* q ]
        ])
    res =! [Det "olive"]

let ``conda fails if a subsequent clause fails``() =
    let res = run -1 (fun q ->
        conda [ [ ~~"virgin" *=* q; fail ]
                [ ~~"olive" *=* q ]
                [ ~~"oil" *=* q ]
        ])
    res =! []

let ``conde succeeds each goal at most once``() =
    let res = run -1 (fun q ->
        condu [ [ fail ]
                [ alwayso ]
              ]
        &&& ~~true *=* q)
    res =! [Det true]

let ``onceo succeeds the goal at most once``() =
    let res = run -1 (fun _ -> onceo alwayso)
    res.Length =! 1
