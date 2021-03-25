namespace FITSDownloader.Tests

open System.Net
open System.Text
open FSharp.Data


module FetchTesting =
    open FITSDownloader
    open NUnit.Framework            

    let fromLocation = "http://skyserver.sdss.org/dr12/en/tools/quicklook/summary.aspx?sid=4115175242539397120"
    let saveTo = System.IO.Path.Combine("/home/mark","FITS")
        
    let noopClientDownloader =
        { get =
            let callsMade = ResizeArray<string*string>()
            callsMade.Add  }
        
    [<Test>]
    let ``Downloads a FITS file to save location``() =
        let survey = "sdss"
        let seed = 1
        let result = downloadFitsFileFrom webClientDownloader fromLocation saveTo survey seed
        if result = (Result.Ok "FITS File Successfully Downloaded.")
        then Assert.Pass()
        else
            printfn "%A does not equal %A" result (Result.Ok "FITS File Successfully Downloaded")
            Assert.Fail "results don't match."
                
    [<Test>]
    let ``Can obtain the interactive spectrum link`` () =

        match doc fromLocation |> getHtml with
        | Some element ->
            let body = body element
            match body |> findAnchors (InnerText"Interactive spectrum") with
            | Result.Ok link ->
                match link.TryGetAttribute "href" with
                | Some href ->
                    printfn "link to Interactive Spectrum: %s" (href.Value())
                    Assert.Pass()
                | _ -> Assert.Fail "Couldn't find the hyperlink."
            | Result.Error error -> Assert.Fail error
        | None -> Assert.Fail "Couldn't find the document."
        
    [<Test>]
    let ``Can download to specific location``() =
        let survey = "sdss"
        let seed = 1
        let downloadDestination = getOrCreateDownloadDirectory saveTo
        let downloadTo = buildDownloadFileName fromLocation survey seed saveTo downloadDestination
        let downloadResult =
            download
            <| noopClientDownloader
            <| "https://dr12.sdss.org/sas/dr12/boss/spectro/redux/v5_7_0/spectra/3655/spec-3655-55240-0040.fits"
            <| downloadTo

        match downloadResult with
        | Result.Ok _ -> Assert.Pass()
        | Result.Error error -> Assert.Fail error