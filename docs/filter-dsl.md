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

## Plain text search

Anything without an operator, a quote or a bracket is treated as a phrase to find in the message,
not as an expression. Typing `exception` is the same as typing `text = "exception"`.

That rule is deliberate rather than a blanket fallback. An input that *does* contain `=`, `<`,
`>`, `!`, `"`, `(` or `)` was clearly meant to be an expression, so a mistake in one still reports
a parse error. `level >= Warnn` is an error, not a silent search for that literal string, which
would look identical to a filter that simply matched nothing.

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

### `ctx.mod_id`

Every entry attributed to a mod carries `mod_id`, the mod's packageId, whether it came through
RimLogging's own API or was captured from a `Verse.Log` call. It is the one context key you can
rely on being there.

```
ctx.mod_id = "cebarks.rimworld.linuxmisc"
level >= Warn AND ctx.mod_id = "brrainz.*"
```

A key you pass yourself wins. If your own context already has `mod_id`, RimLogging leaves it
alone, so the value you filter on is the one you set.

Typing `ctx.` completes against the keys the viewer has actually seen, and opening a quote after
`ctx.<key> =` completes against that key's values. Both come from the entries in the buffer, so a
key appears once something has logged it.
