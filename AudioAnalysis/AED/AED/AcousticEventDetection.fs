﻿module QutSensors.AudioAnalysis.AED.AcousticEventDetection

open GetAcousticEvents
open TowseyLib

// TODO should this return a matrix of int
let toBlackAndWhite t = Math.Matrix.map (fun e -> if e > t then 1.0 else 0.0)
    
let toFillIn (m:matrix) i j t =
    // TODO right & left should really be generalised to work with sequences? instead of matrix
    let rec right n = if j + n >= m.NumCols or n > t - 2 then 0 else if m.[i,j+n] = 1.0 then n else right (n+1)
    let rec left n  = if j - n < 0          or n > t - 2 then 0 else if m.[i,j-n] = 1.0 then n else left (n+1)
    let l, r = left 1, right 1
    l > 0 && r > 0 && l + r <= t

let joinHorizontalLines m = Math.Matrix.mapi (fun i j x -> if x = 1.0 or (toFillIn m i j 3) then 1.0 else 0.0) m
    
let joinVerticalLines = Math.Matrix.transpose << joinHorizontalLines << Math.Matrix.transpose

let detectEventsMatlab m =
    let i1 = Math.Matrix.to_array2 m
    let i2 = Wiener.wiener2 i1 5 |> Math.Matrix.of_array2 
    let i3 = SubbandMode.removeSubbandModeIntensities2 i2
    let i4 = toBlackAndWhite 9.0 i3
    let i6 = joinVerticalLines i4 |> joinHorizontalLines
    let ae = GetAcousticEvents.getAcousticEvents i6
    ae
    
let detectEvents a =
    Math.Matrix.of_array2 a |> Math.Matrix.transpose |> detectEventsMatlab
                            |> List.map (fun r -> new Oblong(r.Left, r.Top, right r, bottom r)) // transpose results back