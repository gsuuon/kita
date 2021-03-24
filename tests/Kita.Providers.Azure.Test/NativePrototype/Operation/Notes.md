The `Microsoft.Azure.Management.*.Fluent` set of libraries are the 'old' stuff and will become deprecated at some point [comment](https://github.com/Azure/azure-libraries-for-net/issues/1226#issuecomment-804659466)

The `Azure.ResourceManager.*` set of libraries are the newer stuff and work nice with `Azure.Identity`, but they're missing a lot of services currently (including App Service, Functions)

We'll have to use a mix for now and replace the old set with newer libraries as they become available.

I'm going to split them into two modules, AzurePreviousApi and AzureNextApi

Credentials (subid, credential) are static so clients can be static and shared in the instance. This means each program can only be deployed to one subscription/tenant. If users want to target multiple subscriptions, they'll need a separate project for each.
