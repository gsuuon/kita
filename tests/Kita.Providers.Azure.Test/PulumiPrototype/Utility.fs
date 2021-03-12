module PulumiPrototype.Utility

let waitUntilValue
    (interval: int)
    (opt: 'T option ref)
    =
    async {
        while (!opt).IsNone do
            do! Async.Sleep interval

        return (!opt).Value
    }
