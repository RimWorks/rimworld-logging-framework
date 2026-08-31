# Channels

Channels are dotted, hierarchical names. Define them in XML to set defaults, or just pass any string at call time for a transient channel.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
    <RimWorks.RimLogging.Channels.ChannelDef>
        <defName>Cosmere.Roshar.Surgebinding</defName>
        <label>Surgebinding</label>
        <description>Stormlight bonding, surge investiture, oath progression.</description>
        <defaultLevel>Debug</defaultLevel>
        <color>(0.7, 0.85, 1.0)</color>
        <captureStackAt>Error</captureStackAt>
        <destinations>
            <li>RollingText</li>
        </destinations>
        <format>[{Channel}] {Message}</format>
    </RimWorks.RimLogging.Channels.ChannelDef>
</Defs>
```

> The XML element name is the fully namespace-qualified type, `RimWorks.RimLogging.Channels.ChannelDef`, not `RimLogging.ChannelDef`.

`ChannelDef` fields:

| Field | Default | Meaning |
|---|---|---|
| `defaultLevel` | `Info` | Minimum level emitted on this channel. |
| `color` | none | RGB tuple for the viewer, e.g. `(0.7, 0.85, 1.0)`. |
| `captureStackAt` | `Error` | Level at/above which a stack trace is captured. |
| `destinations` | all sinks | Sink defNames this channel routes to (empty = every registered sink). |
| `format` | default | Per-channel format template override. |

**Transient fallback / prefix resolution:** when you log to a channel name with no exact `ChannelDef`, resolution walks up the dotted prefix to the nearest registered ancestor, then falls back to the built-in `default` channel. So `Cosmere.Roshar.Surgebinding.Windrunner` uses the `Cosmere.Roshar.Surgebinding` def if that is the closest registered ancestor.

Built-in channels: `default` (catch-all), `Vanilla` (captured `Verse.Log` calls), `Unity` (captured `UnityEngine.Debug.Log` calls).
