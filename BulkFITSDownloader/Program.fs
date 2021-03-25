namespace BulkFITSDownloader

open System.Text


module Program =
    open System.IO
    open CommandLine
    open FITSDownloader

    let good = StringBuilder()
    let bad = StringBuilder()

    let downloadAndLogEachItem saveTo survey i (item:string) =
        let fromLocation = item.Split(',') |> Seq.last

        try
            match downloadFitsFileFrom fromLocation saveTo survey i with
            | Result.Ok _ ->
                printfn " - Downloaded %s" fromLocation 
                good.AppendLine(fromLocation) |> ignore
            | Result.Error err -> bad.AppendLine(sprintf "%s+%s" fromLocation err) |> ignore
        with e -> bad.AppendLine (sprintf "Unexpected error: %s while processing %s" e.Message fromLocation) |> ignore    

    [<EntryPoint>]
    let main argv =
        try
            let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

            let survey = results.GetResult(Survey)
            let saveTo = results.GetResult(SaveTo)
            let inputFile = results.GetResult(InputFile)
            let data = File.ReadAllLines  inputFile |> Seq.skip 1 |> Seq.take 1
            let badLog = Path.Combine(Path.GetDirectoryName (inputFile), "bad.log")
            let goodLog = Path.Combine(Path.GetDirectoryName(inputFile), "good.log")
            printfn "Will save a log of errors to: %s" badLog
            printfn "Will save a log for successful downloads to: %s" goodLog
            
            printfn "Bulk Download Started..."
            data |> Seq.iteri (downloadAndLogEachItem saveTo survey)
            printfn "Bulk Download Completed."

            File.AppendAllText(badLog, bad.ToString())            
            File.AppendAllText(goodLog, good.ToString())
            
        with e ->
            printfn "%s" e.Message
        0
