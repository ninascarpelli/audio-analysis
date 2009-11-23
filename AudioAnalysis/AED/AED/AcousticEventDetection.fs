﻿module QutSensors.AudioAnalysis.AED.AcousticEventDetection

open GetAcousticEvents
open Matlab
open TowseyLib
open Util

// TODO should this return a matrix of int
let toBlackAndWhite t = Math.Matrix.map (fun e -> if e > t then 1.0 else 0.0)
    
let toFillIn (m:matrix) i j t =
    // TODO right & left should really be generalised to work with sequences? instead of matrix
    let rec right n = if j + n >= m.NumCols || n > t - 2 then 0 else if m.[i,j+n] = 1.0 then n else right (n+1)
    let rec left n  = if j - n < 0          || n > t - 2 then 0 else if m.[i,j-n] = 1.0 then n else left (n+1)
    let l, r = left 1, right 1
    l > 0 && r > 0 && l + r <= t

let joinHorizontalLines m = Math.Matrix.mapi (fun i j x -> if x = 1.0 || (toFillIn m i j 3) then 1.0 else 0.0) m
    
let joinVerticalLines = Math.Matrix.transpose << joinHorizontalLines << Math.Matrix.transpose
    
let aeToMatrix ae =
    let r = ae.Bounds
    Math.Matrix.init r.Height r.Width (fun i j -> if ae.Elements.Contains (r.Top + i, r.Left + j) then 1.0 else 0.0)

let separateLargeEvents aes =
    let areat = 3000
    let f ae =
        let freqt = 20.0
        let m = aeToMatrix ae
        let s = sumRows m |> Seq.map (fun x -> x / (float) m.NumCols * 100.0 <= freqt) 
        let m1 = Math.Matrix.mapi (fun i _ x -> if Seq.nth i s then 0.0 else x) m
        let rs = getAcousticEvents m1
                 |> List.map (fun x -> let b1, b2 = ae.Bounds, x.Bounds in {Left=b1.Left+b2.Left; Top=b1.Top+b2.Top; Width=b2.Width; Height=b2.Height})
        let timet = 100.0 / 3.0
        let m2 = m - m1
        rs @ (getAcousticEvents m2
              |> List.filter (fun x -> (float) x.Bounds.Height * 100.0 / (float) m2.NumRows >= timet)
              |> List.map (fun x -> let b1, b2 = ae.Bounds, x.Bounds in {Left=b1.Left+b2.Left; Top=b1.Top; Width=b2.Width; Height=b1.Height}))
    Seq.collect (fun ae -> if area ae.Bounds < areat then [ae.Bounds] else f ae) aes

let smallFirstMin cs h t =
    let s = Seq.pairwise h |> Seq.map (fun (x,y) -> x-y) |> Seq.zip cs
    let tf g = Seq.tryFind (fun (_,x) -> g x) s
    tf ((>) 0) |? lazy tf ((=) 0) |> Option.map fst |?| t

let smallThreshold t rs =
    let (%%) x y = (float x) * y |> rnd |> (int)
    let cs = seq {for i in 0..9 -> (i * (t %% 0.1)) + (t %% 0.05)}
    let as' = areas rs |> Seq.filter (fun x -> x <= t)
    smallFirstMin cs (hist as' cs) t

let filterOutSmallEvents t rs =
    let t' = smallThreshold t rs
    Seq.filter (fun r -> area r > t') rs

let detectEventsMatlab intensityThreshold smallAreaThreshold m =
    //let start = System.DateTime.Now.TimeOfDay
    let m1 = Matlab.wiener2 5 m 
    //printfn "wiener2: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m2 = SubbandMode.removeSubbandModeIntensities2 m1
    //printfn "removeSubbandModeIntensities2: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m3 = toBlackAndWhite intensityThreshold m2
    //printfn "toBlackAndWhite: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m4 = joinVerticalLines m3
    //printfn "joinVerticalLines: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m5 = joinHorizontalLines m4
    //printfn "joinHorizontalLines: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m6 = getAcousticEvents m5
    //printfn "getAcousticEvents: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m7 = separateLargeEvents m6
    //printfn "separateLargeEvents: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    let m8 = filterOutSmallEvents smallAreaThreshold m7
    //printfn "filterOutSmallEvents: %A" (System.DateTime.Now.TimeOfDay.Subtract(start))
    m8
    
// TODO it would be nicer if this returned an Option/Either rather than an exception
let detectEvents intensityThreshold smallAreaThreshold a =
    let m = Math.Matrix.ofArray2D a |> Math.Matrix.transpose
    if m.NumRows = 257 then
        let m' = m.Region (1, 0, 256, m.NumCols) // remove first row (DC values) like in matlab
        detectEventsMatlab intensityThreshold smallAreaThreshold m'
            |> Seq.map (fun r -> new Oblong(r.Left, r.Top+1, right r, bottom r + 1)) // transpose results back & compensate for removing first row
        else failwith (sprintf "Expecting matrix with 257 frequency cols, but got %d" m.NumRows)