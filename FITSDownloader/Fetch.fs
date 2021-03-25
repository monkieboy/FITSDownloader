namespace FITSDownloader

open System.IO
open System.Net
open System.Text
open FSharp.Data

[<AutoOpen>]
module Fetch =
    let fitsBaseAddress = "https://dr12.sdss.org"

    type FindBy =
        | InnerText of string
        | Containing of string
        
    let doc href : HtmlDocument =
        let doc = HtmlDocument.Load(href, Encoding.UTF8)
        doc

    let findAnchors findBy (node:HtmlNode) =
        let htmlNodes = node.Descendants["a"]
        
        let htmlNodesFunc = 
            match findBy with
            | InnerText t -> Seq.tryFind(fun (d:HtmlNode) -> d.InnerText().Trim() = t)
            | Containing t -> Seq.tryFind(fun (d:HtmlNode) -> d.InnerText().Contains(t) )
        
        match htmlNodesFunc htmlNodes with
        | None -> Result.Error "Couldn't find the anchor."
        | Some element -> Result.Ok element
        
    let interactiveSpectrumLink html =
        html |> findAnchors (InnerText "Interactive spectrum")
        
    let fitsDownloadLink html =
        html |> findAnchors (Containing "Click to Download")
        
    let tryGetHref (fitsLocation:HtmlNode) =
        fitsLocation.TryGetAttribute "href"

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
        
    let download (fromLocation:string) saveTo survey seed =
        async {
            try
                // EXAMPLE: https://dr12.sdss.org/sas/dr12/boss/spectro/redux/v5_7_0/spectra/3655/spec-3655-55240-0040.fits
                let wc = new WebClient()
                wc.Encoding <- Encoding.ASCII
                wc.Headers.Set("Content-Type", "image/fits")
                
                let downloadDestination = getOrCreateDownloadDirectory saveTo
                let downloadTo = buildDownloadFileName fromLocation survey seed saveTo downloadDestination
                
                let from = fitsBaseAddress + fromLocation
                wc.DownloadFile( from, downloadTo )
                
                return Result.Ok "FITS File Successfully Downloaded."
            with e ->
                printfn "inner: %s" e.InnerException.Message
                return Result.Error e.Message
        }
        
    let private downloadFitsFile (fitsLocation:HtmlNode) saveTo survey seed =
        match tryGetHref fitsLocation with
        | Some fromHref -> download (fromHref.Value()) saveTo survey seed |> Async.RunSynchronously
        | None -> Result.Error "Couldn't download the FITS file"


    let loadInteractiveSpectrum (href:HtmlAttribute) saveTo survey seed =
        match doc(href.Value()) |> getHtml with
        | Some html ->
            match fitsDownloadLink html with
            | Result.Ok fitsLocation -> downloadFitsFile fitsLocation saveTo survey seed
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the quickview html document."
            
    let downloadFitsFileFrom fromLocation saveTo survey seed =
        match doc fromLocation |> getHtml with
        | Some html ->
            match interactiveSpectrumLink html with
            | Result.Ok interactiveSpectrumLocation ->
                let hrefOpt = tryGetHref interactiveSpectrumLocation
                match hrefOpt with
                | Some href -> loadInteractiveSpectrum href saveTo survey seed
                | None -> Result.Error "Couldn't locate the FITS file hyperlink."
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the main html document."
                