---
title: "Outputs and ARM Expressions"
date: 2020-06-19T23:50:54+02:00
draft: false
weight: 2
---

Outputs can be created in Farmer for any [ARM Expression](../../api-overview/expressions), Resource Name or any optional string. ARM Expressions are most useful in this case for referring to values that only exist at *deployment time*, such as connection strings.

###

### Creating ARM Expressions
Farmer ARM expressions are in reality just wrapped strings, and are easy to create. For example, the code to create a Storage Key property  is similar to this:

```fsharp
let buildKey accountName : ArmExpression =
    // Create the raw string of the expression
    let rawValue =
        sprintf
            "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)"
            accountName
            accountName

    // Wrap the raw value in an ARM Expression and return it
    ArmExpression rawValue
```

Notice that you do *not* wrap the expression in square brackets [ ]; Farmer will do this when writing out the ARM template.

### Extracting the value of an ARM Expression
ARM expressions also have the following members on them:
* `Map` - standard map
* `Bind` - standard bind
* `Value` - Returns the raw string value
* `Eval` - Returns the string as a formatted ARM expression i.e. surround in `[]`