# Transformation Config Reference

This document describes the `transformation_config` JSON column for every supported transformation type.
Each section shows the available fields, their types, and multiple real-world examples.

> **Key facts**
> - The column is stored as **JSONB** (PostgreSQL).
> - All keys are **case-sensitive** unless noted otherwise.
> - `null` or an absent `transformation_config` is acceptable for types that do not require one (Constant, ArrayMap).
> - `PrefixMap` uses `source_path` as a **key prefix**, not an exact field path.

---

## Table of Contents

1. [Constant](#1-constant)
2. [Concat](#2-concat)
3. [DateFormat](#3-dateformat)
4. [Lookup](#4-lookup)
5. [ArrayMap](#5-arraymap)
6. [ArrayFlatten](#6-arrayflatten)
7. [Substring](#7-substring)
8. [Template](#8-template)
9. [Math](#9-math)
10. [Conditional](#10-conditional)
11. [PrefixMap](#11-prefixmap)
12. [ConditionalDateFormat](#12-conditionaldateformat)

---

## 1. Constant

Writes a fixed value to the target field regardless of the source document.
The constant value is stored in the **`default_value`** column of the field mapping — `transformation_config` is not used and should be left `null`.

| Column | Where | Description |
|---|---|---|
| `default_value` | field mapping column | The literal value written to the target |
| `transformation_config` | — | Not used; leave `null` |

### Examples

**Hard-coded country code:**

```sql
source_path           = ''
target_path           = 'shipment.country'
transformation_type   = 'Constant'
default_value         = 'US'
transformation_config = null
```

**Fixed service type:**

```sql
source_path           = ''
target_path           = 'service.type'
transformation_type   = 'Constant'
default_value         = 'GROUND'
transformation_config = null
```

**Static API version flag:**

```sql
source_path           = ''
target_path           = 'meta.api_version'
transformation_type   = 'Constant'
default_value         = 'v2'
transformation_config = null
```

---

## 2. Concat

Joins multiple source fields into a single string.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `Fields` | `string[]` | Yes | — | Ordered list of source paths to concatenate |
| `Separator` | `string` | No | `" "` | String inserted between each value |
| `SkipEmpty` | `"true"` \| `"false"` | No | `"false"` | When `"true"`, omits fields whose value is null or blank |

### Examples

**Full driver name (space separator):**

```json
{
  "Fields": ["driver.first_name", "driver.last_name"]
}
```

**Street address with comma separator, skip empty lines:**

```json
{
  "Fields": ["customer.address1", "customer.address2", "customer.city", "customer.state"],
  "Separator": ", ",
  "SkipEmpty": "true"
}
```

**Pro number with carrier prefix joined by dash:**

```json
{
  "Fields": ["carrier.prefix", "movement.pro_number"],
  "Separator": "-"
}
```

**Composite reference with no separator:**

```json
{
  "Fields": ["order.type_code", "order.id"],
  "Separator": ""
}
```

---

## 3. DateFormat

Parses a date/time string from the source and re-formats it for the target.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `DateInputFormat` | `string` | No | Auto-detect | .NET format string for parsing the source value. Omit to let the parser auto-detect common formats |
| `DateOutputFormat` | `string` | No | `"o"` | .NET format string for the output. `"o"` produces ISO 8601 UTC |

> If parsing fails with the given `DateInputFormat`, the strategy falls back to auto-detect before returning the original string unchanged.

### Examples

**TMS compact format → ISO 8601:**

```json
{
  "DateInputFormat": "yyyyMMddHHmmsszzz",
  "DateOutputFormat": "o"
}
```
`"20240315103000+00:00"` → `"2024-03-15T10:30:00.0000000Z"`

**ISO 8601 source → date-only:**

```json
{
  "DateOutputFormat": "yyyy-MM-dd"
}
```
`"2024-03-15T10:30:00Z"` → `"2024-03-15"`

**US date input → ISO date:**

```json
{
  "DateInputFormat": "MM/dd/yyyy",
  "DateOutputFormat": "yyyy-MM-dd"
}
```
`"03/15/2024"` → `"2024-03-15"`

**Auto-detect input → time-only output:**

```json
{
  "DateOutputFormat": "HH:mm"
}
```
`"2024-03-15T14:30:00Z"` → `"14:30"`

---

## 4. Lookup

Replaces the source field value using a pre-loaded lookup table.
Returns the mapped value, the lookup table's `default_value` if no match, or the original source value as a last resort.

| Key | Type | Required | Description |
|---|---|---|---|
| `LookupTableId` | `GUID` string | Yes | ID of the lookup table record in the database |

### Examples

**Map stop-type codes to readable labels:**

```json
{
  "LookupTableId": "a1b2c3d4-0000-0000-0000-000000000001"
}
```
Lookup table contents: `{ "PU": "Pickup", "SO": "Stop-Off", "D": "Delivery" }`

**Map carrier SCAC codes to full carrier names:**

```json
{
  "LookupTableId": "f7e6d5c4-0000-0000-0000-000000000002"
}
```

**Map internal status codes to partner-facing statuses:**

```json
{
  "LookupTableId": "11223344-5566-7788-99aa-bbccddeeff00"
}
```

---

## 5. ArrayMap

Extracts a single field from every element of a source array and returns the results as a new array.
Driven entirely by the **`source_path`** column using `[*]` wildcard notation — `transformation_config` is not used.

| `source_path` pattern | Meaning |
|---|---|
| `stops[*].stop_type` | Extract `stop_type` from every element of `stops` |
| `orders[*].id` | Extract `id` from every element of `orders` |
| `items[*]` | Return each element of `items` as-is |

### Examples

**All stop types:**

```sql
source_path           = 'stops[*].stop_type'
target_path           = 'stop_types'
transformation_type   = 'ArrayMap'
transformation_config = null
```
`[{"stop_type":"PU"}, {"stop_type":"SO"}, {"stop_type":"D"}]` → `["PU", "SO", "D"]`

**All order IDs:**

```sql
source_path           = 'orders[*].order_id'
target_path           = 'order_ids'
transformation_type   = 'ArrayMap'
transformation_config = null
```

**All item weights:**

```sql
source_path           = 'line_items[*].weight_lbs'
target_path           = 'weights'
transformation_type   = 'ArrayMap'
transformation_config = null
```

---

## 6. ArrayFlatten

Collects values from a field inside every element of a source array and returns a flat list.
Config takes precedence over `source_path` notation when both are supplied.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `SourceArrayPath` | `string` | No* | Derived from `source_path` before `[*]` | Path to the array |
| `ItemField` | `string` | No | Derived from `source_path` after `[*]` | Field to extract from each element |
| `FilterEmpty` | `"true"` \| `"false"` | No | `"false"` | Skip elements whose value is null or blank |

\* If omitted, `source_path` must contain `[*]` notation.

### Examples

**Reference numbers from all stops, skip blanks:**

```json
{
  "SourceArrayPath": "stops",
  "ItemField": "reference_number",
  "FilterEmpty": "true"
}
```
`[{"reference_number":"REF-1"}, {"reference_number":""}, {"reference_number":"REF-3"}]` → `["REF-1", "REF-3"]`

**Driver last names from all movements:**

```json
{
  "SourceArrayPath": "movement",
  "ItemField": "driver_last_name",
  "FilterEmpty": "true"
}
```

**Via source_path notation (no config needed):**

```sql
source_path           = 'line_items[*].commodity_code'
transformation_type   = 'ArrayFlatten'
transformation_config = null
```

**Include blank values in output:**

```json
{
  "SourceArrayPath": "stops",
  "ItemField": "notes"
}
```

---

## 7. Substring

Extracts a portion of a source field's string value.

| Key | Type | Required | Description |
|---|---|---|---|
| `Start` | `integer` | Yes | 0-based character index where extraction begins. Clamped to string boundaries |
| `Length` | `integer` | No | Number of characters to extract. Omit to take through the end of the string. Clamped if it exceeds available characters |

Returns the original value unchanged when config is absent or `Start` is missing.

### Examples

**Extract carrier prefix from pro number:**

```json
{
  "Start": 0,
  "Length": 4
}
```
`"FXFE123456"` → `"FXFE"`

**Strip leading prefix from order ID:**

```json
{
  "Start": 4
}
```
`"ORD-98765"` → `"98765"`

**Extract year from a date string:**

```json
{
  "Start": 0,
  "Length": 4
}
```
`"2024-03-15"` → `"2024"`

**Extract month and day:**

```json
{
  "Start": 5,
  "Length": 5
}
```
`"2024-03-15"` → `"03-15"`

**Take last segment of a reference code (from known offset):**

```json
{
  "Start": 8
}
```
`"CARRIER-98765-SUFFIX"` → `"98765-SUFFIX"`

---

## 8. Template

Builds a string from a template containing `{{path}}` placeholders that are resolved from the source document at runtime.

| Key | Type | Required | Description |
|---|---|---|---|
| `Template` | `string` | Yes | Template string with `{{source.path}}` placeholders |

Each `{{path}}` is replaced with the value found at that dot-notation path in the source document.
Unresolved paths (null or missing) are replaced with an empty string.

### Examples

**Shipment label combining carrier, mode, and PRO number:**

```json
{
  "Template": "{{carrier.name}} | {{shipment.mode}} | PRO: {{movement.pro_number}}"
}
```
→ `"FedEx Freight | LTL | PRO: PRO-789"`

**Formatted city/state/zip address line:**

```json
{
  "Template": "{{customer.city}}, {{customer.state}} {{customer.zip}}"
}
```
→ `"Atlanta, GA 30301"`

**Carrier SCAC + order ID composite key:**

```json
{
  "Template": "{{carrier.scac}}-{{order.id}}"
}
```
→ `"FXFE-98765"`

**Tracking URL with embedded reference:**

```json
{
  "Template": "https://track.example.com/shipment/{{movement.pro_number}}"
}
```
→ `"https://track.example.com/shipment/PRO-456"`

**Driver assignment note:**

```json
{
  "Template": "Assigned to {{driver.first_name}} {{driver.last_name}} on {{pickup_date}}"
}
```
→ `"Assigned to John Doe on 2024-03-15"`

---

## 9. Math

Performs an arithmetic or rounding operation on a numeric source field.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `Operation` | `string` | Yes | — | Operation to apply (see table below) |
| `Operand` | `number` | No* | — | Second operand for binary operations |
| `Precision` | `integer` | No | — | Rounds the final result to N decimal places (applied after the operation) |

\* Required for `add`, `subtract`, `multiply`, `divide`, `mod`.

**Supported operations** (case-insensitive):

| Operation | Formula | Operand required |
|---|---|---|
| `add` | `value + Operand` | Yes |
| `subtract` | `value − Operand` | Yes |
| `multiply` | `value × Operand` | Yes |
| `divide` | `value ÷ Operand` | Yes (returns original if 0) |
| `mod` | `value % Operand` | Yes (returns original if 0) |
| `abs` | `\|value\|` | No |
| `ceil` | ceiling of value | No |
| `floor` | floor of value | No |
| `round` | nearest integer (away from zero on midpoint) | No |

Returns the original source value unchanged when it cannot be parsed as a number, or when config is absent.
Returns a whole number (no decimal point) when the result has no fractional part.

### Examples

**Convert kilograms to pounds:**

```json
{
  "Operation": "multiply",
  "Operand": 2.20462,
  "Precision": 2
}
```
`100` → `220.46`

**Convert kilometres to miles:**

```json
{
  "Operation": "divide",
  "Operand": 1.60934,
  "Precision": 2
}
```
`100` → `62.14`

**Add a fixed surcharge:**

```json
{
  "Operation": "add",
  "Operand": 15.00,
  "Precision": 2
}
```
`84.99` → `99.99`

**Round freight weight up to nearest whole number:**

```json
{
  "Operation": "ceil"
}
```
`1250.3` → `1251`

**Absolute value of a temperature reading:**

```json
{
  "Operation": "abs"
}
```
`-23` → `23`

**Apply a percentage discount (multiply then round):**

```json
{
  "Operation": "multiply",
  "Operand": 0.85,
  "Precision": 2
}
```
`200` → `170.0`

**Truncate to whole units:**

```json
{
  "Operation": "floor"
}
```
`47.9` → `47`

---

## 10. Conditional

Evaluates one or more field comparisons and writes a value to the target based on the outcome.
Supports two config shapes — **flat** (all conditions share one operator) and **grouped** (mix AND/OR across groups).

### 10.1 Flat conditions

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `Conditions` | `object[]` | Yes* | — | Array of condition objects |
| `ConditionLogic` | `"AND"` \| `"OR"` | No | `"AND"` | How conditions are combined |
| `MapSourceOnTrue` | `boolean` | No | `false` | When `true` and condition passes, maps the `source_path` value directly to the target |
| `TruePath` | `string` | No | — | Source path resolved when condition passes (takes precedence over `TrueValue`) |
| `TrueValue` | `string` | No | — | Static literal written when condition passes |
| `FalsePath` | `string` | No | — | Source path resolved when condition fails (takes precedence over `FalseValue`) |
| `FalseValue` | `string` | No | — | Static literal written when condition fails |

\* Either `Conditions` or `ConditionGroups` must be present.

**Condition object fields:**

| Key | Type | Required | Description |
|---|---|---|---|
| `Field` | `string` | Yes | Source path of the field to evaluate |
| `Operator` | `string` | Yes | Comparison operator (see table below) |
| `Value` | `string` | No* | Value to compare against. Not required for `isempty`/`isnotempty`. For `in`/`notin`, comma-separated list |

**Supported operators** (case-insensitive):

| Operator | Aliases | Behaviour |
|---|---|---|
| `equals` | `eq` | Case-insensitive string equality |
| `notequals` | `ne` | Case-insensitive string inequality |
| `contains` | — | Field value contains the given string |
| `startswith` | — | Field value starts with the given string |
| `endswith` | — | Field value ends with the given string |
| `greaterthan` | `gt` | Numeric greater-than; falls back to string compare |
| `lessthan` | `lt` | Numeric less-than |
| `greaterthanorequals` | `gte` | Numeric ≥ |
| `lessthanorequals` | `lte` | Numeric ≤ |
| `isempty` | — | Field is null or empty string |
| `isnotempty` | — | Field is not null and not empty |
| `in` | — | Field value is one of a comma-separated list |
| `notin` | — | Field value is not in the comma-separated list |

**Output resolution when condition passes** (evaluated in order):
1. `MapSourceOnTrue: true` → returns value at `source_path`
2. `TruePath` → resolves path from source data
3. `TrueValue` → static literal

**Output resolution when condition fails:**
1. `FalsePath` → resolves path from source data
2. `FalseValue` → static literal

#### Flat condition examples

**Simple equality — static output:**

```json
{
  "Conditions": [
    { "Field": "status", "Operator": "equals", "Value": "ACTIVE" }
  ],
  "TrueValue": "Active",
  "FalseValue": "Inactive"
}
```

**Pass-through on match, static fallback:**

```json
{
  "Conditions": [
    { "Field": "status", "Operator": "notequals", "Value": "CANCELLED" }
  ],
  "MapSourceOnTrue": true,
  "FalseValue": "N/A"
}
```
If the source field is not `CANCELLED`, its value is written to the target as-is. Otherwise `"N/A"` is written.

**Membership check — path-based output:**

```json
{
  "Conditions": [
    { "Field": "mode", "Operator": "in", "Value": "TL,LTL,PTL" }
  ],
  "TruePath":  "carrier.pro_number",
  "FalsePath": "movement[0].alternate_reference"
}
```

**Null guard:**

```json
{
  "Conditions": [
    { "Field": "notes", "Operator": "isnotempty" }
  ],
  "MapSourceOnTrue": true,
  "FalseValue": "N/A"
}
```

**Numeric threshold:**

```json
{
  "Conditions": [
    { "Field": "total_weight_lbs", "Operator": "greaterthan", "Value": "44000" }
  ],
  "TrueValue": "OVERWEIGHT",
  "FalseValue": "OK"
}
```

**Multiple conditions, all must match (AND):**

```json
{
  "Conditions": [
    { "Field": "status",   "Operator": "equals", "Value": "ACTIVE" },
    { "Field": "mode",     "Operator": "equals", "Value": "TL"     },
    { "Field": "priority", "Operator": "equals", "Value": "HIGH"   }
  ],
  "ConditionLogic": "AND",
  "TrueValue": "Priority TL",
  "FalseValue": "Standard"
}
```

**Multiple conditions, any may match (OR):**

```json
{
  "Conditions": [
    { "Field": "stop_type", "Operator": "equals", "Value": "PU" },
    { "Field": "stop_type", "Operator": "equals", "Value": "SO" }
  ],
  "ConditionLogic": "OR",
  "TrueValue": "Valid Stop",
  "FalseValue": "Unknown Stop"
}
```

---

### 10.2 Grouped conditions

Use `ConditionGroups` to mix AND and OR logic in the same mapping, e.g. `(A AND B) OR C`.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `ConditionGroups` | `object[]` | Yes* | — | Array of group objects |
| `GroupLogic` | `"AND"` \| `"OR"` | No | `"AND"` | How groups are combined |
| `MapSourceOnTrue` | `boolean` | No | `false` | Same as flat mode |
| `TruePath` / `TrueValue` | `string` | No | — | Same as flat mode |
| `FalsePath` / `FalseValue` | `string` | No | — | Same as flat mode |

**Group object fields:**

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `Logic` | `"AND"` \| `"OR"` | No | `"AND"` | How conditions inside this group are combined |
| `Conditions` | `object[]` | Yes | — | Same condition objects as flat mode |

#### Grouped condition examples

**`(status==ACTIVE AND mode==TL) OR (priority==HIGH)` — GroupLogic OR:**

```json
{
  "ConditionGroups": [
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
        { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
      ]
    },
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
      ]
    }
  ],
  "GroupLogic": "OR",
  "TrueValue": "Matched",
  "FalseValue": "Other"
}
```

**`(stop_type==PU OR stop_type==SO) AND weight > 0` — GroupLogic AND:**

```json
{
  "ConditionGroups": [
    {
      "Logic": "OR",
      "Conditions": [
        { "Field": "stop_type", "Operator": "equals",      "Value": "PU" },
        { "Field": "stop_type", "Operator": "equals",      "Value": "SO" }
      ]
    },
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "weight",    "Operator": "greaterthan", "Value": "0"  }
      ]
    }
  ],
  "GroupLogic": "AND",
  "TrueValue": "Valid Stop",
  "FalseValue": "Rejected"
}
```

**Groups with path-based output:**

```json
{
  "ConditionGroups": [
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
        { "Field": "mode",   "Operator": "in",     "Value": "TL,LTL" }
      ]
    },
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
      ]
    }
  ],
  "GroupLogic": "OR",
  "TruePath":  "movement[0].pro_number",
  "FalsePath": "movement[0].order_number"
}
```

**Groups with pass-through on match:**

```json
{
  "ConditionGroups": [
    {
      "Logic": "AND",
      "Conditions": [
        { "Field": "status", "Operator": "notequals", "Value": "CANCELLED" },
        { "Field": "mode",   "Operator": "in",        "Value": "TL,LTL,PTL" }
      ]
    }
  ],
  "GroupLogic": "AND",
  "MapSourceOnTrue": true,
  "FalseValue": "N/A"
}
```

---

## 11. PrefixMap

Scans the source document for all top-level properties whose names share a common prefix, splits each value by a separator, and emits a structured array of objects.

**`source_path`** is the key prefix to match (e.g. `deliveryDriver`). Every property whose name *starts with* the prefix and has at least one additional character is included. Matched keys are sorted lexicographically so numbered suffixes remain in order (`driver1` → `driver2` → `driver3`).

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `Fields` | `string[]` | Yes | — | Ordered list of property names to assign to each split part |
| `Separator` | `string` | No | `" "` | String to split each source value on |
| `SkipEmpty` | `"true"` \| `"false"` | No | `"false"` | When `"true"`, source entries whose value is null or whitespace are omitted from the output array |

> **Part mapping rules**
> - If a value produces **fewer parts** than `Fields`, the remaining field names are set to `null`.
> - If a value produces **more parts** than `Fields`, the extra parts are silently ignored.
> - The prefix match is **case-insensitive**.

### Examples

**Map numbered delivery drivers to a typed array:**

```sql
source_path           = 'deliveryDriver'
target_path           = 'drivers'
transformation_type   = 'PrefixMap'
```
```json
{
  "Fields": ["firstName", "lastName"]
}
```

Source document:
```json
{
  "deliveryDriver1": "Ateeq test",
  "deliveryDriver2": "Ateeq test1"
}
```
Output:
```json
[
  { "firstName": "Ateeq", "lastName": "test"  },
  { "firstName": "Ateeq", "lastName": "test1" }
]
```

**Map pickup drivers with skip-empty:**

```json
{
  "Fields": ["firstName", "lastName"],
  "SkipEmpty": "true"
}
```
Source: `pickupDriver1 = "John Doe"`, `pickupDriver2 = ""`, `pickupDriver3 = "Jane Smith"`
→ Only `John Doe` and `Jane Smith` entries are emitted; the blank `pickupDriver2` is omitted.

**Three-part name split:**

```json
{
  "Fields": ["firstName", "middleName", "lastName"]
}
```
Source: `contact1 = "John Michael Doe"`
→ `{ "firstName": "John", "middleName": "Michael", "lastName": "Doe" }`

**Custom separator (comma-delimited):**

```json
{
  "Fields": ["firstName", "lastName", "suffix"],
  "Separator": ","
}
```
Source: `driver1 = "John,Doe,Jr"`
→ `{ "firstName": "John", "lastName": "Doe", "suffix": "Jr" }`

**Single-field flat list (collect all tag values):**

```json
{
  "Fields": ["value"],
  "SkipEmpty": "true"
}
```
Source: `tag1 = "urgent"`, `tag2 = ""`, `tag3 = "fragile"`
→ `[{ "value": "urgent" }, { "value": "fragile" }]`

---

## 12. ConditionalDateFormat

Resolves a DateTime value from one or more source paths, converts it to UTC, and formats it with a configurable output format. Supports two operating modes:

This type exists because no single existing strategy can chain conditional source selection, path coalescing, and date conversion together in one step:

| Capability | `Conditional` | `DateFormat` | `ConditionalDateFormat` |
|---|---|---|---|
| Branch on a field value | ✓ | ✗ | ✓ |
| Try multiple source paths (coalesce) | ✗ | ✗ | ✓ |
| Convert to UTC + format | ✗ | ✓ | ✓ |

---

### Mode 1 — Coalesce (top-level `SourcePaths`)

Tries each path in order; the **first non-null, non-empty value** wins and is converted to UTC. No condition field or branches needed.

Use this when the logic is: *"Use field A if it has a value, otherwise fall back to field B, and convert whichever one wins to UTC."*

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `SourcePaths` | `string[]` | Yes | — | Ordered source paths to try; first non-null/non-empty wins |
| `OutputFormat` | `string` | No | `"yyyy-MM-ddTHH:mm:ss.ffffffZ"` | .NET format string applied to the UTC result |

**Examples:**

```json
{ "SourcePaths": ["actualPickup", "pickUpBy"] }
```
- `actualPickup = "2024-03-15T10:30:00Z"` → `"2024-03-15T10:30:00.000000Z"`
- `actualPickup = null`, `pickUpBy = "2024-03-15T08:00:00Z"` → `"2024-03-15T08:00:00.000000Z"` (fallback)
- `actualPickup = null`, `pickUpBy = null` → `null`

```json
{ "SourcePaths": ["actualPickup"], "OutputFormat": "yyyy-MM-dd" }
```
- `actualPickup = "2024-03-15T12:30:00+02:00"` → `"2024-03-15"` (converted to UTC `10:30:00Z` first, then date extracted)

---

### Mode 2 — Condition field + branches

Reads a condition field (e.g. `stopType`), matches its value against the `Branches` array, and within the matched branch tries `SourcePaths` in order.

Use this when the source field itself changes depending on a context value.

| Key | Type | Required | Default | Description |
|---|---|---|---|---|
| `ConditionField` | `string` | Yes | — | Source path of the field whose value drives branch selection (e.g. `stopType`) |
| `Branches` | `object[]` | Yes | — | Array of branch objects (see below) |
| `OutputFormat` | `string` | No | `"yyyy-MM-ddTHH:mm:ss.ffffffZ"` | .NET format string applied to the UTC result |

**Branch object fields:**

| Key | Type | Required | Description |
|---|---|---|---|
| `Value` | `string` | Yes | Condition value that activates this branch (case-insensitive) |
| `SourcePaths` | `string[]` | Yes | Ordered source paths to try; the **first non-null, non-empty value** wins |

---

> **Shared behaviour (both modes)**
> - The first non-null, non-empty path value wins.
> - The resolved value is parsed and converted to UTC before formatting.
> - When the value cannot be parsed as a date, it is returned **unchanged** (consistent with `DateFormat`).
> - The default `OutputFormat` produces ISO 8601 UTC with microsecond precision: `2024-03-15T10:30:00.000000Z`.
> - Mode 1 takes precedence when `SourcePaths` exists at the root level.
> - (Mode 2) When no branch matches the condition value, `null` is returned.
> - (Mode 2) When a branch matches but all its `SourcePaths` resolve to null/empty, `null` is returned.

### Examples

**Mode 2 — `actualArrival`, different source fields per stop type with fallback:**

```sql
source_path           = 'irrelevant'
target_path           = 'actualArrival'
transformation_type   = 'ConditionalDateFormat'
```
```json
{
  "ConditionField": "stopType",
  "Branches": [
    {
      "Value": "Origin",
      "SourcePaths": ["actualPickup", "pickUpBy"]
    },
    {
      "Value": "Destination",
      "SourcePaths": ["actualDelivery"]
    }
  ]
}
```
- `stopType = "Origin"`, `actualPickup = "2024-03-15T10:30:00Z"` → `"2024-03-15T10:30:00.000000Z"`
- `stopType = "Origin"`, `actualPickup = null`, `pickUpBy = "2024-03-15T08:00:00Z"` → `"2024-03-15T08:00:00.000000Z"` (fallback)
- `stopType = "Destination"`, `actualDelivery = "2024-03-16T14:00:00Z"` → `"2024-03-16T14:00:00.000000Z"`
- `stopType = "StopOff"` → `null` (no matching branch)

**Mode 2 — `scheduledEarlyArrival`, Destination only with fallback:**

```json
{
  "ConditionField": "stopType",
  "Branches": [
    {
      "Value": "Destination",
      "SourcePaths": ["deliverBy", "deliverByEnd"]
    }
  ]
}
```
- `stopType = "Destination"`, `deliverBy = "2024-03-16T12:00:00Z"` → `"2024-03-16T12:00:00.000000Z"`
- `stopType = "Destination"`, `deliverBy = null`, `deliverByEnd = "2024-03-16T18:00:00Z"` → `"2024-03-16T18:00:00.000000Z"` (fallback)
- `stopType = "Origin"` → `null`

**Mode 2 — three-way stop type switch:**

```json
{
  "ConditionField": "stopType",
  "Branches": [
    { "Value": "Origin",      "SourcePaths": ["actualPickup",  "pickUpBy"]     },
    { "Value": "StopOff",     "SourcePaths": ["stopOffArrival"               ] },
    { "Value": "Destination", "SourcePaths": ["actualDelivery"               ] }
  ]
}
```

**Mode 2 — with timezone offset conversion and custom format:**

```json
{
  "ConditionField": "stopType",
  "OutputFormat": "yyyy-MM-dd",
  "Branches": [
    { "Value": "Origin", "SourcePaths": ["actualPickup"] }
  ]
}
```
`actualPickup = "2024-03-15T12:30:00+02:00"` → `"2024-03-15"` (converted to UTC first: `10:30:00Z`, then date extracted)
