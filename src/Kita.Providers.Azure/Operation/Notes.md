The `Microsoft.Azure.Management.*.Fluent` set of libraries are the 'old' stuff and will become deprecated at some point [comment](https://github.com/Azure/azure-libraries-for-net/issues/1226#issuecomment-804659466)

The `Azure.ResourceManager.*` set of libraries are the newer stuff and work nice with `Azure.Identity`, but they're missing a lot of services currently (including App Service, Functions)

We'll have to use a mix for now and replace the old set with newer libraries as they become available.

I'm going to split them into two modules, AzurePreviousApi and AzureNextApi

Credentials (subid, credential) are static so clients can be static and shared in the instance. This means each program can only be deployed to one subscription/tenant. If users want to target multiple subscriptions, they'll need a separate project for each.



## Deploy


### WEBSITE_RUN_FROM_PACKAGE

Can't seem to use this because scm still tries to do stuff with wwwroot which is set to read only when this is enabled

The error:
    Unhandled exception. System.AggregateException: One or more errors occurred. (Long running operation failed with status 'failed'. Additional Info:'Package deployment failed
    ARM-MSDeploy Deploy Failed: 'Microsoft.Web.Deployment.DeploymentClientServerException: An error was encountered when processing operation 'Create Directory' on 'C:\home\site\wwwroot\.azurefunctions'. ---&gt; Microsoft.Web.Deployment.DeploymentException: The error code was 0x80070002. ---&gt; System.IO.FileNotFoundException: Could not find file 'C:\home\site\wwwroot\.azurefunctions'.
       at Microsoft.Web.Deployment.NativeMethods.RaiseIOExceptionFromErrorCode(Win32ErrorCode errorCode, String maybeFullPath)
       at Microsoft.Web.Deployment.DirectoryEx.CreateDirectory(String path)
       at Microsoft.Web.Deployment.DirPathProviderBase.CreateDirectory(String fullPath, DeploymentObject source)
       at Microsoft.Web.Deployment.DirPathProviderBase.Add(DeploymentObject source, Boolean whatIf)
       --- End of inner exception stack trace ---
       --- End of inner exception stack trace ---
       at Microsoft.Web.Deployment.FilePathProviderBase.HandleKnownRetryableExceptions(DeploymentBaseContext baseContext, Int32[] errorsToIgnore, Exception e, String path, String operation)
       at Microsoft.Web.Deployment.DirPathProviderBase.Add(DeploymentObject source, Boolean whatIf)
       at Microsoft.Web.Deployment.DeploymentObject.AddChild(DeploymentObject source, Int32 position, DeploymentSyncContext syncContext)
       at Microsoft.Web.Deployment.DeploymentSyncContext.HandleAddChild(DeploymentObject destParent, DeploymentObject sourceObject, Int32 position)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncDirPathChildren(DeploymentObject destRoot, DeploymentObject sourceRoot)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildrenNoOrder(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildrenOrder(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.ProcessSync(DeploymentObject destinationObject, DeploymentObject sourceObject)
       at Microsoft.Web.Deployment.DeploymentObject.SyncToInternal(DeploymentObject destObject, DeploymentSyncOptions syncOptions, PayloadTable payloadTable, ContentRootTable contentRootTable, Nullable`1 syncPassId, String syncSessionId)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(DeploymentProviderOptions providerOptions, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(String provider, String path, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(DeploymentWellKnownProvider provider, String path, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.WebApi.AppGalleryPackage.Deploy(String deploymentSite, String siteSlotId, Boolean doNotDelete)
       at Microsoft.Web.Deployment.WebApi.DeploymentController.&lt;DownloadAndDeployPackage&gt;d__25.MoveNext()'')
     ---> Microsoft.Rest.Azure.CloudException: Long running operation failed with status 'failed'. Additional Info:'Package deployment failed
    ARM-MSDeploy Deploy Failed: 'Microsoft.Web.Deployment.DeploymentClientServerException: An error was encountered when processing operation 'Create Directory' on 'C:\home\site\wwwroot\.azurefunctions'. ---&gt; Microsoft.Web.Deployment.DeploymentException: The error code was 0x80070002. ---&gt; System.IO.FileNotFoundException: Could not find file 'C:\home\site\wwwroot\.azurefunctions'.
       at Microsoft.Web.Deployment.NativeMethods.RaiseIOExceptionFromErrorCode(Win32ErrorCode errorCode, String maybeFullPath)
       at Microsoft.Web.Deployment.DirectoryEx.CreateDirectory(String path)
       at Microsoft.Web.Deployment.DirPathProviderBase.CreateDirectory(String fullPath, DeploymentObject source)
       at Microsoft.Web.Deployment.DirPathProviderBase.Add(DeploymentObject source, Boolean whatIf)
       --- End of inner exception stack trace ---
       --- End of inner exception stack trace ---
       at Microsoft.Web.Deployment.FilePathProviderBase.HandleKnownRetryableExceptions(DeploymentBaseContext baseContext, Int32[] errorsToIgnore, Exception e, String path, String operation)
       at Microsoft.Web.Deployment.DirPathProviderBase.Add(DeploymentObject source, Boolean whatIf)
       at Microsoft.Web.Deployment.DeploymentObject.AddChild(DeploymentObject source, Int32 position, DeploymentSyncContext syncContext)
       at Microsoft.Web.Deployment.DeploymentSyncContext.HandleAddChild(DeploymentObject destParent, DeploymentObject sourceObject, Int32 position)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncDirPathChildren(DeploymentObject destRoot, DeploymentObject sourceRoot)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildrenNoOrder(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildrenOrder(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.SyncChildren(DeploymentObject dest, DeploymentObject source)
       at Microsoft.Web.Deployment.DeploymentSyncContext.ProcessSync(DeploymentObject destinationObject, DeploymentObject sourceObject)
       at Microsoft.Web.Deployment.DeploymentObject.SyncToInternal(DeploymentObject destObject, DeploymentSyncOptions syncOptions, PayloadTable payloadTable, ContentRootTable contentRootTable, Nullable`1 syncPassId, String syncSessionId)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(DeploymentProviderOptions providerOptions, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(String provider, String path, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.DeploymentObject.SyncTo(DeploymentWellKnownProvider provider, String path, DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions)
       at Microsoft.Web.Deployment.WebApi.AppGalleryPackage.Deploy(String deploymentSite, String siteSlotId, Boolean doNotDelete)
       at Microsoft.Web.Deployment.WebApi.DeploymentController.&lt;DownloadAndDeployPackage&gt;d__25.MoveNext()''
       at Microsoft.Rest.ClientRuntime.Azure.LRO.AzureLRO`2.CheckForErrors()
       at Microsoft.Rest.ClientRuntime.Azure.LRO.PutLRO`2.CheckForErrors()
       at Microsoft.Rest.ClientRuntime.Azure.LRO.AzureLRO`2.StartPollingAsync()
       at Microsoft.Rest.ClientRuntime.Azure.LRO.AzureLRO`2.BeginLROAsync()
       at Microsoft.Rest.Azure.AzureClientExtensions.GetLongRunningOperationResultAsync[TBody,THeader](IAzureClient client, AzureOperationResponse`2 response, Dictionary`2 customHeaders, CancellationToken cancellationToken)
       at Microsoft.Rest.Azure.AzureClientExtensions.GetLongRunningOperationResultAsync[TBody](IAzureClient client, AzureOperationResponse`1 response, Dictionary`2 customHeaders, CancellationToken cancellationToken)
       at Microsoft.Rest.Azure.AzureClientExtensions.GetPutOrPatchOperationResultAsync[TBody](IAzureClient client, AzureOperationResponse`1 response, Dictionary`2 customHeaders, CancellationToken cancellationToken)
       at Microsoft.Azure.Management.AppService.Fluent.WebAppsOperations.CreateMSDeployOperationWithHttpMessagesAsync(String resourceGroupName, String name, MSDeploy mSDeploy, Dictionary`2 customHeaders, CancellationToken cancellationToken)
       at Microsoft.Azure.Management.AppService.Fluent.WebAppsOperationsExtensions.CreateMSDeployOperationAsync(IWebAppsOperations operations, String resourceGroupName, String name, MSDeploy mSDeploy, CancellationToken cancellationToken)
       at Microsoft.Azure.Management.AppService.Fluent.AppServiceBaseImpl`6.CreateMSDeploy(MSDeploy msDeployInner, CancellationToken cancellationToken)
       at Microsoft.Azure.Management.AppService.Fluent.WebDeploymentImpl`5.ExecuteAsync(Cance   at AzureNativePrototype.AzurePreviousApi.AppService.deployFunctionApp@93-4.Invoke(Unit unitVar0) in C:\Users\Steven\Projects\Kita\tests\Kita.Providers.Azure.Test\NativePrototype\Operation\AzurePreviousApi.fs:line 93
       at Ply.TplPrimitives.ContinuationStateMachine`1.System-Runtime-CompilerServices-IAsyncStateMachine-MoveNext()
       at <StartupCode$NativePrototype>.$Provider.Deploy@55-5.Invoke(Unit unitVar0) in C:\Users\Steven\Projects\Kita\tests\Kita.Providers.Azure.Test\NativePrototype\Provider.fs:line 55
       at Ply.TplPrimitives.ContinuationStateMachine`1.System-Runtime-CompilerServices-IAsyncStateMachine-MoveNext()
       at <StartupCode$NativePrototype>.$Provider.Run@142-4.Invoke(Unit unitVar0) in C:\Users\Steven\Projects\Kita\tests\Kita.Providers.Azure.Test\NativePrototype\Provider.fs:line 142
       at Ply.TplPrimitives.ContinuationStateMachine`1.System-Runtime-CompilerServices-IAsyncStateMachine-MoveNext()
       --- End of inner exception stack trace ---
       at System.Threading.Tasks.Task.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
       at System.Threading.Tasks.Task.Wait()
       at Program.main(String[] argv) in C:\Users\Steven\Projects\Kita\tests\Kita.Providers.Azure.Test\NativePrototype\App\Program.fs:line 43


### Thread was being aborted

> ARM-MSDeploy Deploy Failed: 'System.Threading.ThreadAbortException: Thread was being aborted.

Race condition with certain changes causing a restart, then deploy request is sent. If deploy gets cut off because of the restart, we get this error.

Not sure which changes are triggering the restart though..

Maybe creating the function app?
I need the function app to deploy the blob.. I don't think I can create the functionapp with blob specified

Logs from portal LogStream
Looks like if host.json gets rewritten, that at least triggers a restart. Should only happen on deploy.

2021-06-11T18:52:30.438 [Information] File change of type 'Changed' detected for 'C:\home\site\wwwroot\host.json'
2021-06-11T18:52:30.439 [Information] Host configuration has changed. Signaling restart
2021-06-11T18:52:30.439 [Information] File change of type 'Changed' detected for 'C:\home\site\wwwroot\host.json'
2021-06-11T18:52:30.439 [Information] Host configuration has changed. Signaling restart
2021-06-11T18:52:30.485 [Information] File change of type 'Changed' detected for 'C:\home\site\wwwroot\host.json'
2021-06-11T18:52:30.485 [Information] Host configuration has changed. Signaling restart


### Naming rules

There are many varied restrictions when it comes to naming resources. Probably not possible to accurately cover all of these, especially if they change.

https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftstorage


### SQL automated authentication with app

https://github.com/MicrosoftDocs/sql-docs/issues/2323

Need to use a workaround to give app's system managed identity access to sql server. Options are:

**Use the hexed object id + type**
CREATE USER [user-group-name]  WITH SID=0x70727A49A319301BC8F1934918799E49, TYPE=X;
X for group, E for application (from comments in issue)
- Unsupported
+ No unnecessary additional roles or permissions (like directory reader)
+ No manual step in provisioning
**Grab user credentials from az CLI**
assuming it's a global administrator, and use that to give read directory permission to a generated group
Does this actually work?
- Means user needs to have az cli installed
- Relies on permissions beyond just scope of subscription
- CI/CD would need a global admin credential (barf)
+ No manual step in provisioning (provided az cli is set up)
**Ask user to do it themselves via portal**
can automate creation, then output a message saying permissions need to be granted manually until issue addressed and link to gh
- Manual step in provisioning
- Bad for CI/CD, or needs admin cred
+ App doesn't use permissions beyond subscription programmatically

Based on that reasoning I'm going with the unsupported workaround.
