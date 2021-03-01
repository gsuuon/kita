namespace Kita.Core.Providers

type Config =
  { name : string
    deploy : string }

module Default =
    let Local =
      { name = "Local"
        deploy = "deploy" }

    let Azure =
      { name = "Azure"
        deploy = "func host start" }
