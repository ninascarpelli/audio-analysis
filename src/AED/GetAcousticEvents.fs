module Acoustics.AED.GetAcousticEvents

open Util
open Microsoft.FSharp.Math
open Microsoft.FSharp

/// Spiders from an anchor. Returns all points that are connected.
/// xs: anchors to check (recursive function multiplies with this list)
/// WARNING, DANGER! m is mutated
let rec spider (m:matrix) xs (v:(int * int) Set) =
    match xs with
    | []    -> v
    | p::ps ->  let (i,j) = p
                let (v', ps') = 
                    if j < 0 || j >= m.NumCols || i < 0 || i >= m.NumRows || m.[i,j] = 0.0 then 
                        (v, ps)
                    else 
                        m.[i,j] <- 0.0
                        (Set.add p v, [(i-1,j-1);(i-1,j);(i-1,j+1);(i,j-1);(i,j+1);(i+1,j-1);(i+1,j);(i+1,j+1)] @ ps) 
                spider m ps' v'
    
type AcousticEvent = {Bounds: Rectangle<int, int>; Elements:(int * int) Set}
let getBounds ae = ae.Bounds
let bounds aes = Seq.map (fun ae -> ae.Bounds) aes

let elementsToAcousticEvent xs =
        let (rs, cs) = List.unzip (Set.toList xs) 
        let l,t = List.min cs, List.min rs
        {Bounds=lengthsToRect l t (List.max cs - l + 1) (List.max rs - t + 1); Elements=xs} 

let getAcousticEvents m =
    let m' = Math.Matrix.copy m
    let f i j a x = if x = 0.0 then a else (elementsToAcousticEvent(spider m' [(i,j)] Set.empty))::a
    Math.Matrix.foldi f [] m'