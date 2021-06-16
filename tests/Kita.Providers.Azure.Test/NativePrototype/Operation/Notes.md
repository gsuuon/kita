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
