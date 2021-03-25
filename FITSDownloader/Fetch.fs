namespace FITSDownloader

open System.IO
open System.Net
open System.Text
open FSharp.Data

[<AutoOpen>]
module Fetch =
    
    type Downloader = {
        get : string*string->unit
    }
    
    let webClientDownloader =
        { get =
            let wc = new WebClient() // extract to parameter maybe, will help testing
            wc.Encoding <- Encoding.ASCII
            wc.Headers.Set("Content-Type", "image/fits")
            wc.DownloadFile }
    
    let fitsBaseAddress = "https://dr12.sdss.org"
    
    let getOrCreateDownloadDirectory saveTo =
        let downloadDestination = Path.Combine ( (Path.GetFullPath saveTo), "downloads")
        if not <| Directory.Exists(downloadDestination )
        then
            printfn "Creating %s" downloadDestination
            Directory.CreateDirectory(downloadDestination).Refresh()
        downloadDestination

    let buildDownloadFileName (fromLocation:string) survey seed saveTo downloadDestination =
        // TODO: convert this to configuration     
        let defaultFileNameRaw = sprintf "%s_%s_%s" survey (seed.ToString().PadLeft(4, '0')) <| (fromLocation.Split('/') |> Seq.rev |> Seq.head)
        let defaultFileName = defaultFileNameRaw.Replace(".aspx?sid=", "")
        Path.Combine(downloadDestination, defaultFileName)
        
    let download downloader from downloadTo  =
        try
            downloader.get( from, downloadTo )
            Result.Ok "FITS File Successfully Downloaded." // Big assumption that success happened just because the IO op didn't throw an exn
        with e ->
            printfn "inner: %s" e.InnerException.Message
            Result.Error e.Message
        
    let private downloadFitsFile downloader fitsLocation saveTo survey seed =
        match tryGetHref fitsLocation with
        | Some fromHref ->
            let fromLocation = fromHref.Value()
            // EXAMPLE: https://dr12.sdss.org/sas/dr12/boss/spectro/redux/v5_7_0/spectra/3655/spec-3655-55240-0040.fits

            
            let downloadDestination = getOrCreateDownloadDirectory saveTo
            let downloadTo = buildDownloadFileName fromLocation survey seed saveTo downloadDestination
        
            let from = fitsBaseAddress + fromLocation
            
            download downloader from downloadTo
        | None -> Result.Error "Couldn't download the FITS file"


    let loadInteractiveSpectrum downloader (href:HtmlAttribute) saveTo survey seed =
        match doc(href.Value()) |> getHtml with
        | Some html ->
            match fitsDownloadLink html with
            | Result.Ok fitsLocation -> downloadFitsFile downloader fitsLocation saveTo survey seed
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the quickview html document."
            
    let downloadFitsFileFrom downloader fromLocation saveTo survey seed =
        match doc fromLocation |> getHtml with
        | Some html ->
            match interactiveSpectrumLink html with
            | Result.Ok interactiveSpectrumLocation ->
                let hrefOpt = tryGetHref interactiveSpectrumLocation
                match hrefOpt with
                | Some href -> loadInteractiveSpectrum downloader href saveTo survey seed
                | None -> Result.Error "Couldn't locate the FITS file hyperlink."
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the main html document."
                