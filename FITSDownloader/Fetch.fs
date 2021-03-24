namespace FITSDownloader

open System.IO
open System.Net
open System.Text
open FSharp.Data

[<AutoOpen>]
module Fetch =

    type FindBy =
        | InnerText of string
        | Containing of string
        
    let doc href : HtmlDocument =
        let doc = HtmlDocument.Load(href, Encoding.UTF8)
        doc

    let findAnchor findBy (node:HtmlNode) =
        let htmlNodes = node.Descendants["a"]
        
        let htmlNodesFunc = 
            match findBy with
            | InnerText t -> Seq.tryFind(fun (d:HtmlNode) -> d.InnerText().Trim() = t)
            | Containing t -> Seq.tryFind(fun (d:HtmlNode) -> d.InnerText().Contains(t) )
        
        match htmlNodesFunc htmlNodes with
        | None -> Result.Error "Couldn't find the anchor."
        | Some element -> Result.Ok element
        
    let interactiveSpectrumLink html =
        html |> findAnchor (InnerText "Interactive spectrum")
        
    let fitsDownloadLink html =
        html |> findAnchor (Containing "Click to Download")
    
    let download (fromLocation:string) saveTo survey seed =
        async {
            try
                let dataLoc = fromLocation // "https://dr12.sdss.org" + 
                let wc = new WebClient()
                
                let downloadDestination = Path.Combine ( (Path.GetFullPath saveTo), "downloads")
                if not <| Directory.Exists(downloadDestination )
                then Directory.CreateDirectory(downloadDestination) |> ignore
                                        
                let defaultFileName = sprintf "%s_%s_%s" survey (seed.ToString().PadLeft(4, '0')) <| (dataLoc.Split('/') |> Seq.rev |> Seq.head)
                
                wc.Encoding <- Encoding.ASCII
                wc.Headers.Set("Content-Type", "image/fits")
                wc.DownloadFile(dataLoc, Path.Combine(downloadDestination, defaultFileName) )

                return Result.Ok "FITS File Successfully Downloaded."
            with e -> return Result.Error e.Message
        }
        
    let private downloadFitsFile (fitsLocation:HtmlNode) saveTo survey seed =
        match fitsLocation.TryGetAttribute "href" with
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
                let hrefOpt = interactiveSpectrumLocation.TryGetAttribute "href"
                match hrefOpt with
                | Some href -> loadInteractiveSpectrum href saveTo survey seed
                | None -> Result.Error "Couldn't locate the FITS file hyperlink."
            | Result.Error error -> Result.Error error
        | None -> Result.Error "Couldn't load the main html document."
                