## Types

Infra:

Internal state of monad - State
Exposed type - Block

Builder instance scoped
    Provider type
    Name
Block scoped state
    Resources
    Handles
    Nested (nested blocks, could be different scope)

State (block scoped):
    resources
    handles
    nested
    * bind (resource, provider) -> State
    * nest (block) -> State
    * run (state, provider type) -> Block

Block
    Name
    Provider
    * attach (state) -> State
