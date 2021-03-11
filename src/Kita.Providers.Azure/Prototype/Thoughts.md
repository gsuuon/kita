## Thoughts on Farmer
There are some documentation issues - the docs are for a different version (vNext?) than the current latest release. I don't know how to pull vNext via NuGet.
 - add_table/add_tables is missing from 1.4.0 but present in master Branch.
 - use_managed_keyvault exists in docs but not present anywhere?

It's more F# native, so they care about static type safety. Required parameters are actually statically required, by e.g. parenting.


## Thoughts on Pulumi
Way more verbose, but most of that can be moved to helper functions. Much more likely to break at runtime, or be very environment dependent (certain plugins need to be installed).
API is complete, docs are good.
Requires Pulumi account -- is there a way around this? It's oss, there must be.

## Comparison
Farmer is much nicer to use, but is quite limited. May not support all services, or be missing apis (currently missing storage / tables)
Pulumi supports many providers, but breaks at runtime(deploytime?) easily. It also requires a pulumi account.

### Farmer
Pros:
feels safer
more concise
programmatic by default

Cons:
missing apis (can't create tables)
hand written, will continue to miss apis
azure only


### Pulumi.FSharp.Extensions
Pros:
tons of providers
azure apis generated from azure's openapi specs (won't be missing services)

Cons:
unsafe (runtime errors galore when deploying)
  - known issue -- https://github.com/pulumi/pulumi/issues/3808
  - may be just marginally better than the naked azure rest/json api
  * but - the docs specify which items are required
programmatic access only via preview Automation package
requires pulumi account (and so, downstream users would require a pulumi account)


### Summary

I think I'll go with Pulumi, because of the multiple vendor support. That was one of the primary goals of Kita (abstract away provider), and Pulumi does that while Farmer ties us to Azure.
The two main drawbacks are:

1. Runtime errors
    - I'll just need to have the docs open while using Pulumi to know which parameters are required

2. Pulumi account
    - Users will need the platform vendor account + vendor cli installed + pulumi account. The trade-off for this is actual multi-cloud.
