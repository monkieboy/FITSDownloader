namespace FITSDownloader

[<AutoOpen>]
module FITSHtmlFunctions =
        
    open FSharp.Data
    open System.Text
    
    let getHtml (htmlDocument:HtmlDocument) =
        htmlDocument.TryGetHtml()

    let body (node:HtmlNode) =
        node.Elements["body"] |> Seq.exactlyOne

    let hasClass cls (node:HtmlNode) =
        node.HasClass cls
        

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