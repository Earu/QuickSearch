namespace QuickSearch.Notification

open System
open QuickSearch.Result

module Notifications = 
    let notify_bad (prefix: String) (text: String) = 
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine(sprintf "[%s] >> %s" prefix text)

    let notify (prefix: String) (color: ConsoleColor) (text: String) = 
        Console.ForegroundColor <- ConsoleColor.White
        Console.Write("[")
        Console.ForegroundColor <- color
        Console.Write(prefix)
        Console.ForegroundColor <- ConsoleColor.White
        Console.Write(sprintf "] >> %s\n" text)

    let notify_result (res: Result) = 
        Console.ForegroundColor <- ConsoleColor.DarkGray
        Console.WriteLine(sprintf "\n@%s" res.path)
        notify (sprintf "L%d" res.line_number) ConsoleColor.Magenta (res.line.Trim())