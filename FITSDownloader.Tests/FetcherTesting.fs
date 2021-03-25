namespace FITSDownloader.Tests

open HtmlAgilityPack

module FetchTesting =
    
    open FITSDownloader
    open NUnit.Framework            
    open FSharp.Data
    
    let fromLocation = "http://skyserver.sdss.org/dr12/en/tools/quicklook/summary.aspx?sid=4115175242539397120"
    let saveTo = System.IO.Path.Combine("/home/mark","FITS")
        
    type NoOpClientDownloader() =
        let callsMade = ResizeArray<string*string>()
        interface IDownloader with
            member __.Get x y = callsMade.Add(x,y)
            
        member __.getCallsMade = callsMade
        
    let webClientDownloader = WebClientDownloader()
    
    
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
    let ``Downloads a FITS file with name including redshift and bestObjId values``() =
        let noopClientDownloader = NoOpClientDownloader()
        let survey = "sdss"
        let seed = 1
        let result = downloadFitsFileFrom noopClientDownloader fromLocation saveTo survey seed
        if result = (Result.Ok "FITS File Successfully Downloaded.")
        then
            let _,filename = noopClientDownloader.getCallsMade |> Seq.exactlyOne                                                                    
            Assert.IsTrue(filename.Contains("sdss_0001_2-7094_4115175242539397120_spec-3655-55240-0040.fits"))
        else
            printfn "%A does not equal %A" result (Result.Ok "FITS File Successfully Downloaded")
            Assert.Fail "results don't match."

    [<Test>]
    let ``Can obtain the interactive spectrum link`` () =
        match getHtmlDocument fromLocation |> getHtml with
        | Some element ->
            let body = body element
            
            match body |> findAnchors (InnerText"Interactive spectrum") with
            | Result.Ok link ->
                match link.TryGetAttribute "href" with
                | Some href ->
                    printfn "link to Interactive Spectrum: %s" (href.Value()); Assert.Pass()
                | _ -> Assert.Fail "Couldn't find the hyperlink."
            | Result.Error error -> Assert.Fail error
        
        | None -> Assert.Fail "Couldn't find the document."    
                    
    [<Test>]
    let ``Can download to specific location``() =
        let noopClientDownloader = NoOpClientDownloader()
        let survey = "sdss"
        let seed = 1
        
        let downloadTo = buildDownloadFileName fromLocation survey seed saveTo "2.1233" "4115175242539397120"
        let downloadResult =
            download
            <| noopClientDownloader
            <| "https://dr12.sdss.org/sas/dr12/boss/spectro/redux/v5_7_0/spectra/3655/spec-3655-55240-0040.fits"
            <| downloadTo

        match downloadResult with
        | Result.Ok _ -> Assert.Pass()
        | Result.Error error -> Assert.Fail error
        
    open HtmlAgilityPack.FSharp
        
    [<Test>]
    let ``Can locate the red shift on the quick view page``() =
        let redShiftValue = findRedShift fromLocation
        Assert.AreEqual("2-7094", redShiftValue)
        
    [<Test>]
    let ``Can locate the objId on the quick view page``() =
        let bestObjId = getBestObjId fromLocation
        Assert.AreEqual("4115175242539397120", bestObjId)