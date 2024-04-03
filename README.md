## NOTE _(April 2024)_

To the best of my knowledge, this is still a valid project. I do not believe EF Core has changed their `OptionConverter` interface since it was released. If this library needs attention, please open an issue!

# FSharp.EFCore.OptionConverter
A value converter that maps nullable database columns to F# `option` fields/properties

[![Build status](https://ci.appveyor.com/api/projects/status/y68nd0ucc56d3all?svg=true)](https://ci.appveyor.com/project/danieljsummers/fsharp-efcore-optionconverter)  [![NuGet](https://img.shields.io/nuget/dt/FSharp.EFCore.OptionConverter.svg)](https://www.nuget.org/packages/FSharp.EFCore.OptionConverter)

This is a NuGet package of the code described in [a recent blog post](https://blog.bitbadger.solutions/2018/f-sharp-options-with-ef-core.html), which provides for mapping nullable database columns to F# options in the entity's fields or properties.

The blog post describes how to set it all up; the test project here demonstrates it (along with SQL to ensure that the results are what they should be).
