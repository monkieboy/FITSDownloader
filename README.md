# FITSDownloader

The program expects the link to the quickview page to be the last parameter in the CSV input file.

To run execute on commandline like so (options are case sensitive):


`dotnet run --project BulkFITSDownloader --survey "<boss|sdss>" --saveto "<full path directory>" --inputfile "<full path>/<file name>.csv"`


when it downloads it will save in a folder called `downloads` inside where you specified `--saveto`.

