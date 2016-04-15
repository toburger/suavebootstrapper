module Async =
    let bind f m = async {
        let! r = m
        return! f r
    }
    let returnM f m = async {
        return! f m
    }
    let map f m = async {
        let! r = m
        return f r
    }

let (<!>) m f = Async.map f m
let (>>=) m f = Async.bind f m
