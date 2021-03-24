namespace FITSDownloader

[<AutoOpen>]
module HtmlFunctions =
        
    open FSharp.Data
    
    let getHtml (htmlDocument:HtmlDocument) =
        htmlDocument.TryGetHtml()

    let body (node:HtmlNode) =
        node.Elements["body"] |> Seq.exactlyOne

    let hasClass cls (node:HtmlNode) =
        node.HasClass cls
        

