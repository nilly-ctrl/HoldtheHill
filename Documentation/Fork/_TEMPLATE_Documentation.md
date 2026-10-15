# Technical Documentation: <System or File>.cs

**Target Unity Version**: Unity 6 (`6000.6.0f1`)  
**Context**: 2D Tower Defense Game  
**File Location**: `Assets/_Game/Features/<Feature>/Scripts/<File>.cs`  
**Companion Files**: `<File2>.cs`, `<File3>.cs`

---

## 1. Overview & Architecture

<!-- prose:overview -->
One paragraph: what the system is for and what it deliberately does not do.

### Key Architectural Pillars
- **Pillar**: one sentence.
- **Pillar**: one sentence.
<!-- /prose -->

---

## 2. State Machine / Data Structures

<!-- prose:structure -->
Only when the system has one. A `mermaid` stateDiagram-v2 or classDiagram, and the enum it draws.
<!-- /prose -->

---

## 3. Inspector Fields Reference

<!-- generated:fields -->
One table per [Header]: | Field | Type | Default | Description | from [SerializeField] fields, their [Tooltip], [Min] and [Range].
<!-- /generated -->

---

## 4. Public API Reference

<!-- generated:api -->
### Properties
| Property | Type | Description |

### C# Events
| Event | Signature | Raised when |

### Public Methods
#### `signature`
Summary text from the /// comment.
<!-- /generated -->

---

## 5. Lifecycle & Execution Flow

<!-- prose:lifecycle -->
Awake, OnEnable, Start, Update in the order they matter; what is virtual and why; what a subclass or a
sibling component may rely on.
<!-- /prose -->

---

## 6. Integration With Other Systems

<!-- prose:integration -->
### With `<Neighbour>.cs`
Which events cross the line, in which direction, and what each side assumes.
<!-- /prose -->

---

## 7. Setup & Integration Guide

<!-- prose:setup -->
### Step 1: ...
Numbered steps a teammate can follow in the Editor without reading the code.
<!-- /prose -->

---

## 8. Against the Indev version

<!-- prose:changes -->
Only where an Indev document covers the same system: what this version does that it does not, what it drops, and what a caller changes. Removed once the team has chosen.
<!-- /prose -->
