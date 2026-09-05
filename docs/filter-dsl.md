# Filter DSL

Used by the in-game viewer to filter the live log. Grammar:

```
expr    := orExpr
orExpr  := andExpr ( "OR" andExpr )*
andExpr := notExpr ( "AND" notExpr )*
notExpr := "NOT" notExpr | "(" expr ")" | term
term    := "level" levelOp LEVEL
         | "channel" strOp STRING
         | "text" strOp STRING
         | "mod" strOp STRING
         | CTXKEY strOp STRING
levelOp := "=" | "!=" | "<" | "<=" | ">" | ">="
strOp   := "=" | "!="
LEVEL   := Trace | Debug | Info | Warn | Error | Fatal
CTXKEY  := "ctx." followed by a structured-context key
STRING  := "double-quoted, supports * wildcards"
```

Channel string matching supports `*` wildcards; a trailing `.*` matches the channel itself or any dotted descendant.

Examples:

```
level >= Warn
level >= Warn OR channel = "Cosmere.*"
channel = "Cosmere.Roshar.*" AND level >= Debug
NOT (channel = "Unity")
level != Trace AND NOT channel = "Vanilla"
ctx.pawn = "Randy*"
level >= Warn AND ctx.days_left = "2"
```

## Context keys

`ctx.<key>` reads the structured context attached to an entry, from either an anonymous-object
argument or a `{Placeholder}` in the message template.

- **Key lookup ignores case.** A template writing `{Pawn}` is found by `ctx.pawn`.
- **Values are compared as text**, formatted with the invariant culture. A context value of `2`
  matches `ctx.days_left = "2"` on every machine.
- **A missing key never matches.** `ctx.pawn = "Randy"` is false when the entry has no `pawn`
  key, which means `ctx.pawn != "Randy"` is true for those entries. That follows from the first
  rule but catches people out.
- The key cannot contain a dot. `ctx.a.b` is a parse error.

The suggestion box offers `ctx.` but cannot complete the key, because the viewer does not track
which context keys it has seen. Type the key yourself.
