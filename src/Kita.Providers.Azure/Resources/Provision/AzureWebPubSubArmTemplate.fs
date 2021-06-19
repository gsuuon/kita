namespace Kita.Providers.Azure.Resources.Provision

type WebPubSubArmParameters =
    { location : string
      name : string
      tier : string
      skuName : string
      capacity : int
    }

module AzureWebPubSubTemplates =
    let parameters (wpsParams: WebPubSubArmParameters) = $"""{{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
    "contentVersion": "1.0.0.0",
    "parameters":
        "location": {{
            "value": "{wpsParams.location}"
        }},
        "name": {{
            "value": "{wpsParams.name}"
        }},
        "skuName": {{
            "value": "{wpsParams.skuName}"
        }},
        "tier": {{
            "value": "{wpsParams.tier}"
        }},
        "capacity": {{
            "value": {wpsParams.capacity}
        }}
}}"""

    let armTemplate = """{
    "$schema": "http://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "location": {
            "type": "string"
        },
        "name": {
            "type": "string"
        },
        "skuName": {
            "type": "string"
        },
        "tier": {
            "type": "string"
        },
        "capacity": {
            "type": "int"
        }
    },
    "variables": {},
    "resources": [
        {
            "name": "[parameters('name')]",
            "type": "Microsoft.SignalRService/WebPubSub",
            "apiVersion": "2021-04-01-preview",
            "location": "[parameters('location')]",
            "properties": {},
            "sku": {
                "name": "[parameters('skuName')]",
                "tier": "[parameters('tier')]",
                "capacity": "[parameters('capacity')]"
            },
            "dependsOn": [],
            "tags": {}
        }
    ],
    "outputs": {
        "primaryConString" : {
            "type":"securestring",
            "value":"[listKeys(parameters('name'), '2021-04-01-preview').primaryConnectionString]"
        }
    }
}"""
