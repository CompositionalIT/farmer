module private Assembly

open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Tests")>]
do()