namespace BulkFITSDownloader

module CommandLine =
    
    open Argu

    type CliArgs =
        | [<Mandatory>] Survey    of survey:string
        | [<Mandatory>] SaveTo    of saveTo:string
        | [<Mandatory>] InputFile of inputFile:string

        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Survey    survey    -> "specify the survey that the file is taken from."
                | SaveTo    saveTo    -> "specify a the file system location to save to."
                | InputFile inpufFile -> "the full path to the input csv file."

    let parser = ArgumentParser.Create<CliArgs>(programName = "BulkFITSDownloader.exe")

