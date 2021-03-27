The default template is broken, needed to change in ProxyApp.fsproj <None Update="host.json"> to <None Include...>, for both host.json and local.settings.json
    https://github.com/Azure/azure-functions-core-tools/issues/1963
    https://github.com/Azure/azure-functions-templates/pull/954


Found this:
https://github.com/giraffe-fsharp/Giraffe.AzureFunctions/tree/master/demo
