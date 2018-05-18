namespace QuickSearch.Result

open System

type Result(line,line_number,path) =
    member self.line: String = line
    member self.line_number: int = line_number
    member self.path: String = path