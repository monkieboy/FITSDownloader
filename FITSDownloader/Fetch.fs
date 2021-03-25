namespace FITSDownloader



[<AutoOpen>]
module Fetch =

    open System.IO
    open FSharp.Data
    open HtmlAgilityPack.FSharp
    open System
    open System.Net
    open System.Text
    

    type IDownloader =
        abstract member Get : string->string->unit
    
    type WebClientDownloader() =
        interface IDownloader with
            member this.Get x y =
                    let wc = new WebClient()
                    wc.Encoding <- Encoding.ASCII
                    wc.Headers.Set("Content-Type", "image/fits")
                    wc.DownloadFile(x,y) 
    
    let fitsBaseAddress = "https://dr12.sdss.org" // TODO: obtain from a configuration
    
    let findRedShift fromLocation =
        let uri = fromLocation |> Uri
        let doc = uri |> loadDoc

        let descendantTds = doc |> descendants "td"
        let redshiftTd = descendantTds |> Seq.find (fun ele -> ele |> innerText = "Redshift (z):")
        let nextTds = redshiftTd.FollowingSiblings "td"
        let redshiftValueTd = nextTds |> Seq.exactlyOne
        let roundedString = Math.Round(redshiftValueTd |> innerText |> float, 4) |> string
        roundedString.Replace('.','-')
        
    let getBestObjId (fromLocation:string) =
        fromLocation.Split('=') |> Seq.last

    let getOrCreateDownloadDirectory saveTo =
        let downloadDestination = Path.Combine ( (Path.GetFullPath saveTo), "downloads")
        if not <| Directory.Exists(downloadDestination )
        then
            printfn "Creating %s" downloadDestination
            Directory.CreateDirectory(downloadDestination).Refresh()
        downloadDestination

    let buildDownloadFileName (fromLocation:string) survey seed saveTo redshift bestObjId =
        // TODO: convert this to configuration
        let defaultFileNameRaw = sprintf "%s_%s_%s_%s_%s" survey (seed.ToString().PadLeft(4, '0')) redshift bestObjId <| (fromLocation.Split('/') |> Seq.rev |> Seq.head)
        let defaultFileName = defaultFileNameRaw.Replace(".aspx?sid=", "")
        Path.Combine(getOrCreateDownloadDirectory saveTo, defaultFileName)
        
    let download (downloader:IDownloader) from downloadTo  =
        try
            downloader.Get from downloadTo
            Result.Ok "FITS File Successfully Downloaded." // Big assumption that success happened just because the IO op didn't throw an exn
        with e ->
            printfn "inner: %s" e.InnerException.Message
            Result.Error e.Message
        
    let private downloadFitsFile downloader fitsLocation saveTo survey seed redshift bestObjId =
        match tryGetHref fitsLocation with
        | Some fromHref ->
            // EXAMPLE: https://dr12.sdss.org/sas/dr12/boss/spectro/redux/v5_7_0/spectra/3655/spec-3655-55240-0040.fits
            let fromLocation = fromHref.Value()

            let downloadTo = buildDownloadFileName fromLocation survey seed saveTo redshift bestObjId
        
            let from = fitsBaseAddress + fromLocation
            
            download downloader from downloadTo
        | None -> Result.Error "Couldn't download the FITS file"


    let loadInteractiveSpectrum downloader (href:HtmlAttribute) saveTo survey seed redshift bestObjId =
        match getHtmlDocument(href.Value()) |> getHtml with
        | Some html ->
            match fitsDownloadLink html with
            | Result.Ok fitsLocation -> downloadFitsFile downloader fitsLocation saveTo survey seed redshift bestObjId
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the quickview html document."
            
    let downloadFitsFileFrom downloader fromLocation saveTo survey seed =
        match getHtmlDocument fromLocation |> getHtml with
        | Some html ->
            match interactiveSpectrumLink html with
            | Result.Ok interactiveSpectrumLocation ->
                match tryGetHref interactiveSpectrumLocation with
                | Some href ->
                    let redshift = findRedShift fromLocation
                    let bestObjId = getBestObjId fromLocation
                    loadInteractiveSpectrum downloader href saveTo survey seed redshift bestObjId
                | None -> Result.Error "Couldn't locate the FITS file hyperlink."
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the main html document."
                