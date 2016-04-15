open System

module internal Generator =
    let dateTimeOffset generator =
        generator (fun () -> DateTimeOffset.Now)
                  (fun d -> DateTimeOffset.Now <= d)
    let dateTime generator =
        generator (fun () -> DateTime.Now)
                  (fun d -> DateTime.Now <= d)

module Cacher =
    let cacheGen (initKey : unit -> 'CKey)
                 (condition : 'CKey -> bool)
                 (operation : unit -> 'CKey * 'CValue) : unit -> 'CValue =
        let nextUpdate = ref (initKey ())
        let channels = ref None
        fun () ->
            if not (condition !nextUpdate) then
                let nextUpdate', channels' = operation ()
                nextUpdate := nextUpdate'
                channels := Some channels'
            match !channels with
            | Some channels -> channels
            | None -> failwith "Please check your initKey and condition logic!"

    let dateTimeOffsetCaching operation =
        Generator.dateTimeOffset cacheGen operation

    let dateTimeCaching operation =
        Generator.dateTime cacheGen operation

module CacherAsync =
    let cacheGen (initKey : unit -> 'CKey)
                 (condition : 'CKey -> bool)
                 (operation : Async<'CKey * 'CValue>) : unit -> Async<'CValue> =
        let nextUpdate = ref (initKey ())
        let channels = ref None
        fun () -> async {
            if not (condition !nextUpdate) then
                let! nextUpdate', channels' = operation
                nextUpdate := nextUpdate'
                channels := Some channels'
            match !channels with
            | Some channels -> return channels
            | None -> return failwith "Please check your initKey and condition logic!"
    }

    let dateTimeOffsetCaching operation =
        Generator.dateTimeOffset cacheGen operation

    let dateTimeCaching operation =
        Generator.dateTime cacheGen operation

#r "System.Runtime.Caching"

module MemoryCache =

    open System.Runtime.Caching

    let cache = MemoryCache.Default

    let getOrUpdate (key : 'TKey) (update : unit -> DateTimeOffset * 'TValue) =
        let key = string (hash key)
        match cache.Get(key) with
        | null ->
            let expiration, value = update ()
            cache.Set(key, value, expiration)
            value
        | value ->
            unbox value

    let asyncGetOrUpdate (key : 'TKey) (asyncUpdate : Async<DateTimeOffset * 'TValue>) = async {
        let key = string (hash key)
        match cache.Get(key) with
        | null ->
            let! expiration, value = asyncUpdate
            cache.Set(key, value, expiration)
            return value
        | value ->
            return unbox value
    }
