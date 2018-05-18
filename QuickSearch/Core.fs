namespace QuickSearch

open System
open System.IO
open System.Text
open System.Threading.Tasks
open System.Text.RegularExpressions
open QuickSearch.Notification
open QuickSearch.Result

module Core =
    let get_encoding filename =
        // Read the BOM
        let bom: byte[] = Array.create 4 0uy
        let file = new FileStream(filename, FileMode.Open, FileAccess.Read)
        file.Read(bom, 0, 4) |> ignore

        // Analyze the BOM
        if bom.[0].Equals(0x2buy) && bom.[1].Equals(0x2fuy) && bom.[2].Equals(0x76uy) then Encoding.UTF7
        else if bom.[0].Equals(0xefuy) && bom.[1].Equals(0xbbuy) && bom.[2].Equals(0xbfuy) then Encoding.UTF8
        else if bom.[0].Equals(0xffuy) && bom.[1].Equals(0xfeuy) then Encoding.Unicode //UTF-16LE
        else if bom.[0].Equals(0xfeuy) && bom.[1].Equals(0xffuy) then Encoding.BigEndianUnicode //UTF-16BE
        else if bom.[0].Equals(0uy) && bom.[1].Equals(0uy) && bom.[2].Equals(0xfeuy) && bom.[3].Equals(0xffuy) then Encoding.UTF32
        else Encoding.ASCII

    let rec search (input: String) (dir: String) (file_count: int ref) (base_dir: String) =
        let dinfo = DirectoryInfo(dir)
        match dinfo.Attributes.HasFlag(FileAttributes.Hidden) with
        | false ->
            let files = try Directory.GetFiles(dir)
                        with _ -> 
                        Notifications.notify_bad "ERROR" (sprintf "Couldn't get file list from %s" dir)
                        [| |]
            let dirs = try Directory.GetDirectories(dir)
                       with _ -> 
                       Notifications.notify_bad "ERROR" (sprintf "Couldn't get directory list from %s" dir)
                       [| |]

            let mutable results: List<Result> = []
    
            let cur_count = file_count.Value
            file_count := cur_count + files.Length

            let search_file = fun (f: String) ->
                try
                    let bytes = try File.ReadAllBytes(f) with _ -> [| |]
                    let encoding = get_encoding f
                    let content = encoding.GetString(bytes)
                    let lines = content.Split('\n')

                    match Regex.IsMatch(f.Remove(0,dir.Length),input) with
                    | true -> 
                        let d = sprintf "~%s" (f.Remove(0,base_dir.Length))
                        let res = Result("Filename matched ^^^^^",0,d)
                        results <- res :: results
                    | false -> ()
            
                    let mutable line = 1
                    for l in lines do
                        match Regex.IsMatch(l,input) with
                        | true -> 
                            let found = (if l.Length > 250 then sprintf "%s...\n%d chars left" l.[0..250] (l.Length - 250) else l)
                            let d = sprintf "~%s" (f.Remove(0,base_dir.Length))
                            let res = Result(found,line,d)
                            results <- res :: results
                        | false -> ()
                        line <- line + 1
                with
                | e -> Notifications.notify_bad "ERROR" (sprintf "Couldn't read from %s\n\t -> %s" f e.Message)
    
            let search_dir = fun (d: String) -> results <- results @ (search input d file_count base_dir) 

            Parallel.ForEach(files,search_file) |> ignore
            Parallel.ForEach(dirs,search_dir) |> ignore
    
            results
        | true -> List.empty

    let prompt =
        Console.Clear()
        Console.Write("search: ")
        let input = Console.ReadLine()
        Console.Write("dir: ")
        let dir = Console.ReadLine()
    
        printfn "\n\t-----------"
        let before = DateTime.Now
        let file_count = ref 0
        let results = match Directory.Exists(dir) with
                        | true -> search input dir file_count dir
                        | false -> []
        let time_taken = (DateTime.Now - before).Milliseconds
    
        Notifications.notify "SEARCH" ConsoleColor.DarkYellow (sprintf "Browsed %d files in %dms" file_count.Value time_taken)
        match results.IsEmpty with
        | true -> printfn "Nothing was found."
        | false ->
            Console.WriteLine(sprintf "%d results were found for %s." results.Length input)
            Console.Write("Display? Y/N: ")
            let key = Console.ReadKey()
            match key.Key.Equals(ConsoleKey.Y) with
            | true ->
                for x in results do
                    Notifications.notify_result x
            | false -> ()
        printfn "\t-----------"
        Console.Read()

